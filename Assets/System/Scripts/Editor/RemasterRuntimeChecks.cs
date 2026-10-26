using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ballance2.Entry;
using Ballance2.Game.GamePlay;
using Ballance2.Game.LevelBuilder;
using Ballance2.Game.Remaster;
using Ballance2.Res;
using Ballance2.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ballance2.Editor
{
  // Directed lifecycle checks do not establish that a player can complete a level.
  [InitializeOnLoad]
  public static class RemasterRuntimeChecks
  {
    private const string Session = "Reballance.RuntimeChecks";
    private static readonly FieldInfo Preferences = typeof(RemasterPreferences).GetField("editorTestPrefix", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly FieldInfo ProfilePath = typeof(GamePathManager).GetField("persistentDataPath", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly List<string> Errors = new List<string>();
    private static readonly List<string> EditorDiagnostics = new List<string>();
    private static readonly List<object> Results = new List<object>();
    private static string scenario;

    static RemasterRuntimeChecks()
    {
      EditorApplication.playModeStateChanged += OnPlayMode;
      if (SessionState.GetBool(Session, false)) {
        Isolate();
        Application.logMessageReceived += OnLog;
      }
    }

    public static void Run()
    {
      Require(!EditorApplication.isPlaying, "start from edit mode");
      Require(!Application.isBatchMode, "omit -batchmode: level loading waits for rendered end-of-frame events");
      try { EnvironmentChecks.Run(); }
      catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); return; }
      string root = Path.GetFullPath("Builds/checks/runtime-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
      Directory.CreateDirectory(root + "/profile");
      SessionState.SetString(Session + ".root", root);
      SessionState.SetBool(Session, true);
      SessionState.SetInt(Session + ".exit", 1);
      Isolate();
      EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
      var entry = UnityEngine.Object.FindAnyObjectByType<GameEntry>();
      entry.DebugType = GameDebugType.NoDebug;
      entry.DebugLoadCustomPackages = false;
      entry.DebugSkipIntro = true;
      entry.DebugSetFrameRate = false;
      EditorApplication.ExecuteMenuItem("Window/General/Game");
      EditorApplication.EnterPlaymode();
    }

    private static void Isolate()
    {
      string root = SessionState.GetString(Session + ".root", "");
      Require(root.Contains("/Builds/checks/runtime-"), "isolated fixture directory");
      Preferences.SetValue(null, RemasterPreferences.Prefix + Path.GetFileName(root) + ".");
      ProfilePath.SetValue(null, root + "/profile");
      RemasterPreferences.SetInt("tutorial.dismissed", 1);
    }

    private static void OnPlayMode(PlayModeStateChange state)
    {
      if (!SessionState.GetBool(Session, false)) return;
      if (state == PlayModeStateChange.EnteredPlayMode) Execute();
      if (state == PlayModeStateChange.EnteredEditMode) {
        int exit = SessionState.GetInt(Session + ".exit", 1);
        string reportPath = SessionState.GetString(Session + ".root", "") + "/report.json";
        if (File.Exists(reportPath)) {
          var report = JObject.Parse(File.ReadAllText(reportPath));
          report["editorExitErrors"] = JArray.FromObject(Errors);
          if (Errors.Count > 0) exit = 1;
          report["passed"] = exit == 0;
          File.WriteAllText(reportPath, report.ToString());
        }
        RemasterPreferences.Reset();
        Preferences.SetValue(null, null);
        ProfilePath.SetValue(null, null);
        SessionState.EraseBool(Session);
        Application.logMessageReceived -= OnLog;
        EditorApplication.Exit(exit);
      }
    }

    private static void OnLog(string message, string stack, LogType type)
    {
      // Unity 6000.6's editor search index can fail before gameplay starts.
      if (stack.Contains("UnityEditor.Search.SearchDatabase") && stack.Contains("EnumerateAll")) {
        EditorDiagnostics.Add(message + "\n" + stack);
        return;
      }
      if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
        Errors.Add(message + "\n" + stack);
    }

    private static void Require(bool condition, string message)
    {
      if (!condition) throw new InvalidOperationException((scenario ?? "Runtime checks") + ": " + message);
    }

    private static async Awaitable Wait(Func<bool> ready, string label, double timeout = 90)
    {
      double deadline = EditorApplication.timeSinceStartup + timeout;
      while (!ready()) {
        Require(Errors.Count == 0, "Unity error during " + label + ": " + string.Join("\n", Errors));
        Require(EditorApplication.timeSinceStartup < deadline, "timeout: " + label);
        await Awaitable.NextFrameAsync();
      }
      Require(Errors.Count == 0, "Unity error during " + label + ": " + string.Join("\n", Errors));
    }

    private static async Awaitable Delay(float seconds)
    {
      float end = Time.realtimeSinceStartup + seconds;
      await Wait(() => Time.realtimeSinceStartup >= end, "settle", seconds + 30);
    }

    private static bool MenuReady() => GameUIManager.Instance != null &&
      GameUIManager.Instance.GetCurrentPage()?.PageName == "PageMain";
    private static bool BallReady() => GamePlayManager.Instance != null &&
      GamePlayManager.Instance.BallManager != null && GamePlayManager.Instance.BallManager.CanControll &&
      GamePlayManager.Instance.BallManager.GetControllingStatus() == BallControlStatus.Control;

    private static string Argument(string name)
    {
      var args = Environment.GetCommandLineArgs();
      int index = Array.IndexOf(args, name);
      return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static async void Execute()
    {
      double started = EditorApplication.timeSinceStartup;
      string failure = null;
      try {
        await Wait(MenuReady, "normal startup menu");
        var console = UnityEngine.Object.FindAnyObjectByType<Ballance2.DebugTools.DebugConsole>(FindObjectsInactive.Include);
        Require(console != null, "editor diagnostic console registered");
        console.gameObject.SetActive(true);
        await Delay(.3f);
        console.gameObject.SetActive(false);
        for (int level = 1; level <= 13; level++) {
          string name = "level" + level.ToString("00");
          if (Argument("-checkLevel") != null && Argument("-checkLevel") != name) continue;
          foreach (EnvironmentPreset preset in Enum.GetValues(typeof(EnvironmentPreset))) {
            if (Argument("-checkPreset") != null && Argument("-checkPreset") != preset.ToString()) continue;
            scenario = name + "/" + preset;
            double caseStart = EditorApplication.timeSinceStartup;
            Debug.Log("RUNTIME_CHECK_START: " + scenario);
            RemasterPreferences.Preset = preset;
            GameMediator.Instance.NotifySingleEvent("CoreStartLoadLevel", name);
            await Wait(() => EnvironmentController.Active != null && BallReady(), "level load");
            var game = GamePlayManager.Instance;
            var environment = EnvironmentController.Active;
            var world = game.GamePhysicsWorld;
            Require(environment.Profile != null && environment.Run.Preset == preset, "built-in environment attached");
            Require(world.Handle != IntPtr.Zero && world.Simulate, "native world running");
            Require(LevelBuilder.Instance.CurrentLevelSkyLayer == null, "legacy cloud plane cannot obscure the volumetric sky");
            await Delay(1.5f);
            await Awaitable.EndOfFrameAsync();
            ScreenCapture.CaptureScreenshot(SessionState.GetString(Session + ".root", "") + "/" + name + "-" + preset + ".png");

            game.PauseLevel(false);
            float time = environment.Run.Time;
            int score = game.CurrentPoint;
            Vector3 position = game.BallManager.CurrentBall.transform.position;
            EnvironmentController.Select(preset);
            await Delay(.2f);
            Require(environment.Run.Time == time && game.CurrentPoint == score, "pause freezes weather and score");
            Require(Vector3.Distance(position, game.BallManager.CurrentBall.transform.position) < .0001f, "pause freezes ball");
            game.ResumeLevel();
            await Wait(BallReady, "resume");

            foreach (string ball in new[] { "BallWood", "BallStone", "BallPaper" }) {
              game.BallManager.SetNextRecoverPos(game.SectorManager.CurrentLevelRestPoints[game.CurrentSector].point.transform.position);
              game.BallManager.SetCurrentBall(ball, BallControlStatus.Control);
              await Delay(.25f);
              Require(game.BallManager.CurrentBallName == ball && game.BallManager.CurrentBall._Rigidbody.IsPhysicalized,
                "native body created: " + ball);
            }

            int sectors = game.SectorManager.CurrentLevelSectorCount;
            for (int sector = 2; sector <= sectors; sector++) {
              game.PauseLevel(false);
              game.SectorManager.SetCurrentSector(sector);
              game.ResumeLevel(true);
              await Wait(BallReady, "checkpoint respawn");
              await Delay(.1f + game.SectorManager.CurrentLevelSectors[sector].moduls.Count * .025f);
              Require(game.CurrentSector == sector && environment.Run.Sector == sector, "checkpoint environment and mechanisms agree");
            }

            game.PauseLevel(false);
            var before = environment.Run.Current;
            var alternate = preset == EnvironmentPreset.London ? EnvironmentPreset.Moscow : EnvironmentPreset.London;
            EnvironmentController.Select(alternate);
            Require(environment.Run.Current.Equals(before), "selection begins continuously");
            time = environment.Run.Time;
            await Delay(.2f);
            Require(environment.Run.Time == time && environment.Run.Current.Equals(before), "selection remains paused");
            EnvironmentController.Select(preset);
            Require(environment.Run.Current.Equals(before), "interrupted selection remains continuous");
            game.ResumeLevel();
            await Wait(BallReady, "resume after selection");
            int lives = game.CurrentLife;
            Require(game.Fall(), "fall accepted");
            await Wait(BallReady, "fall recovery");
            await Delay(1.1f);
            Require(game.CurrentLife == lives - 1, "one life consumed by fall");
            Require(environment.Run.Preset == preset, "checkpoint retains manually selected preset");

            game.RestartLevel();
            await Wait(BallReady, "restart");
            Require(game.CurrentSector == 1 && game.CurrentLife == game.StartLife, "restart resets sector and lives");
            Require(environment.Run.Sector == 1 && environment.Run.Preset == preset, "restart resets environment");
            game.QuitLevel();
            await Wait(() => MenuReady() && EnvironmentController.Active == null, "unload and menu");
            Require(world == null || world.Handle == IntPtr.Zero, "native world released");
            Results.Add(new { level = name, preset = preset.ToString(), sectors, seconds = EditorApplication.timeSinceStartup - caseStart, passed = true });
            WriteReport(started, null);
            Debug.Log("RUNTIME_CHECK_PASSED: " + scenario);
          }
        }
        Require(Results.Count > 0, "at least one scenario selected");
        SessionState.SetInt(Session + ".exit", 0);
      } catch (Exception exception) {
        failure = exception.ToString();
        Debug.LogWarning("RUNTIME_CHECK_FAILED: " + failure);
      } finally {
        WriteReport(started, failure);
        Debug.Log("RUNTIME_CHECK_REPORT: " + SessionState.GetString(Session + ".root", "") + "/report.json");
        EditorApplication.ExitPlaymode();
      }
    }

    private static void WriteReport(double started, string failure)
    {
      File.WriteAllText(SessionState.GetString(Session + ".root", "") + "/report.json", JsonConvert.SerializeObject(new {
        kind = "directed-runtime-integration", fullPlaythrough = false, version = Application.version,
        unity = Application.unityVersion, gpu = SystemInfo.graphicsDeviceName,
        seconds = EditorApplication.timeSinceStartup - started, results = Results, scenario, failure, errors = Errors,
        editorDiagnostics = EditorDiagnostics
      }, Formatting.Indented));
    }
  }
}
