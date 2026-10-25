#if UNITY_EDITOR
using System;
using UnityEditor;
#endif
using UnityEngine;

/*
* Copyright (c) 2020  mengyu
* 
* Module:
* DebugSettings.cs
* 
* Purpose:
* Access debug settings.
* Configure development settings through Ballance -> Development Settings -> Debug Settings in Unity.
* 
* Author:
* mengyu
*/

namespace Ballance2.Config.Settings
{
  /// <summary>
  /// Debug settings.
  /// </summary>
  public class DebugSettings : ScriptableObject
  {
    [Tooltip("Debug output folder for Ballance levels and packages.")]
    public string DebugFolder = "";
    [Tooltip("Ballance build output folder.")]
    public string OutputFolder = "";
    [Tooltip("Ballance macOS build output folder.")]
    public string OutputAppMac = "";
    [Tooltip("Ballance Linux build output folder.")]
    public string OutputAppLinux = "";
    [Tooltip("Enable system debug testing.")]
    public bool EnableSystemDebugTests = true;
    [Tooltip("System initialization file loading mode.")]
    public LoadResWay SystemInitLoadWay = LoadResWay.InDebugFolder;
    [Tooltip("Package file loading mode.")]
    public LoadResWay PackageLoadWay = LoadResWay.InDebugFolder;

    private static DebugSettings _instance = null;

    /// <summary>
    /// Get the debug settings instance.
    /// </summary>
    public static DebugSettings Instance
    {
      get
      {
        if (_instance == null)
        {
          _instance = Resources.Load<DebugSettings>("DebugSettings");
#if UNITY_EDITOR
          if (_instance == null)
          {
            _instance = CreateInstance<DebugSettings>();
            try
            {
              AssetDatabase.CreateAsset(_instance, "Assets/Resources/DebugSettings.asset");
            }
            catch (Exception e)
            {
              Debug.LogError("CreateInstance DebugSetting.asset failed!" + e.Message + "\n\n" + e.ToString());
            }
          }
#endif
        }
        return _instance;
      }
    }

#if UNITY_EDITOR
    [MenuItem("Ballance/Settings/Development Settings", priority = 298)]
    public static void Open()
    {
      Selection.activeObject = Instance;
    }
#endif

  }

  public enum LoadResWay
  {
    InDebugFolder,
    InUnityEditorProject,
  }
}