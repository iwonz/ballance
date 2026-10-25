using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Ballance2.Base;
using Ballance2.Config;
using Ballance2.Config.Settings;
using Ballance2.DebugTools;
using Ballance2.Entry;
using Ballance2.Game;
using Ballance2.Package;
using Ballance2.Res;
using Ballance2.Services.Debug;
using Ballance2.Utils;
using UnityEngine;
using UnityEngine.Profiling;

/*
* Copyright(c) 2021 imengyu
*
* Module:
* GameManager.cs
* 
* Purpose:
* Main game manager providing shared components and runtime coordination.
* Provides global events, startup/shutdown, logical scenes, and
* convenience helpers. The former Lua runtime is not part of the current implementation.
*
* Author:
* mengyu
*/

namespace Ballance2.Services
{
    /// <summary>
    /// Game manager.
    /// </summary>
    public class GameManager : GameService<GameManager>
    {
        public GameManager() : base("GameManager") { }

        #region Shared components

        /// <summary>
        /// Get the GameMediator instance.
        /// </summary>
        public static GameMediator GameMediator { get; internal set; }
        /// <summary>
        /// Get system settings.
        /// </summary>
        public GameSettingsActuator GameSettings { get; private set; }
        /// <summary>
        /// Get the base camera.
        /// </summary>
        public Camera GameBaseCamera { get; private set; }
        /// <summary>
        /// Get the root Canvas.
        /// </summary>
        public RectTransform GameCanvas { get; internal set; }
        /// <summary>
        /// Get the debug command controller.
        /// </summary>
        /// <value></value>
        public GameDebugCommandServer GameDebugCommandServer { get; private set; }
        /// <summary>
        /// Get the global light.
        /// </summary>
        public static Light GameLight { get; private set; }
        /// <summary>
        /// GameTimeMachine instance.
        /// </summary>
        /// <value></value>
        public static GameTimeMachine GameTimeMachine
        {
            get
            {
                if (_GameTimeMachine == null)
                    _GameTimeMachine = GameSystem.GetSystemService<GameTimeMachine>();
                return _GameTimeMachine;
            }
        }

        private static GameTimeMachine _GameTimeMachine = null;
        private static bool _DebugMode = false;

        /// <summary>
        /// Get or set developer mode.
        /// </summary>
        /// <remark>
        /// Restart is required after changing debug mode.
        /// </remark>
        /// <value></value>
        public static bool DebugMode
        {
            get
            {
                return _DebugMode;
            }
            set
            {
                if (_DebugMode != value)
                {
                    _DebugMode = value;
                    if (Instance != null && Instance.GameSettings != null)
                        Instance.GameSettings.SetBool("DebugMode", value);
                }
            }
        }

        private readonly string TAG = "GameManager";

        #endregion

        #region Initialization

        public override bool Initialize()
        {
            Log.D(TAG, "Initialize");

            GameEntry gameEntryInstance = GameEntry.Instance;
            GameSettings = GameSettingsManager.GetSettings(GamePackageManager.SYSTEM_PACKAGE_NAME);
            GameBaseCamera = gameEntryInstance.GameBaseCamera;
            GameCanvas = gameEntryInstance.GameCanvas;
            DebugMode = gameEntryInstance.DebugMode;
            GameDebugCommandServer = new GameDebugCommandServer();
            GameLight = GameObject.Find("GameLight").GetComponent<Light>();

            try
            {
                InitBase(gameEntryInstance, () => StartCoroutine(InitAsysc()));
            }
            catch (Exception e)
            {
                GameErrorChecker.ThrowGameError(GameError.ExecutionFailed, "An exception occurred during system initialization. Please report this bug.\n" + e.ToString());
            }

            return true;
        }

        /// <summary>
        /// Initialize base settings and event integration.
        /// </summary>
        /// <param name="gameEntryInstance">Entry configuration instance.</param>
        /// <param name="finish">Completion callback.</param>
        private void InitBase(GameEntry gameEntryInstance, System.Action finish)
        {
            Instance = this;

            Application.wantsToQuit += Application_wantsToQuit;

            InitDebugConfig(gameEntryInstance);
            InitCommands();
            InitVideoSettings();

            GameMediator.RegisterGlobalEvent(GameEventNames.EVENT_GAME_MANAGER_INIT_FINISHED);
            GameMediator.SubscribeSingleEvent(GamePackage.GetSystemPackage(), "GameManagerWaitPackageManagerReady", TAG, (evtName, param) =>
            {
                finish.Invoke();
                return false;
            });
            GameMediator.RegisterEventHandler(GamePackage.GetSystemPackage(), GameEventNames.EVENT_UI_MANAGER_INIT_FINISHED, TAG, (evtName, param) =>
            {
                //Init Debug
                if (DebugMode)
                    DebugManager.Init();
                return false;
            });
        }

        private bool sLoadUserPackages = true;
        private bool sEnablePackageLoadFilter = false;
        private string sCustomDebugName = "";
        private List<string> sLoadCustomPackages = new List<string>();

        private XmlDocument systemInit = new XmlDocument();

        /// <summary>
        /// Load debug configuration.
        /// </summary>
        /// <param name="gameEntryInstance">Entry configuration instance.</param>
        private void InitDebugConfig(GameEntry gameEntryInstance)
        {
#if UNITY_EDITOR
            // Copy GameEntry debug configuration into fields used during loading.
            sLoadUserPackages = !DebugMode || gameEntryInstance.DebugLoadCustomPackages;
            sCustomDebugName = DebugMode && gameEntryInstance.DebugType == GameDebugType.CustomDebug ? gameEntryInstance.DebugCustomEntryEvent : "";
            sEnablePackageLoadFilter = DebugMode && gameEntryInstance.DebugType != GameDebugType.FullDebug && gameEntryInstance.DebugType != GameDebugType.NoDebug;
            if (DebugMode && gameEntryInstance.DebugType != GameDebugType.NoDebug)
            {
                foreach (var package in gameEntryInstance.DebugInitPackages)
                {
                    if (package.Enable) sLoadCustomPackages.Add(package.PackageName);
                }
            }
#endif
        }

