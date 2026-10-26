using Ballance2.Base;
using Ballance2.Config;
using Ballance2.Config.Settings;
using Ballance2.Package;
using Ballance2.Res;
using Ballance2.Services.Debug;
using Ballance2.UI.Core;
using Ballance2.Utils;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using UnityEngine.Profiling;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* GamePackageManager.cs
* 
* Purpose:
* Manage package registration, loading, and unloading.
* Also provide package resource access APIs.
*
* Author:
* mengyu
*/

namespace Ballance2.Services
{
  /// <summary>
  /// Framework package manager.
  /// </summary>
  public class GamePackageManager : GameService<GamePackageManager>
  {
    private static readonly string TAG = "GamePackageManager";

    /// <summary>
    /// System package identifier.
    /// </summary>
    public const string SYSTEM_PACKAGE_NAME = "core";
    public GamePackageManager() : base(TAG) { }

    /// <summary>
    /// No-package mode disables loading third-party mods.
    /// </summary>
    /// <value></value>
    public bool NoPackageMode { get; set; }

    public override void Destroy()
    {
      UnLoadAllPackages();

      var systemPackage = GamePackage.GetSystemPackage();
      systemPackage.flag = (GamePackage.FLAG_PACK_NOT_UNLOADABLE | GamePackage.FLAG_PACK_SYSTEM_PACKAGE);
      systemPackage._Status = GamePackageStatus.NotLoad;
      registeredPackages.Remove(SYSTEM_PACKAGE_NAME);
      registeredPackages.Clear();
      packagesLoadStatus.Clear();
      loadedPackages.Clear();

      if(GameManager.GameMediator) {
        GameManager.GameMediator.UnRegisterGlobalEvent(GameEventNames.EVENT_PACKAGE_LOAD_FAILED);
        GameManager.GameMediator.UnRegisterGlobalEvent(GameEventNames.EVENT_PACKAGE_LOAD_SUCCESS);
        GameManager.GameMediator.UnRegisterGlobalEvent(GameEventNames.EVENT_PACKAGE_REGISTERED);
        GameManager.GameMediator.UnRegisterGlobalEvent(GameEventNames.EVENT_PACKAGE_UNLOAD);
      }
    }
    
    public override bool Initialize()
    {
      var systemPackage = GamePackage.GetSystemPackage();

      GameManager.GameMediator.RegisterGlobalEvent(GameEventNames.EVENT_PACKAGE_LOAD_FAILED);
      GameManager.GameMediator.RegisterGlobalEvent(GameEventNames.EVENT_PACKAGE_LOAD_SUCCESS);
      GameManager.GameMediator.RegisterGlobalEvent(GameEventNames.EVENT_PACKAGE_REGISTERED);
      GameManager.GameMediator.RegisterGlobalEvent(GameEventNames.EVENT_PACKAGE_UNREGISTERED);
      GameManager.GameMediator.RegisterGlobalEvent(GameEventNames.EVENT_PACKAGE_UNLOAD);
      GameManager.GameMediator.RegisterEventHandler(systemPackage,
        GameEventNames.EVENT_UI_MANAGER_INIT_FINISHED, "GamePackageManagerHandler", (evtName, param) =>
        {
          InitPackageCommands();
          return false;
        });
      GameManager.GameMediator.DelayedNotifySingleEvent("GameManagerWaitPackageManagerReady", 0.5f);
      GameManager.GameMediator.SubscribeSingleEvent(systemPackage, "DoSendAllPackageUILoad", TAG, (evtName, param) => {
        foreach (var package in loadedPackages)
          package.Value.PackageEntry.OnLoadUI?.Invoke(package.Value);
        return true;
      });
      

      systemPackage._Status = GamePackageStatus.LoadSuccess;
      registeredPackages.Add(SYSTEM_PACKAGE_NAME, new GamePackageRegisterInfo(systemPackage));
      loadedPackages.Add(SYSTEM_PACKAGE_NAME, systemPackage);
      return true;
    }

    #region Automatic package loading

    internal class GamePackageRegisterInfo
    {
      public GamePackageRegisterInfo(GamePackage package)
      {
        this.package = package;
        this.packageName = package.PackageName;
        this.enableLoad = true;
      }
      public GamePackageRegisterInfo(string packageName, bool enableLoad, bool trustPackage)
      {
        this.packageName = packageName;
        this.enableLoad = enableLoad;
        this.trustPackage = trustPackage;
      }

      public GamePackage package;

      public string packageName;
      public bool enableLoad;
      public bool trustPackage;
    }

    internal Dictionary<string, int> packagesLoadStatus = new Dictionary<string, int>();
    internal Dictionary<string, GamePackageRegisterInfo> registeredPackages = new Dictionary<string, GamePackageRegisterInfo>();
    internal Dictionary<string, GamePackage> loadedPackages = new Dictionary<string, GamePackage>();

