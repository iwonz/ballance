using Ballance2.Services.Debug;
using Ballance2.Utils;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using UnityEngine.Networking;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* GameAssetBundlePackage.cs
* 
* Purpose:
* AssetBundle game package definition.
*
* Author:
* mengyu
*/

namespace Ballance2.Package
{
  /// <summary>
  /// Package AssetBundle.
  /// </summary>
  public class GameAssetBundlePackage : GamePackage
  {
    public override void Destroy()
    {
      base.Destroy();
    }

    public override async Task<bool> LoadInfo(string filePath)
    {
      PackageFilePath = PathUtils.FixFilePathScheme(filePath);

      using var request = UnityWebRequest.Get(PathUtils.ToRequestUrl(filePath));
      await request.SendWebRequest();

      if (request.result == UnityWebRequest.Result.Success)
      {
        AssetBundleCreateRequest assetBundleCreateRequest = AssetBundle.LoadFromMemoryAsync(request.downloadHandler.data);
        await assetBundleCreateRequest;
        AssetBundle = assetBundleCreateRequest.assetBundle;

        if (AssetBundle == null)
        {
          LoadError = "Invalid package: failed to load AssetBundle.";
          GameErrorChecker.SetLastErrorAndLog(GameError.AssetBundleNotFound, TAG, "Not found AssetBundle in Package");
          return false;
        }
        else
        {
          PreLoadI18NResource(null);

          TextAsset modDefTextAsset = AssetBundle.LoadAsset<TextAsset>("PackageDef.xml");
          if (modDefTextAsset == null || string.IsNullOrEmpty(modDefTextAsset.text))
          {
            GameErrorChecker.SetLastErrorAndLog(GameError.PackageDefNotFound, TAG, "PackageDef.xml not found");
            LoadError = "Package does not contain PackageDef.xml.";
            return false;
          }
          else
          {
            PackageDef = new XmlDocument();

            try
            {
              PackageDef.LoadXml(modDefTextAsset.text);
            }
            catch (System.Exception e)
            {
              GameErrorChecker.SetLastErrorAndLog(GameError.PackageIncompatible, TAG, "Format error in PackageDef.xml : " + e);
              return false;
            }

            if (StringUtils.IsUrl(filePath)) UpdateTime = System.DateTime.Now;
            else UpdateTime = System.IO.File.GetLastWriteTime(PathUtils.FixFilePathScheme(filePath));

            if(ReadInfo(PackageDef)) 
            {
              if (!string.IsNullOrEmpty(BaseInfo.Logo))
                LoadLogo();
              return true;
            }
            return false;
          }
        }
      }
      else
      {
        if (request.responseCode == 404)
          LoadError = "Resource package not found.";
        else if (request.responseCode == 403)
          LoadError = "Permission denied while reading the resource package.";
        else
          LoadError = "HTTP request error: " + request.responseCode;

        GameErrorChecker.SetLastErrorAndLog(GameError.NetworkError, TAG, "Load AssetBundle failed : " + LoadError + "(" + request.responseCode + ")");
        return false;
      }
    }    
    private void LoadLogo()
    {
      try
      {
        Texture2D texture2D = AssetBundle.LoadAsset<Texture2D>(BaseInfo.Logo);
        BaseInfo.LogoTexture = Sprite.Create(texture2D,
            new Rect(Vector2.zero, new Vector2(texture2D.width, texture2D.height)),
            new Vector2(0.5f, 0.5f));
      }
      catch (System.Exception e)
      {
        BaseInfo.LogoTexture = null;
        Log.E(TAG, "Failed to load package logo {0}\nError: {1}", BaseInfo.Logo, e.ToString());
      }
    }
  }
}
