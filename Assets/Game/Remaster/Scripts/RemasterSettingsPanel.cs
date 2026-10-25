using System;
using System.Collections.Generic;
using Ballance2.Game;
using Ballance2.Services;
using Ballance2.Services.I18N;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ballance2.Game.Remaster
{
  public sealed class RemasterSettingsPanel : MonoBehaviour
  {
    private Font font;
    private RectTransform content;
    private ScrollRect scroll;
    private Text resolution;
    private readonly List<Action> refresh = new List<Action>();
    public static string PresetName(EnvironmentPreset preset) => I18N.Tr("core.ui.Remaster.Preset" + (int)preset);
    private static string Tr(string key) => I18N.Tr("core.ui.Remaster." + key);

    public static void Install(RectTransform root)
    {
      var panel = root.gameObject.AddComponent<RemasterSettingsPanel>();
      panel.font = RemasterTheme.Font ?? root.GetComponentInChildren<Text>(true).font;
      var layout = root.GetComponent<LayoutGroup>();
      if (layout != null) layout.enabled = false;
      foreach (Transform child in root) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
      panel.Build(root);
    }

    private RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
      var result = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
      result.SetParent(parent, false);
      result.anchorMin = min; result.anchorMax = max;
      result.offsetMin = result.offsetMax = Vector2.zero;
      return result;
    }

    private Text Label(Transform parent, string text, int size, TextAnchor alignment, Vector2 min, Vector2 max)
    {
      var label = Rect("Label", parent, min, max).gameObject.AddComponent<Text>();
      label.font = font; label.text = text; label.fontSize = size;
      label.color = new Color(0.91f, 0.95f, 0.94f);
      label.alignment = alignment;
      label.raycastTarget = false;
      label.resizeTextForBestFit = true; label.resizeTextMinSize = 12; label.resizeTextMaxSize = size;
      return label;
    }

    private Button Button(Transform parent, string text, Vector2 min, Vector2 max, UnityAction action)
    {
      var rect = Rect("Button " + text, parent, min, max);
      var image = rect.gameObject.AddComponent<Image>();
      image.color = new Color(0.22f, 0.34f, 0.36f);
      var button = rect.gameObject.AddComponent<Button>();
      button.targetGraphic = image;
      var colors = button.colors;
      colors.highlightedColor = new Color(0.73f, 0.94f, 0.89f);
      colors.selectedColor = colors.highlightedColor;
      colors.pressedColor = new Color(0.55f, 0.79f, 0.73f);
      button.colors = colors;
      button.onClick.AddListener(action);
      if (text == "‹" || text == "›") {
        var icon = RemasterIcons.Add(rect, "Direction", RemasterIcon.Right);
        icon.rectTransform.localRotation = Quaternion.Euler(0,0,text == "‹" ? 180 : 0);
        RemasterTheme.Place(icon.rectTransform,.18f,.18f,.82f,.82f);
      } else Label(rect, text, 20, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
      if (scroll != null) rect.gameObject.AddComponent<RemasterScrollFocus>().Scroll = scroll;
      return button;
    }

    private void Build(RectTransform root)
    {
      var panel = root.GetComponent<Image>() ?? root.gameObject.AddComponent<Image>();
      panel.color = new Color(0.035f, 0.085f, 0.11f, 0.96f);
      Label(root, Tr("Settings"), 28, TextAnchor.MiddleLeft, new Vector2(0.08f, 0.89f), new Vector2(0.92f, 0.98f));
      var viewport = Rect("Viewport", root, new Vector2(0.08f, 0.19f), new Vector2(0.92f, 0.87f));
      viewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.01f);
      viewport.gameObject.AddComponent<RectMask2D>();
      scroll = viewport.gameObject.AddComponent<ScrollRect>();
      scroll.viewport = viewport; scroll.horizontal = false;
      scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30;
      content = Rect("Settings", viewport, new Vector2(0, 1), Vector2.one);
      content.pivot = new Vector2(0.5f, 1);
      var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
      layout.childControlHeight = true; layout.childControlWidth = true;
      layout.childForceExpandHeight = false; layout.spacing = 8;
      content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
      scroll.content = content;
      var scrollbarRect = Rect("Scroll bar", root, new Vector2(.94f,.19f), new Vector2(.95f,.87f));
      scrollbarRect.gameObject.AddComponent<Image>().color = RemasterTheme.Control;
      var scrollbar = scrollbarRect.gameObject.AddComponent<Scrollbar>();
      var handle = Rect("Handle",scrollbarRect,Vector2.zero,Vector2.one);
      var handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = RemasterTheme.Accent;
      scrollbar.handleRect = handle; scrollbar.targetGraphic = handleImage; scrollbar.direction = Scrollbar.Direction.BottomToTop;
      scroll.verticalScrollbar = scrollbar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
      var presetNames = new List<string>();
      foreach (EnvironmentPreset preset in Enum.GetValues(typeof(EnvironmentPreset))) presetNames.Add(PresetName(preset));
      Choice(Tr("Environment"), presetNames.ToArray(), () => (int)RemasterPreferences.Preset,
        value => EnvironmentController.Select((EnvironmentPreset)value));
      Preference("Quality", "graphics.quality", 1, I18N.Tr("core.ui.SettingsQualityLow"), I18N.Tr("core.ui.SettingsQualityMedium"), I18N.Tr("core.ui.SettingsQualityHigh"));
      Preference("Resolution", "graphics.resolution", 0, "Auto", "1920 × 1080", "2560 × 1440 · QHD", "3840 × 2160 · 4K");
      Preference("RenderScale", "graphics.scale", 3, "50%", "67%", "75%", "100%");
      Preference("UIScale", "graphics.uiScale", 1, "85%", "100%", "115%", "130%");
      Preference("Shadows", "graphics.shadows", 1, Tr("Off"), Tr("Medium"), Tr("High"));
      Preference("AA", "graphics.aa", 1, Tr("Off"), "2×", "4×");
      Preference("Effects", "graphics.effects", 1, Tr("Off"), Tr("Medium"), Tr("High"));
      var settings = GameSettingsManager.GetSettings(GamePackageManager.SYSTEM_PACKAGE_NAME);
      Choice(Tr("Clouds"), new[] { Tr("Off"), Tr("On") }, () => settings.GetBool(SettingConstants.SettingsVideoCloud) ? 1 : 0,
        value => { settings.SetBool(SettingConstants.SettingsVideoCloud, value == 1); settings.NotifySettingsUpdate(SettingConstants.SettingsVideoCloud); });
      Choice(I18N.Tr("core.ui.SettingsMenuFullScreen"), new[] { Tr("Off"), Tr("On") }, () => Screen.fullScreen ? 1 : 0,
        value => { settings.SetBool(SettingConstants.SettingsVideoFullScreen, value == 1); Screen.fullScreen = value == 1; });
      Choice(I18N.Tr("core.ui.SettingsMenuVSync"), new[] { Tr("Off"), Tr("On") },
        () => settings.GetInt(SettingConstants.SettingsVideoVsync, 0) > 0 ? 1 : 0,
        value => { settings.SetInt(SettingConstants.SettingsVideoVsync, value); settings.NotifySettingsUpdate(SettingConstants.SettingsVideoVsync); });
      var device = Label(content, "", 14, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one);
      device.gameObject.AddComponent<LayoutElement>().preferredHeight = 72;
      device.gameObject.AddComponent<Ballance2.Menu.GraphicsDeviceInfo>();
      resolution = Label(root, "", 14, TextAnchor.MiddleLeft, new Vector2(0.08f, 0.12f), new Vector2(0.7f, 0.18f));
      Label(root, Tr("PresetNote"), 12, TextAnchor.UpperLeft, new Vector2(0.08f, 0.02f), new Vector2(0.7f, 0.11f));
      var back = Button(root, I18N.Tr("core.ui.Back"), new Vector2(0.75f, 0.04f), new Vector2(0.92f, 0.13f),
        () => GameUIManager.Instance.BackPreviusPage());
      Destroy(back.GetComponent<RemasterScrollFocus>());
      var selection = root.GetComponent<Ballance2.UI.Utils.DefaultSelection>();
      if (selection != null) selection.select = content.GetComponentInChildren<Button>().gameObject;
    }

    private void Preference(string title, string key, int fallback, params string[] options)
    {
      Choice(Tr(title), options, () => RemasterPreferences.GetInt(key, fallback), value => {
        RemasterPreferences.SetInt(key, value); RemasterGraphics.ApplyQuality();
      });
    }

    private void Choice(string title, string[] values, Func<int> get, Action<int> set)
    {
      var row = Rect(title, content, Vector2.zero, Vector2.one);
      row.gameObject.AddComponent<LayoutElement>().preferredHeight = 52;
      Label(row, title, 19, TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(0.4f, 1));
      var value = Label(row, "", 18, TextAnchor.MiddleCenter, new Vector2(0.48f, 0), new Vector2(0.91f, 1));
      Action update = () => value.text = values[Mathf.Clamp(get(), 0, values.Length - 1)];
      void Change(int direction) { set((Mathf.Clamp(get(), 0, values.Length - 1) + direction + values.Length) % values.Length); update(); }
      Button(row, "‹", new Vector2(0.41f, 0.08f), new Vector2(0.48f, 0.92f), () => Change(-1));
      Button(row, "›", new Vector2(0.92f, 0.08f), new Vector2(0.99f, 0.92f), () => Change(1));
      refresh.Add(update); update();
    }

    private void OnEnable() { foreach (var update in refresh) update(); }
    private void Update()
    {
      if (resolution == null) return;
      var size = RemasterGraphics.ActualResolution;
      resolution.text = Tr("Actual") + "  " + size.x + " × " + size.y;
    }
  }

  public sealed class RemasterScrollFocus : MonoBehaviour, ISelectHandler
  {
    public ScrollRect Scroll;
    public void OnSelect(BaseEventData data)
    {
      Canvas.ForceUpdateCanvases();
      float position = -Scroll.content.InverseTransformPoint(transform.position).y;
      float top = Scroll.content.anchoredPosition.y;
      float height = Scroll.viewport.rect.height;
      if (position < top + 30) top = position - 30;
      else if (position > top + height - 30) top = position - height + 30;
      Scroll.content.anchoredPosition = new Vector2(0, Mathf.Clamp(top, 0, Mathf.Max(0, Scroll.content.rect.height - height)));
    }
  }
}