    private XmlDocument packageEnableStatusListXml = new XmlDocument();

    /// <summary>
    /// Scan packages.
    /// </summary>
    internal void ScanUserPackages(Dictionary<string, GamePackageRegisterInfo> list) {
#if UNITY_EDITOR
      DirectoryInfo direction = new DirectoryInfo(GamePathManager.DEBUG_PACKAGE_FOLDER);
      DirectoryInfo[] dirs = direction.GetDirectories("*", SearchOption.TopDirectoryOnly);
      for (int i = 0; i < dirs.Length; i++) {
        // Add newly discovered packages to the list.
        var rel = Path.GetFileName(dirs[i].FullName.Replace('\\', '/'));
        if(StringUtils.IsPackageName(rel) && !list.ContainsKey(rel) && !IsPackageRegistered(rel)) {
          list.Add(rel, new GamePackageRegisterInfo(rel, false, true));
        }
      }
#else
      foreach (var file in GamePathManager.builtInPackagesFiles)
      {
        var name = Path.GetFileNameWithoutExtension(file);
        if (!list.ContainsKey(name) && !IsPackageRegistered(name))
          list.Add(name, new GamePackageRegisterInfo(name, false, true));
      }
      var dirPath = GamePathManager.GetResRealPath("package", "", false, false);
      if(Directory.Exists(dirPath)) {
        // Add newly discovered packages to the list.
        DirectoryInfo direction = new DirectoryInfo(dirPath);
        FileInfo[] files = direction.GetFiles("*.ballance", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < files.Length; i++)
        {
          var rel = files[i].FullName.Replace('\\', '/');
          var fileNameNoExt = Path.GetFileNameWithoutExtension(rel);
          if(StringUtils.IsPackageName(fileNameNoExt) && !list.ContainsKey(fileNameNoExt) && !IsPackageRegistered(fileNameNoExt)) {
            list.Add(fileNameNoExt, new GamePackageRegisterInfo(fileNameNoExt, false, true));
          }
        } 
      }
#endif
    }
    /// <summary>
    /// Read package state.
    /// </summary>
    /// <returns></returns>
    internal Dictionary<string, GamePackageRegisterInfo> LoadPackageRegisterInfo()
    {
      string pathPackageStatus = Application.persistentDataPath + "/PackageStatus.xml";
      if (File.Exists(pathPackageStatus))
      {
        StreamReader sr = new StreamReader(pathPackageStatus, Encoding.UTF8);
        try
        {
          packageEnableStatusListXml.LoadXml(sr.ReadToEnd());
        }
        catch
        {
          packageEnableStatusListXml.LoadXml(ConstStrings.DEFAULT_PACKAGE_STATUS_XML);
        }
        sr.Close();
        sr.Dispose();
      }
      else// Load the default XML document.
        packageEnableStatusListXml.LoadXml(ConstStrings.DEFAULT_PACKAGE_STATUS_XML);

      Dictionary<string, GamePackageRegisterInfo> lastRegisteredPackages = new Dictionary<string, GamePackageRegisterInfo>();
      XmlNode nodeNoPackageMode = packageEnableStatusListXml.SelectSingleNode("PackageConfig/NoPackageMode");
      if (nodeNoPackageMode != null)
        NoPackageMode = bool.Parse(nodeNoPackageMode.InnerText);
      XmlNode nodePackageList = packageEnableStatusListXml.SelectSingleNode("PackageConfig/PackageList");
      if (nodePackageList != null)
      {
        foreach (XmlNode n in nodePackageList) {
          if(!lastRegisteredPackages.ContainsKey(n.InnerText))
            lastRegisteredPackages.Add(n.InnerText, new GamePackageRegisterInfo(
              n.InnerText, 
              n.Attributes["enabled"].Value == "True",
              n.Attributes["trusted"].Value == "True")
            );
        }
      }

      return lastRegisteredPackages;
    }
    /// <summary>
    /// Save package state.
    /// </summary>
    internal void SavePackageRegisterInfo()
    {
      StreamWriter sw = new StreamWriter(Application.persistentDataPath + "/PackageStatus.xml", false, Encoding.UTF8);

      XmlDocument xml = new XmlDocument();
      XmlNode nodePackageConfig = xml.CreateElement("PackageConfig");
      XmlNode nodePackageList = xml.CreateElement("PackageList");
      XmlNode nodeNoPackageMode = xml.CreateElement("NoPackageMode");

      xml.AppendChild(xml.CreateXmlDeclaration("1.0", "utf-8", null));
      xml.AppendChild(nodePackageConfig);
      nodePackageConfig.AppendChild(nodePackageList);
      nodePackageConfig.AppendChild(nodeNoPackageMode);

      nodeNoPackageMode.InnerText = NoPackageMode.ToString();
      foreach (var s in registeredPackages.Values)
      {
        if(s.package.IsSystemPackage())
          continue;

        XmlNode node = xml.CreateElement("Package");
        XmlAttribute attr = xml.CreateAttribute("enabled");
        attr.Value = s.enableLoad.ToString();
        node.Attributes.Append(attr);
        attr = xml.CreateAttribute("trusted");
        attr.Value = s.trustPackage.ToString();
        node.Attributes.Append(attr);
        node.InnerText = s.package.PackageName;
        nodePackageList.AppendChild(node);
      }

      //save
      xml.Save(sw);
      sw.Close();
      sw.Dispose();

    }
    /// <summary>
    /// Mark a package as trusted.
    /// </summary>
    /// <param name="packageName">Package identifier.</param>
    internal void SetPackageTrusted(string packageName)
    {
      if (registeredPackages.TryGetValue(packageName, out var outPackage))
        outPackage.trustPackage = true;
    }
    /// <summary>
    /// Set whether a package is enabled.
    /// </summary>
    /// <param name="packageName">Package identifier.</param>
    /// <param name="val">Enabled state.</param>
    internal void SetPackageEnableLoad(string packageName, bool val)
    {
      if (registeredPackages.TryGetValue(packageName, out var outPackage))
        outPackage.enableLoad = val;
    }
    
