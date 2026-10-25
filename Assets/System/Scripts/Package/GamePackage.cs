using Ballance2.Config;
using Ballance2.Services.Debug;
using Ballance2.Services.I18N;
using Ballance2.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using UnityEngine.Profiling;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* GamePackage.cs
* 
* Purpose:
* Game package definition and operations.
* Own package code initialization/unloading and resource access.
*
* Author:
* mengyu
*/

namespace Ballance2.Package
{
  /// <summary>
  /// Game package instance.
  /// </summary>
  public class GamePackage
  {
    /// <summary>
    /// Tag.
    /// </summary>
    public string TAG
    {
      get { return "GamePackage:" + PackageName; }
    }

    internal int DependencyRefCount = 0;
    internal bool UnLoadWhenDependencyRefNone = false;

    public virtual Task<bool> LoadInfo(string filePath)
    {
      if (filePath != null)
        PackageFilePath = filePath;
      return Task.FromResult(true);
    }
    public virtual Task<bool> LoadPackage()
    {
      LoadI18NResource();

      // Initialize package code.
      if (Type == GamePackageType.Module)
        return Task.FromResult(LoadPackageCodeBase());
      return Task.FromResult(true);
    }
    public virtual void Destroy()
    {
      Log.D(TAG, "Destroy package {0}", PackageName);

      if(!IsUnloadCodeExecuted())
        RunPackageBeforeUnLoadCode();

      // Release the AssetBundle.
      if (AssetBundle != null)
      {
        AssetBundle.Unload(true);
        AssetBundle = null;
      }

      if (CSharpAssembly != null)
        CSharpAssembly = null;
      if (PackageEntry != null)
        PackageEntry = null;
    }

    #region System package

#if UNITY_EDITOR
    private static GamePackage _SystemPackage = new GameEditorSystemPackage();
#else
    private static GamePackage _SystemPackage = new GameSystemPackage();
#endif

    /// <summary>
    /// Get the core system package, identified by GamePackageManager.SYSTEM_PACKAGE_NAME.
    /// </summary>
    /// <returns></returns>
    public static GamePackage GetSystemPackage() { 
      return _SystemPackage; 
    }

    #endregion

    #region Constants

    public const int FLAG_CODE_BASE_LOADED = 0x000000001;
    public const int FLAG_CODE_ENTRY_CODE_RUN = 0x000000008;
    public const int FLAG_CODE_UNLOD_CODE_RUN = 0x000000010;
    public const int FLAG_PACK_NOT_UNLOADABLE = 0x000000020;
    public const int FLAG_PACK_SYSTEM_PACKAGE = 0x000000040;

    #endregion

    #region Package code environment

    /// <summary>
    /// C# assembly.
    /// </summary>
    public Assembly CSharpAssembly { get; protected set; }
    /// <summary>
    /// Package entry point.
    /// </summary>
    public GamePackageEntry PackageEntry = new GamePackageEntry();
    
    internal int flag = 0;

    /// <summary>
    /// Whether the package is protected from unloading.
    /// </summary>
    /// <returns></returns>
    public bool IsNotUnLoadable() { return (flag & FLAG_PACK_NOT_UNLOADABLE) == FLAG_PACK_NOT_UNLOADABLE; }
    /// <summary>
    /// Whether this is a system package.
    /// </summary>
    /// <returns></returns>
    public bool IsSystemPackage() { return (flag & FLAG_PACK_SYSTEM_PACKAGE) == FLAG_PACK_SYSTEM_PACKAGE; }
    /// <summary>
    /// Whether entry code has run.
    /// </summary>
    /// <returns></returns>
    public bool IsEntryCodeExecuted() { return (flag & FLAG_CODE_ENTRY_CODE_RUN) == FLAG_CODE_ENTRY_CODE_RUN; }
    /// <summary>
    /// Whether unload code has run.
    /// </summary>
    /// <returns></returns>
    public bool IsUnloadCodeExecuted() { return (flag & FLAG_CODE_UNLOD_CODE_RUN) == FLAG_CODE_UNLOD_CODE_RUN; }

    protected virtual Assembly LoadCodeCSharp(string pathorname) {
#if UNITY_EDITOR
      return Assembly.GetAssembly(typeof(GameSystem));
#else
      throw new NotImplementedException();
#endif
    }

