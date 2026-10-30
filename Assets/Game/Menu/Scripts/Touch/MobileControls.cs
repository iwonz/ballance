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
    private GamePlayManager play;
    private KeypadUIControl keys;
    private SimpleTouchController joystick;
    private CanvasGroup group;
    private Text status;
    private Rect safe;
    private Vector2 screen;
    private int? swipePointer;
    private Vector2 swipeStart;
    private bool tilt, active;

    private void Start()
    {
      play = GamePlayManager.Instance;
      keys = GetComponent<KeypadUIControl>();
      joystick = GetComponentInChildren<SimpleTouchController>();
      group = gameObject.AddComponent<CanvasGroup>();
      tilt = GameManager.Instance.GameSettings.GetString(SettingConstants.SettingsControlKeypad) == "Tilt";
      // A background raycast receives free-area gestures; child controls keep their own pointers.
      var surface = gameObject.AddComponent<Image>();
      surface.color = Color.clear;
      Button("Pause", I18N.Tr("core.ui.MobilePause"), new Vector2(.5f, 1), new Vector2(tilt ? -90 : 0, -48), () => {
        Release(); if (play._IsGamePlaying && play.CanEscPause) play.PauseLevel(true);
      });
      if (tilt) {
        Button("Recenter", I18N.Tr("core.ui.MobileRecenter"), new Vector2(.5f, 1), new Vector2(90, -48), () => MobileTilt.Start(true));
        MobileTilt.Start(false);
      }
      status = Label("Mobile hint", I18N.Tr(tilt ? "core.ui.MobileTiltHint" : "core.ui.MobileTouchHint"));
      var rect = status.rectTransform;
      rect.anchorMin = new Vector2(.2f, 1); rect.anchorMax = new Vector2(.8f, 1);
      rect.pivot = new Vector2(.5f, 1); rect.anchoredPosition = new Vector2(0, -90); rect.sizeDelta = new Vector2(0, 64);
      subscriptions.Add(play.EventPause.On(_ => Show(false)));
      subscriptions.Add(play.EventResume.On(_ => { MobileTilt.Calibrate(); Show(true); }));
      subscriptions.Add(play.EventStart.On(_ => Show(play._IsGamePlaying)));
      subscriptions.Add(play.EventQuit.On(_ => Show(false)));
      subscriptions.Add(play.EventPass.On(_ => Show(false)));
      Fit(); Show(play._IsGamePlaying);
    }

    private Text Label(string name, string text)
    {
      var label = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
      label.transform.SetParent(transform, false);
      label.font = GamePlayUIControl.Instance._ScoreText.font;
      label.fontSize = 22; label.resizeTextForBestFit = true; label.resizeTextMinSize = 14; label.resizeTextMaxSize = 22;
      label.alignment = TextAnchor.MiddleCenter; label.color = Color.white; label.raycastTarget = false; label.text = text;
      var shadow = label.gameObject.AddComponent<Shadow>();
      shadow.effectDistance = new Vector2(2, -2); shadow.effectColor = Color.black;
      return label;
    }

    private void Button(string name, string title, Vector2 anchor, Vector2 position, UnityEngine.Events.UnityAction action)
    {
      var button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
      var rect = (RectTransform)button.transform;
      rect.SetParent(transform, false); rect.anchorMin = rect.anchorMax = anchor;
      rect.anchoredPosition = position; rect.sizeDelta = new Vector2(160, 64);
      button.targetGraphic = button.GetComponent<Image>(); button.targetGraphic.color = new Color(.08f, .16f, .18f, .88f);
      button.onClick.AddListener(action);
      var label = Label(name + " label", title); label.transform.SetParent(rect, false);
      label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
      label.rectTransform.offsetMin = new Vector2(8, 4); label.rectTransform.offsetMax = new Vector2(-8, -4);
    }

    private void Show(bool visible)
    {
      Release(); active = visible;
      group.alpha = visible ? 1 : 0; group.interactable = group.blocksRaycasts = visible;
    }
    private void Fit()
    {
      Release(); MobileTilt.Calibrate();
      safe = Screen.safeArea; screen = new Vector2(Screen.width, Screen.height);
      var rect = (RectTransform)transform;
      rect.anchorMin = new Vector2(safe.xMin / screen.x, safe.yMin / screen.y);
      rect.anchorMax = new Vector2(safe.xMax / screen.x, safe.yMax / screen.y);
      rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private void Update()
    {
      if (safe != Screen.safeArea || screen != new Vector2(Screen.width, Screen.height)) Fit();
      if (!active || !play._IsGamePlaying || !tilt) return;
      bool ready = MobileTilt.Read(out var value);
      status.text = I18N.Tr(ready ? "core.ui.MobileTiltHint" : "core.ui.MobileTiltUnavailable");
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
      if (!active || swipePointer.HasValue || data.pointerPressRaycast.gameObject != gameObject) return;
      swipePointer = data.pointerId; swipeStart = data.position;
    }
    public void OnDrag(PointerEventData data) { }
    public void OnPointerUp(PointerEventData data)
    {
      if (swipePointer != data.pointerId) return;
      swipePointer = null;
      var delta = data.position - swipeStart;
      if (!active || !play.BallManager.CanControllCamera || Mathf.Abs(delta.x) < Screen.width * .07f || Mathf.Abs(delta.x) < Mathf.Abs(delta.y) * 1.5f) return;
      if (delta.x > 0) play.CamManager.RotateRight(); else play.CamManager.RotateLeft();
    }
    private void OnApplicationFocus(bool focused) { if (!focused) Interrupt(); }
    private void OnApplicationPause(bool paused) { if (paused) Interrupt(); }
    private void Interrupt()
    {
      Release();
      if (active && play != null && play._IsGamePlaying && play.CanEscPause) play.PauseLevel(true);
    }
    private void OnEnable() { if (tilt) MobileTilt.Start(false); }
    private void OnDisable() { Release(); if (tilt) MobileTilt.Stop(); }
    private void OnDestroy()
    {
      foreach (var subscription in subscriptions) subscription.Off();
    }
  }
}
