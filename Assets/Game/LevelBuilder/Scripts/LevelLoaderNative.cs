using System.Collections;
using System.IO;
using Ballance2.Config.Settings;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using Ballance2.Res;
using Ballance2.Utils;
using SimpleFileBrowser;

/*
 * Copyright (c) 2020  mengyu
 * 
 * Module:
 * GameLevelLoaderNative.cs
 * 
 * Purpose:
 * Load level AssetBundles.
 * The AssetBundle loading code was separated into C# for the historical Lua integration.
 * 
 * Author:
 * mengyu
 */

namespace Ballance2.Game.LevelBuilder
{

  public delegate void GameLevelLoaderNativeCallback(GameObject mainObj, string jsonString, LevelAssets level);
  public delegate void GameLevelLoaderNativeErrCallback(string code, string err);

  public class LevelAssets
  {
    public AssetBundle AssetBundle;
    public bool LoadInEditor = false;
    public string Path;

    public LevelAssets(string path, bool loadInEditor = false)
    {
      LoadInEditor = loadInEditor;
      Path = path;
#if UNITY_EDITOR
      LoadAllFileNames();
#endif
    }

    public virtual Texture GetTextureAsset(string name)
    {
      return GetLevelAsset<Texture>(name);
    }
    public virtual Mesh GetMeshAsset(string name)
    {
      return GetLevelAsset<Mesh>(name);
    }
    public virtual TextAsset GetTextAssetAsset(string name)
    {
      return GetLevelAsset<TextAsset>(name);
    }
    public virtual Texture2D GetTexture2DAsset(string name)
    {
      return GetLevelAsset<Texture2D>(name);
    }
    public virtual AudioClip GetAudioClipAsset(string name)
    {
      return GetLevelAsset<AudioClip>(name);
    }
    public virtual GameObject GetPrefabAsset(string name)
    {
      return GetLevelAsset<GameObject>(name);
    }
    public virtual Material GetMaterialAsset(string name)
    {
      return GetLevelAsset<Material>(name);
    }

#if UNITY_EDITOR
    private Dictionary<string, string> fileList = new Dictionary<string, string>();
    private void LoadAllFileNames()
    {
      DirectoryInfo theFolder = new DirectoryInfo(Path);
      FileInfo[] thefileInfo = theFolder.GetFiles("*.*", SearchOption.AllDirectories);
      foreach (FileInfo NextFile in thefileInfo)
      { 
        // Enumerate files.
        string path = NextFile.FullName.Replace("\\", "/");
        if(path.EndsWith(".meta")) continue;
        int index = path.IndexOf("Assets/");
        if (index > 0)
          path = path.Substring(index);

        fileList.Add(NextFile.Name, path);
      }
    }
    private string GetFullPathByName(string name)
    {
      if (fileList.TryGetValue(name, out string fullpath))
        return fullpath;
      return null;
    }
#endif
    private T GetLevelAsset<T>(string name) where T : Object
    {
#if UNITY_EDITOR
      if (LoadInEditor)
      {
        if (name.StartsWith("Assets/"))
          return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(name);
        else
        {
          var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(Path + "/" + name);
          if (asset == null && !name.Contains("/") && !name.Contains("\\"))
          {
            string fullPath = GetFullPathByName(name);
            if (fullPath != null)
              asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(fullPath);
          }
          return asset;
        }
      }
      else
#endif
      if (AssetBundle != null)
        return AssetBundle.LoadAsset<T>(name);
      return null;
    }
  }

  public class LevelLoaderNative : MonoBehaviour
  {
    private readonly string TAG = "LevelLoaderNative";