        /// <summary>
        /// Asynchronous main initialization flow.
        /// </summary>
        /// <returns></returns>
        private IEnumerator InitAsysc()
        {
            GameMediator.DispatchGlobalEvent(GameEventNames.EVENT_BASE_INIT_FINISHED);

            // Load system packages.
            yield return StartCoroutine(LoadSystemCore());

            yield return StartCoroutine(LoadSystemPackages());

            // Load user-selected packages.
            if (sLoadUserPackages)
                yield return StartCoroutine(LoadUserPackages());

            // Initialize all packages after loading finishes.
            GetSystemService<GamePackageManager>().NotifyAllPackageRun("*");

            // Notify initialization completion.
            GameMediator.DispatchGlobalEvent(GameEventNames.EVENT_GAME_MANAGER_INIT_FINISHED);
            GameMediator.NotifySingleEvent("DoSendAllPackageUILoad");

            // Enter the custom debug scene when CustomDebugName is set; otherwise enter the default scene.
            if (string.IsNullOrEmpty(sCustomDebugName))
            {
#if UNITY_EDITOR
                yield return new WaitForSeconds(0.5f);

                // Enter the logical scene.
                if (DebugMode && GameEntry.Instance.DebugSkipIntro)
                {
                    // Enter the logical scene.
                    if (!RequestEnterLogicScense("MenuLevel"))
                        GameErrorChecker.ShowSystemErrorMessage("Enter firstScense failed");
                    // Hide the initial loading animation.
                    HideGlobalStartLoading();
                }
                else
#endif
                if (GameSettings.GetBool("debugDisableIntro", false))
                {
                    // Enter the logical scene.
                    if (!RequestEnterLogicScense("MenuLevel"))
                        GameErrorChecker.ShowSystemErrorMessage("Enter firstScense failed");
                    // Hide the initial loading animation.
                    HideGlobalStartLoading();
                }
                else if (firstScense != "")
                {
                    // Enter the logical scene.
                    if (!RequestEnterLogicScense(firstScense))
                        GameErrorChecker.ShowSystemErrorMessage("Enter firstScense failed");
                    // Hide the initial loading animation.
                    HideGlobalStartLoading();
                }
            }
            else
            {
                // Enter the logical scene.
                Log.D(TAG, "Enter GameDebug.");
                if (!RequestEnterLogicScense("GameDebug"))
                    GameErrorChecker.ShowSystemErrorMessage("Enter GameDebug failed");
                GameMediator.DispatchGlobalEvent(sCustomDebugName, GameEntry.Instance.DebugCustomEntryEventParamas);
                // Hide the initial loading animation.
                HideGlobalStartLoading();
            }
        }
        /// <summary>
        /// Load user-defined mods.
        /// </summary>
        /// <returns></returns>
        private IEnumerator LoadUserPackages()
        {
            var pm = GetSystemService<GamePackageManager>();
            var list = pm.LoadPackageRegisterInfo();
            pm.ScanUserPackages(list);

            foreach (var info in list)
            {
                if (sEnablePackageLoadFilter && !sLoadCustomPackages.Contains(info.Value.packageName)) continue;

                var task = pm.RegisterPackage((info.Value.enableLoad ? "Enable:" : "") + info.Value.packageName);
                yield return new WaitUntil(() => task.IsCompleted);

                if (task.GetAwaiter().GetResult() && info.Value.enableLoad)
                {
                    var task2 = pm.LoadPackage(info.Value.packageName);
                    yield return new WaitUntil(() => task2.IsCompleted);
                    task2.GetAwaiter().GetResult();
                }
            }
        }
        /// <summary>
        /// Load configured system packages.
        /// </summary>
        /// <returns></returns>
        private IEnumerator LoadSystemCore()
        {
            yield return 1;

            var pm = GetSystemService<GamePackageManager>();
            var systemPackage = GamePackage.GetSystemPackage();

            yield return 1;

            #region Read SystemInit

            systemInit = new XmlDocument();
            TextAsset systemInitAsset = Resources.Load<TextAsset>("SystemInit");
            if (systemInitAsset == null)
            {
                GameErrorChecker.ThrowGameError(GameError.FileNotFound, "SystemInit.xml missing! ");
                StopAllCoroutines();
                yield break;
            }
            yield return 1;
            systemInit.LoadXml(systemInitAsset.text);

            #endregion

            yield return new WaitForSeconds(0.1f);

            #region System configuration

            XmlNode LogToFileEnabled = systemInit.SelectSingleNode("System/SystemOptions/LogToFileEnabled");
            if (LogToFileEnabled != null && LogToFileEnabled.InnerText.ToLower() == "true")
                Log.StartLogFile();

            #endregion

            #region Load system core package

#if UNITY_EDITOR
            Task<bool> task = systemPackage.LoadInfo(GamePathManager.DEBUG_CORE_FOLDER);
#else
      Task<bool> task = systemPackage.LoadInfo(GamePathManager.GetResRealPath("core", "core.ballance"));
#endif

            yield return new WaitUntil(() => task.IsCompleted);
            if (!task.GetAwaiter().GetResult())
            {
                GameErrorChecker.ThrowGameError(GameError.SystemPackageLoadFailed, "Failed to load system package info");
                StopAllCoroutines();
                yield break;
            }

            task = systemPackage.LoadPackage();
            yield return new WaitUntil(() => task.IsCompleted);

            if (!task.GetAwaiter().GetResult())
            {
                GameErrorChecker.ThrowGameError(GameError.SystemPackageLoadFailed, "Failed to load system package");
                StopAllCoroutines();
                yield break;
            }

            if (!systemPackage.RunPackageExecutionCode())
            {
                GameErrorChecker.ThrowGameError(GameError.SystemPackageLoadFailed, "Failed to run system package");
                StopAllCoroutines();
                yield break;
            }

            #endregion

            #region Logical scene configuration

            XmlNode SystemScenses = systemInit.SelectSingleNode("System/SystemScenses");
            if (SystemScenses != null)
            {
                foreach (XmlNode node in SystemScenses.ChildNodes)
                {
                    if (node.NodeType == XmlNodeType.Element && node.Name == "Scense" && !StringUtils.isNullOrEmpty(node.InnerText))
                        logicScenses.Add(node.InnerText);
                }
            }
            XmlNode FirstEnterScense = systemInit.SelectSingleNode("System/SystemOptions/FirstEnterScense");
            if (FirstEnterScense != null && !StringUtils.isNullOrEmpty(FirstEnterScense.InnerText))
                firstScense = FirstEnterScense.InnerText;
            if (!logicScenses.Contains(firstScense))
            {
                GameErrorChecker.ThrowGameError(GameError.ConfigueNotRight, "FirstEnterScense Configue not right");
                StopAllCoroutines();
                yield break;
            }

            #endregion
        }
        /// <summary>
        /// Load the system core package.
        /// </summary>
        /// <returns></returns>
        private IEnumerator LoadSystemPackages()
        {
            var pm = GetSystemService<GamePackageManager>();

            XmlNode nodeSystemPackages = systemInit.SelectSingleNode("System/SystemPackages");

            int loadedPackageCount = 0, failedPackageCount = 0;
            for (int loadStepNow = 0; loadStepNow < 2; loadStepNow++)
            {
                for (int i = 0; i < nodeSystemPackages.ChildNodes.Count; i++)
                {
                    yield return new WaitForSeconds(0.02f);

                    XmlNode nodePackage = nodeSystemPackages.ChildNodes[i];

                    if (nodePackage.Name == "Package" && nodePackage.Attributes["name"] != null)
                    {
                        string packageName = nodePackage.Attributes["name"].InnerText;
                        int minVer = 0;
                        int loadStep = 0;
                        bool mustLoad = false;
                        foreach (XmlAttribute attribute in nodePackage.Attributes)
                        {
                            if (attribute.Name == "minVer")
                                minVer = ConverUtils.StringToInt(attribute.Value, 0, "Package/minVer");
                            else if (attribute.Name == "mustLoad")
                                mustLoad = ConverUtils.StringToBoolean(attribute.Value, false, "Package/mustLoad");
                            else if (attribute.Name == "loadStep")
                                loadStep = ConverUtils.StringToInt(attribute.Value, 0, "Package/loadStep");
                        }
                        if (string.IsNullOrEmpty(packageName))
                        {
                            Log.W(TAG, "The Package node {0} name is empty!", i);
                            continue;
                        }
                        if (loadStepNow != loadStep) continue;
                        if (packageName == "core.debug" && !DebugMode) continue;
                        if (sEnablePackageLoadFilter && !sLoadCustomPackages.Contains(packageName)) continue;

                        // Load the package.
                        Task<bool> task = pm.LoadPackage(packageName);
                        yield return new WaitUntil(() => task.IsCompleted);

                        if (task.GetAwaiter().GetResult())
                        {
                            GamePackage package = pm.FindPackage(packageName);
                            if (package == null)
                            {
                                StopAllCoroutines();
                                GameErrorChecker.ThrowGameError(GameError.UnKnow, packageName + " not found!\nTry restarting the game.");
                                yield break;
                            }
                            if (package.PackageVersion < minVer)
                            {
                                StopAllCoroutines();
                                GameErrorChecker.ThrowGameError(GameError.SystemPackageNotLoad,
                                    string.Format("Package {0} version {1} is below the required version {2}. Try reinstalling the game.",
                                    packageName, package.PackageVersion, minVer));
                                yield break;
                            }
                            package.flag |= GamePackage.FLAG_PACK_SYSTEM_PACKAGE | GamePackage.FLAG_PACK_NOT_UNLOADABLE;
                            loadedPackageCount++;
                        }
                        else
                        {
                            failedPackageCount++;
                            if (mustLoad)
                            {
                                StopAllCoroutines();
                                GameErrorChecker.ThrowGameError(GameError.SystemPackageNotLoad,
                                    "System-defined package: " + packageName + " failed to load.\nError: " +
                                    GameErrorChecker.GetLastErrorMessage() + " (" + GameErrorChecker.LastError + ")\nTry restarting the game.");
                                yield break;
                            }
                        }
                    }
                }
                Log.D(TAG, "Load " + loadedPackageCount + " packages, " + failedPackageCount + " failed.");

                // Load base packages on the first pass and wait for them to run.
                if (loadStepNow == 0)
                {
                    pm.NotifyAllPackageRun("*");

                    if (
#if UNITY_EDITOR
                    string.IsNullOrEmpty(sCustomDebugName) && (!DebugMode || !GameEntry.Instance.DebugSkipIntro))
                    // Check DebugSkipIntro only in the editor.
#else
          !GameSettings.GetBool("debugDisableIntro", false))
#endif
                    {
                        // Enter Intro after the base packages load.
                        RequestEnterLogicScense(firstScense);
                        firstScense = "";
                        HideGlobalStartLoading();
                    }
                }
            }