    #endregion

    #region Package management API

    // Package trust dialog.

    private bool _isTrustPackageDialogFinished = false;
    private bool _trustPackageDialogResult = false;
    private GamePackage _trustPackageCurrent = null;
    
    internal bool GetTrustPackageDialogResult() { return _isTrustPackageDialogFinished; }
    internal bool IsTrustPackageDialogFinished() { return _trustPackageDialogResult; }
    internal void TrustPackageDialogFinish(bool result, bool delete) { 
      _trustPackageDialogResult = result;
      _isTrustPackageDialogFinished = true;
      if(!result && delete) {
        UnLoadPackage(_trustPackageCurrent.PackageName, true);
        // Delete.
        if(File.Exists(_trustPackageCurrent.PackageFilePath))
          File.Delete(_trustPackageCurrent.PackageFilePath);
      }
    }
    // Check whether a package is trusted for loading.
    internal bool IsTrustPackage(string packageName) {
      if (registeredPackages.TryGetValue(packageName, out var outPackage))
        return outPackage.trustPackage;
      return false;
    }
    internal void ShowTrustPackageDialog(GamePackage p) { 
      _isTrustPackageDialogFinished = false;
      _trustPackageDialogResult = false;
      _trustPackageCurrent = p;

      GameUIManager uIManager = GameSystem.GetSystemService<GameUIManager>();
      Dictionary<string, string> options = new Dictionary<string, string>();
      options.Add("PackageName", p.PackageName);
      options.Add("PackageTitle", p.BaseInfo.Name);
      uIManager.GoPageWithOptions("PagePackageManagerTrust", options);
    }