    /// <summary>
    /// Set package flags.
    /// </summary>
    /// <param name="flag">Flags from GamePackage.FLAG_*.</param>
    public void SetFlag(int flag)  {

      if((this.flag & FLAG_PACK_NOT_UNLOADABLE) == FLAG_PACK_NOT_UNLOADABLE && (flag & FLAG_PACK_NOT_UNLOADABLE) != FLAG_PACK_NOT_UNLOADABLE) {
        Log.E(TAG, "Not allow set FLAG_PACK_NOT_UNLOADABLE flag for not unloadable packages.");
        flag |= FLAG_PACK_NOT_UNLOADABLE;
      }
      if((this.flag & FLAG_PACK_SYSTEM_PACKAGE) == FLAG_PACK_SYSTEM_PACKAGE && (flag & FLAG_PACK_NOT_UNLOADABLE) != FLAG_PACK_NOT_UNLOADABLE) {
        Log.E(TAG, "Not allow set FLAG_PACK_NOT_UNLOADABLE flag for not system packages.");
        flag |= FLAG_PACK_NOT_UNLOADABLE;
      }
      this.flag = flag;
    }
    /// <summary>
    /// Get package flags.
    /// </summary>
    public int GetFlag() { return flag; }

    /// <summary>
    /// Load package code.
    /// </summary>
    /// <returns></returns>
    protected bool LoadPackageCodeBase() {
      // Check initialization.
      if((FLAG_CODE_BASE_LOADED & flag) == FLAG_CODE_BASE_LOADED) {
        Log.E(TAG, "Cannot initialize twice.");
        return false;
      }
      if (IsSystemPackage())
        return true;

      // Built-in mods are compiled with the player, including AOT platforms.
      switch (PackageName)
      {
        case "mod.invlife": PackageEntry = mod.invlife.PackageEntry.Main(); break;
        case "mod.selfkill": PackageEntry = mod.selfkill.PackageEntry.Main(); break;
        case "mod.speedball": PackageEntry = mod.speedball.PackageEntry.Main(); break;
        default: PackageEntry = null; break;
      }
      if (PackageEntry != null)
      {
        flag |= FLAG_CODE_BASE_LOADED;
        return true;
      }

      Type type = null;

      #if UNITY_EDITOR

      CSharpAssembly = Assembly.GetAssembly(typeof(GamePackage));
      type = CSharpAssembly.GetType(PackageName + ".PackageEntry");

      #else

      // Load the C# assembly.
      CSharpAssembly = LoadCodeCSharp(PackageName + ".dll");
      if (CSharpAssembly == null) {
        Log.E(TAG, "Cannot load DLL: " + PackageName + ".dll");
        return false;
      }
      type = CSharpAssembly.GetType(PackageName + ".PackageEntry");

      #endif
      
      if (type == null)
      {
        Log.W(TAG, "PackageEntry not found.");
        GameErrorChecker.LastError = GameError.ClassNotFound;
        return false;
      }
      else
      {
        try {
          object CSharpPackageEntry = Activator.CreateInstance(type);
          MethodInfo methodInfo = type.GetMethod("Main");  // Find MethodInfo by method name.
          if (type == null)
          {
            Log.W(TAG, "PackageEntry.Main() not found.");
            GameErrorChecker.LastError = GameError.FunctionNotFound;
          } 
          else  
          {
            object b = methodInfo.Invoke(null, new object[0]);
            if (b.GetType() == typeof(GamePackageEntry)) 
            {
              PackageEntry = b as GamePackageEntry;
            } 
            else
            {
              Log.W(TAG, "PackageEntry.Main returned an unknown type.");
              return false;
            }
          }
        } catch (System.Exception e) {
          Log.W(TAG, "PackageEntry.Main failed: " + e.ToString());
          return false;
        }
      }
      flag |= FLAG_CODE_BASE_LOADED;
      return true;
    }    