            yield return new WaitForSeconds(0.1f);

            // Initialize all packages after loading finishes.
            pm.NotifyAllPackageRun("*");
        }

        #region System debug commands

        private void InitCommands()
        {
            var srv = GameManager.Instance.GameDebugCommandServer;
            srv.RegisterCommand("quit", (keyword, fullCmd, argsCount, args) =>
            {
                QuitGame();
                return false;
            }, 0, "quit > Quit the game");
            srv.RegisterCommand("restart-game", (keyword, fullCmd, argsCount, args) =>
            {
                RestartGame();
                return false;
            }, 0, "restart-game > Restart the game");
            srv.RegisterCommand("s", (keyword, fullCmd, argsCount, args) =>
            {
                var type = (string)args[0];
                switch (type)
                {
                    case "set":
                        {
                            if (args.Length == 4)
                            {
                                string setName = "";
                                string setType = "";
                                string setVal = "";
                                if (!DebugUtils.CheckDebugParam(1, args, out setName)) break;
                                if (!DebugUtils.CheckDebugParam(2, args, out setType)) break;
                                if (!DebugUtils.CheckDebugParam(3, args, out setVal)) break;

                                switch (setType)
                                {
                                    case "bool":
                                        GameSettings.SetBool(setName, bool.Parse(setVal));
                                        return true;
                                    case "string":
                                        GameSettings.SetString(setName, setVal);
                                        return true;
                                    case "float":
                                        GameSettings.SetFloat(setName, float.Parse(setVal));
                                        return true;
                                    case "int":
                                        GameSettings.SetInt(setName, int.Parse(setVal));
                                        return true;
                                    default:
                                        Log.E(TAG, "Unknown setting type: {0}", setType);
                                        break;
                                }
                            }
                            else if (args.Length >= 5)
                            {
                                string setName = "";
                                string setPackage = "";
                                string setType = "";
                                string setVal = "";
                                if (!DebugUtils.CheckDebugParam(1, args, out setPackage)) break;
                                if (!DebugUtils.CheckDebugParam(2, args, out setName)) break;
                                if (!DebugUtils.CheckDebugParam(3, args, out setType)) break;
                                if (!DebugUtils.CheckDebugParam(4, args, out setVal)) break;

                                var settings = GameSettingsManager.GetSettings(setPackage);
                                if (settings == null)
                                {
                                    Log.E(TAG, "Settings package {0} was not found.", setPackage);
                                    break;
                                }

                                switch (setType)
                                {
                                    case "bool":
                                        settings.SetBool(setName, bool.Parse(setVal));
                                        return true;
                                    case "string":
                                        settings.GetString(setName, setVal);
                                        return true;
                                    case "float":
                                        settings.GetFloat(setName, float.Parse(setVal));
                                        return true;
                                    case "int":
                                        settings.GetInt(setName, int.Parse(setVal));
                                        return true;
                                    default:
                                        Log.E(TAG, "Unknown setting type: {0}", setType);
                                        break;
                                }
                            }
                            break;
                        }
                    case "get":
                        {
                            if (args.Length == 3)
                            {
                                string setName = "";
                                string setType = "";
                                if (!DebugUtils.CheckDebugParam(1, args, out setName)) break;
                                if (!DebugUtils.CheckDebugParam(2, args, out setType)) break;

                                switch (setType)
                                {
                                    case "bool":
                                        Log.V(TAG, GameSettings.GetBool(setName, false).ToString());
                                        return true;
                                    case "string":
                                        Log.V(TAG, GameSettings.GetString(setName, ""));
                                        return true;
                                    case "float":
                                        Log.V(TAG, GameSettings.GetFloat(setName, 0).ToString());
                                        return true;
                                    case "int":
                                        Log.V(TAG, GameSettings.GetInt(setName, 0).ToString());
                                        return true;
                                    default:
                                        Log.E(TAG, "Unknown setting type: {0}", setType);
                                        break;
                                }
                            }
                            else if (args.Length >= 4)
                            {
                                string setName = "";
                                string setPackage = "";
                                string setType = "";
                                if (!DebugUtils.CheckDebugParam(1, args, out setPackage)) break;
                                if (!DebugUtils.CheckDebugParam(2, args, out setName)) break;
                                if (!DebugUtils.CheckDebugParam(3, args, out setType)) break;

                                var settings = GameSettingsManager.GetSettings(setPackage);
                                if (settings == null)
                                {
                                    Log.E(TAG, "Settings package {0} was not found.", setPackage);
                                    break;
                                }

                                switch (setType)
                                {
                                    case "bool":
                                        Log.V(TAG, settings.GetBool(setName, false).ToString());
                                        return true;
                                    case "string":
                                        Log.V(TAG, settings.GetString(setName, ""));
                                        return true;
                                    case "float":
                                        Log.V(TAG, settings.GetFloat(setName, 0).ToString());
                                        return true;
                                    case "int":
                                        Log.V(TAG, settings.GetInt(setName, 0).ToString());
                                        return true;
                                    default:
                                        Log.E(TAG, "Unknown setting type: {0}", setType);
                                        break;
                                }
                            }
                            break;
                        }
                    case "reset":
                        {
                            GameSettingsManager.ResetDefaultSettings();
                            return true;
                        }
                    case "list":
                        {
                            GameSettingsManager.ListActuators();
                            return true;
                        }
                    case "notify":
                        {
                            string setPackage = "";
                            string setGroup = "";
                            if (!DebugUtils.CheckDebugParam(1, args, out setPackage)) break;
                            if (!DebugUtils.CheckDebugParam(2, args, out setGroup)) break;

                            var settings = GameSettingsManager.GetSettings(setPackage);
                            if (settings == null)
                            {
                                Log.E(TAG, "Settings package {0} was not found.", setPackage);
                                break;
                            }

                            settings.NotifySettingsUpdate(setGroup);
                            return true;
                        }
                }
                return false;
            }, 0, "s <set/get/reset/list/notify> Settings commands\n" +
                    "  set <packageName:string> <setKey:string> <bool/string/float/int> <newValue> ▶ Set a package actuator setting\n" +
                    "  set <setKey:string> <bool/string/float/int> <newValue>                      ▶ Set a system actuator setting\n" +
                    "  get <packageName:string> <setKey:string> <bool/string/float/int>            ▶ Read a package actuator setting\n" +
                    "  get <setKey:string> <bool/string/float/int>                                 ▶ Read a system actuator setting\n" +
                    "  reset <packageName:string>                                                  ▶ Reset settings to defaults\n" +
                    "  notify <packageName:string> <group:string>                                  ▶ Notify a settings group of an update\n" +
                    "  list                                                                        ▶ List package settings actuators"
            );
            srv.RegisterCommand("r", (keyword, fullCmd, argsCount, args) =>
            {
                var type = (string)args[0];
                switch (type)
                {
                    case "fps":
                        {
                            int fpsVal;
                            if (DebugUtils.CheckIntDebugParam(1, args, out fpsVal, false, 0))
                            {
                                if (fpsVal <= 0 && fpsVal > 120) { Log.E(TAG, "Invalid argument: {0}", args[0]); break; }
                                Application.targetFrameRate = fpsVal;
                            }
                            Log.V(TAG, "TargetFrameRate is {0}", Application.targetFrameRate);
                            return true;
                        }
                    case "resolution":
                        {
                            if (args.Length >= 3)
                            {
                                int width, height, mode;
                                if (!DebugUtils.CheckIntDebugParam(1, args, out width)) break;
                                if (!DebugUtils.CheckIntDebugParam(2, args, out height)) break;
                                DebugUtils.CheckIntDebugParam(1, args, out mode, false, (int)Screen.fullScreenMode);

                                Screen.SetResolution(width, height, (FullScreenMode)mode);
                            }
                            Log.V(TAG, "Screen resolution is {0}x{1} {2}", Screen.width, Screen.height, Screen.fullScreenMode);
                            return true;
                        }
                    case "full":
                        {
                            int mode;
                            if (!DebugUtils.CheckIntDebugParam(1, args, out mode)) break;
                            Screen.SetResolution(Screen.width, Screen.height, (FullScreenMode)mode);
                            return true;
                        }
                    case "vsync":
                        {
                            int mode;
                            if (!DebugUtils.CheckIntDebugParam(1, args, out mode)) break;
                            if (mode < 0 && mode > 2) { Log.E(TAG, "Invalid argument: {0}", args[0]); break; }
                            QualitySettings.vSyncCount = mode;
                            return true;
                        }
                    case "device":
                        {
                            Log.V(TAG, SystemInfo.deviceName + " (" + SystemInfo.deviceModel + ")");
                            Log.V(TAG, SystemInfo.operatingSystem + " (" + SystemInfo.processorCount + "/" + SystemInfo.processorType + "/" + SystemInfo.processorFrequency + ")");
                            Log.V(TAG, SystemInfo.graphicsDeviceName + " " + SystemInfo.graphicsDeviceVendor + " /" +
                                FileUtils.GetBetterFileSize(SystemInfo.graphicsMemorySize) + " (" + SystemInfo.graphicsDeviceID + ")");
                            break;
                        }
                }
                return false;
            }, 0, "r <fps/resolution/full/device>\n" +
                    "  fps [fps:number(1-120)]                                             ▶ Get or set the target frame rate\n" +
                    "  resolution <width:number> <height:number> [fullScreenMode:number(0-3)] ▶ Set resolution or fullscreen mode\n" +
                    "  vsync <count:number(0-2)>                                           ▶ Set vertical sync: 0 off, 1 every refresh, 2 every second refresh\n" +
                    "  full <fullScreenMode:number(0-3)>                                   ▶ Set fullscreen mode: 0 ExclusiveFullScreen, 1 FullScreenWindow, 2 MaximizedWindow, 3 Windowed\n" +
                    "  device > Show current device information");
            srv.RegisterCommand("lc", (keyword, fullCmd, argsCount, args) =>
            {
                var type = (string)args[0];
                switch (type)
                {
                    case "show":
                        {
                            Log.V(TAG, "CurrentScense is {0} ({1})", logicScenses[currentScense], currentScense);
                            return true;
                        }
                    case "show-all":
                        {
                            for (int i = 0; i < logicScenses.Count; i++)
                                Log.V(TAG, "LogicScense[{0}] = {1}", i, logicScenses[i]);
                            return true;
                        }
                    case "go":
                        {
                            if (args.Length >= 1)
                            {
                                if (!DebugUtils.CheckStringDebugParam(1, args)) break;

                                string name = args[0];
                                if (RequestEnterLogicScense(name))
                                    Log.V(TAG, "EnterLogicScense " + name);
                                else
                                    Log.E(TAG, "EnterLogicScense " + name + " failed. LastError: " + GameErrorChecker.LastError.ToString());
                            }
                            return true;
                        }
                }
                return false;
            }, 0, "lc <show/go>\n" +
                    "  show             ▶ Show the current logical scene\n" +
                    "  go <name:string> ▶ Enter the specified logical scene");
            srv.RegisterCommand("le", (keyword, fullCmd, argsCount, args) =>
            {
                Log.V(TAG, "LastError is {0}", GameErrorChecker.LastError.ToString());
                return true;
            }, 1, "le > Show LastError");
        }