    /// <summary>
    /// Register a package.
    /// </summary>
    /// <param name="packageName">Package identifier.</param>
    /// <param name="load">Load the package immediately after registration.</param>
    /// <returns>Whether loading succeeded; inspect <see cref="GameErrorChecker.LastError"/> for the error code.</returns>
    public async Task<bool> RegisterPackage(string packageName)
    {
      bool forceEnablePackage = false;
      if (packageName.StartsWith("Enable:"))
      {
        packageName = packageName.Substring(7);
        forceEnablePackage = true;
      }

      if (packagesLoadStatus.ContainsKey(packageName))
      {
        GameErrorChecker.SetLastErrorAndLog(GameError.IsLoading, TAG, "Package {0} is loading", packageName);
        return false;
      }

      if (registeredPackages.ContainsKey(packageName))
      {
        Log.W(TAG, "Package {0} already registered!", packageName);
        return true;
      }

      string realPackagePath = null;
      GamePackage gamePackage = null;

      realPackagePath = GamePathManager.DEBUG_PACKAGE_FOLDER + "/" + packageName;
#if UNITY_EDITOR
      // Load inside the editor.
      if (DebugSettings.Instance.PackageLoadWay == LoadResWay.InUnityEditorProject && Directory.Exists(realPackagePath))
      {
        gamePackage = new GameEditorDebugPackage();
        Log.D(TAG, "Load package in editor : {0}", realPackagePath);
      }
      else
#else
      if(true) 
#endif
      {
        #if UNITY_EDITOR
        Log.D(TAG, "Check package path : {0}", realPackagePath);
        #endif

        // Resolve the path.
        realPackagePath = GamePathManager.GetResRealPath("package", packageName + ".ballance");
        string realPackagePathInCore = GamePathManager.GetResRealPath("core", packageName + ".ballance");

        // Use the Core path when present.
        if (FileUtils.FileExists(realPackagePathInCore)) realPackagePath = realPackagePathInCore;
        else if (PathUtils.IsRemotePath(realPackagePath) || FileUtils.FileExists(realPackagePath))
          realPackagePathInCore = "";
        else if (PathUtils.IsRemotePath(realPackagePathInCore) || FileUtils.FileExists(realPackagePathInCore)) // Use the Packages path when present.
          realPackagePath = realPackagePathInCore;
        else {
          // Report an error if neither exists.
          Log.E(TAG, "Package {0} register failed because file {1} not found", packageName, realPackagePath);
          return false;
        }
      }

      // Mark as loading.
      packagesLoadStatus.Add(packageName, 1);

      Log.D(TAG, "Registing package {0}", packageName);

      if (gamePackage == null)
      {
        // Detect the file type.
        if (PathUtils.IsRemotePath(realPackagePath) || FileUtils.TestFileIsZip(realPackagePath))
          gamePackage = new GameZipPackage();
        else if (FileUtils.TestFileIsAssetBundle(realPackagePath))
          gamePackage = new GameAssetBundlePackage();
        else
        {
          packagesLoadStatus.Remove(packageName);
          // Unsupported file format.
          GameErrorChecker.LastError = GameError.NotSupportFileType;
          Log.E(TAG, "Package file type not support {0}", realPackagePath);
          return false;
        }
      }

      gamePackage._Status = GamePackageStatus.Registing;

      // Load information.
      if (await gamePackage.LoadInfo(realPackagePath))
      {

        packagesLoadStatus.Remove(packageName);
        if (!registeredPackages.ContainsKey(packageName))
        {
          var info = new GamePackageRegisterInfo(gamePackage);
          info.enableLoad = forceEnablePackage;
          registeredPackages.Add(packageName, info);
        } 
        else 
        {
          var info = registeredPackages[packageName];
          info.enableLoad = forceEnablePackage;
          info.package = gamePackage;
        }

        gamePackage._Status = GamePackageStatus.Registered;

        Log.D(TAG, "Package {0} registered", packageName);
        // Send the notification event.
        GameManager.GameMediator.DispatchGlobalEvent(GameEventNames.EVENT_PACKAGE_REGISTERED, packageName);

        return true;
      }
      else
      {
        Log.E(TAG, "Package {0} failed LoadInfo", packageName);
      }

      packagesLoadStatus.Remove(packageName);
      return false;
    }
    /// <summary>
    /// Find a registered package.
    /// </summary>
    /// <param name="packageName">Package identifier.</param>
    /// <returns>Package instance, or null if not found.</returns>
    public GamePackage FindRegisteredPackage(string packageName)
    {
      registeredPackages.TryGetValue(packageName, out var outPackage);
      return outPackage != null ? outPackage.package : null;
    }
    /// <summary>
    /// Whether the user enabled the package.
    /// </summary>
    /// <param name="packageName">Package identifier.</param>
    /// <returns></returns>
    public bool IsPackageEnableLoad(string packageName)
    {
      registeredPackages.TryGetValue(packageName, out var outPackage);
      return outPackage != null && outPackage.enableLoad;
    }
    /// <summary>
    /// Unregister a package.
    /// </summary>
    /// <param name="packageName">Package identifier.</param>
    /// <param name="unLoadImmediately">Unload immediately.</param>
    /// <returns>Whether the operation succeeded.</returns>
    public bool UnRegisterPackage(string packageName, bool unLoadImmediately)
    {
      bool success = false;

      GamePackage package = FindPackage(packageName);
      if (package != null)
      {
        if ((package.GetFlag() & GamePackage.FLAG_PACK_SYSTEM_PACKAGE) == GamePackage.FLAG_PACK_SYSTEM_PACKAGE) {
          GameErrorChecker.SetLastErrorAndLog(GameError.AccessDenined, TAG, "Can not unload system package " + packageName + " ! ");
          return false;
        }
        if ((package.GetFlag() & GamePackage.FLAG_PACK_NOT_UNLOADABLE) == GamePackage.FLAG_PACK_NOT_UNLOADABLE) {
          GameErrorChecker.SetLastErrorAndLog(GameError.AccessDenined, TAG, "Can not unload package " + packageName + " with flag FLAG_PACK_NOT_UNLOADABLE ! ");
          return false;
        }
      }

      if (registeredPackages.ContainsKey(packageName))
      {
        registeredPackages.Remove(packageName);
        success = true;
      }
      if (packagesLoadStatus.ContainsKey(packageName))
        packagesLoadStatus.Remove(packageName);

      if (IsPackageLoaded(packageName))
        UnLoadPackage(packageName, unLoadImmediately);

      Log.D(TAG, "Package {0} unregistered", packageName);
      // Send the notification event.
      GameManager.GameMediator.DispatchGlobalEvent(GameEventNames.EVENT_PACKAGE_UNREGISTERED, packageName);
      return success;
    }