    /// <summary>
    /// Run package initialization once; repeated execution is not allowed.
    /// </summary>
    /// <returns>Whether the operation succeeded.</returns>
    public bool RunPackageExecutionCode()
    {
      if (Type != GamePackageType.Module)
      {
        GameErrorChecker.LastError = GameError.PackageCanNotRun;
        return false;
      }
      if (IsEntryCodeExecuted()) {
        GameErrorChecker.SetLastErrorAndLog(GameError.ExecutionFailed, TAG, "Run ExecutionCode failed, an not run twice");
        return false;
      }

      flag |= FLAG_CODE_ENTRY_CODE_RUN;
      flag ^= FLAG_CODE_UNLOD_CODE_RUN;

      if(PackageEntry.OnLoad != null) {
        
        Profiler.BeginSample(TAG + "PackageEntry.OnLoad");

        bool result = PackageEntry.OnLoad.Invoke(this);

        Profiler.EndSample();

        return result;
      }
      return true;
    }
    /// <summary>
    /// Run the package unload callback once; repeated execution is not allowed.
    /// </summary>
    /// <returns>Whether the operation succeeded.</returns>
    public bool RunPackageBeforeUnLoadCode()
    {
      if (Type != GamePackageType.Module)
      {
        GameErrorChecker.LastError = GameError.PackageCanNotRun;
        return false;
      }
      if (IsUnloadCodeExecuted()) {
        GameErrorChecker.SetLastErrorAndLog(GameError.ExecutionFailed, TAG, "Run BeforeUnLoadCode failed, an not run twice");
        return false;
      }

      flag |= FLAG_CODE_UNLOD_CODE_RUN;
      flag ^= FLAG_CODE_ENTRY_CODE_RUN;

      if(PackageEntry.OnBeforeUnLoad != null) {
        
        Profiler.BeginSample(TAG + "PackageEntry.OnBeforeUnLoad");
        
        bool result = PackageEntry.OnBeforeUnLoad.Invoke(this);

        Profiler.EndSample();

        return result;
      }
      return true;
    }

    #endregion

    #region Package metadata

    private int VerConverter(string s)
    {
      if (s == "{internal.core.version}")
        return GameConst.GameBulidVersion;
      return ConverUtils.StringToInt(s, 0, "Package/version");
    }

    protected bool ReadInfo(XmlDocument xml)
    {
      XmlNode nodePackage = xml.SelectSingleNode("Package");
      XmlAttribute attributeName = nodePackage.Attributes["name"];
      XmlAttribute attributeVersion = nodePackage.Attributes["version"];
      XmlNode nodeBaseInfo = nodePackage.SelectSingleNode("BaseInfo");

      if (attributeName == null)
      {
        LoadError = "Invalid PackageDef.xml: name is missing.";
        GameErrorChecker.SetLastErrorAndLog(GameError.MissingAttribute, TAG, "Package attribute name is null");
        return false;
      }
      if (attributeVersion == null)
      {
        LoadError = "Invalid PackageDef.xml: version is missing.";
        GameErrorChecker.SetLastErrorAndLog(GameError.MissingAttribute, TAG, "Package attribute version is null");
        return false;
      }
      if (nodeBaseInfo == null)
      {
        LoadError = "Invalid PackageDef.xml: BaseInfo is missing.";
        GameErrorChecker.SetLastErrorAndLog(GameError.MissingAttribute, TAG, "Package node BaseInfo is null");
        return false;
      }

      //Version and PackageName
      PackageName = attributeName.Value;
      PackageVersion = VerConverter(attributeVersion.Value);

      //BaseInfo
      BaseInfo = new GamePackageBaseInfo(nodeBaseInfo, this);

      //Compatibility
      XmlNode nodeCompatibility = nodePackage.SelectSingleNode("Compatibility");
      if (nodeCompatibility != null)
        for (int i = 0; i < nodeCompatibility.Attributes.Count; i++)
        {
          switch (nodeCompatibility.ChildNodes[i].Name)
          {
            case "TargetVersion":
              TargetVersion = ConverUtils.StringToInt(nodeCompatibility.ChildNodes[i].InnerText,
                  GameConst.GameBulidVersion, "Compatibility/TargetVersion");
              break;
            case "MinVersion":
              MinVersion = ConverUtils.StringToInt(nodeCompatibility.ChildNodes[i].InnerText,
                  GameConst.GameBulidVersion, "Compatibility/MinVersion");
              break;
          }
        }

      // Check compatibility.
      if (MinVersion > GameConst.GameBulidVersion)
      {
        Log.E(TAG, "MinVersion {0} greater than game version {1}", MinVersion, GameConst.GameBulidVersion);
        LoadError = "Package is incompatible with this game version. Required version >=" + MinVersion;
        GameErrorChecker.LastError = GameError.PackageIncompatible;
        IsCompatible = false;
        return false;
      }
      else
      {
        IsCompatible = true;
      }

      // Arguments.
      XmlNode nodeEntryCode = nodePackage.SelectSingleNode("EntryCode");
      if (nodeEntryCode != null)
        EntryCode = nodeEntryCode.InnerText;
      XmlNode nodeType = nodePackage.SelectSingleNode("Type");
      if (nodeType != null)
        Type = ConverUtils.StringToEnum(nodeType.InnerText, GamePackageType.Asset, "Type");

      return true;
    }