        #endregion

        #region Video settings

        private Resolution[] resolutions = null;
        private int defaultResolution = 0;

        private void InitVideoSettings()
        {
            // Emit the screen size changed event.
            GameManager.GameMediator.RegisterGlobalEvent(GameEventNames.EVENT_SCREEN_SIZE_CHANGED);

            resolutions = Screen.resolutions;

            for (int i = 0; i < resolutions.Length; i++)
                if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
                {
                    defaultResolution = i;
                    break;
                }

            // Settings update event.
            GameSettings.RegisterSettingsUpdateCallback(SettingConstants.SettingsVideo, OnVideoSettingsUpdated);
            GameSettings.RequireSettingsLoad(SettingConstants.SettingsVideo);
        }

        // Apply video settings to the engine.
        private bool OnVideoSettingsUpdated(string groupName, int action)
        {
            int resolutionsSet = GameSettings.GetInt(SettingConstants.SettingsVideoResolution, -1);
            bool fullScreen = GameSettings.GetBool(SettingConstants.SettingsVideoFullScreen, Screen.fullScreen);
            int quality = GameSettings.GetInt(SettingConstants.SettingsVideoQuality, -1);
            int vSync = GameSettings.GetInt(SettingConstants.SettingsVideoVsync, -1);

            Log.V(TAG, "OnVideoSettingsUpdated:\nresolutionsSet: {0}\nfullScreen: {1}\nquality: {2}\nvSync : {3}", resolutionsSet, fullScreen, quality, vSync);

            if (resolutionsSet >= 0)
            {
                if (resolutionsSet < resolutions.Length)
                {
                    var resolution = resolutions[resolutionsSet];
                    var resolutionWidth = resolution.width;
                    var resolutionHeight = resolution.height;

                    var currentResolution = Screen.currentResolution;


                    // Aspect ratio.
                    var currentResolutionAspectRatio = currentResolution.width * 1F / currentResolution.height;
                    var resolutionAspectRatio = resolution.width * 1F / resolution.height;

                    var isCurrentResolutionLandspace = currentResolutionAspectRatio > 1;
                    var isResolutionLandspace = resolutionAspectRatio > 1;

                    if (isCurrentResolutionLandspace ^ isResolutionLandspace)
                    {
                        var tmpInt = resolutionWidth;
                        resolutionWidth = resolutionHeight;
                        resolutionHeight = tmpInt;
                    }
                    var mode = fullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                    if (fullScreen && Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen)
                        mode = FullScreenMode.ExclusiveFullScreen;
                    Screen.SetResolution(resolutionWidth, resolutionHeight, mode, resolution.refreshRateRatio);
                    // Emit the screen size changed event.
                    GameMediator.DispatchGlobalEvent(GameEventNames.EVENT_SCREEN_SIZE_CHANGED, resolutionWidth, resolutionHeight);
                }
            }
            if (fullScreen != Screen.fullScreen)
                Screen.fullScreen = fullScreen;
            if (quality >= 0)
                QualitySettings.SetQualityLevel(quality, true);
            if (vSync >= 0)
                QualitySettings.vSyncCount = vSync;
            return true;
        }