    /// <summary>
    /// Whether a package is loading.
    /// </summary>
    /// <param name="packageName">Package identifier.</param>
    /// <returns></returns>
    public bool IsPackageLoading(string packageName)
    {
      return packagesLoadStatus.ContainsKey(packageName) && packagesLoadStatus[packageName] == 2;
    }
    /// <summary>
    /// Whether a package is registering.
    /// </summary>
    /// <param name="packageName">Package identifier.</param>
    /// <returns></returns>
    public bool IsPackageRegistering(string packageName)
    {
      return packagesLoadStatus.ContainsKey(packageName) && packagesLoadStatus[packageName] == 1;
    }
    /// <summary>
    /// Whether a package is registered.
    /// </summary>
    /// <param name="packageName">Package identifier.</param>
    /// <returns></returns>
    public bool IsPackageRegistered(string packageName)
    {
      return registeredPackages.ContainsKey(packageName);
    }
    /// <summary>
    /// Whether a package is loaded.
    /// </summary>
    /// <param name="packageName">Package identifier.</param>
    /// <returns></returns>
    public bool IsPackageLoaded(string packageName)
    {
      return loadedPackages.ContainsKey(packageName);
    }
    /// <summary>
    /// Run package entry points.
    /// </summary>
    /// <param name="packageNameFilter">Package filter: * selects all packages; otherwise use a regular expression.</param>
    public void NotifyAllPackageRun(string packageNameFilter)
    {
      Profiler.BeginSample("NotifyAllPackageRun");

      foreach (GamePackage package in loadedPackages.Values)
      {
        if (package.Status == GamePackageStatus.LoadSuccess && !package.IsEntryCodeExecuted() &&
            (packageNameFilter == "*" || Regex.IsMatch(package.PackageName, packageNameFilter))) 
          package.RunPackageExecutionCode();
      }

      Profiler.EndSample();
    }

