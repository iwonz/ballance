using System.Collections.Generic;
using Ballance2.Base;
using Ballance2.Game;
using Ballance2.Game.GamePlay;
using Ballance2.Services;
using Ballance2.Services.I18N;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ballance2.Menu.Touch
{
  public sealed class MobileControls : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
  {
#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")] private static extern int BallanceTouchAvailable();
    public static bool Available => BallanceTouchAvailable() != 0;
#else
    public static bool Available => Application.isMobilePlatform || UnityEngine.InputSystem.Touchscreen.current != null;
#endif
    private readonly List<GameEventEmitterHandler> subscriptions = new List<GameEventEmitterHandler>();
    private readonly List<GameObject> movement = new List<GameObject>();
    private readonly List<GameObject> cameraControls = new List<GameObject>();
    private GamePlayManager play;
    private KeypadUIControl keys;
    private SimpleTouchController joystick;
    private CanvasGroup group;
    private Image surface;
    private RectTransform menu, recenter;
    private Text status;
    private Rect safe;
    private Vector2 screen;
    private float unit, savedKeySize;
    private bool fitted;
    public float ControlTop { get; private set; } = 200;
    public float ControlSideWidth { get; private set; } = 200;
    public float CenterControlTop { get; private set; }
    private int? swipePointer;
    private Vector2 swipeStart;
    private bool tilt, active, canMove, canCamera;

    private void Awake()
    {
      keys = GetComponent<KeypadUIControl>();
      joystick = GetComponentInChildren<SimpleTouchController>(true);
      if (keys.KeypadActionButtons.Length == 0) {
        // The center preset nests camera buttons inside movement pads; give them independent visibility.
        var row = new GameObject("Mobile camera row", typeof(RectTransform), typeof(KeypadGridLayout)).GetComponent<KeypadGridLayout>();
        row.transform.SetParent(transform, false);
        ((RectTransform)row.transform).anchorMin = ((RectTransform)row.transform).anchorMax = new Vector2(.5f, 0);
        foreach (var grid in GetComponentsInChildren<KeypadGridLayout>(true)) {
          if (grid == row) continue;
          for (int i = grid.Items.Count - 1; i >= 0; i--) {
            var item = grid.Items[i];
            if (item.Item.name != "ButtonCamLeft" && item.Item.name != "ButtonCamRight" && item.Item.name != "ButtonSpace") continue;
            item.Item.transform.SetParent(row.transform, false);
            item.Position = new Vector2(item.Item.name == "ButtonCamLeft" ? 0 : item.Item.name == "ButtonSpace" ? 1 : 2, 0);
            row.Items.Add(item); grid.Items.RemoveAt(i);
          }
        }
      }
      group = gameObject.AddComponent<CanvasGroup>();
      group.alpha = 0; group.interactable = group.blocksRaycasts = false;
      transform.SetAsFirstSibling();
      surface = gameObject.AddComponent<Image>();
      surface.color = Color.clear;
    }

    private void Start()
    {
      play = GamePlayManager.Instance;
      tilt = GameManager.Instance.GameSettings.GetString(SettingConstants.SettingsControlKeypad) == "Tilt";
      foreach (var rect in GetComponentsInChildren<RectTransform>(true)) {
        if (rect.name.StartsWith("Joystick") && rect.GetComponent<SimpleTouchController>() != null ||
            rect.GetComponent<SimpleTouchDirectionKeyController>() != null) movement.Add(rect.gameObject);
        else if (rect.name.StartsWith("Button") && rect.GetComponent<Image>() != null) {
          if (rect.name == "ButtonUp" || rect.name == "ButtonDown") { if (GameManager.DebugMode) movement.Add(rect.gameObject); }
          else cameraControls.Add(rect.gameObject);
        }
      }
      menu = CreateButton("Game menu", "", () => { Release(); play.PauseLevel(true); });
      for (int i = 0; i < 3; i++) {
        var bar = new GameObject("Menu bar", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        bar.transform.SetParent(menu, false); bar.raycastTarget = false;
        bar.rectTransform.anchorMin = new Vector2(.25f, .28f + i * .19f);
        bar.rectTransform.anchorMax = new Vector2(.75f, .34f + i * .19f);
        bar.rectTransform.offsetMin = bar.rectTransform.offsetMax = Vector2.zero;
      }
      if (tilt) {
        recenter = CreateButton("Recenter", I18N.Tr("core.ui.MobileRecenter"), () => MobileTilt.Start(true));
        status = Label("Tilt status", "");
        MobileTilt.Start(false);
      }
      subscriptions.Add(play.EventPause.On(_ => Refresh()));
      subscriptions.Add(play.EventResume.On(_ => { MobileTilt.Calibrate(); Refresh(); }));
      subscriptions.Add(play.EventQuit.On(_ => { Release(); Refresh(); }));
      subscriptions.Add(play.EventPass.On(_ => { Release(); Refresh(); }));
      Refresh();
    }

    private Text Label(string name, string text)
    {
      var label = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
      label.transform.SetParent(transform, false);
      label.font = GamePlayUIControl.Instance._ScoreText.font;
      label.alignment = TextAnchor.MiddleCenter; label.color = Color.white; label.raycastTarget = false; label.text = text;
      return label;
    }

    private RectTransform CreateButton(string name, string title, UnityEngine.Events.UnityAction action)
    {
      var button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
      var rect = (RectTransform)button.transform;
      rect.SetParent(transform, false); rect.anchorMin = rect.anchorMax = Vector2.one;
      button.targetGraphic = button.GetComponent<Image>(); button.targetGraphic.color = new Color(.24f, .22f, .16f, .9f);
      button.navigation = new Navigation { mode = Navigation.Mode.None };
      button.onClick.AddListener(action);
      if (title.Length > 0) {
        var label = Label(name + " label", title); label.transform.SetParent(rect, false);
        label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
      }
      return rect;
    }

    private void Refresh()
    {
      bool visible = play.IsSessionOpen && !play.CurrentLevelPass && !play.IsUserPaused && GameUIManager.Instance.GetCurrentPage() == null;
      bool move = visible && play._IsGamePlaying && play.BallManager.CanControll;
      bool camera = visible && play._IsGamePlaying && play.BallManager.CanControllCamera;
      if (active != visible || canMove != move || canCamera != camera) Release();
      active = visible; canMove = move; canCamera = camera;
      group.alpha = visible ? 1 : 0; group.interactable = group.blocksRaycasts = visible;
      surface.raycastTarget = camera;
      foreach (var control in movement) if (control.activeSelf != move) control.SetActive(move);
      foreach (var control in cameraControls) if (control.activeSelf != camera) control.SetActive(camera);
      if (recenter != null) recenter.gameObject.SetActive(move);
      if (status != null) status.gameObject.SetActive(move);
    }

    private void Fit()
    {
      Release();
      safe = Screen.safeArea; screen = new Vector2(Screen.width, Screen.height);
      MobileUILayout.SafeArea((RectTransform)transform);
      unit = MobileUILayout.Unit(transform);
      menu.sizeDelta = Vector2.one * 48 * unit;
      menu.anchoredPosition = new Vector2(-32, -32) * unit;
      if (recenter != null) {
        recenter.sizeDelta = new Vector2(128, 48) * unit;
        recenter.anchorMin = recenter.anchorMax = new Vector2(.5f, 0);
        recenter.anchoredPosition = new Vector2(0, 28) * unit;
        recenter.GetComponentInChildren<Text>().fontSize = Mathf.RoundToInt(16 * unit);
        status.fontSize = Mathf.RoundToInt(12 * unit);
        status.rectTransform.anchorMin = new Vector2(.23f, 1); status.rectTransform.anchorMax = new Vector2(.60f, 1);
        status.rectTransform.pivot = new Vector2(.5f, 1);
        status.rectTransform.anchoredPosition = new Vector2(0, -8) * unit;
        status.rectTransform.sizeDelta = new Vector2(0, 48 * unit);
      }
      savedKeySize = GameManager.Instance.GameSettings.GetFloat(SettingConstants.SettingsControlKeySize);
      ControlTop = FitControls(keys, savedKeySize, unit);
      fitted = true;
    }

    internal static float FitControls(KeypadUIControl keys, float requestedSize, float unit)
    {
      var bounds = ((RectTransform)keys.transform).rect.size;
      float maximum = Mathf.Min(64 * unit, (bounds.x * .5f - 12 * unit) / 3, (bounds.y - 128 * unit) / 3);
      if (keys.KeypadActionButtons.Length == 0 && bounds.y > bounds.x) maximum = 48 * unit;
      float keySize = Mathf.Clamp(requestedSize, 48 * unit, Mathf.Max(48 * unit, maximum));
      float top = 0;
      foreach (var layout in keys.GetComponentsInChildren<KeypadGridLayout>(true)) {
        var direction = layout.GetComponent<SimpleTouchDirectionKeyController>();
        if (direction != null) {
          float start = float.MaxValue;
          foreach (var item in layout.Items)
            if (!item.Item.name.StartsWith("Button")) start = Mathf.Min(start, item.Position.x);
          foreach (var item in layout.Items)
            if (!item.Item.name.StartsWith("Button")) item.Position -= new Vector2(start, 0);
          direction.SplitStart = Vector2.zero;
        }
        layout.DoLayout(keySize);
        // Rotate themed arrows around the cell center, not its top-left corner.
        foreach (var item in layout.Items) {
          var imageRect = item.Item.rectTransform;
          imageRect.pivot = new Vector2(.5f, .5f);
          imageRect.anchoredPosition += new Vector2(layout.Anchor == KeypadGridLayoutAnchor.Left ? .5f : -.5f, -.5f) * keySize;
        }
        // Debug-only buttons must not consume mobile layout space.
        float width = 0, height = 0;
        foreach (var item in layout.Items) {
          if (!GameManager.DebugMode && (item.Item.name == "ButtonUp" || item.Item.name == "ButtonDown")) continue;
          width = Mathf.Max(width, Mathf.Abs(item.Position.x) + 1);
          height = Mathf.Max(height, item.Position.y + 1);
        }
        ((RectTransform)layout.transform).sizeDelta = new Vector2(width, height) * keySize;
        if (direction != null) direction.SplitCount = new Vector2(width, height);
        var rect = (RectTransform)layout.transform;
        PlaceControl(rect, unit);
        if (layout.name == "Mobile camera row")
          rect.anchoredPosition = new Vector2(0, bounds.y > bounds.x ? 56 * unit + 3.5f * keySize : keySize * .5f);
        top = Mathf.Max(top, rect.anchoredPosition.y + rect.rect.height * .5f);
      }
      var joystick = keys.GetComponentInChildren<SimpleTouchController>(true);
      if (joystick != null) {
        var control = joystick.GetComponent<KeyPadJoystickController>();
        control.Joystick.rectTransform.sizeDelta = Vector2.one * keySize * 2.5f;
        control.JoystickImage.rectTransform.sizeDelta = Vector2.one * keySize;
      }
      if (joystick != null) {
        var rect = (RectTransform)joystick.transform;
        PlaceControl(rect, unit);
        top = Mathf.Max(top, rect.anchoredPosition.y + rect.rect.height * .5f);
      }
      float sideWidth = 0, centerTop = 0;
      foreach (Transform child in keys.transform) {
        if (child.GetComponent<KeypadGridLayout>() == null && child.GetComponent<SimpleTouchController>() == null) continue;
        var rect = (RectTransform)child;
        if (rect.anchorMin.x < .25f || rect.anchorMin.x > .75f)
          sideWidth = Mathf.Max(sideWidth, rect.rect.width / unit + 8);
        else centerTop = Mathf.Max(centerTop, (rect.anchoredPosition.y + rect.rect.height * .5f) / unit);
      }
      var mobile = keys.GetComponent<MobileControls>();
      mobile.ControlSideWidth = sideWidth;
      mobile.CenterControlTop = centerTop;
      mobile.ControlTop = top / unit + 8;
      return mobile.ControlTop;
    }

    private static void PlaceControl(RectTransform rect, float unit)
    {
      float anchor = rect.anchorMin.x;
      float side = anchor < .25f ? 1 : anchor > .75f ? -1 : 0;
      rect.anchorMin = rect.anchorMax = new Vector2(anchor, 0);
      rect.pivot = new Vector2(.5f, .5f);
      rect.anchoredPosition = new Vector2(side * (rect.rect.width * .5f + 8 * unit), rect.rect.height * .5f + 48 * unit);
    }

    private void LateUpdate()
    {
      if (play == null) return;
      if (!fitted || savedKeySize != GameManager.Instance.GameSettings.GetFloat(SettingConstants.SettingsControlKeySize) || safe != Screen.safeArea || screen != new Vector2(Screen.width, Screen.height) || !Mathf.Approximately(unit, MobileUILayout.Unit(transform))) Fit();
      Refresh();
      if (!canMove || !tilt) return;
      bool ready = MobileTilt.Read(out var value);
      status.text = ready ? "" : I18N.Tr("core.ui.MobileTiltUnavailable");
      if (joystick == null || !joystick.IsHeld) play.BallManager.SetBallPushValue(value.x, value.y);
    }
    private void Release()
    {
      swipePointer = null;
      if (keys != null) keys.Release();
      MobileTilt.Calibrate();
    }
    public void OnPointerDown(PointerEventData data)
    {
      if (!canCamera || swipePointer.HasValue || data.pointerPressRaycast.gameObject != gameObject) return;
      swipePointer = data.pointerId; swipeStart = data.position;
    }
    public void OnDrag(PointerEventData data) { }
    public void OnPointerUp(PointerEventData data)
    {
      if (swipePointer != data.pointerId) return;
      swipePointer = null;
      var delta = data.position - swipeStart;
      if (!canCamera || Mathf.Abs(delta.x) < Screen.width * .07f || Mathf.Abs(delta.x) < Mathf.Abs(delta.y) * 1.5f) return;
      if (delta.x > 0) play.CamManager.RotateRight(); else play.CamManager.RotateLeft();
    }
    private void OnApplicationFocus(bool focused) { if (!focused) Interrupt(); }
    private void OnApplicationPause(bool paused) { if (paused) Interrupt(); }
    private void Interrupt()
    {
      Release();
      if (active && play != null && play.IsSessionOpen) play.PauseLevel(true);
    }
    private void OnEnable() { if (tilt) MobileTilt.Start(false); }
    private void OnDisable() { Release(); if (tilt) MobileTilt.Stop(); }
    private void OnDestroy() { foreach (var subscription in subscriptions) subscription.Off(); }
  }
}