        #endregion

        #endregion

        #region Shutdown and destruction

        /// <summary>
        /// Clear the entire scene.
        /// </summary>
        internal void ClearScense()
        {
            foreach (Camera c in Camera.allCameras)
                c.gameObject.SetActive(false);
            for (int i = 0, c = GameCanvas.transform.childCount; i < c; i++)
            {
                GameObject go = GameCanvas.transform.GetChild(i).gameObject;
                if (go.name == "GameUIWindow")
                {
                    GameObject go1 = null;
                    for (int j = 0, c1 = go.transform.childCount; j < c1; j++)
                    {
                        go1 = go.transform.GetChild(j).gameObject;
                        if (go1.tag != "DebugWindow")
                            go1.SetActive(false);
                    }
                }
                else if (go.name != "DebugToolbar")
                    go.SetActive(false);
            }
            for (int i = 0, c = transform.childCount; i < c; i++)
            {
                GameObject go = transform.GetChild(i).gameObject;
                if (go.name != "GameManager")
                    go.SetActive(false);
            }
            GameBaseCamera.gameObject.SetActive(true);
        }
        /// <summary>
        /// Release resources.
        /// </summary>
        public override void Destroy()
        {
            Log.D(TAG, "Destroy");
            Instance = null;
        }

        /// <summary>
        /// Request the game shutdown flow.
        /// </summary>
        public void QuitGame()
        {
            Log.D(TAG, "QuitGame");
            if (!gameIsQuitEmitByGameManager)
            {
                gameIsQuitEmitByGameManager = true;
                DoQuit();
            }
            GameSystem.IsRestart = false;
        }
        /// <summary>
        /// Restart the game.
        /// </summary>
        public void RestartGame()
        {
            QuitGame();
            GameSystem.IsRestart = true;
        }