    /// <summary>
    /// Load a package.
    /// </summary>
    /// <param name="packageName">Package identifier.</param>
    /// <returns>Whether loading succeeded.</returns>
    public async Task<bool> LoadPackage(string packageName)
    {
      if (IsPackageLoaded(packageName))
        return true;
      if (IsPackageLoading(packageName))
      {
        GameErrorChecker.SetLastErrorAndLog(GameError.IsLoading, TAG,
            "Package {0} is loading!", packageName);
        return false;
      }

      // Register the package.
      GamePackage package = FindRegisteredPackage(packageName);
      if (package == null)
      {
        if (!await RegisterPackage(packageName))
        {
          packagesLoadStatus.Remove(packageName);

          var msg = string.Format("Package {0} could not load, because RegisterPackage failed", packageName);
          // Send the notification event.
          GameManager.GameMediator.DispatchGlobalEvent(GameEventNames.EVENT_PACKAGE_LOAD_FAILED, packageName, msg);
          Log.E(TAG, msg);
          return false;
        }

        package = FindRegisteredPackage(packageName);
      }

      packagesLoadStatus.Add(packageName, 2);
      package._Status = GamePackageStatus.Loading;

      Log.D(TAG, "Loading package {0}", packageName);

      // Load dependencies.
      List<GamePackageDependencies> dependencies = package.BaseInfo.Dependencies;
      if (dependencies.Count > 0)
      {
        GamePackageDependencies dependency;
        GamePackage dependencyPackage;

        // Load dependencies.
        for (int i = 0; i < dependencies.Count; i++)
        {
          dependency = dependencies[i];
          if (!IsPackageLoaded(dependency.Name))
          {
            bool loadSuccess = await LoadPackage(dependency.Name);
            if (!loadSuccess)
            {
              packagesLoadStatus.Remove(packageName);
              string err = string.Format("Package {0} load failed because a dependency {1}/{2} " +
                 "load failed",
                 packageName, dependency.Name, dependency.MinVersion);

              Log.E(TAG, err);
              // Send the notification event.
              GameManager.GameMediator.DispatchGlobalEvent(
                  GameEventNames.EVENT_PACKAGE_LOAD_FAILED, packageName, err);

              package._Status = GamePackageStatus.LoadFailed;
              return false;
            }
          }
        }
        // Check dependency versions.
        for (int i = 0; i < dependencies.Count; i++)
        {
          dependency = dependencies[i];

          if (IsPackageLoaded(dependency.Name))
          {
            dependencyPackage = loadedPackages[dependency.Name];
            if (dependencyPackage.PackageVersion < dependency.MinVersion)
            {
              string err = string.Format("Package {0} load failed because dependency {1} {2} " +
                  "less than required version {3}",
                  packageName, dependency.Name, dependencyPackage.PackageVersion,
                  dependency.MinVersion);

              packagesLoadStatus.Remove(packageName);
              package._Status = GamePackageStatus.LoadFailed;

              Log.E(TAG, err);
              // Send the notification event.
              GameManager.GameMediator.DispatchGlobalEvent(
                  GameEventNames.EVENT_PACKAGE_LOAD_FAILED, packageName, err);

              return false;
            }
            // Increment dependency reference counts.
            dependencyPackage.DependencyRefCount++;
          }
        }
      }

      // Load.
      if (!await package.LoadPackage())
      {
        package._Status = GamePackageStatus.LoadFailed;
        packagesLoadStatus.Remove(packageName);

        string err = string.Format("Package {0} load failed {1}", packageName, GameErrorChecker.GetLastErrorMessage());
        Log.E(TAG, err);

        // Send the notification event.
        GameManager.GameMediator.DispatchGlobalEvent(GameEventNames.EVENT_PACKAGE_LOAD_FAILED, packageName, err);
        return false;
      }

      package._Status = GamePackageStatus.LoadSuccess;
      loadedPackages.Add(packageName, package);
      packagesLoadStatus.Remove(packageName);

      Log.D(TAG, "Package {0} loaded", packageName);

      // Send the notification event.
      GameManager.GameMediator.DispatchGlobalEvent(GameEventNames.EVENT_PACKAGE_LOAD_SUCCESS, packageName, package);

      return true;
    }
    /// <summary>
    /// Unload a package.
    /// </summary>
    /// <param name="packageName">Package identifier.</param>
    /// <param name="unLoadImmediately">
    /// Whether to unload immediately. If false, this package
    /// waits until all packages depending on it have unloaded.
    /// </param>
    /// <returns>Whether the operation succeeded.</returns>
    public bool UnLoadPackage(string packageName, bool unLoadImmediately)
    {
      GamePackage package = FindPackage(packageName);
      if (package == null)
      {
        GameErrorChecker.SetLastErrorAndLog(GameError.NotLoad, TAG, "Can not unload package " + packageName + " because it isn't load! ");
        return false;
      }
      if ((package.GetFlag() & GamePackage.FLAG_PACK_SYSTEM_PACKAGE) == GamePackage.FLAG_PACK_SYSTEM_PACKAGE) {
        GameErrorChecker.SetLastErrorAndLog(GameError.AccessDenined, TAG, "Can not unload system package " + packageName + " ! ");
        return false;
      }
      if ((package.GetFlag() & GamePackage.FLAG_PACK_NOT_UNLOADABLE) == GamePackage.FLAG_PACK_NOT_UNLOADABLE) {
        GameErrorChecker.SetLastErrorAndLog(GameError.AccessDenined, TAG, "Can not unload package " + packageName + " with flag FLAG_PACK_NOT_UNLOADABLE ! ");
        return false;
      }
      return UnLoadPackageInternal(package, unLoadImmediately);
    }

    private bool UnLoadPackageInternal(GamePackage package, bool unLoadImmediately)
    {
      var packageName = package.PackageName;

      Log.D(TAG, "Unload package {0}", packageName);

      Profiler.BeginSample("UnLoadPackageInternal" + package.PackageName);

      if (package.UnLoadWhenDependencyRefNone)
        return true;

      // Defer ordinary unloading while dependency references remain.
      if (!unLoadImmediately && package.DependencyRefCount > 0)
      {
        package.UnLoadWhenDependencyRefNone = true;
        package._Status = GamePackageStatus.UnloadWaiting;
        Log.D(TAG, "Set package {0} to unload waiting", packageName);
        return true;
      }

      // Decrement dependency reference counts because this package is unloading.
      List<GamePackageDependencies> dependencies = package.BaseInfo.Dependencies;
      GamePackage dependencyPackage;
      for (int i = 0; i < dependencies.Count; i++)
      {
        if (IsPackageLoaded(dependencies[i].Name))
        {
          dependencyPackage = loadedPackages[dependencies[i].Name];
          dependencyPackage.DependencyRefCount--;
          if (dependencyPackage.DependencyRefCount <= 0
              && dependencyPackage.UnLoadWhenDependencyRefNone)
            UnLoadPackage(dependencyPackage.PackageName, true);
        }
      }

      Profiler.EndSample();

      // Unload.

      // Send the notification event.
      if(GameManager.GameMediator)
        GameManager.GameMediator.DispatchGlobalEvent(GameEventNames.EVENT_PACKAGE_UNLOAD, packageName, package);

      package._Status = GamePackageStatus.NotLoad;
      package.Destroy();
      packagesLoadStatus.Remove(packageName);
      loadedPackages.Remove(packageName);

      Log.D(TAG, "Package {0} unloaded", packageName);
      return true;
    }

