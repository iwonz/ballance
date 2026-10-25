using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Ballance2.Package;
using Ballance2.Services.I18N;
using Ballance2.Services.Pool;
using Ballance2.Utils;
using Unity.SharpZipLib.Zip;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ballance2.Editor
{
  public static class ModernizationChecks
  {
    private static void Require(bool condition, string message)
    {
      if (!condition) throw new InvalidOperationException("Modernization check failed: " + message);
    }

    private static void Throws<T>(Action action) where T : Exception
    {
      try { action(); }
      catch (T) { return; }
      throw new InvalidOperationException("Expected " + typeof(T).Name);
    }

    public static void Run()
    {
      string directory = Path.Combine(Path.GetTempPath(), "ballance-checks-" + Guid.NewGuid());
      Directory.CreateDirectory(directory);
      try
      {
        CheckFiles(directory);
        CheckArchives(directory);
        CheckPools();
        CheckScrollPool();
        Debug.Log("MODERNIZATION_CHECKS_PASSED: archive compatibility, CRC/size bounds, streaming, files, pool ownership and disposal.");
      }
      finally { Directory.Delete(directory, true); }
    }

    private static void CheckFiles(string directory)
    {
      string file = Path.Combine(directory, "file with spaces.txt");
      FileUtils.WriteFile(file, false, "English \u00e9");
      FileUtils.WriteFile(file, true, " appended");
      Require(FileUtils.ReadFile(file) == "English \u00e9 appended", "UTF-8 write/append/read");
      Require(FileUtils.ReadAllToBytes(PathUtils.ToRequestUrl(file)).SequenceEqual(File.ReadAllBytes(file)), "file URL bytes");
      File.WriteAllBytes(file, new byte[] { 0x50, 0x4b, 3 });
      Require(!FileUtils.TestFileIsZip(file), "short ZIP header rejected");
      File.WriteAllBytes(file, new byte[] { 0x50, 0x4b, 3, 4 });
      Require(FileUtils.TestFileIsZip(file), "ZIP header accepted");
      using var exclusive = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    private static void CheckArchives(string directory)
    {
      const string definition = "<Package name='test.fixture' version='1'><BaseInfo><Name>{fixture.name}</Name></BaseInfo></Package>";
      const string language = "<I18n><Language name='English'><Text name='fixture.name'>Fixture name</Text></Language></I18n>";
      string payload = Path.Combine(directory, "payload.bin");
      using (var file = File.Create(payload)) file.SetLength(4 * 1024 * 1024);
      string archive = Path.Combine(directory, "fixture.ballance");
      long calibrationStart = GC.GetAllocatedBytesForCurrentThread();
      GC.KeepAlive(new byte[1024 * 1024]);
      bool allocationCounterWorks = GC.GetAllocatedBytesForCurrentThread() - calibrationStart >= 1024 * 1024;
      using (var zip = ZipUtils.CreateZipFile(archive))
      {
        long before = GC.GetAllocatedBytesForCurrentThread();
        ZipUtils.AddFileToZip(zip, payload, "/assets\\fixture.assetbundle");
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        if (allocationCounterWorks)
          Require(allocated < 2 * 1024 * 1024, "writer must not allocate a whole 4 MiB input");
        Debug.Log(allocationCounterWorks
          ? "MODERNIZATION_WRITE_ALLOCATED_BYTES=" + allocated + " INPUT_BYTES=4194304"
          : "MODERNIZATION_ALLOCATION_COUNTER_UNAVAILABLE: calibration allocation was not reported.");
        WriteEntry(zip, "/PackageDef.xml", Encoding.UTF8.GetBytes(definition));
        WriteEntry(zip, "/PackageLanguageResPre.xml", Encoding.UTF8.GetBytes(language));
        WriteEntry(zip, "empty", Array.Empty<byte>());
      }

      var stream = new CountingStream(File.ReadAllBytes(archive));
      using (var zip = ZipUtils.OpenZipStream(stream))
      {
        var entry = ZipUtils.FindEntry(zip, "PackageDef.xml");
        Require(Encoding.UTF8.GetString(ZipUtils.ReadEntry(zip, entry)) == definition, "legacy leading slash");
        Require(stream.BytesRead < 65536, "metadata must not scan the preceding 4 MiB payload");
        Debug.Log("MODERNIZATION_INDEXED_METADATA_BYTES_READ=" + stream.BytesRead);
        var awaiter = ZipUtils.ReadEntryAsync(zip, entry).GetAwaiter();
        Require(awaiter.IsCompleted && Encoding.UTF8.GetString(awaiter.GetResult()) == definition, "native async small read");
        Require(ZipUtils.ReadEntry(zip, zip.GetEntry("empty")).Length == 0, "empty entry");
        Require(zip.GetEntry("assets/fixture.assetbundle") != null, "canonical packed paths");
        entry.Size = ZipUtils.MaxEntryBytes + 1L;
        Throws<InvalidDataException>(() => ZipUtils.ReadEntry(zip, entry));
        entry = ZipUtils.FindEntry(zip, "PackageDef.xml");
        entry.Size += 1;
        Throws<EndOfStreamException>(() => ZipUtils.ReadEntry(zip, entry));
        entry = ZipUtils.FindEntry(zip, "PackageDef.xml");
        entry.Size -= 1;
        Throws<InvalidDataException>(() => ZipUtils.ReadEntry(zip, entry));
      }
      Require(!stream.CanRead, "archive owns its input stream");
      var invalid = new MemoryStream(new byte[3]);
      Throws<ZipException>(() => ZipUtils.OpenZipStream(invalid));
      Require(!invalid.CanRead, "invalid archive closes its stream");

      I18NProvider.SetCurrentLanguage(SystemLanguage.English);
      var package = new GameZipPackage();
      var info = package.LoadInfo(archive);
      Require(info.IsCompleted && info.GetAwaiter().GetResult(), "legacy package metadata loads");
      Require(package.BaseInfo.Name == "Fixture name", "language preloads before metadata translation");

      string corrupt = Path.Combine(directory, "corrupt.zip");
      using (var zip = ZipUtils.CreateZipFile(corrupt))
        WriteEntry(zip, "stored", Encoding.ASCII.GetBytes("unique-corruption-fixture"));
      byte[] bytes = File.ReadAllBytes(corrupt);
      int index = Encoding.ASCII.GetString(bytes).IndexOf("unique-corruption-fixture", StringComparison.Ordinal);
      Require(index >= 0, "stored fixture data located");
      bytes[index] ^= 1;
      File.WriteAllBytes(corrupt, bytes);
      using (var zip = ZipUtils.OpenZipFile(corrupt))
        Throws<InvalidDataException>(() => ZipUtils.ReadEntry(zip, zip.GetEntry("stored")));
      using var unlocked = new FileStream(corrupt, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    private static void WriteEntry(ZipOutputStream zip, string name, byte[] bytes)
    {
      zip.PutNextEntry(new ZipEntry(name) { CompressionMethod = CompressionMethod.Stored, Size = bytes.Length });
      zip.Write(bytes, 0, bytes.Length);
      zip.CloseEntry();
    }

    private sealed class CountingStream : MemoryStream
    {
      public long BytesRead { get; private set; }
      public CountingStream(byte[] bytes) : base(bytes, false) { }
      public override int Read(byte[] buffer, int offset, int count)
      {
        int read = base.Read(buffer, offset, count);
        BytesRead += read;
        return read;
      }
      public override int ReadByte()
      {
        int value = base.ReadByte();
        if (value >= 0) BytesRead++;
        return value;
      }
    }

    private static void CheckPools()
    {
      var page = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Menu/Prefabs/ManagerUI/PagePackageManagerInfo.prefab");
      var details = page.GetComponent<Ballance2.Menu.PackageManagerContentUIControl>();
      Require(!details.ItemDependsPrefab.activeSelf, "embedded dependency template must stay hidden");
      var root = new GameObject("PoolCheckRoot");
      var prefab = new GameObject("PoolCheckPrefab");
      prefab.SetActive(false);
      try
      {
        using var pool = new GameObjectPool("first", prefab, 2, 3, root.transform);
        using var otherPool = new GameObjectPool("second", prefab, 0, 1, root.transform);
        var foreign = otherPool.NextAvailableObject();
        var first = pool.NextAvailableObject();
        var second = pool.NextAvailableObject();
        var third = pool.NextAvailableObject();
        Require(first.activeSelf && second.activeSelf && third.activeSelf, "acquired clones activate an inactive template");
        Require(first != second && first != third && second != third, "distinct prewarmed and grown items");
        Require(pool.NextAvailableObject() == null, "total capacity enforced");
        pool.Clear();
        pool.Clear();
        Require(foreign.activeSelf, "clear does not return another pool's items");
        first = pool.NextAvailableObject();
        second = pool.NextAvailableObject();
        third = pool.NextAvailableObject();
        Require(first != second && first != third && second != third, "repeated clear does not duplicate items");
        Throws<InvalidOperationException>(() => pool.ReturnObjectToPool(foreign));
        Throws<ArgumentException>(() => pool.ReturnObjectToPool("wrong", first));
        pool.ReturnObjectToPool(first);
        Throws<InvalidOperationException>(() => pool.ReturnObjectToPool(first));
        pool.Dispose();
        pool.Dispose();
        Require(first == null && second == null && third == null && foreign != null, "dispose active/inactive owned items only");
        Throws<ObjectDisposedException>(() => pool.NextAvailableObject());

        using var generic = new Ballance2.Services.Pool.ObjectPool<StringBuilder>(null, value => value.Clear());
        var item = generic.Get();
        item.Append("test");
        generic.Release(item);
        Require(generic.countAll == 1 && generic.countInactive == 1 && generic.countActive == 0, "generic counts");
        Throws<InvalidOperationException>(() => generic.Release(item));
        Require(ReferenceEquals(item, generic.Get()) && item.Length == 0, "generic reuse and release callback");
        generic.Release(item);
      }
      finally
      {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(prefab);
      }
    }

    private static void CheckScrollPool()
    {
      var root = new GameObject("ScrollPoolCheck", typeof(RectTransform));
      var template = new GameObject("ItemTemplate", typeof(RectTransform));
      try
      {
        var scroll = root.AddComponent<Ballance2.UI.Core.Controls.ScrollView>();
        scroll.itemTemplate = template.GetComponent<RectTransform>();
        scroll.poolSize = 1;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(Ballance2.UI.Core.Controls.ScrollView).GetMethod("InitPool", flags).Invoke(scroll, null);
        var pool = (UnityEngine.Pool.ObjectPool<RectTransform>)typeof(Ballance2.UI.Core.Controls.ScrollView)
          .GetField("itemPool", flags).GetValue(scroll);
        var first = pool.Get();
        var second = pool.Get();
        pool.Release(first);
        pool.Release(second);
        Require(first != null && second == null, "scroll pool destroys excess rows");
        Object.DestroyImmediate(root);
        Require(first == null, "scroll destruction disposes retained rows");
      }
      finally
      {
        if (root != null) Object.DestroyImmediate(root);
        Object.DestroyImmediate(template);
      }
    }
  }
}