        private static bool gameIsQuitEmitByGameManager = false;

        private bool Application_wantsToQuit()
        {
            if (!gameIsQuitEmitByGameManager)
            {
                DoQuit();
                return false;
            }
            return true;
        }
        private void DoQuit()
        {
            // Clean up critical state and emit the quit event.

            GameBaseCamera.clearFlags = CameraClearFlags.Color;
            GameBaseCamera.backgroundColor = Color.black;

            Application.wantsToQuit -= Application_wantsToQuit;

            delayItems.Clear();

            var pm = GetSystemService<GamePackageManager>();
            pm.SavePackageRegisterInfo();

            ObjectStateBackupUtils.ClearAll();

            if (DebugMode)
                DebugManager.Destroy();

            GameMediator.DispatchGlobalEvent(GameEventNames.EVENT_BEFORE_GAME_QUIT);
            ReqGameQuit();
        }

        #endregion

        #region Update

        public delegate void VoidDelegate();

        private int nextGameQuitTick = -1;

        /// <summary>
        /// Request game exit.
        /// </summary>
        private void ReqGameQuit()
        {
            nextGameQuitTick = 40;
        }
        protected override void Update()
        {
            // Run delayed callbacks.
            if (delayItems.Count > 0)
            {
                LinkedListNode<DelayItem> item = delayItems.First;
                while (item != null)
                {
                    item.Value.Time -= Time.deltaTime;
                    if (item.Value.Time <= 0)
                    {
                        delayItems.Remove(item);
                        item.Value.Callback.Invoke();
                        item = item.Next;
                        continue;
                    }
                    item = item.Next;
                }
            }
            // Run timer callbacks.
            if (timerItems.Count > 0)
            {
                LinkedListNode<TimerItem> item = timerItems.First;
                while (item != null)
                {
                    var timeItem = item.Value;
                    timeItem.Time -= Time.deltaTime;
                    if (timeItem.Time <= 0)
                    {
                        timeItem.Time = timeItem.LoopTime;
                        item.Value.Callback.Invoke();
                    }
                    item = item.Next;
                }
            }
            // Exit delay.
            if (nextGameQuitTick >= 0)
            {
                nextGameQuitTick--;
                if (nextGameQuitTick == 30)
                    ClearScense();
                if (nextGameQuitTick == 0)
                {
                    nextGameQuitTick = -1;
                    gameIsQuitEmitByGameManager = false;
                    GameSystem.Destroy();
                }
            }
        }