    /// <summary>
    /// Find a loaded package.
    /// </summary>
    /// <param name="packageName">Package identifier.</param>
    /// <returns>Package instance, or null if not found.</returns>
    public GamePackage FindPackage(string packageName)
    {
      loadedPackages.TryGetValue(packageName, out var outPackage);
      return outPackage;
    }

    /// <summary>
    /// Check whether a package is loaded at the required version.
    /// </summary>
    /// <param name="packageName">Package identifier.</param>
    /// <param name="ver">Minimum package version.</param>
    /// <returns>True if loaded at a sufficient version; otherwise false.</returns>
    public bool CheckRequiredPackage(string packageName, int ver)
    {
      if (loadedPackages.TryGetValue(packageName, out var outPackage))
        return outPackage.PackageVersion >= ver;
      return false;
    }

    /// <summary>
    /// Unload all packages.
    /// </summary>
    private void UnLoadAllPackages()
    {
      List<string> packageNames = new List<string>(loadedPackages.Keys);
      foreach (string key in packageNames) {
        if (key != SYSTEM_PACKAGE_NAME) {
          GamePackage package = FindPackage(key);
          if (package != null)
            UnLoadPackageInternal(package, true);
        }
      }
      packageNames.Clear();
    }

    #endregion

    #region Package management window

    private void InitPackageCommands()
    {
      var srv = GameManager.Instance.GameDebugCommandServer;
      srv.RegisterCommand("pm", (keyword, fullCmd, argsCount, args) =>
      {
        var type = (string)args[0];
        switch (type)
        {
          case "load":
            {
              string packageName = "";
              if (!DebugUtils.CheckDebugParam(1, args, out packageName)) break;
              Task<bool> task = LoadPackage(args[1]);
              return true;
            }
          case "unload":
            {
              string packageName = "";
              bool unLoadImmediately;
              if (!DebugUtils.CheckDebugParam(1, args, out packageName)) break;
              DebugUtils.CheckBoolDebugParam(2, args, out unLoadImmediately, false, false);

              UnLoadPackage(args[1], unLoadImmediately);
              return true;
            }
          case "unload-all":
            {
              UnLoadAllPackages();
              return true;
            }
          case "info":
            {
              string packageName = "";
              if (!DebugUtils.CheckDebugParam(1, args, out packageName)) break;

              GamePackage p = FindPackage(packageName);
              if(p == null) {
                Log.E(TAG, "Package not found: " + packageName);
                break;
              }

              Log.V(TAG, p.ToString());
              return true;
            }
          case "list-res":
            {
              string packageName = "";
              if (!DebugUtils.CheckDebugParam(1, args, out packageName)) break;

              GamePackage p = FindPackage(packageName);
              if(p == null) {
                Log.E(TAG, "Package not found: " + packageName);
                break;
              }

              Log.V(TAG, p.ListResource());
              return true;
            }
          case "enable":
            {
              string packageName = "";
              if (!DebugUtils.CheckDebugParam(1, args, out packageName)) break;

              SetPackageEnableLoad(args[1], true);
              return true;
            }
          case "disable":
            {
              string packageName = "";
              if (!DebugUtils.CheckDebugParam(1, args, out packageName)) break;

              SetPackageEnableLoad(args[1], false);
              return true;
            }
          case "reg":
            {
              string packageName = "";
              if (!DebugUtils.CheckDebugParam(1, args, out packageName)) break;

              Task<bool> task = RegisterPackage(packageName);
              return true;
            }
          case "list-loaded":
            {
              foreach (var i in loadedPackages)
                Log.V(TAG, string.Format("{0} => {1}", i.Value.PackageName, i.Value.Status));
              return true;
            }
          case "list-reged":
            {
              foreach (var i in registeredPackages)
                Log.V(TAG, string.Format("{0} enableLoad: {1}", i.Value.packageName, i.Value.enableLoad));
              return true;
            }
          case "notify-run":
            {
              string packageNameFilter = "";
              if (!DebugUtils.CheckDebugParam(1, args, out packageNameFilter)) break;

              NotifyAllPackageRun(packageNameFilter);
              return true;
            }
        }
        return false;
      }, 1, "pm <reg/load/info/unload/list-loaded/list-reged/notify-run/wnd> [packageName] Package manager commands\n" +
              "  reg <packageName:string>                                ▶ Register a package\n" +
              "  load <packageName:string>                               ▶ Load a package\n" +
              "  info <packageName:string>                               ▶ Show package information\n" +
              "  list-res <packageName:string>                           ▶ List package resources\n" +
              "  unload <packageName:string> [unLoadImmediately:boolean] ▶ Unload a package; unLoadImmediately defaults to false\n" +
              "  enable <packageName:string>                             ▶ Enable package loading at startup\n" +
              "  disable <packageName:string>                            ▶ Disable package loading at startup\n" +
              "  list-loaded                                             ▶ List loaded packages\n" +
              "  list-reged                                              ▶ List registered packages\n" +
              "  notify-run <packageNameFilter>                          ▶ Notify packages to run; use '*' for all or a regular expression to filter package names.\n" +
              "  wnd                                                     ▶ Show the package manager window");
    }

