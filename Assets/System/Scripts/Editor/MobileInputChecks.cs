using System;
using Ballance2.Menu.Touch;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Ballance2.Editor
{
  public static class MobileInputChecks
  {
    public static void Run()
    {
      Require(SimpleTouchController.Normalize(new Vector2(1, 1), 100) == Vector2.zero, "Joystick neutral dead zone");
      Require(SimpleTouchController.Normalize(new Vector2(300, 400), 100).magnitude <= 1.0001f, "Joystick diagonal bound");
      Require(MobileTilt.Steering(new Vector2(.2f, -.4f), new Vector2(.2f, -.4f)) == Vector2.zero, "Tilt calibration");
      Require(MobileTilt.Steering(new Vector2(.21f, -.4f), new Vector2(.2f, -.4f)) == Vector2.zero, "Tilt jitter dead zone");
      Require(MobileTilt.Steering(Vector2.one, Vector2.zero).magnitude <= 1.0001f, "Tilt diagonal bound");
      Require(MobileTilt.Steering(Vector2.left, Vector2.zero).x == -1, "Tilt direction");
      Require(MobileTilt.Steering(new Vector2(float.NaN, 0), Vector2.zero) == Vector2.zero, "Invalid tilt sample is neutral");
      Require(MobileTilt.Steering(new Vector2(float.PositiveInfinity, 0), Vector2.zero) == Vector2.zero, "Infinite tilt sample is neutral");
      var events = new GameObject("Input check events", typeof(EventSystem));
      var go = new GameObject("Input check joystick", typeof(RectTransform));
      try {
        ((RectTransform)go.transform).sizeDelta = new Vector2(200, 200);
        var joystick = go.AddComponent<SimpleTouchController>();
        var first = new PointerEventData(events.GetComponent<EventSystem>()) { pointerId = 10, position = new Vector2(70, 0) };
        var second = new PointerEventData(events.GetComponent<EventSystem>()) { pointerId = 20, position = new Vector2(-70, 0) };
        joystick.OnPointerDown(first);
        Require(joystick.IsHeld && joystick.GetTouchPosition.x > .9f, "Joystick starts without a drag threshold");
        joystick.OnPointerDown(second); joystick.OnDrag(second); joystick.OnPointerUp(second);
        Require(joystick.IsHeld && joystick.GetTouchPosition.x > .9f, "Second finger cannot steal or release joystick");
        joystick.OnPointerUp(first);
        Require(!joystick.IsHeld && joystick.GetTouchPosition == Vector2.zero, "Owner release resets joystick");
        joystick.OnPointerDown(first);
        // Edit-mode checks invoke the player lifecycle message explicitly.
        joystick.SendMessage("OnDisable");
        Require(!joystick.IsHeld && joystick.GetTouchPosition == Vector2.zero, "Disabled joystick resets");
      } finally { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(events); }

      Debug.Log("Mobile input checks passed: pointer ownership, immediate touch, cancellation, tilt calibration and bounded analog input.");
    }

    public static void RunPrefabIntegration()
    {
      RunLayoutIntegration();
      RunMenuIntegration();
      var events = new GameObject("Prefab input events", typeof(EventSystem));
      var canvasObject = new GameObject("Prefab input canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.GraphicRaycaster));
      var cameraObject = new GameObject("Prefab input camera", typeof(Camera));
      var camera = cameraObject.GetComponent<Camera>();
      var target = new RenderTexture(1024, 768, 24);
      camera.targetTexture = target;
      var raycaster = canvasObject.GetComponent<UnityEngine.UI.GraphicRaycaster>();
      typeof(UnityEngine.EventSystems.BaseRaycaster).GetMethod("OnEnable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(raycaster, null);
      var canvas = canvasObject.GetComponent<Canvas>();
      canvas.renderMode = RenderMode.ScreenSpaceCamera;
      canvas.worldCamera = camera; canvas.planeDistance = 10;
      try {
        var tutorial = UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
          "Assets/Game/GamePlay/Prefabs/Tutorial/GameTutorialUI.prefab"), canvas.transform);
        var keypad = UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
          "Assets/Game/Menu/Prefabs/Keypad/KeypadJoyLeft.prefab"), canvas.transform);
        var mobile = keypad.AddComponent<MobileControls>();
        mobile.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(mobile, null);
        var inputGroup = keypad.GetComponent<UnityEngine.CanvasGroup>();
        Require(!inputGroup.blocksRaycasts, "A replaced keypad starts inert until session permissions are evaluated");
        inputGroup.alpha = 1; inputGroup.interactable = inputGroup.blocksRaycasts = true;
        var joystick = keypad.GetComponentInChildren<SimpleTouchController>();
        joystick.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(joystick, null);
        var analog = joystick.GetComponent<KeyPadJoystickController>();
        analog.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(analog, null);
        Vector2 delivered = Vector2.zero;
        analog.ValueChanged = (x, y) => delivered = new Vector2(x, y);
        var next = tutorial.transform.GetChild(1).GetChild(0).GetComponent<UnityEngine.UI.Button>();
        var skip = tutorial.transform.GetChild(1).GetChild(1).GetComponent<UnityEngine.UI.Button>();
        var body = tutorial.transform.GetChild(0).GetChild(0).GetComponent<UnityEngine.UI.Text>();
        tutorial.AddComponent<Ballance2.Game.GamePlay.Other.TutorialCardLayout>().Initialize(body, next, skip, true);
        var passthrough = tutorial.GetComponentInChildren<Ballance2.UI.Utils.UIRayIgnore>();
        passthrough.GetType().GetMethod("OnDisable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(passthrough, null);
        Require(((RectTransform)next.transform).rect.height / MobileUILayout.Unit(next.transform) >= 48, "Tutorial action is at least 48 logical pixels high");
        var placement = typeof(MobileControls).GetMethod("PlaceControl", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var cameraPad = (RectTransform)keypad.transform.Find("KeypadActionButtons");
        float cameraAnchor = cameraPad.anchorMin.x;
        placement.Invoke(null, new object[] { cameraPad, 1f });
        Require(cameraPad.anchorMin.x == cameraAnchor && cameraPad.anchoredPosition.x < 0,
          "Left joystick layout retains its authored camera controls on the right");
        int clicks = 0;
        next.onClick.RemoveAllListeners(); next.onClick.AddListener(() => clicks++);
        Canvas.ForceUpdateCanvases();
        camera.Render();
        var system = events.GetComponent<EventSystem>();
        var data = new PointerEventData(system) { pointerId = 10, button = PointerEventData.InputButton.Left };
        data.position = RectTransformUtility.WorldToScreenPoint(camera, next.transform.position);
        var hits = new System.Collections.Generic.List<RaycastResult>();
        system.RaycastAll(data, hits);
        Require(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == next.gameObject,
          "Real tutorial Continue must be hit before the later-created swipe surface: " + string.Join(", ", hits.ConvertAll(hit => hit.gameObject.name)) + " depth=" + next.GetComponent<UnityEngine.UI.Image>().depth + " position=" + data.position);
        data.pointerPressRaycast = hits[0];
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, data, ExecuteEvents.pointerClickHandler);
        Require(clicks == 1, "Raycast routes Continue once");
        data.position = RectTransformUtility.WorldToScreenPoint(camera, tutorial.transform.GetChild(0).TransformPoint(((RectTransform)tutorial.transform.GetChild(0)).rect.center));
        hits.Clear(); system.RaycastAll(data, hits);
        Require(hits.Count > 0 && ExecuteEvents.GetEventHandler<IDragHandler>(hits[0].gameObject) == tutorial.transform.GetChild(0).gameObject,
          "Tutorial text scroll receives the drag instead of the gameplay swipe surface");
        tutorial.SetActive(false);
        Canvas.ForceUpdateCanvases(); camera.Render();
        var area = (RectTransform)joystick.transform;
        data.position = RectTransformUtility.WorldToScreenPoint(camera, area.TransformPoint(area.rect.center + Vector2.right * area.rect.width * .3f));
        hits.Clear(); system.RaycastAll(data, hits);
        Require(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerDownHandler>(hits[0].gameObject) == joystick.gameObject,
          "Real joystick receives touch above swipe surface");
        data.pointerPressRaycast = hits[0];
        var owner = ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, data, ExecuteEvents.pointerDownHandler);
        Require(joystick.IsHeld && delivered.x > .7f, "Prefab joystick dispatches movement through its real analog controller");
        ExecuteEvents.Execute(owner, data, ExecuteEvents.pointerUpHandler);
        Require(!joystick.IsHeld && delivered == Vector2.zero, "Prefab pointer release stops movement");
        ExecuteEvents.Execute(owner, data, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(owner, new BaseEventData(system), ExecuteEvents.cancelHandler);
        Require(!joystick.IsHeld && delivered == Vector2.zero, "Cancelled touch stops movement");
        ExecuteEvents.Execute(owner, data, ExecuteEvents.pointerDownHandler);
        keypad.GetComponent<Ballance2.Menu.KeypadUIControl>().Release();
        Require(!joystick.IsHeld && delivered == Vector2.zero, "Menu release stops the real prefab joystick");
        Debug.Log("Mobile prefab integration checks passed: tutorial raycast priority, analog dispatch, release, cancellation and menu cleanup.");
      } finally {
        UnityEngine.Object.DestroyImmediate(canvasObject);
        UnityEngine.Object.DestroyImmediate(events);
        UnityEngine.Object.DestroyImmediate(cameraObject);
        UnityEngine.Object.DestroyImmediate(target);
      }
    }
    private static void RunMenuIntegration()
    {
      var events = new GameObject("Menu events", typeof(EventSystem));
      var canvasObject = new GameObject("Menu canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.GraphicRaycaster));
      var cameraObject = new GameObject("Menu camera", typeof(Camera));
      var camera = cameraObject.GetComponent<Camera>();
      var target = new RenderTexture(320, 522, 24); camera.targetTexture = target;
      var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera;
      canvas.worldCamera = camera; canvas.planeDistance = 10;
      var raycaster = canvasObject.GetComponent<UnityEngine.UI.GraphicRaycaster>();
      typeof(BaseRaycaster).GetMethod("OnEnable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(raycaster, null);
      var hostObject = UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
        "Assets/Game/Menu/Prefabs/PagePrefab/GameUIPageBallanceCommon.prefab"), canvas.transform);
      var host = hostObject.GetComponent<Ballance2.UI.Core.GameUIPage>().ContentHost;
      try {
        foreach (string pageName in new[] { "PageStart", "PageSettingsInGame", "PageGameQuitAsk", "PageGameRestartAsk" }) {
        var menu = UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
          "Assets/Game/Menu/Prefabs/Pages/" + pageName + ".prefab"), host);
        var layout = menu.AddComponent<MobileMenuList>();
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        typeof(MobileMenuList).GetMethod("Initialize", flags).Invoke(layout, null);
        var buttons = menu.GetComponentsInChildren<UnityEngine.UI.Button>();
        Canvas.ForceUpdateCanvases();
        typeof(MobileMenuList).GetMethod("Fit", flags).Invoke(layout, new object[] { buttons.Length });
        Canvas.ForceUpdateCanvases(); camera.Render();
        foreach (var button in buttons) Require(((RectTransform)button.transform).rect.height >= 47.99f, "Original level target is at least 48 pixels");
        var system = events.GetComponent<EventSystem>();
        var data = new PointerEventData(system);
        var hits = new System.Collections.Generic.List<RaycastResult>();
        var first = buttons[0].gameObject;
        data.position = RectTransformUtility.WorldToScreenPoint(camera, first.transform.position);
        system.RaycastAll(data, hits);
        Require(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == first,
          "First level receives touch above the scroll viewport");
        Require(ExecuteEvents.GetEventHandler<IDragHandler>(first) == menu.transform.parent.gameObject,
          "Dragging a level button scrolls the original list");
        var scroll = menu.GetComponentInParent<UnityEngine.UI.ScrollRect>(); scroll.verticalNormalizedPosition = 0;
        Canvas.ForceUpdateCanvases(); camera.Render();
        var back = buttons[buttons.Length - 1];
        data.position = RectTransformUtility.WorldToScreenPoint(camera, back.transform.position); hits.Clear(); system.RaycastAll(data, hits);
        Require(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == back.gameObject,
          "The last original menu action is reachable after scrolling");
        UnityEngine.Object.DestroyImmediate(menu.transform.parent.gameObject);
        }
        var pause = UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
          "Assets/Game/Menu/Prefabs/Pages/PageGamePause.prefab"), host);
        var pauseLayout = pause.AddComponent<MobilePauseLayout>();
        typeof(MobilePauseLayout).GetMethod("Fit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(pauseLayout, null);
        Canvas.ForceUpdateCanvases(); camera.Render();
        Require(host.rect.width >= 295, "Original common menu host expands for the mobile viewport");
        foreach (var button in pause.GetComponentsInChildren<UnityEngine.UI.Button>()) {
          var label = button.GetComponentInChildren<UnityEngine.UI.Text>();
          label.text = RussianLabel("core.ui.RestartLevel");
          Require(label.preferredHeight <= label.rectTransform.rect.height + .1f,
            "Pause action text fits without clipping: " + button.name);
        }
        UnityEngine.Object.DestroyImmediate(pause);
        var settings = UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
          "Assets/Game/Menu/Prefabs/Pages/PageSettingsControlsMobile.prefab"), host);
        var settingsControl = settings.GetComponent<Ballance2.Menu.MobileControlSettingsUIControl>();
        settingsControl.GetType().GetMethod("FitTouch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(settingsControl, null);
        Canvas.ForceUpdateCanvases(); camera.Render();
        foreach (var button in settings.GetComponentsInChildren<UnityEngine.UI.Button>(true)) {
          var rect = (RectTransform)button.transform;
          Require(rect.rect.width >= 47.99f && rect.rect.height >= 47.99f,
            "Control settings target is at least 48 pixels: " + button.name + " " + rect.rect.size);
        }
        Debug.Log("Mobile menu checks passed: real level/settings/confirmation prefabs, 48-pixel targets, action hits and scroll routing.");
      } finally {
        UnityEngine.Object.DestroyImmediate(canvasObject); UnityEngine.Object.DestroyImmediate(events);
        camera.targetTexture = null; UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(cameraObject);
      }
    }

    private static void RunLayoutIntegration()
    {
      var canvasObject = new GameObject("Layout canvas", typeof(RectTransform), typeof(Canvas));
      var root = (RectTransform)canvasObject.transform;
      try {
        foreach (var size in new[] { new Vector2(320, 522), new Vector2(412, 793), new Vector2(568, 274), new Vector2(844, 344) }) {
          root.sizeDelta = size;
          foreach (var name in new[] { "KeypadJoyLeft", "KeypadJoyRight", "KeypadLeft", "KeypadRight", "KeypadCenter" }) {
            var keypad = UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
              "Assets/Game/Menu/Prefabs/Keypad/" + name + ".prefab"), root);
            try {
              var mobile = keypad.AddComponent<MobileControls>();
              typeof(MobileControls).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(mobile, null);
              foreach (float requested in new[] { 1f, 90f, 1000f }) {
                typeof(MobileControls).GetMethod("FitControls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null,
                  new object[] { keypad.GetComponent<Ballance2.Menu.KeypadUIControl>(), requested, 1f });
                Canvas.ForceUpdateCanvases();
                var regions = new System.Collections.Generic.List<Rect>();
                foreach (Transform child in keypad.transform) {
                  if (child.GetComponent<Ballance2.Menu.KeypadGridLayout>() == null && child.GetComponent<SimpleTouchController>() == null) continue;
                  var rect = (RectTransform)child;
                  var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                  var min = (Vector2)root.InverseTransformPoint(corners[0]);
                  var max = (Vector2)root.InverseTransformPoint(corners[2]);
                  var bounds = new Rect(min, max - min);
                  Require(bounds.xMin >= root.rect.xMin - .1f && bounds.xMax <= root.rect.xMax + .1f &&
                    bounds.yMin >= root.rect.yMin && bounds.yMax <= root.rect.yMax,
                    name + " controls fit " + size + " at size " + requested + ": " + child.name + " " + bounds);
                  foreach (var other in regions) Require(!bounds.Overlaps(other), name + " controls do not overlap at " + size + ": " + child.name + " " + bounds + " versus " + other);
                  regions.Add(bounds);
                  var grid = child.GetComponent<Ballance2.Menu.KeypadGridLayout>();
                  if (grid != null) foreach (var item in grid.Items) {
                    Require(item.Item.rectTransform.rect.width >= 47.99f && item.Item.rectTransform.rect.height >= 47.99f,
                      name + " grid targets remain at least 48 logical pixels");
                    if (!item.Item.enabled || item.Item.name == "ButtonUp" || item.Item.name == "ButtonDown") continue;
                    var imageCorners = new Vector3[4]; item.Item.rectTransform.GetWorldCorners(imageCorners);
                    foreach (var corner in imageCorners) {
                      var local = rect.InverseTransformPoint(corner);
                      Require(local.x >= rect.rect.xMin - .1f && local.x <= rect.rect.xMax + .1f &&
                        local.y >= rect.rect.yMin - .1f && local.y <= rect.rect.yMax + .1f,
                        name + " visible arrow stays inside its grid: " + item.Item.name);
                    }
                  }
                }
                var tutorial = UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                  "Assets/Game/GamePlay/Prefabs/Tutorial/GameTutorialUI.prefab"), root);
                try {
                  var next = tutorial.transform.GetChild(1).GetChild(0).GetComponent<UnityEngine.UI.Button>();
                  var skip = tutorial.transform.GetChild(1).GetChild(1).GetComponent<UnityEngine.UI.Button>();
                  tutorial.AddComponent<Ballance2.Game.GamePlay.Other.TutorialCardLayout>().Initialize(
                    tutorial.transform.GetChild(0).GetChild(0).GetComponent<UnityEngine.UI.Text>(), next, skip, true);
                  next.GetComponentInChildren<UnityEngine.UI.Text>().text = RussianLabel("core.ui.Continue");
                  skip.GetComponentInChildren<UnityEngine.UI.Text>().text = RussianLabel("core.ui.TutorialSkip");
                  Canvas.ForceUpdateCanvases();
                  foreach (var action in new[] { next, skip }) {
                    var label = action.GetComponentInChildren<UnityEngine.UI.Text>();
                    Require(label.preferredHeight <= label.rectTransform.rect.height + .1f,
                      name + " tutorial action label fits at " + size + ": " + label.text);
                  }
                  foreach (RectTransform region in tutorial.transform) {
                    var corners = new Vector3[4]; region.GetWorldCorners(corners);
                    var min = (Vector2)root.InverseTransformPoint(corners[0]);
                    var bounds = new Rect(min, (Vector2)root.InverseTransformPoint(corners[2]) - min);
                    foreach (var control in regions) Require(!bounds.Overlaps(control),
                      name + " tutorial stays outside controls at " + size + ": " + bounds + " / " + control);
                  }
                } finally { UnityEngine.Object.DestroyImmediate(tutorial); }
              }
            } finally { UnityEngine.Object.DestroyImmediate(keypad); }
          }
        }
        Debug.Log("Mobile layout checks passed: five saved presets, four viewport sizes and minimum/default/maximum size settings.");
      } finally { UnityEngine.Object.DestroyImmediate(canvasObject); }
    }

    private static string RussianLabel(string key)
    {
      var resources = new System.Xml.XmlDocument();
      resources.Load("Assets/Game/PackageLanguageRes.xml");
      return resources.SelectSingleNode("/I18n/Language[@name='Russian']/Text[@name='" + key + "']").InnerText;
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
  }
}
