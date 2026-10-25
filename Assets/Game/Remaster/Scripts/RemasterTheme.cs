using UnityEngine;
using UnityEngine.UI;
using Ballance2.UI.Core.Controls;

namespace Ballance2.Game.Remaster
{
  public sealed class RemasterTheme : ScriptableObject
  {
    public Font font;
    public static readonly Color Panel = new Color(.035f, .085f, .11f, .94f);
    public static readonly Color Control = new Color(.14f, .25f, .28f);
    public static readonly Color Accent = new Color(.55f, .83f, .74f);
    private static Font cachedFont;
    public static Font Font {
      get {
        if (cachedFont == null) cachedFont = Resources.Load<RemasterTheme>("Remaster/Theme")?.font;
        return cachedFont;
      }
    }
    public static void Place(RectTransform rect, float left, float bottom, float right, float top)
    {
      rect.anchorMin = new Vector2(left, bottom); rect.anchorMax = new Vector2(right, top);
      rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    public static Text Label(Transform parent, string name, int size, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
      var text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
      text.transform.SetParent(parent, false);
      text.font = Font; text.fontSize = size; text.alignment = alignment;
      text.color = new Color(.91f, .95f, .94f); text.raycastTarget = false;
      text.resizeTextForBestFit = true; text.resizeTextMinSize = 14; text.resizeTextMaxSize = size;
      return text;
    }
    public static void Apply(GameObject root, bool panel = false)
    {
      if (panel) {
        var background = root.GetComponent<Image>() ?? root.AddComponent<Image>();
        background.sprite = null;
        background.color = Panel;
        var layout = root.GetComponent<VerticalLayoutGroup>();
        if (layout != null) {
          layout.padding = new RectOffset(32, 32, 24, 24);
          layout.childControlWidth = true;
          layout.childForceExpandWidth = true;
        }
      }
      foreach (var text in root.GetComponentsInChildren<Text>(true)) {
        text.color = new Color(0.91f, 0.95f, 0.94f, text.color.a);
        text.fontStyle = FontStyle.Normal;
        if (Font != null && (text.font == null || !text.font.name.Contains("SourceCode"))) text.font = Font;
        if (text.gameObject.name.IndexOf("Title", System.StringComparison.OrdinalIgnoreCase) >= 0)
          text.fontStyle = FontStyle.Bold;
      }
      foreach (var selectable in root.GetComponentsInChildren<Selectable>(true)) {
        selectable.transition = Selectable.Transition.ColorTint;
        var colors = selectable.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.76f, 0.94f, 0.86f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.57f, 0.81f, 0.71f);
        colors.disabledColor = new Color(0.5f, 0.6f, 0.6f, 0.5f);
        colors.fadeDuration = 0.12f;
        selectable.colors = colors;
        if (selectable.targetGraphic is Image image && (!(selectable is Button) || selectable.GetComponentInChildren<Text>(true) != null)) Surface(image, Control);
      }
      foreach (var button in root.GetComponentsInChildren<Button>(true)) {
        var label = button.GetComponentInChildren<Text>(true);
        if (label != null) {
          label.fontSize = Mathf.Max(18, label.fontSize);
          label.resizeTextForBestFit = true; label.resizeTextMinSize = 14; label.resizeTextMaxSize = label.fontSize;
          var rect = label.rectTransform;
          if (rect.anchorMin.x == 0 && rect.anchorMax.x == 1) { rect.offsetMin = new Vector2(10, rect.offsetMin.y); rect.offsetMax = new Vector2(-10, rect.offsetMax.y); }
        }
      }
      foreach (var field in root.GetComponentsInChildren<InputField>(true)) {
        if (field.targetGraphic is Image image) { image.sprite = null; image.color = new Color(0.06f, 0.14f, 0.16f); }
        field.selectionColor = new Color(0.43f, 0.72f, 0.65f, 0.5f);
      }
      foreach (var slider in root.GetComponentsInChildren<Slider>(true)) {
        Surface(slider.transform.Find("Background")?.GetComponent<Image>(), Control);
        if (slider.fillRect != null) Surface(slider.fillRect.GetComponent<Image>(), Accent);
        if (slider.handleRect != null) {
          Surface(slider.handleRect.GetComponent<Image>(), Color.white);
          slider.handleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 16);
        }
      }
      foreach (var toggle in root.GetComponentsInChildren<Toggle>(true)) {
        Surface(toggle.targetGraphic as Image, Control);
        if (toggle.graphic is Image check) RemasterIcons.Set(check, RemasterIcon.Check);
        if (toggle.graphic != null) {
          var rect = toggle.graphic.rectTransform;
          Place(rect, .22f, .22f, .78f, .78f);
        }
      }
      foreach (var scrollbar in root.GetComponentsInChildren<Scrollbar>(true)) {
        Surface(scrollbar.GetComponent<Image>(), new Color(.06f, .13f, .16f));
        Surface(scrollbar.targetGraphic as Image, Accent);
      }
      foreach (var dropdown in root.GetComponentsInChildren<Dropdown>(true)) {
        if (dropdown.template != null) Surface(dropdown.template.GetComponent<Image>(), Panel);
        var arrow = dropdown.transform.Find("Arrow");
        if (arrow != null) {
          var old = arrow.GetComponent<Image>(); if (old != null) old.enabled = false;
          if (arrow.Find("Chevron") == null) {
            var icon = RemasterIcons.Add(arrow, "Chevron", RemasterIcon.Right);
            icon.rectTransform.localRotation = Quaternion.Euler(0,0,-90);
            Place(icon.rectTransform, 0, 0, 1, 1);
          }
        }
      }
      foreach (var updown in root.GetComponentsInChildren<Updown>(true)) {
        Surface(updown.GetComponent<Image>(), new Color(.06f, .13f, .16f));
        Arrow(updown.DownBtn, "−"); Arrow(updown.UpBtn, "+");
        Place(updown.TextTitle.rectTransform, .04f, .50f, .96f, .98f);
        Place(updown.TextValue.rectTransform, .27f, .02f, .73f, .50f);
        Place((RectTransform)updown.DownBtn.transform, .04f, .05f, .21f, .48f);
        Place((RectTransform)updown.UpBtn.transform, .79f, .05f, .96f, .48f);
        updown.TextTitle.alignment = TextAnchor.MiddleLeft;
        updown.TextTitle.fontSize = 18; updown.TextValue.fontSize = 18;
      }
      foreach (var key in root.GetComponentsInChildren<KeyChoose>(true)) {
        Place(key.Text.rectTransform, .04f, 0, .62f, 1);
        Place(key.TextValue.rectTransform, .65f, 0, .96f, 1);
        key.Text.alignment = TextAnchor.MiddleLeft; key.Text.fontSize = 18;
        key.Text.resizeTextForBestFit = true; key.Text.resizeTextMaxSize = 18; key.Text.resizeTextMinSize = 12;
        key.TextValue.alignment = TextAnchor.MiddleRight; key.TextValue.fontSize = 16;
        key.TextValue.resizeTextForBestFit = true; key.TextValue.resizeTextMaxSize = 16; key.TextValue.resizeTextMinSize = 12;
      }
      foreach (var toggle in root.GetComponentsInChildren<ToggleEx>(true)) {
        Surface(toggle.GetComponent<Image>(), new Color(.06f, .13f, .16f));
        if (toggle.Text != null) {
          Place(toggle.Text.rectTransform, .04f, .5f, .96f, .98f);
          toggle.Text.alignment = TextAnchor.MiddleLeft; toggle.Text.fontSize = 18;
        }
        if (toggle.UseButtonStyle) {
          Place(toggle.CheckedButton.rectTransform, .30f, .05f, .60f, .48f);
          Place(toggle.UnCheckedButton.rectTransform, .64f, .05f, .94f, .48f);
        }
      }
      foreach (var image in root.GetComponentsInChildren<Image>(true)) {
        if (image.sprite != null && image.sprite.texture != null && image.sprite.texture.name.StartsWith("Button")) Surface(image, Control);
        if (image.name == "Panel" || image.name == "PanelContent" || image.name == "ImageIntroductionBg" || image.name == "ImageBg") Surface(image, Panel);
        if (image.name == "ImageBgActive") Surface(image, new Color(.18f,.37f,.36f));
      }
      foreach (var button in root.GetComponentsInChildren<Button>(true)) {
        if (button.name == "TextRefresh") Arrow(button, "↻");
        if (button.GetComponentInChildren<Text>(true) == null) continue;
        Surface(button.targetGraphic as Image, Color.white);
        var colors = button.colors;
        colors.normalColor = Control; colors.highlightedColor = new Color(.25f, .43f, .45f);
        colors.selectedColor = new Color(.29f, .48f, .47f); colors.pressedColor = new Color(.18f, .34f, .36f);
        colors.disabledColor = new Color(.14f, .20f, .22f, .6f); button.colors = colors;
      }
      foreach (var scroll in root.GetComponentsInChildren<ScrollRect>(true))
        Surface(scroll.GetComponent<Image>(), new Color(.035f, .085f, .11f, .5f));
      foreach (var toggle in root.GetComponentsInChildren<ToggleEx>(true)) toggle.UpdateOn();
      if (root.GetComponent<Ballance2.Menu.KeypadUIControl>() != null) RemasterIcons.Keypad(root);
      if (root.name.StartsWith("PageStartCustomLevel") && root.transform.Find("Panel") is RectTransform libraryPanel)
        Place(libraryPanel,.04f,.04f,.96f,.96f);
      var details = root.GetComponent<Ballance2.Menu.PackageManagerContentUIControl>();
      if (details != null) RemasterPackages.Details(details);
      var library = root.GetComponent<Ballance2.Menu.StartCustomLevel>();
      if (library != null) {
        library.IconSuccess = RemasterIcons.Get(RemasterIcon.Check);
        library.IconError = RemasterIcons.Get(RemasterIcon.Close);
        library.NoPreviewImage = RemasterIcons.Get(RemasterIcon.Ball);
        RemasterIcons.Set(library.ImageDepends, RemasterIcon.Check);
        if (library.ImageBigImage.sprite == null || library.ImageBigImage.sprite.texture.name == "no_image_large")
          RemasterIcons.Set(library.ImageBigImage, RemasterIcon.Ball);
      }
      if (root.name.StartsWith("PagePackageManagerTrust"))
        foreach (var image in root.GetComponentsInChildren<Image>(true))
          if (image.name == "Image") RemasterIcons.Set(image, RemasterIcon.Package);
      var error = root.GetComponent<Ballance2.UI.CoreUI.GameGlobalErrorUI>();
      if (error != null) {
        RemasterIcons.Set(root.transform.Find("Panel/Image").GetComponent<Image>(), RemasterIcon.Close);
        Arrow(error.ButtonShowErrorContent,"›"); Arrow(error.ButtonHideErrorContent,"‹");
        Surface(root.transform.Find("Panel/ErrorContent/ImageBackground").GetComponent<Image>(), Panel);
      }
      if (root.name.StartsWith("PageLanguage"))
        foreach (string language in new[] { "Russian", "English", "ChineseSimplified", "ChineseTraditional" }) {
          var button = root.transform.Find("Btn" + language);
          var flag = button.Find("Language flag")?.GetComponent<Image>();
          if (flag == null) {
            flag = new GameObject("Language flag", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            flag.transform.SetParent(button, false);
          }
          flag.sprite = Resources.Load<Sprite>("Remaster/Flags/" + language);
          flag.color = Color.white; flag.preserveAspect = true; flag.raycastTarget = false;
          flag.rectTransform.anchorMin = flag.rectTransform.anchorMax = new Vector2(0,.5f);
          flag.rectTransform.anchoredPosition = new Vector2(38,0); flag.rectTransform.sizeDelta = new Vector2(36,24);
          var label = button.GetComponentInChildren<Text>(true);
          Place(label.rectTransform,0,0,1,1);
          label.rectTransform.offsetMin = new Vector2(70,0); label.rectTransform.offsetMax = new Vector2(-20,0);
          label.alignment = TextAnchor.MiddleLeft;
        }
    }

