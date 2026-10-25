using Ballance2.Services.Debug;
using Ballance2.Utils;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using UnityEngine.Networking;
using Unity.SharpZipLib.Zip;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* GameZipPackage.cs
* 
* Purpose:
* ZIP game package definition.
*
* Author:
* mengyu
*/
namespace Ballance2.Package
{
  public class GameZipPackage : GamePackage
  {
    private async Task<ZipFile> OpenArchive()
    {
      if (!PathUtils.IsRemotePath(PackageFilePath))
        return ZipUtils.OpenZipFile(PackageFilePath);

      using var request = UnityWebRequest.Get(PackageFilePath);
      await request.SendWebRequest();
      if (request.result != UnityWebRequest.Result.Success)
        throw new IOException("Request " + PackageFilePath + " failed: " + request.error);
      return ZipUtils.OpenZipStream(new MemoryStream(request.downloadHandler.data, false));
    }

    public override async Task<bool> LoadInfo(string filePath)
    {
      PackageFilePath = PathUtils.FixFilePathScheme(filePath);
      try
      {
        using var zip = await OpenArchive();
        var definition = ZipUtils.FindEntry(zip, "PackageDef.xml");
        if (definition == null)
        {
          LoadError = "Package does not contain PackageDef.xml.";
          GameErrorChecker.SetLastErrorAndLog(GameError.PackageDefNotFound, TAG, LoadError);
          return false;
        }

        PackageDef = new XmlDocument();
        using (var content = new MemoryStream(await ZipUtils.ReadEntryAsync(zip, definition), false))
          PackageDef.Load(content);
        var language = ZipUtils.FindEntry(zip, "PackageLanguageResPre.xml");
        if (language != null)
          PreLoadI18NResource(StringUtils.GetUtf8Bytes(StringUtils.FixUtf8BOM(await ZipUtils.ReadEntryAsync(zip, language))));
        if (!ReadInfo(PackageDef)) return false;

        var logo = ZipUtils.FindEntry(zip, "PackageLogo.png");
        if (logo != null)
        {
          var texture = new Texture2D(2, 2);
          try
          {
            if (texture.LoadImage(await ZipUtils.ReadEntryAsync(zip, logo)))
              BaseInfo.LogoTexture = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
            else
              UnityEngine.Object.Destroy(texture);
          }
          catch
          {
            UnityEngine.Object.Destroy(texture);
            throw;
          }
        }
        UpdateTime = PathUtils.IsRemotePath(PackageFilePath) ? DateTime.MinValue : File.GetLastWriteTime(PackageFilePath);
        return true;
      }
      catch (Exception error)
      {
        LoadError = "Failed to read package metadata: " + error.Message;
        GameErrorChecker.SetLastErrorAndLog(GameError.PackageIncompatible, TAG, LoadError);
        return false;
      }
    }

    public override async Task<bool> LoadPackage()
    {
      try
      {
        using var zip = await OpenArchive();
        var entry = zip.GetEntry("assets/" + PackageName + ".assetbundle");
        if (entry == null)
        {
          LoadError = "Package AssetBundle is missing: " + PackageName;
          GameErrorChecker.SetLastErrorAndLog(GameError.AssetBundleNotFound, TAG, LoadError);
          return false;
        }
        var request = AssetBundle.LoadFromMemoryAsync(await ZipUtils.ReadEntryAsync(zip, entry));
        await request;
        AssetBundle = request.assetBundle;
        if (AssetBundle == null)
        {
          LoadError = "Invalid package: failed to load AssetBundle.";
          GameErrorChecker.SetLastErrorAndLog(GameError.AssetBundleNotFound, TAG, LoadError);
          return false;
        }
        return await base.LoadPackage();
      }
      catch (Exception error)
      {
        LoadError = "Failed to load package: " + error.Message;
        GameErrorChecker.SetLastErrorAndLog(GameError.ExecutionFailed, TAG, LoadError);
        return false;
      }
    }

    protected override Assembly LoadCodeCSharp(string pathorname)
    {
#if ENABLE_MONO || ENABLE_DOTNET
      try
      {
        using var zip = ZipUtils.OpenZipFile(PackageFilePath);
        var entry = zip.GetEntry("csharp/" + pathorname);
        if (entry == null) throw new FileNotFoundException("Package code entry not found", pathorname);
        return Assembly.Load(ZipUtils.ReadEntry(zip, entry));
      }
      catch (Exception error)
      {
        Log.E(TAG, "Package {0} failed to load code {1}: {2}", PackageName, pathorname, error.Message);
      }
#else
      Log.E(TAG, "Package {0} cannot load C# DLLs under IL2CPP.", PackageName);
#endif
      return null;
    }
  }
}
