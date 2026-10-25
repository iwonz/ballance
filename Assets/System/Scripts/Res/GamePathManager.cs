using Ballance2.Config;
using Ballance2.Config.Settings;
using System;
using Ballance2.Utils;
using Ballance2.Services.Debug;
using UnityEngine;
using System.IO;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* GameInit.cs
* 
* Purpose:
* External resource path configuration and conversion helpers.
*
* Author:
* mengyu
*/

namespace Ballance2.Res
{
  /// <summary>
  /// Path manager.
  /// </summary>
  public static class GamePathManager
  {
    private static string persistentDataPath;
    public static string PersistentDataPath
    {
      get {
        if (persistentDataPath == null) {
          persistentDataPath = ResolvePersistentDataPath(Application.persistentDataPath, Application.platform);
          Directory.CreateDirectory(persistentDataPath);
        }
        return persistentDataPath;
      }
    }

    public static string ResolvePersistentDataPath(string path, RuntimePlatform platform)
    {
      bool desktop = platform == RuntimePlatform.OSXPlayer || platform == RuntimePlatform.WindowsPlayer ||
        platform == RuntimePlatform.LinuxPlayer || platform == RuntimePlatform.OSXEditor ||
        platform == RuntimePlatform.WindowsEditor || platform == RuntimePlatform.LinuxEditor;
      if (!desktop) return path;
      // Retain the development edition's storage identity when its public title changes.
      string name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
      if (name == "Reballance") return Path.Combine(Path.GetDirectoryName(path), "Ballance Remaster");
      if (name == "unity.iwonz.Reballance") return Path.Combine(Path.GetDirectoryName(path), "unity.iwonz.Ballance Remaster");
      return path;
    }

    /// <summary>
    /// Debug package directory.
    /// </summary>
    public const string DEBUG_PACKAGE_FOLDER = "Assets/Packages";

    

    public const string DEBUG_CORE_FOLDER = "Assets/Game";
    
    /// <summary>
    /// Debug level directory.
    /// </summary>
    public const string DEBUG_LEVEL_FOLDER = "Assets/Game/Levels";

    private static string _DEBUG_PATH = "";
    private static string _DEBUG_OUTPUT_PATH = "";
 
    private static string FixMaker(string src) {
#if UNITY_EDITOR_WIN
      if(Directory.Exists(src + "/StandaloneWindows64/"))
        src += "/StandaloneWindows64/";
      else
        src += "/StandaloneWindows/";
#elif UNITY_EDITOR_LINUX
      if(Directory.Exists(src + "/StandaloneLinux64/"))
        src += "/StandaloneLinux64/";
      else
        src += "/StandaloneLinux/";
#elif UNITY_EDITOR_OSX
      src += "/StandaloneOSX/";
#endif
      return src;
    }

    /// <summary>
    /// Debug output directory; configure it in Ballance -> Development Settings -> Debug Settings.
    /// </summary>
    public static string DEBUG_PATH
    {
      get
      {
        if(_DEBUG_PATH == "") {
          DebugSettings debugSettings = DebugSettings.Instance;
          if (debugSettings != null) {
            var realtivePath = debugSettings.DebugFolder.Replace("\\", "/");

            if(PathUtils.IsAbsolutePath(realtivePath))
              _DEBUG_PATH = realtivePath;
            else
              _DEBUG_PATH = PathUtils.JoinTwoPath(Directory.GetCurrentDirectory(), realtivePath);

            _DEBUG_PATH = FixMaker(_DEBUG_PATH);
          }
        }
        return _DEBUG_PATH;
      }
    }
    /// <summary>
    /// Output directory; configure it in Ballance -> Development Settings -> Debug Settings.
    /// </summary>
    public static string DEBUG_OUTPUT_PATH
    {
      get
      {
        if(_DEBUG_OUTPUT_PATH == "") {
          DebugSettings debugSettings = DebugSettings.Instance;
          if (debugSettings != null) {
            var realtivePath = debugSettings.OutputFolder.Replace("\\", "/");
            if(PathUtils.IsAbsolutePath(realtivePath))
              _DEBUG_OUTPUT_PATH = realtivePath;
            else
              _DEBUG_OUTPUT_PATH = PathUtils.JoinTwoPath(Directory.GetCurrentDirectory(), realtivePath);

            _DEBUG_OUTPUT_PATH = FixMaker(_DEBUG_OUTPUT_PATH);
          }
        }
        return _DEBUG_OUTPUT_PATH;
      }
    }
    /// <summary>
    /// Debug package path.
    /// </summary>
    public static string DEBUG_PACKAGES_PATH { get { return DEBUG_PATH + "/Packages/"; } }
    /// <summary>
    /// Debug level path.
    /// </summary>
    public static string DEBUG_LEVELS_PATH { get { return DEBUG_PATH + "/Levels/"; } }