    public static void Surface(Image image, Color color)
    {
      if (image == null) return;
      image.sprite = null; image.overrideSprite = null; image.type = Image.Type.Simple; image.color = color;
    }

    public static void Arrow(Button button, string symbol)
    {
      if (button == null) return;
      foreach (var image in button.GetComponentsInChildren<Image>(true)) image.enabled = false;
      var background = button.GetComponent<Image>() ?? button.gameObject.AddComponent<Image>();
      background.enabled = true; Surface(background, Control); button.targetGraphic = background;
      var label = button.GetComponentInChildren<Text>(true);
      if (label != null) label.gameObject.SetActive(false);
      var kind = symbol == "↻" ? RemasterIcon.Restart : symbol == "+" ? RemasterIcon.Plus
        : symbol == "−" ? RemasterIcon.Minus : RemasterIcon.Right;
      var icon = RemasterIcons.Add(button.transform, "Direction icon", kind);
      icon.rectTransform.localRotation = Quaternion.Euler(0,0,symbol == "‹" ? 180 : 0);
      Place(icon.rectTransform, .18f, .18f, .82f, .82f);
      Surface(background, Color.white);
      var colors = button.colors; colors.normalColor = Control;
      colors.highlightedColor = colors.selectedColor = new Color(.29f, .48f, .47f);
      colors.pressedColor = new Color(.18f, .34f, .36f); button.colors = colors;
    }
  }
}
