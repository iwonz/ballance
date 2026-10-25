using Ballance2.Services.Debug;
using Ballance2.Utils;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using UnityEngine.Networking;

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
    public override async Task<bool> LoadInfo(string filePath)
    {
      PackageFilePath = PathUtils.FixFilePathScheme(filePath);

      ZipInputStream zip = null;
      MemoryStream ms = null;

      try {
        // Read jar paths through the web request path.
        if(PathUtils.IsRemotePath(PackageFilePath)) {

          using var request = UnityWebRequest.Get(PackageFilePath);
          await request.SendWebRequest();

          if (request.result == UnityWebRequest.Result.Success)
          {
            ms = new MemoryStream(request.downloadHandler.data);
            zip = ZipUtils.OpenZipStream(ms);
          }
          else {
            GameErrorChecker.SetLastErrorAndLog(GameError.RequestFailed, TAG, "Request " + PackageFilePath + " failed. Error: " + request.error);
            return false;
          }
        }
        else {
          // Load the definition from the ZIP.
          zip = ZipUtils.OpenZipFile(PackageFilePath);
        }
        if (zip == null)
          return false;

        UpdateTime = PathUtils.IsRemotePath(PackageFilePath) ? DateTime.MinValue : File.GetLastWriteTime(PackageFilePath);

        XmlDocument packageDef = null;
        Sprite logo = null;

        // Search ZIP contents.
        ZipEntry theEntry;
        while ((theEntry = zip.GetNextEntry()) != null)
        {
          if (theEntry.Name == "/PackageDef.xml" || theEntry.Name == "PackageDef.xml")
            packageDef = await LoadPackageDefInZip(zip, theEntry); // Load the definition file.
          else if (theEntry.Name == "/PackageLanguageResPre.xml" || theEntry.Name == "PackageLanguageResPre.xml")
            await LoadPackageLanguageResPreInZip(zip, theEntry); // Load the language file.
          else if (theEntry.Name == "/PackageLogo.png" || theEntry.Name == "PackageLogo.png")
            logo = await LoadLogoInZip(zip, theEntry); // Load the logo.
        }

        zip.Close();
        zip.Dispose();

        if (packageDef == null)
        {
          GameErrorChecker.SetLastErrorAndLog(GameError.PackageDefNotFound, TAG, "PackageDef.xml not found");
          LoadError = "Package does not contain PackageDef.xml.";
          return false;
        } 
        else 
        {
          bool res = ReadInfo(packageDef);
          BaseInfo.LogoTexture = logo;
          return res;
        }
      }
      catch(Exception e) {
        GameErrorChecker.SetLastErrorAndLog(GameError.ExecutionFailed, TAG, "Loading exception: " + e.ToString());
      }
      finally { 
        if(ms != null)
          ms.Dispose();
        if(zip != null)
          zip.Dispose();
      }
      return false;
    }
    public override async Task<bool> LoadPackage()
    {
      ZipInputStream zip = null;
      MemoryStream ms = null;

      try{
        // Read jar paths through the web request path.
        if(PathUtils.IsRemotePath(PackageFilePath)) {

          using var request = UnityWebRequest.Get(PackageFilePath);
          await request.SendWebRequest();

          if (request.result == UnityWebRequest.Result.Success)
          {
            ms = new MemoryStream(request.downloadHandler.data);
            zip = ZipUtils.OpenZipStream(ms);
          }
          else {
            GameErrorChecker.SetLastErrorAndLog(GameError.RequestFailed, TAG, "Request " + PackageFilePath + " failed. Error: " + request.error);
            return false;
          }
        }
        else {
          // Load the definition from the ZIP.
          zip = ZipUtils.OpenZipFile(PathUtils.FixFilePathScheme(PackageFilePath));
        }

        if (zip == null)
          return false;

        bool result = false;
        bool assetbundleFound = false;

        ZipEntry theEntry;
        while ((theEntry = zip.GetNextEntry()) != null)
        {
          if (theEntry.Name == "assets/" + PackageName + ".assetbundle")
          {
            if (await LoadAssetBundleToMemoryInZipAsync(zip, theEntry))
              assetbundleFound = true;
          }
        }

        if(assetbundleFound) 
          result = await base.LoadPackage();
        else
          GameErrorChecker.SetLastErrorAndLog(GameError.FileNotFound, TAG, "Not found: " + PackageName + ".assetbundle");
        return result;
      }
      catch(Exception e) {
        GameErrorChecker.SetLastErrorAndLog(GameError.ExecutionFailed, TAG, "Loading exception: " + e.ToString());
      }
      finally { 
        if(ms != null)
          ms.Dispose();
        if(zip != null)
          zip.Dispose();
      }
      return false;
    }

    private async Task<XmlDocument> LoadPackageDefInZip(ZipInputStream zip, ZipEntry theEntry)
    {
      MemoryStream ms = await ZipUtils.ReadZipFileToMemoryAsync(zip);

      PackageDef = new XmlDocument();

      string content = "";

      try
      {
        content = StringUtils.GetUtf8Bytes(StringUtils.FixUtf8BOM(ms.ToArray()));
        PackageDef.LoadXml(content);
      }
      catch (System.Exception e)
      {
        GameErrorChecker.SetLastErrorAndLog(GameError.PackageIncompatible, TAG, "Format error in PackageDef.xml : " + e + "\nCheck content: " + content);
        return null;
      }
      finally
      {
        ms.Close();
        ms.Dispose();
      }

      return PackageDef;
    }
    private async Task<bool> LoadPackageLanguageResPreInZip(ZipInputStream zip, ZipEntry theEntry)
    {
      MemoryStream ms = await ZipUtils.ReadZipFileToMemoryAsync(zip);
      try
      {
        PreLoadI18NResource(StringUtils.GetUtf8Bytes(StringUtils.FixUtf8BOM(ms.ToArray())));
      }
      catch
      {
        return false;
      }
      finally
      {
        ms.Close();
        ms.Dispose();
      }
      return true;
    }
    private async Task<Sprite> LoadLogoInZip(ZipInputStream zip, ZipEntry theEntry)
    {
      try
      {
        Texture2D texture2D = new Texture2D(128, 128);
        MemoryStream ms = await ZipUtils.ReadZipFileToMemoryAsync(zip);
        texture2D.LoadImage(ms.ToArray());
        ms.Close();
        ms.Dispose();

        return Sprite.Create(texture2D,
            new Rect(Vector2.zero, new Vector2(texture2D.width, texture2D.height)),
            new Vector2(0.5f, 0.5f));
      }
      catch (Exception e)
      {
        Log.E(TAG, "Failed to load package logo {0}\nError: {1}", BaseInfo.Logo, e.ToString());
        return null;
      }
    }

    private async Task<bool> LoadAssetBundleToMemoryInZipAsync(ZipInputStream zip, ZipEntry theEntry)
    {
      MemoryStream ms = await ZipUtils.ReadZipFileToMemoryAsync(zip);

      if (ms == null)
      {
        LoadError = "Invalid package: AssetBundle is missing.";
        GameErrorChecker.SetLastErrorAndLog(GameError.AssetBundleNotFound, TAG, "Not found AssetBundle in Package");
        return false;
      }

      // Load the AssetBundle.
      AssetBundleCreateRequest createRequest = AssetBundle.LoadFromMemoryAsync(ms.ToArray());
      await createRequest;

      AssetBundle = createRequest.assetBundle;

      ms.Close();
      ms.Dispose();

      if (AssetBundle == null)
      {
        LoadError = "Invalid package: failed to load AssetBundle.";
        GameErrorChecker.SetLastErrorAndLog(GameError.AssetBundleNotFound, TAG, "AssetBundle load failed in Package");
        return false;
      }

      Log.D(TAG, "AssetBundle {0} loaded", theEntry.Name);
      return true;
    }
    protected override Assembly LoadCodeCSharp(string pathorname)
    {
#if ENABLE_MONO || ENABLE_DOTNET

      ZipInputStream zip = ZipUtils.OpenZipFile(PathUtils.FixFilePathScheme(PackageFilePath));
      if (zip == null)
        return null;

      ZipEntry theEntry;
      while ((theEntry = zip.GetNextEntry()) != null)
      {
        if (theEntry.Name == "csharp/" + pathorname)
        {
          MemoryStream ms = ZipUtils.ReadZipFileToMemory(zip);
          try
          {
            return Assembly.Load(ms.ToArray());
          }
          catch (Exception e)
          {
            Log.E(TAG, "Package {0} failed to load code {1}. Error:\n{2}", PackageName, pathorname, e.ToString());
            return null;
          } 
          finally 
          {
            if(ms != null)
              ms.Dispose();
          }
        }
      }

      Log.E(TAG, "Package {0} failed to load code: {1} was not found.", PackageName, pathorname);
#else
      Log.E(TAG, "Package {0} cannot load C# DLLs under IL2CPP.", PackageName);
#endif
      return null;
    }
  }
}