        #endregion

        #region System service access wrappers

        /// <summary>
        /// Get a system service.
        /// </summary>
        /// <typeparam name="T">Service type derived from GameService.</typeparam>
        /// <returns>Service instance, or null if not found.</returns>
        public static T GetSystemService<T>() where T : GameService<T>
        {
            return GameSystem.GetSystemService<T>();
        }

        #endregion

        #region Other methods

        private Camera lastActiveCam = null;

        /// <summary>
        /// Capture a screenshot.
        /// </summary>
        public string CaptureScreenshot()
        {
            // Create the directory.
            string saveDir = Application.persistentDataPath + "/Screenshot/";
            if (!Directory.Exists(saveDir))
                Directory.CreateDirectory(saveDir);

            // Save the image.
            string savePath = saveDir + "Screenshot" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".png";
            ScreenCapture.CaptureScreenshot(savePath);
            Log.D(TAG, "CaptureScreenshot to " + savePath);
            // Show a notification.
            Delay(1.0f, () => GetSystemService<GameUIManager>().GlobalToast(I18N.I18N.TrF("global.CaptureScreenshotSuccess", "", savePath)));
            return savePath;
        }
        /// <summary>
        /// Set base camera visibility.
        /// </summary>
        /// <param name="visible">Whether to show it.</param>
        public void SetGameBaseCameraVisible(bool visible)
        {
            if (visible)
            {
                foreach (var c in Camera.allCameras)
                    if (c.gameObject.activeSelf && c.gameObject.tag == "MainCamera")
                    {
                        lastActiveCam = c;
                        lastActiveCam.gameObject.SetActive(false);
                        break;
                    }
            }
            GameBaseCamera.gameObject.SetActive(visible);
            if (!visible && lastActiveCam != null)
            {
                lastActiveCam.gameObject.SetActive(true);
                lastActiveCam = null;
            }
        }
        /// <summary>
        /// Hide the initial global loading animation.
        /// </summary>
        public void HideGlobalStartLoading() { GameEntry.Instance.GameGlobalIngameLoading.SetActive(false); }
        /// <summary>
        /// Show the initial global loading animation.
        /// </summary>
        public void ShowGlobalStartLoading() { GameEntry.Instance.GameGlobalIngameLoading.SetActive(true); }

        #region Prefab methods

        // Prefab instantiation helpers.
        // Convenience methods equivalent to CloneUtils.

        /// <summary>
        /// Instantiate a prefab and set its name.
        /// </summary>
        /// <param name="prefab">Prefab.</param>
        /// <param name="name">New object name.</param>
        /// <returns>New object.</returns>
        public GameObject InstancePrefab(GameObject prefab, string name)
        {
            return InstancePrefab(prefab, transform, name);
        }

        /// <summary>
        /// Instantiate a prefab and set its parent and name.
        /// </summary>
        /// <param name="prefab">Prefab.</param>
        /// <param name="parent">Parent.</param>
        /// <param name="name">New object name.</param>
        /// <returns>New object.</returns>
        public GameObject InstancePrefab(GameObject prefab, Transform parent, string name)
        {
            return CloneUtils.CloneNewObjectWithParent(prefab, parent, name);
        }

        /// <summary>
        /// Create a GameObject with a parent and name.
        /// </summary>
        /// <param name="parent">Parent.</param>
        /// <param name="name">New object name.</param>
        /// <returns>New object.</returns>
        public GameObject InstanceNewGameObject(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            return go;
        }

