using System;
using Ballance2.Services.I18N;
using UnityEngine;
using UnityEngine.UI;

namespace Ballance2.Game.Remaster
{
  public sealed class RemasterMenuLayout : MonoBehaviour
  {
    private bool levels;
    private RectTransform root, shade;
    private Text title, subtitle;
    private Button replay;
    private bool ready;
    private static readonly string[] mainButtons = { "ButtonStart", "ButtonHighscroll", "ButtonSettings", "ButtonAbout", "ButtonQuit" };

    public static void Install(RectTransform root, bool levels, Action replayTutorial = null)
    {
      var view = root.gameObject.AddComponent<RemasterMenuLayout>();
      view.root = root; view.levels = levels;
      var layout = root.GetComponent<LayoutGroup>();
      if (layout != null) layout.enabled = false;
      root.GetComponent<Image>().color = levels ? new Color(.035f, .085f, .11f, .94f) : Color.clear;
      foreach (Transform child in root) if (child.name == "Space") child.gameObject.SetActive(false);
      view.title = RemasterTheme.Label(root, "Edition title", 46);
      view.title.alignment = TextAnchor.MiddleCenter;
      view.title.fontStyle = FontStyle.Bold;
      view.title.resizeTextForBestFit = true;
      view.title.resizeTextMinSize = 24; view.title.resizeTextMaxSize = 46;
      view.subtitle = RemasterTheme.Label(root, "Edition description", 19);
      view.subtitle.alignment = TextAnchor.MiddleCenter;
      view.subtitle.color = new Color(.66f, .84f, .78f);
      if (levels) {
        var go = new GameObject("Replay tutorial", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(root, false);
        view.replay = go.GetComponent<Button>();
        view.replay.targetGraphic = go.GetComponent<Image>();
        view.replay.onClick.AddListener(() => replayTutorial());
        var text = RemasterTheme.Label(go.transform, "Label", 18, TextAnchor.MiddleCenter);
        RemasterTheme.Place(text.rectTransform, .04f, .05f, .96f, .95f);
        RemasterTheme.Apply(go);
      } else {
        var panel = new GameObject("Menu shade", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root, false); panel.transform.SetAsFirstSibling();
        view.shade = panel.GetComponent<RectTransform>();
        panel.GetComponent<Image>().color = new Color(.035f, .085f, .11f, .88f);
      }
      view.ready = true;
      view.Refresh();
    }

    private void OnEnable() { if (ready) Refresh(); }
    private void OnRectTransformDimensionsChange() { if (ready) Arrange(); }
    private void Refresh()
    {
      title.text = levels ? I18N.Tr("core.ui.Remaster.Levels") : "REBALLANCE";
      subtitle.text = levels ? RemasterSettingsPanel.PresetName(RemasterPreferences.Preset) : I18N.Tr("core.ui.Remaster.Tagline");
      if (replay != null) replay.GetComponentInChildren<Text>().text = I18N.Tr("core.ui.Remaster.ReplayTutorial");
      Arrange();
    }

    private void Position(string name, float left, float bottom, float right, float top)
    {
      var child = root.Find(name) as RectTransform;
      if (child == null) return;
      RemasterTheme.Place(child, left, bottom, right, top);
      foreach (var text in child.GetComponentsInChildren<Text>(true)) {
        text.fontSize = levels ? 21 : 24;
        text.resizeTextForBestFit = true; text.resizeTextMinSize = 14; text.resizeTextMaxSize = text.fontSize;
      }
    }

    private void Arrange()
    {
      bool portrait = root.rect.width < root.rect.height * 1.1f;
      if (!levels) {
        float halfWidth = Mathf.Min(.5f, 230f / Mathf.Max(1, root.rect.width));
        float left = .5f - halfWidth, right = .5f + halfWidth;
        RemasterTheme.Place(shade, left, 0, right, 1);
        float padding = halfWidth * .14f;
        left += padding; right -= padding;
        RemasterTheme.Place(title.rectTransform, left, .75f, right, .96f);
        RemasterTheme.Place(subtitle.rectTransform, left, .66f, right, .74f);
        for (int i = 0; i < mainButtons.Length; i++) {
          float top = .61f - i * .108f;
          Position(mainButtons[i], left, top - .085f, right, top);
        }
        return;
      }
      RemasterTheme.Place(title.rectTransform, .06f, .89f, .94f, .98f);
      RemasterTheme.Place(subtitle.rectTransform, .06f, .83f, .94f, .89f);
      int columns = portrait ? 3 : 4;
      int rows = (13 + columns - 1) / columns;
      float width = .88f / columns, height = .62f / rows;
      for (int i = 0; i < 13; i++) {
        float x = .06f + i % columns * width, y = .80f - i / columns * height;
        Position(i == 12 ? "ButtonLightZone" : "ButtonLevel" + (i + 1), x, y - height + .016f, x + width - .016f, y);
      }
      Position("ButtonCustom", .06f, .09f, .26f, .16f);
      Position("ButtonNMO", .28f, .09f, .48f, .16f);
      RemasterTheme.Place((RectTransform)replay.transform, .50f, .09f, .72f, .16f);
      Position("ButtonBack", .74f, .09f, .94f, .16f);
    }
  }
}