    #endregion

    #region Shared resource loading methods

    private GamePackage TryGetPackageByPath(string pathOrName, out string resName)
    {
      var lastIdx = pathOrName.IndexOf("__/");
      if (pathOrName.StartsWith("__") && lastIdx >= 0)
      {
        var packname = pathOrName.Substring(2, lastIdx - 2);
        GamePackage pack = null;
        switch(packname) {
          case "game":
          case "ballance":
          case "core":
            pack = GamePackage.GetSystemPackage();
            break;
          default:
            pack = FindPackage(packname);
            break;
        }
        if (pack == null)
          throw new System.Exception("Package " + packname + " not found");
        resName = pathOrName.Substring(lastIdx + 3);
        return pack;
      }
      else
      {
        resName = pathOrName;
        return GamePackage.GetSystemPackage();
      }
    }
    
    /// <summary>
    /// Read a text asset through the global package lookup.
    /// </summary>
    /// <param name="pathorname">Resource path.</param>
    /// <returns>The TextAsset, or null if not found.</returns>
    /// <exception cref="RequireFailedException">
    /// The requested package was not found.
    /// </exception>
    public TextAsset GetTextAsset(string pathorname)
    {
      var pack = TryGetPackageByPath(pathorname, out var path);
      return pack.GetTextAsset(path);
    }
    /// <summary>
    /// Read a prefab through the global package lookup.
    /// </summary>
    /// <param name="pathorname">Resource path.</param>
    /// <returns>The GameObject, or null if not found.</returns>
    /// <exception cref="RequireFailedException">
    /// The requested package was not found.
    /// </exception>
    public GameObject GetPrefabAsset(string pathorname)
    {
      var pack = TryGetPackageByPath(pathorname, out var path);
      return pack.GetPrefabAsset(path);
    }
    /// <summary>
    /// Read a Texture through the global package lookup.
    /// </summary>
    /// <param name="pathorname">Resource path.</param>
    /// <returns>The Texture, or null if not found.</returns>
    /// <exception cref="RequireFailedException">
    /// The requested package was not found.
    /// </exception>
    public Texture GetTextureAsset(string pathorname)
    {
      var pack = TryGetPackageByPath(pathorname, out var path);
      return pack.GetTextureAsset(path);
    }
    /// <summary>
    /// Read a Texture2D through the global package lookup.
    /// </summary>
    /// <param name="pathorname">Resource path.</param>
    /// <returns>The Texture2D, or null if not found.</returns>
    /// <exception cref="RequireFailedException">
    /// The requested package was not found.
    /// </exception>
    public Texture2D GetTexture2DAsset(string pathorname)
    {
      var pack = TryGetPackageByPath(pathorname, out var path);
      return pack.GetTexture2DAsset(path);
    }
    /// <summary>
    /// Read a Sprite through the global package lookup.
    /// </summary>
    /// <param name="pathorname">Resource path.</param>
    /// <returns>The Sprite, or null if not found.</returns>
    /// <exception cref="RequireFailedException">
    /// The requested package was not found.
    /// </exception>
    public Sprite GetSpriteAsset(string pathorname)
    {
      var pack = TryGetPackageByPath(pathorname, out var path);
      return pack.GetSpriteAsset(path);
    }
    /// <summary>
    /// Read a Material through the global package lookup.
    /// </summary>
    /// <param name="pathorname">Resource path.</param>
    /// <returns>The Material, or null if not found.</returns>
    /// <exception cref="RequireFailedException">
    /// The requested package was not found.
    /// </exception>
    public Material GetMaterialAsset(string pathorname)
    {
      var pack = TryGetPackageByPath(pathorname, out var path);
      return pack.GetMaterialAsset(path);
    }
    /// <summary>
    /// Read an AudioClip through the global package lookup.
    /// </summary>
    /// <param name="pathorname">Resource path.</param>
    /// <returns>The AudioClip, or null if not found.</returns>
    /// <exception cref="RequireFailedException">
    /// The requested package was not found.
    /// </exception>
    public AudioClip GetAudioClipAsset(string pathorname)
    {
      var pack = TryGetPackageByPath(pathorname, out var path);
      return pack.GetAudioClipAsset(path);
    }

    #endregion
  }
}
