using Ballance2.Config;
using Ballance2.Utils;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* GamePackageInfo.cs
* 
* Purpose:
* Game package metadata structures and enums.
*
* Author:
* mengyu
*/

namespace Ballance2.Package
{
  /// <summary>
  /// Package type.
  /// </summary>
  public enum GamePackageType
  {
    /// <summary>
    /// Code package.
    /// </summary>
    Module,
    /// <summary>
    /// Resource package.
    /// </summary>
    Asset
  }
  /// <summary>
  /// Package load state.
  /// </summary>  
  public enum GamePackageStatus
  {
    /// <summary>
    /// Not loaded.
    /// </summary>
    NotLoad = 0,
    /// <summary>
    /// Registering.
    /// </summary>
    Registing = 1,
    /// <summary>
    /// Loading.
    /// </summary>
    Loading = 2,
    /// <summary>
    /// Loaded successfully.
    /// </summary>
    LoadSuccess = 3,
    /// <summary>
    /// Load failed.
    /// </summary>
    LoadFailed = 4,
    /// <summary>
    /// Waiting to unload.
    /// </summary>
    UnloadWaiting = 5,
    /// <summary>
    /// Registered but not loaded.
    /// </summary>
    Registered = 6,
  }

  /// <summary>
  /// Package metadata.
  /// </summary>
  public class GamePackageBaseInfo
  {
    private GamePackage package;

    public GamePackageBaseInfo(XmlNode xmlNodeBaseInfo, GamePackage package)
    {
      this.package = package;
      if (xmlNodeBaseInfo != null)
        for (int i = 0; i < xmlNodeBaseInfo.ChildNodes.Count; i++)
        {
          switch (xmlNodeBaseInfo.ChildNodes[i].Name)
          {
            case "Name": Name = CheckAndGetI18NString(xmlNodeBaseInfo.ChildNodes[i].InnerText); break;
            case "Author": Author = FixCdData(CheckAndGetI18NString(xmlNodeBaseInfo.ChildNodes[i].InnerText)); break;
            case "Introduction": Introduction = FixCdData(CheckAndGetI18NString(xmlNodeBaseInfo.ChildNodes[i].InnerXml)); break;
            case "Logo": Logo = CheckAndGetI18NString(xmlNodeBaseInfo.ChildNodes[i].InnerText); break;
            case "Link": Link = CheckAndGetI18NString(xmlNodeBaseInfo.ChildNodes[i].InnerText); break;
            case "DocLink": DocLink = CheckAndGetI18NString(xmlNodeBaseInfo.ChildNodes[i].InnerText); break;
            case "AuthorLink": AuthorLink = CheckAndGetI18NString(xmlNodeBaseInfo.ChildNodes[i].InnerText); break;
            case "Description": Description = FixCdData(CheckAndGetI18NString(xmlNodeBaseInfo.ChildNodes[i].InnerText)); break;
            case "VersionName": VersionName = FixCdData(CheckAndGetI18NString(xmlNodeBaseInfo.ChildNodes[i].InnerText)); break;
            case "Dependencies":
              for (int j = 0, jc = xmlNodeBaseInfo.ChildNodes[i].ChildNodes.Count; j < jc; j++)
              {
                var node = xmlNodeBaseInfo.ChildNodes[i].ChildNodes[j];
                if (node.Attributes != null)
                  Dependencies.Add(new GamePackageDependencies(node));
              }
              break;
          }
        }
    }

    private string CheckAndGetI18NString(string x)
    {
      if (x.StartsWith("{") && x.EndsWith("}")) {
        var key = x.Substring(1, x.Length - 2);
        if (key == "internal.core.versionName")
          return GameConst.GameVersion;
        else {
          var str = package.GetPackageI18NResourceInPre(key);
          return str == null ? "[unknow:" + key + "]" : str;
        }
      }
      return x;
    }
    private string FixCdData(string x)
    {
      if (x.Length > 12 && x.StartsWith("<![CDATA[") && x.EndsWith("]]>"))
        return x.Substring(9, x.Length - 12);
      return x;
    }

    /// <summary>
    /// Package display name.
    /// </summary>
    public string Name { get; private set; }
    /// <summary>
    /// Package author.
    /// </summary>
    public string Author { get; private set; }
    /// <summary>
    /// Package introduction.
    /// </summary>
    public string Introduction { get; private set; }
    /// <summary>
    /// Description text.
    /// </summary>
    public string Description { get; private set; }
    /// <summary>
    /// Package logo.
    /// </summary>
    public string Logo { get; private set; }
    /// <summary>
    /// Package logo Sprite.
    /// </summary>
    public Sprite LogoTexture { get; set; }
    /// <summary>
    /// Package URL.
    /// </summary>
    public string Link { get; private set; }
    /// <summary>
    /// Author homepage URL.
    /// </summary>
    public string AuthorLink { get; private set; }
    /// <summary>
    /// Package documentation URL.
    /// </summary>
    public string DocLink { get; private set; }
    /// <summary>
    /// User-facing version.
    /// </summary>
    public string VersionName { get; private set; }
    /// <summary>
    /// Package dependencies.
    /// </summary>
    public List<GamePackageDependencies> Dependencies { get; } = new List<GamePackageDependencies>();
  }
  
  /// <summary>
  /// Package dependency metadata.
  /// </summary>
  [System.Serializable]
  public class GamePackageDependencies
  {
    public GamePackageDependencies(XmlNode xmlNode)
    {
      if (xmlNode != null)
      {
        for (int i = 0; i < xmlNode.Attributes.Count; i++)
        {
          switch (xmlNode.Attributes[i].Name)
          {
            case "name":
              Name = xmlNode.Attributes[i].Value;
              break;
            case "minVer":
              MinVersion = ConverUtils.StringToInt(xmlNode.Attributes[i].Value,
                  0, "Dependencies/minVer");
              break;
            case "mustLoad":
              MustLoad = ConverUtils.StringToBoolean(xmlNode.Attributes[i].Value,
                  false, "Dependencies/mustLoad");
              break;
          }
        }
      }
    }

    /// <summary>
    /// Dependency package identifier.
    /// </summary>
    public string Name { get; private set; }
    /// <summary>
    /// Minimum dependency version.
    /// </summary>
    public int MinVersion { get; private set; }
    /// <summary>
    /// Whether the dependency must load.
    /// </summary>
    public bool MustLoad { get; private set; }
  }
}
