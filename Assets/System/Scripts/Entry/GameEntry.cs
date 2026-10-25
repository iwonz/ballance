using Ballance2.Game.Remaster;
using Ballance2.Config;
using Ballance2.Services.Debug;
using Ballance2.Services.Init;
using Ballance2.Services.InputManager;
using Ballance2.Tests;
using Ballance2.UI.CoreUI;
using SubjectNerd.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

/*
* Copyright(c) 2021  mengyu
*
* Module:
* GameEntry.cs
* 
* Purpose:
* Game entry point: start the framework and configure global settings, debugging, and command-line options.
* Also manages the user agreement dialog and its actions.
*
* Author:
* mengyu
*/

namespace Ballance2.Entry
{
  public class GameEntry : MonoBehaviour
  {
    public static GameEntry Instance { get; private set; }

    #region Global configuration properties

    [Tooltip("Enable debug mode; enabled by default in the editor.")]
    public bool DebugMode = false;
    [Tooltip("Target frame rate when DebugSetFrameRate is true.")]
    [Range(10, 120)]
    public int DebugTargetFrameRate = 60;
    [Tooltip("Set a fixed frame rate (editor only).")]
    public bool DebugSetFrameRate = true;
    [Tooltip("Debug mode.")]
    public GameDebugType DebugType = GameDebugType.NoDebug;
    [Tooltip("Package name to initialize for this debug session.")]
    [Reorderable("DebugInitPackages", true, "PackageName")]
    public List<GameDebugPackageInfo> DebugInitPackages = null;
    [Tooltip("Global event dispatched on entering custom debug mode; use it as the entry point for the custom test.")]
    public string DebugCustomEntryEvent = "DebugEntry";
    [Tooltip("Arguments for the custom debug entry event.")]
    [Reorderable("DebugCustomEntryEventParamas")]
    public List<GameDebugCustomEventParamas> DebugCustomEntryEventParamas = null;
    [SerializeField]
    [Tooltip("Preset custom debug entry event name.")]
    [Reorderable("DebugCustomEntries", true, "", true, true)]
    public List<string> DebugCustomEntries = new List<string>();
    [Tooltip("Load user packages in system or custom debug mode.")]
    public bool DebugLoadCustomPackages = true;
    [Tooltip("Skip the intro in system or custom debug mode.")]
    public bool DebugSkipIntro = false;

    #endregion

    #region Static references

    public Camera GameBaseCamera = null;
    public RectTransform GameCanvas = null;

    public GameGlobalErrorUI GameGlobalErrorUI = null;
    public Text GameDebugBeginStats;

    public GlobalGameScriptErrDialog GlobalGameScriptErrDialog = null;
    public GameObject GameGlobalIngameLoading = null;


    #endregion

    void Start()
    {
      Instance = this;

      InitCommandLine();
      InitBaseSettings();

#if UNITY_EDITOR
      if (DebugMode && DebugSetFrameRate) Application.targetFrameRate = DebugTargetFrameRate;
#elif UNITY_ANDROID || UNITY_IOS
      Application.targetFrameRate = 60;
#endif

      StartCoroutine(InitMain());
    }
    private void OnDestroy()
    {
      GameSystem.Destroy();
    }
    public static void Destroy()
    {
      UnityEngine.Object.Destroy(Instance.gameObject);
    }

    /// <summary>
    /// Quit the game.
    /// </summary>
    public void QuitGame()
    {
      StopAllCoroutines();
      GameSystem.QuitPlayer();
    }

    private void InitCommandLine()
    {
      string[] CommandLineArgs = Environment.GetCommandLineArgs();
      int len = CommandLineArgs.Length;
      if (len > 1)
      {
        for (int i = 0; i < len; i++)
        {
          if (CommandLineArgs[i] == "-debug")
            RemasterPreferences.SetInt("core.DebugMode", 1);
        }
      }
    }
    private void InitBaseSettings()
    {
#if UNITY_EDITOR
      DebugMode = true;
#else
      if(!DebugMode) {
        if(RemasterPreferences.GetString("core.DebugMode", "") == "True") DebugMode = true;
        else DebugMode = false;
      }
#endif
    }
    private void InitUi() {
      GameGlobalIngameLoading.SetActive(true);
    }

    private IEnumerator InitMain()
    {
      InitUi();

      GameSystem.RegSysHandler(GameSystemInit.GetSysHandler());
      GameSystem.PreInit();

      GameErrorChecker.SetGameErrorUI(GameGlobalErrorUI);
      GameSystemInit.FillStartParameters(this);

      if (DebugMode && DebugType == GameDebugType.SystemDebug)
        GameSystem.RegSysDebugProvider(GameSystemDebugTests.RequestDebug);
      else if (!DebugMode || DebugType != GameDebugType.SystemDebug)
        GameSystem.Init();
      else
        GameErrorChecker.ThrowGameError(GameError.ConfigueNotRight, "DebugMode not right.");

      yield break;
    }
  }
  /// <summary>
  /// Debug type.
  /// </summary>
  public enum GameDebugType
  {
    /// <summary>
    /// Normal execution.
    /// </summary>
    NoDebug,
    /// <summary>
    /// Full debugging, including system and custom tests with the complete game environment.
    /// </summary>
    FullDebug,
    /// <summary>
    /// Custom debugging without system tests, using a partial game environment.
    /// </summary>
    CustomDebug,
    /// <summary>
    /// System debugging without loading the gameplay environment.
    /// </summary>
    SystemDebug
  }
  [Serializable]
  public class GameDebugPackageInfo
  {
    public bool Enable;
    public string PackageName;
    public override string ToString() { return PackageName; }
  }
  [Serializable]
  public class GameDebugCustomEventParamas
  {
    public UnityEngine.Object Object;
    public override string ToString() { return Object != null ? Object.name : "(null)"; }
  }
}