    /// <summary>
    /// Load a level.
    /// </summary>
    /// <param name="name">Name or path.</param>
    /// <param name="callback">Success callback.</param>
    /// <param name="errCallback">Failure callback.</param>
    public void LoadLevel(string name, GameLevelLoaderNativeCallback callback, GameLevelLoaderNativeErrCallback errCallback)
    {
#if UNITY_EDITOR
      string realPackagePath = GamePathManager.DEBUG_LEVEL_FOLDER + "/" + name;
      // Load inside the editor.
      if (DebugSettings.Instance.PackageLoadWay == LoadResWay.InUnityEditorProject && Directory.Exists(realPackagePath))
      {
        Log.D(TAG, "Load package in editor : {0}", realPackagePath);
        StartCoroutine(Loader(new LevelAssets(realPackagePath, true), callback, errCallback));
      }
      else
#else
      if(true) 
#endif
      {
        // Path.
        string path = name;
        if (!PathUtils.IsAbsolutePath(path))
          path = GamePathManager.GetLevelRealPath(name.ToLower(), false);

        // Remove the file:// prefix.
        path = PathUtils.FixFilePathScheme(path);
        // Handle jar paths separately.
        if(!PathUtils.IsRemotePath(path) && !File.Exists(path))
        {
          Log.E(TAG, "File not exist : {0}", path);
          errCallback("FILE_NOT_EXISTS", "File not exist: \"" + path + "\"");
          return;
        }
        path = PathUtils.ToRequestUrl(path);

        // Load NMO.
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        var ext = Path.GetExtension(path).ToLower();
        Log.D(TAG, "Load nmo file : {0} ext: {1}", path, ext);
        if (ext == ".nmo" || ext == ".cmo") {
          StartCoroutine(LevelLoaderNMO.LoaderNMO(new LevelNMOAssets(path), callback, errCallback));
          return;
        }
#endif
        Log.D(TAG, "Load package : {0}", path);
        // Load the resource bundle.
        StartCoroutine(Loader(new LevelAssets(path), callback, errCallback));
      }
    }
    /// <summary>
    /// Unload a level.
    /// </summary>
    /// <param name="level">Level instance returned by LoadLevel.</param>
    public void UnLoadLevel(LevelAssets level)
    {
      if (level != null && level.AssetBundle != null) {
        level.AssetBundle.Unload(true);
        level.AssetBundle = null;
      }
    }
    /// <summary>
    /// Open a file picker for a level file.
    /// </summary>
    /// <param name="ext">File extension, including the dot.</param>
    /// <param name="callback">Success callback.</param>
    public static void PickLevelFile(string ext, GameLevelLoaderNativeErrCallback callback) {
      FileBrowser.SetFilters(false, ext);
      FileBrowser.ShowLoadDialog((paths) => {
        callback(paths[0], "");
      }, () => {}, FileBrowser.PickMode.Files);
    }

    private IEnumerator Loader(LevelAssets level, GameLevelLoaderNativeCallback callback, GameLevelLoaderNativeErrCallback errCallback)
    {
      if (!level.LoadInEditor)
      {
        using var request = UnityWebRequest.Get(level.Path);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
          if (request.responseCode == 404)
            errCallback("FILE_NOT_FOUND", "Level file not found");
          else if (request.responseCode == 403)
            errCallback("ACCESS_DENINED", "No permission to read file");
          else
            errCallback("REQUEST_ERROR", "Http error: " + request.responseCode);
          yield break;
        }

        AssetBundleCreateRequest assetBundleCreateRequest = AssetBundle.LoadFromMemoryAsync(request.downloadHandler.data);
        yield return assetBundleCreateRequest;
        var assetBundle = assetBundleCreateRequest.assetBundle;

        if (assetBundle == null)
        {
          errCallback("FAILED_LOAD_ASSETBUNDLE", "Wrong level, failed to load AssetBundle");
          yield break;
        }

        level.AssetBundle = assetBundle;
      }
      
      Log.D(TAG, "Level package {0} loaded", level.Path);

      TextAsset LevelJsonTextAsset = level.GetTextAssetAsset("Level.json");
      if (LevelJsonTextAsset == null || string.IsNullOrEmpty(LevelJsonTextAsset.text))
      {
        errCallback("BAD_LEVEL_JSON", "Level.json is empty or invalid");
        yield break;
      }
      GameObject LevelPrefab = level.GetPrefabAsset("Level.prefab");
      if (LevelPrefab == null)
      {
        errCallback("BAD_LEVEL", "The level is invalid. Level.prefab cannot be found");
        yield break;
      }
      GameObject LevelMainObj = CloneUtils.CloneNewObject(LevelPrefab, "");
      callback(LevelMainObj, LevelJsonTextAsset.text, level);
    }
  }
}
