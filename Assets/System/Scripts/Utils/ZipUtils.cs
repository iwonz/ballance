using System;
using System.IO;
using UnityEngine;
using Unity.SharpZipLib.Checksum;
using Unity.SharpZipLib.Zip;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* ZipUtils.cs
* 
* Purpose:
* ZIP archive helpers.
*
* Author:
* mengyu
*
*/
namespace Ballance2.Utils
{
  public static class ZipUtils
  {
    public const int MaxEntryBytes = 512 * 1024 * 1024;
    private const int ReadChunkBytes = 1024 * 1024;

    public static void AddFileToZip(ZipOutputStream zip, string file, string entryName)
    {
      using var input = File.OpenRead(file);
      zip.PutNextEntry(new ZipEntry(entryName.Replace('\\', '/').TrimStart('/'))
      {
        Size = input.Length,
        DateTime = File.GetLastWriteTime(file)
      });
      input.CopyTo(zip);
      zip.CloseEntry();
    }

    public static ZipOutputStream CreateZipFile(string file)
    {
      var zip = new ZipOutputStream(File.Create(file));
      zip.SetLevel(0); // AssetBundles already use chunk compression.
      return zip;
    }

    public static ZipFile OpenZipFile(string file) => OpenZipStream(File.OpenRead(file));

    public static ZipFile OpenZipStream(Stream stream)
    {
      try { return new ZipFile(stream) { IsStreamOwner = true }; }
      catch { stream.Dispose(); throw; }
    }

    public static ZipEntry FindEntry(ZipFile zip, string name) => zip.GetEntry(name) ?? zip.GetEntry("/" + name);

    private static byte[] AllocateEntry(ZipEntry entry)
    {
      if (entry.Size < 0 || entry.Size > MaxEntryBytes)
        throw new InvalidDataException("ZIP entry exceeds the 512 MiB limit: " + entry.Name);
      return new byte[(int)entry.Size];
    }

    private static void ValidateEntry(Stream stream, ZipEntry entry, Crc32 crc)
    {
      if (stream.ReadByte() != -1 || crc.Value != entry.Crc)
        throw new InvalidDataException("ZIP entry length or CRC mismatch: " + entry.Name);
    }

    public static byte[] ReadEntry(ZipFile zip, ZipEntry entry)
    {
      byte[] data = AllocateEntry(entry);
      using var input = zip.GetInputStream(entry);
      var crc = new Crc32();
      int offset = 0;
      while (offset < data.Length)
      {
        int count = input.Read(data, offset, Math.Min(ReadChunkBytes, data.Length - offset));
        if (count == 0) throw new EndOfStreamException("Truncated ZIP entry: " + entry.Name);
        crc.Update(new ArraySegment<byte>(data, offset, count));
        offset += count;
      }
      ValidateEntry(input, entry, crc);
      return data;
    }

    public static async Awaitable<byte[]> ReadEntryAsync(ZipFile zip, ZipEntry entry)
    {
      byte[] data = AllocateEntry(entry);
      using var input = zip.GetInputStream(entry);
      var crc = new Crc32();
      int offset = 0;
      while (offset < data.Length)
      {
        // Synchronous chunks also work on Web, where worker threads are disabled.
        int count = input.Read(data, offset, Math.Min(ReadChunkBytes, data.Length - offset));
        if (count == 0) throw new EndOfStreamException("Truncated ZIP entry: " + entry.Name);
        crc.Update(new ArraySegment<byte>(data, offset, count));
        offset += count;
        if (offset < data.Length) await Awaitable.NextFrameAsync();
      }
      ValidateEntry(input, entry, crc);
      return data;
    }
  }
}
