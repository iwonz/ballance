using System;
using System.IO;
using System.Reflection;
using Ballance2.Entry;
using Ballance2.Game.GamePlay;
using Ballance2.Game.GamePlay.Other;
using Ballance2.Res;
using Ballance2.Services;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Ballance2.Editor
{
  // Directed real-level lifecycle checks complement browser touch and prefab raycast checks.
  [InitializeOnLoad]
  public static class MobileTutorialChecks
  {
    private const string Session = "Ballance.MobileTutorialChecks";
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly FieldInfo Profile = typeof(GamePathManager).GetField("persistentDataPath", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly Type Remaster = typeof(GameManager).Assembly.GetType("Ballance2.Game.Remaster.RemasterPreferences");

    static MobileTutorialChecks()
    {
      EditorApplication.playModeStateChanged += state => {
        if (!SessionState.GetBool(Session, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode) Execute();
        if (state == PlayModeStateChange.EnteredEditMode) {
          Profile?.SetValue(null, null);
          if (Profile == null) PlayerSettings.productName = SessionState.GetString(Session + ".product", "Ballance");
          Remaster?.GetField("editorTestPrefix", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);
          SessionState.EraseBool(Session);
          EditorApplication.Exit(SessionState.GetInt(Session + ".exit", 1));
        }
      };
      if (SessionState.GetBool(Session, false)) Isolate();
    }

    public static void Run()
    {
      Require(!Application.isBatchMode && !EditorApplication.isPlaying, "Use rendered edit mode without -batchmode or -quit");
      string root = Path.GetFullPath("Builds/checks/tutorial-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
      Directory.CreateDirectory(root + "/profile");
      SessionState.SetString(Session + ".root", root);
      SessionState.SetInt(Session + ".exit", 1);
      SessionState.SetString(Session + ".product", PlayerSettings.productName);
      if (Profile == null) PlayerSettings.productName = "Ballance-MobileTutorialChecks";
      SessionState.SetBool(Session, true);
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
      Require(root.Contains("/Builds/checks/tutorial-"), "Isolated profile directory");
      Profile?.SetValue(null, root + "/profile");
      Remaster?.GetField("editorTestPrefix", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, "MobileTutorialChecks." + Path.GetFileName(root) + ".");
      ResetTutorial();
    }
    private static void ResetTutorial() => Remaster?.GetMethod("SetInt").Invoke(null, new object[] { "tutorial.dismissed", 0 });
    private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Fields).GetValue(target);
    private static TutorialController Tutorial => UnityEngine.Object.FindAnyObjectByType<TutorialController>();
    private static bool At(int step) => Tutorial != null && Field<int>(Tutorial, "step") == step && Field<RectTransform>(Tutorial, "card") != null && Field<RectTransform>(Tutorial, "card").gameObject.activeSelf;
    private static bool Menu => GameUIManager.Instance?.GetCurrentPage()?.PageName == "PageMain";
    private static void Require(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); }
    private static async Awaitable Wait(Func<bool> ready, string label, float timeout = 90)
    {
      float deadline = Time.realtimeSinceStartup + timeout;
      while (!ready()) {
        Require(Time.realtimeSinceStartup < deadline, "Timeout: " + label);
        await Awaitable.NextFrameAsync();
      }
    }
    private static async Awaitable Delay(float seconds)
    {
      float end = Time.realtimeSinceStartup + seconds;
      await Wait(() => Time.realtimeSinceStartup >= end, "settle", seconds + 10);
    }
    private static async Awaitable Click(string action)
    {
      await Delay(.4f);
      Field<Button>(Tutorial, action).onClick.Invoke();
    }
    private static async Awaitable PauseRoundTrip()
    {
      var game = GamePlayManager.Instance;
      int step = Field<int>(Tutorial, "step"), points = game.CurrentPoint;
      bool simulate = game.GamePhysicsWorld.Simulate, move = game.BallManager.CanControll, camera = game.BallManager.CanControllCamera;
      game.PauseLevel(true);
      await Delay(.6f);
      Require(game.IsUserPaused && !game.GamePhysicsWorld.Simulate && !game.BallManager.CanControll && game.CurrentPoint == points, "User pause freezes state");
      Require(Field<int>(Tutorial, "step") == step, "User pause preserves tutorial step");
      game.ResumeLevel();
      await Awaitable.NextFrameAsync();
      Require(!game.IsUserPaused && game.GamePhysicsWorld.Simulate == simulate && game.BallManager.CanControll == move && game.BallManager.CanControllCamera == camera, "Resume restores exact permissions and simulation");
    }

    private static async void Execute()
    {
      string result = "PASS: introduction, pause snapshots, rapid input, later hints, skips, restart, fall recovery and exit cancellation.";
      Touchscreen touch = null;
      try {
        await Wait(() => Menu, "main menu");
        touch = InputSystem.AddDevice<Touchscreen>();
        GameMediator.Instance.NotifySingleEvent("CoreStartLoadLevel", "level01");
        await Wait(() => At(1), "introduction");
        await PauseRoundTrip();
        await Click("next");
        await Delay(.15f);
        var game = GamePlayManager.Instance;
        game.PauseLevel(true);
        await Delay(2);
        Require(game.IsUserPaused && Field<int>(Tutorial, "step") == 1 && !game.BallManager.CanControll, "Spawn does not complete over menu");
        game.ResumeLevel();
        await Wait(() => At(2), "camera hint");
        await PauseRoundTrip();
        await Click("next");
        Field<Button>(Tutorial, "next").onClick.Invoke();
        Require(At(3) && !game.BallManager.CanControll, "Repeated tap advances only once");
        await PauseRoundTrip();
        await Click("next");
        Require(At(4) && game.BallManager.CanControll, "Movement stage enables ball input");
        await PauseRoundTrip();
        await Click("next");
        Tutorial.Tut_ExtraLife.onTriggerEnter(Tutorial.Tut_ExtraLife.gameObject, game.BallManager.CurrentBall.gameObject);
        Require(At(5) && !game.GamePhysicsWorld.Simulate, "Authored extra-life trigger pauses for later hint");
        await PauseRoundTrip();
        await Click("next");
        Require(game.GamePhysicsWorld.Simulate && game.BallManager.CanControll, "Later Continue restores movement");
        Tutorial.Tut_StoneTranfo.onTriggerEnter(Tutorial.Tut_StoneTranfo.gameObject, game.BallManager.CurrentBall.gameObject);
        Require(At(6), "Authored transformation trigger shows next hint");
        await Click("skip");
        Require(game.GamePhysicsWorld.Simulate && game.BallManager.CanControll && !Field<bool>(Tutorial, "active"), "Later Skip releases tutorial");
        ResetTutorial(); game.RestartLevel();
        await Wait(() => At(1), "restart introduction");
        await Click("next"); await Wait(() => At(2), "restarted camera");
        await Click("skip");
        Require(game.BallManager.CanControll, "Camera-stage skip resumes movement");
        int lives = game.CurrentLife;
        Require(game.Fall(), "Fall accepted");
        await Delay(.2f); game.PauseLevel(true); await Delay(2); game.ResumeLevel();
        await Wait(() => game.BallManager.CanControll && game.CurrentLife == lives - 1, "fall recovery");
        ResetTutorial(); game.RestartLevel(); await Wait(() => At(1), "second restart");
        await Click("next"); await Delay(.15f); game.RestartLevel();
        await Wait(() => At(1), "restart during spawn"); await Delay(2);
        Require(At(1) && !game.BallManager.CanControll, "Old spawn cannot advance restarted tutorial");
        await Click("next"); await Delay(.15f); game.QuitLevel();
        await Wait(() => Menu, "exit during spawn"); await Delay(2);
        var remaining = Tutorial;
        Require(remaining == null || (!Field<bool>(remaining, "active") && Field<RectTransform>(remaining, "card") == null &&
          Field<int>(remaining, "continueKey") == 0 && Field<int>(remaining, "skipKey") == 0 && remaining.Tut_ExtraLife.onTriggerEnter == null),
          "Exit removes tutorial cards, input listeners and trigger callbacks");
        SessionState.SetInt(Session + ".exit", 0);
      } catch (Exception exception) { result = "FAIL: " + exception; Debug.LogError(result); }
      finally {
        if (touch != null) InputSystem.RemoveDevice(touch);
        File.WriteAllText(SessionState.GetString(Session + ".root", "") + "/report.txt", result + "\n");
        Debug.Log("MOBILE_TUTORIAL_CHECK: " + result);
        EditorApplication.ExitPlaymode();
      }
    }
  }
}