    /// <summary>
    /// Built-in package filenames.
    /// </summary>
    internal static readonly string[] builtInPackagesFiles = new string[] {
      "core.assets.skys.ballance",
      "core.sounds.ballance",
      "core.sounds.music.ballance",
      "mod.invlife.ballance",
      "mod.selfkill.ballance",
      "mod.speedball.ballance",
    };
    /// <summary>
    /// Built-in level filenames.
    /// </summary>
    private static string[] builtInLevelsFiles = new string[] {
      "level01",
      "level02",
      "level03",
      "level04",
      "level05",
      "level06",
      "level07",
      "level08",
      "level09",
      "level10",
      "level11",
      "level12",
      "level13",
    };

    internal static bool CheckPackageInBuiltInPackage(string packageName) {
      for (int i = 0; i < builtInPackagesFiles.Length; i++)
      { 
        if(builtInPackagesFiles[i] == packageName)
          return true;
      }
      return false;
    }
    internal static bool CheckPackageInBuiltInLevel(string packageName) {
      var n = Path.GetFileNameWithoutExtension(packageName.ToLower());
      for (int i = 0; i < builtInLevelsFiles.Length; i++)
      { 
        if(builtInLevelsFiles[i] == n)
          return true;
      }
      return false;
    }

    /// <summary>
    /// Resolve a relative resource path to its actual location.
    /// </summary>
    /// <param name="type">Resource category: core, level, or package.</param>
    /// <param name="pathorname">Relative path or name.</param>
    /// <param name="replacePlatform">Replace the [Platform] token in the path.</param>
    /// <returns>Actual resource path.</returns>
    public static string GetResRealPath(string type, string pathorname, bool replacePlatform = true, bool withFileSheme = true)
    {
      string result = null;
      string pathbuf = "";
      string[] spbuf = null;

      if (replacePlatform && pathorname.Contains("[Platform]"))
        pathorname = pathorname.Replace("[Platform]", GameConst.GamePlatformIdentifier);

      spbuf = SplitResourceIdentifier(pathorname, out pathbuf);

      if (type == "" && pathorname.Contains(":"))
        type = spbuf[0].ToLower();

      if (type == "logfile")
      {
#if UNITY_EDITOR
        result = DEBUG_PATH + "/output.log";
#else
        result = Application.temporaryCachePath + "/output.log";
#endif
      }
      else if (type == "level") return GetLevelRealPath(pathbuf, withFileSheme);
      else if (type == "package")
      {
        if (pathbuf.Contains(":"))
        {
          if (PathUtils.IsAbsolutePath(pathbuf)) 
            return pathbuf;
#if UNITY_EDITOR
          pathbuf = DEBUG_PACKAGES_PATH + pathbuf;
#elif UNITY_STANDALONE
          pathbuf= Application.dataPath + "/Packages/" + pathbuf;
#elif UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
          if(CheckPackageInBuiltInPackage(pathbuf))
            pathbuf = Application.streamingAssetsPath + "/BuiltInPackages/Packages/" + pathbuf;
          else 
            pathbuf = PersistentDataPath + "/Packages/" + pathbuf;
#else
          pathbuf = pathbuf;
#endif
          result = ReplacePathInResourceIdentifier(pathbuf, ref spbuf);
        }
        else
        {
#if UNITY_EDITOR
          result = DEBUG_PACKAGES_PATH + pathbuf;
#elif UNITY_STANDALONE
          result = Application.dataPath + "/Packages/" + pathbuf;
#elif UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
          if(CheckPackageInBuiltInPackage(pathbuf))
            result = Application.streamingAssetsPath + "/BuiltInPackages/Packages/" + pathbuf;
          else 
            result = PersistentDataPath + "/Packages/" + pathbuf;
#else
          result = pathorname;
#endif
        }
      }
      else if (type == "core")
      {
        if (pathbuf.Contains(":"))
        {
          if (PathUtils.IsAbsolutePath(pathbuf)) 
            return pathbuf;
#if UNITY_EDITOR
          pathbuf = DEBUG_PATH + "/Core/" + pathbuf;
#elif UNITY_STANDALONE
          pathbuf = Application.dataPath + "/Core/" + pathbuf;
#elif UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
          pathbuf = Application.streamingAssetsPath + "/BuiltInPackages/Core/" + pathbuf;
#else
          GameErrorChecker.LastError = GameError.NotImplemented;
          return pathorname;
#endif
          result = ReplacePathInResourceIdentifier(pathbuf, ref spbuf);
        }
        else
        {
#if UNITY_EDITOR
          result = DEBUG_PATH + "/Core/" + pathbuf;
#elif UNITY_STANDALONE
          result = Application.dataPath + "/Core/" + pathbuf;
#elif UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
          result = Application.streamingAssetsPath + "/BuiltInPackages/Core/" + pathbuf;
#else
          GameErrorChecker.LastError = GameError.NotImplemented;
          return pathorname;
#endif
        }
      }
      else
      {
        GameErrorChecker.LastError = GameError.UnKnowType;
        return pathorname;
      }

      return withFileSheme ? PathUtils.ToRequestUrl(result) : result;
    }
    /// <summary>
    /// Resolve a relative level resource path to its actual location.
    /// </summary>
    /// <param name="pathorname">Relative level path or name.</param>
    /// <returns>Actual resource path.</returns>
    public static string GetLevelRealPath(string pathorname, bool withFileSheme = true)
    {
      string result = "";
      string pathbuf = "";
      string[] spbuf = null;

      if (pathorname.Contains(":"))
      {
        spbuf = SplitResourceIdentifier(pathorname, out pathbuf);

        if (PathUtils.IsAbsolutePath(pathbuf)) 
          return pathbuf;
#if UNITY_EDITOR
        pathbuf = DEBUG_LEVELS_PATH + pathbuf;
#elif UNITY_STANDALONE
        pathbuf = Application.dataPath + "/Levels/" + pathbuf;
#elif UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
        if(CheckPackageInBuiltInLevel(pathbuf))
          pathbuf = Application.streamingAssetsPath + "/BuiltInPackages/Levels/" + pathbuf;
        else
          pathbuf = PersistentDataPath + "/Levels/" + pathbuf;
#else
        pathbuf = pathbuf;
#endif
        result = ReplacePathInResourceIdentifier(pathbuf, ref spbuf);
      }
      else
      {
#if UNITY_EDITOR
        result = DEBUG_LEVELS_PATH + pathorname;
#elif UNITY_STANDALONE
        result = Application.dataPath + "/Levels/" + pathorname;
#elif UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
        if(CheckPackageInBuiltInLevel(pathorname))
          result = Application.streamingAssetsPath + "/BuiltInPackages/Levels/" + pathorname;
        else
          result = PersistentDataPath + "/Levels/" + pathorname;
#else
        result = pathorname;
#endif
      }
      
      if(pathorname != "" && !result.EndsWith(".ballance"))
        result += ".ballance";

      return withFileSheme ? PathUtils.ToRequestUrl(result) : result;
    }
    /// <summary>
    /// Replace Path In Resource Identifier (Identifier:Path:Arg0:Arg1)
    /// </summary>
    /// <param name="oldIdentifier">Identifier Want br replace</param>
    /// <param name="newPath"></param>
    /// <param name="buf"></param>
    /// <returns></returns>
    public static string ReplacePathInResourceIdentifier(string newPath, ref string[] buf)
    {
      if (buf.Length > 1)
      {
        buf[1] = newPath;
        string s = "";
        foreach (string si in buf)
          s += ":" + si;
        return s.Remove(0, 1);
      }
      return newPath;
    }
    /// <summary>
    /// Split a resource identifier.
    /// </summary>
    /// <param name="oldIdentifier">Resource identifier.</param>
    /// <param name="outPath">Resolved resource path.</param>
    /// <returns></returns>
    public static string[] SplitResourceIdentifier(string oldIdentifier, out string outPath)
    {
      string[] buf = oldIdentifier.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

      if (buf.Length > 2)
      {
        if (buf[1].Length == 1 && (buf[2].StartsWith("/") || buf[2].StartsWith("\\")))
        {
          string[] newbuf = new string[buf.Length - 1];
          newbuf[0] = buf[0];
          newbuf[1] = buf[1] + buf[2];
          for (int i = 2; i < newbuf.Length; i++)
            newbuf[i] = buf[i + 1];
          buf = newbuf;
        }
      }
      if (buf.Length > 1) outPath = buf[1];
      else outPath = oldIdentifier;
      return buf;
    }
  }
}