    /// <summary>
    /// Get the package file path.
    /// </summary>
    public string PackageFilePath { get; protected set; }
    /// <summary>
    /// Get the package identifier.
    /// </summary>
    public string PackageName { get; protected set; }
    /// <summary>
    /// Get the package version number.
    /// </summary>
    public int PackageVersion { get; protected set; }
    /// <summary>
    /// Get package metadata.
    /// </summary>
    public GamePackageBaseInfo BaseInfo { get; protected set; }
    /// <summary>
    /// Get the package update time.
    /// </summary>
    public DateTime UpdateTime { get; protected set; }
    /// <summary>
    /// Whether this package is required by the system.
    /// </summary>
    public bool SystemPackage { get; internal set; }

    /// <summary>
    /// Get the package load error.
    /// </summary>
    public string LoadError { get; protected set; } = "";
    /// <summary>
    /// Get the PackageDef document.
    /// </summary>
    public XmlDocument PackageDef { get; protected set; }
    /// <summary>
    /// Get the package AssetBundle.
    /// </summary>
    public AssetBundle AssetBundle { get; protected set; }
    /// <summary>
    /// Get the target game build version.
    /// </summary>
    public int TargetVersion { get; protected set; } = GameConst.GameBulidVersion;
    /// <summary>
    /// Get the minimum supported game build version.
    /// </summary>
    public int MinVersion { get; protected set; } = GameConst.GameBulidVersion;
    /// <summary>
    /// Whether the package is compatible with the current game build.
    /// </summary>
    public bool IsCompatible { get; protected set; }

    /// <summary>
    /// Get package entry code.
    /// </summary>
    public string EntryCode { get; protected set; }
    /// <summary>
    /// Get the package type.
    /// </summary>
    public GamePackageType Type { get; protected set; } = GamePackageType.Asset;

    internal GamePackageStatus _Status = GamePackageStatus.NotLoad;

    /// <summary>
    /// Get the package load state.
    /// </summary>
    public GamePackageStatus Status { get { return _Status; } }
    /// <summary>
    /// Format for display.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
      return "Package: " + PackageName + "(" + PackageVersion + ") => " + _Status;
    }
    /// <summary>
    /// List all resources.
    /// </summary>
    /// <returns></returns>
    public virtual string ListResource()
    {
      StringBuilder sb = new StringBuilder();
      if(AssetBundle != null) {
        var list = AssetBundle.GetAllAssetNames();
        sb.Append("[AssetBundle " + AssetBundle.name + " assets count: " + list.Length + " ]");
        for (int i = 0; i < list.Length; i++)
          sb.AppendLine(list[i]);
      } else {
        sb.Append("[AssetBundle is null]");
      }
      return sb.ToString();
    }

    #endregion

    #region Resource access

    /// <summary>
    /// Read an asset from the package.
    /// </summary>
    /// <param name="pathorname">Resource path.</param>
    /// <returns>The asset, or null if not found.</returns>
    public virtual T GetAsset<T>(string pathorname) where T : UnityEngine.Object
    {
      if (AssetBundle == null)
      {
        GameErrorChecker.LastError = GameError.NotLoad;
        return null;
      }

      return AssetBundle.LoadAsset<T>(pathorname);
    }

    /// <summary>
    /// Read a text asset from the package.
    /// </summary>
    /// <param name="pathorname">Resource path.</param>
    /// <returns>The TextAsset, or null if not found.</returns>
    public virtual TextAsset GetTextAsset(string pathorname) { return GetAsset<TextAsset>(pathorname); }