        /// <summary>
        /// Create a GameObject.
        /// </summary>
        /// <param name="name">New object name.</param>
        /// <returns>New object.</returns>
        public GameObject InstanceNewGameObject(string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform);
            return go;
        }

        #endregion

        #region FileUtils

        // Convenience methods equivalent to FileUtils.

        /// <summary>
        /// Write a string to a file.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <param name="append">Append if true; otherwise overwrite.</param>
        /// <param name="path">File to write.</param>
        public void WriteFile(string path, bool append, string data) { FileUtils.WriteFile(path, append, data); }
        /// <summary>
        /// Check whether a file exists.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <returns>Whether the file exists.</returns>
        public bool FileExists(string path) { return FileUtils.FileExists(path); }
        /// <summary>
        /// Check whether a directory exists.
        /// </summary>
        /// <param name="path">Directory path.</param>
        /// <returns>Whether it exists.</returns>
        public bool DirectoryExists(string path) { return FileUtils.DirectoryExists(path); }
        /// <summary>
        /// Create a directory.
        /// </summary>
        /// <param name="path">Directory path.</param>
        public void CreateDirectory(string path) { FileUtils.CreateDirectory(path); }
        /// <summary>
        /// Read a file as a string.
        /// </summary>
        /// <param name="path">File.</param>
        /// <returns>File contents.</returns>
        public string ReadFile(string path) { return FileUtils.ReadFile(path); }
        /// <summary>
        /// Delete a file or directory.
        /// </summary>
        /// <param name="path">File.</param>
        public void RemoveFile(string path) { FileUtils.RemoveFile(path); }
        /// <summary>
        /// Delete a directory.
        /// </summary>
        /// <param name="path">File.</param>
        public void RemoveDirectory(string path) { FileUtils.RemoveDirectory(path); }

        #endregion

        #region Timers and delays

        private int timerId = -1;
        private class DelayItem
        {
            public int Id;
            public float Time;
            public VoidDelegate Callback;
        }
        private class TimerItem
        {
            public int Id;
            public float DelayTime;
            public float LoopTime;

            public float Time;
            public VoidDelegate Callback;
        }
        private LinkedList<DelayItem> delayItems = new LinkedList<DelayItem>();
        private LinkedList<TimerItem> timerItems = new LinkedList<TimerItem>();

        private int GetNextTimerId()
        {
            if (timerId > 0xffffffe)
                timerId = 0;
            return timerId++;
        }

        /// <summary>
        /// Schedule a delayed callback.
        /// </summary>
        /// <param name="sec">Delay in seconds.</param>
        /// <param name="callback">Callback.</param>
        public int Delay(float sec, VoidDelegate callback)
        {
            var d = new DelayItem();
            d.Time = sec;
            d.Id = GetNextTimerId();
            d.Callback = callback;
            delayItems.AddFirst(d);
            return d.Id;
        }

        /// <summary>
        /// Cancel a delayed callback by ID.
        /// </summary>
        /// <param name="timerId">ID returned by Delay.</param>
        /// <returns>Whether removal succeeded.</returns>
        public bool DeleteDelay(int timerId)
        {
            LinkedListNode<DelayItem> item = delayItems.First;
            while (item != null)
            {
                if (item.Value.Id == timerId)
                {
                    delayItems.Remove(item);
                    return true;
                }
                item = item.Next;
            }
            return false;
        }

        /// <summary>
        /// Create a repeating timer.
        /// </summary>
        /// <param name="loopSec">Timer interval.</param>
        /// <param name="callback">Callback.</param>
        /// <returns>Timer ID; pass it to DeleteTimer to remove the timer.</returns>
        public int Timer(float loopSec, VoidDelegate callback)
        {
            var d = new TimerItem();
            d.Id = GetNextTimerId();
            d.LoopTime = loopSec;
            d.DelayTime = loopSec;
            d.Callback = callback;
            timerItems.AddLast(d);
            return d.Id;
        }

        /// <summary>
        /// Create a repeating timer.
        /// </summary>
        /// <param name="delaySec">Delay before the first timer invocation.</param>
        /// <param name="loopSec">Timer interval.</param>
        /// <param name="callback">Callback.</param>
        /// <returns>Timer ID; pass it to DeleteTimer to remove the timer.</returns>
        public int Timer(float loopSec, float delaySec, VoidDelegate callback)
        {
            var d = new TimerItem();
            d.Id = GetNextTimerId();
            d.LoopTime = loopSec;
            d.DelayTime = delaySec;
            d.Callback = callback;
            timerItems.AddLast(d);
            return d.Id;
        }

        /// <summary>
        /// Delete a timer by ID.
        /// </summary>
        /// <param name="timerId">ID returned by Timer.</param>
        /// <returns>Whether removal succeeded.</returns>
        public bool DeleteTimer(int timerId)
        {
            LinkedListNode<TimerItem> item = timerItems.First;
            while (item != null)
            {
                if (item.Value.Id == timerId)
                {
                    timerItems.Remove(item);
                    return true;
                }
                item = item.Next;
            }
            return false;
        }

        #endregion

        #endregion

        #region Logic Scense

        // Logical scenes resemble Unity scenes without repeated scene loading and unloading.
        // Transitions operate within one Unity scene, making them inexpensive.
        // Transitions emit EVENT_LOGIC_SECNSE_QUIT and EVENT_LOGIC_SECNSE_ENTER;
        // packages show or hide their objects in response.

        /*
        Example: subscribe to logical scene entry/exit and show or hide owned content.
        ```csharp
        GameManager.GameMediator.RegisterEventHandler(thisGamePackage, GameEventNames.EVENT_LOGIC_SECNSE_ENTER, "Intro", (evtName, params) => {
          var scense = params[1];
          if (scense == "Intro") { 
            // Show owned content when entering the scene.
          }
          return false;
        });
        GameManager.GameMediator.RegisterEventHandler(thisGamePackage, GameEventNames.EVENT_LOGIC_SECNSE_QUIT, "Intro", (evtName, params) => {
          var scense = params[1];
          if (scense == "Intro") { 
            // Hide owned content when leaving the scene.
          }
          return false;
        });
        ```
        */

        private List<string> logicScenses = new List<string>();
        private int currentScense = -1;
        private string firstScense = "";

        /// <summary>
        /// Enter a logical scene.
        /// </summary>
        /// <param name="name">Scene name.</param>
        /// <returns>Whether the operation succeeded.</returns>
        public bool RequestEnterLogicScense(string name)
        {
            int curIndex = logicScenses.IndexOf(name);
            if (curIndex < 0)
            {
                GameErrorChecker.LastError = GameError.NotRegister;
                Log.E(TAG, "Scense {0} not register! ", name);
                return false;
            }
            if (curIndex == currentScense)
                return true;

            Log.I(TAG, "Enter logic scense {0} ", name);
            if (currentScense >= 0)
                GameMediator.DispatchGlobalEvent(GameEventNames.EVENT_LOGIC_SECNSE_QUIT, logicScenses[currentScense]);
            currentScense = curIndex;
            GameMediator.DispatchGlobalEvent(GameEventNames.EVENT_LOGIC_SECNSE_ENTER, logicScenses[currentScense]);
            return true;
        }
        /// <summary>
        /// Get all logical scenes.
        /// </summary>
        public string[] GetLogicScenses() { return logicScenses.ToArray(); }

        #endregion
    }
}