    /// <summary>
    /// Read a prefab from the package.
    /// </summary>
    /// <param name="pathorname">Resource path.</param>
    /// <returns>The GameObject, or null if not found.</returns>
    public virtual GameObject GetPrefabAsset(string pathorname) { return GetAsset<GameObject>(pathorname); }
    /// <summary>
    /// Read a Texture from the package.
    /// </summary>
    /// <param name="pathorname">Resource path.</param>
    /// <returns>The Texture, or null if not found.</returns>
    public virtual Texture GetTextureAsset(string pathorname) { return GetAsset<Texture>(pathorname); }
    /// <summary>
    /// Read a Texture2D from the package.
    /// </summary>
    /// <param name="pathorname">Resource path.</param>
    /// <returns>The Texture2D, or null if not found.</returns>
    public virtual Texture2D GetTexture2DAsset(string pathorname) { return GetAsset<Texture2D>(pathorname); }
    /// <summary>
    /// Read a Sprite from the package.
    /// </summary>
    /// <param name="pathorname">Resource path.</param>
    /// <returns>The Sprite, or null if not found.</returns>
    public virtual Sprite GetSpriteAsset(string pathorname) { return GetAsset<Sprite>(pathorname); }
    /// <summary>
    /// Read a Material from the package.
    /// </summary>
    /// <param name="pathorname">Resource path.</param>
    /// <returns>The Material, or null if not found.</returns>
    public virtual Material GetMaterialAsset(string pathorname) { return GetAsset<Material>(pathorname); }
    /// <summary>
    /// Read a PhysicsMaterial from the package.
    /// </summary>
    /// <param name="pathorname">Resource path.</param>
    /// <returns>The PhysicsMaterial, or null if not found.</returns>
    public virtual PhysicsMaterial GetPhysicMaterialAsset(string pathorname) { return GetAsset<PhysicsMaterial>(pathorname); }
    /// <summary>
    /// Read an AudioClip from the package.
    /// </summary>
    /// <param name="pathorname">Resource path.</param>
    /// <returns>The AudioClip, or null if not found.</returns>
    public virtual AudioClip GetAudioClipAsset(string pathorname) { return GetAsset<AudioClip>(pathorname); }

    #endregion

    #region Package operations

    private Dictionary<string, string> preI18NResource = new Dictionary<string, string>();

    /// <summary>
    /// Find a string in the package's preloaded language resources.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <returns>Localized string, or null if not found.</returns>
    public string GetPackageI18NResourceInPre(string key) {
      if(preI18NResource.TryGetValue(key, out var s))
        return s;
      return null;
    }
    /// <summary>
    /// Preload language resources.
    /// </summary>
    protected void PreLoadI18NResource(string resString) {
      if (resString != null)
        preI18NResource = I18NProvider.PreLoadLanguageResources(resString);
      else {
        var res = GetTextAsset("PackageLanguageResPre.xml");
        if (res != null)
          preI18NResource = I18NProvider.PreLoadLanguageResources(res.text);
      }
    }
    /// <summary>
    /// Load the package's language resources.
    /// </summary>
    private void LoadI18NResource()
    {
      var res = GetTextAsset("PackageLanguageRes.xml");
      if (res != null)
      {
        if (!I18NProvider.LoadLanguageResources(res.text))
          Log.E(TAG, "Failed to load PackageLanguageRes.xml for package " + PackageName);
      }
    }

    // Custom data for convenient access.

    private Dictionary<string, object> packageCustomData = new Dictionary<string, object>();

    /// <summary>
    /// Add custom data.
    /// </summary>
    /// <param name="name">Data name.</param>
    /// <param name="data">Data value.</param>
    /// <returns>The data value.</returns>
    public object AddCustomProp(string name, object data)
    {
      if (packageCustomData.ContainsKey(name))
      {
        packageCustomData[name] = data;
        return data;
      }
      packageCustomData.Add(name, data);
      return data;
    }
    /// <summary>
    /// Get custom data.
    /// </summary>
    /// <param name="name">Data name.</param>
    /// <returns>The data value.</returns>
    public object GetCustomProp(string name)
    {
      if (packageCustomData.ContainsKey(name))
        return packageCustomData[name];
      return null;
    }
    /// <summary>
    /// Set custom data.
    /// </summary>
    /// <param name="name">Data name.</param>
    /// <param name="data"></param>
    /// <returns>The previous value, or null if none existed.</returns>
    public object SetCustomProp(string name, object data)
    {
      if (packageCustomData.ContainsKey(name))
      {
        object old = packageCustomData[name];
        packageCustomData[name] = data;
        return old;
      }
      return null;
    }
    /// <summary>
    /// Remove custom data.
    /// </summary>
    /// <param name="name">Data name.</param>
    /// <returns>Whether the operation succeeded.</returns>
    public bool RemoveCustomProp(string name)
    {
      if (packageCustomData.ContainsKey(name))
      {
        packageCustomData.Remove(name);
        return true;
      }
      return false;
    }

    #endregion
  }
}
