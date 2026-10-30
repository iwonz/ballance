using UnityEngine;
using UnityEngine.UI;

namespace Ballance2.Menu.Touch
{
  /// <summary>Retains pause menu assets while making actions reachable on short touch viewports.</summary>
  public sealed class MobilePauseLayout : MonoBehaviour
  {
    private Button[] buttons;
    private Vector2 screen;
    private float unit;
    private void OnEnable() { if (MobileControls.Available) Fit(); }
    private void LateUpdate()
    {
      if (MobileControls.Available && (screen != new Vector2(Screen.width, Screen.height) || !Mathf.Approximately(unit, MobileUILayout.Unit(transform)))) Fit();
    }
    private void Fit()
    {
      if (buttons == null) {
        buttons = GetComponentsInChildren<Button>(true);
        foreach (var layout in GetComponentsInChildren<LayoutGroup>(true)) layout.enabled = false;
        var spacer = transform.Find("Space");
        if (spacer != null) spacer.gameObject.SetActive(false);
      }
      screen = new Vector2(Screen.width, Screen.height); unit = MobileUILayout.Unit(transform);
      MobileUILayout.FitMenuHost(transform);
      var bounds = ((RectTransform)transform).rect.size / unit;
      int columns = bounds.y < 320 ? 2 : 1;
      int rows = Mathf.CeilToInt(buttons.Length / (float)columns);
      float width = Mathf.Min(columns == 1 ? 360 : 600, bounds.x - 24);
      float cell = (width - (columns - 1) * 8) / columns;
      for (int i = 0; i < buttons.Length; i++) {
        var rect = (RectTransform)buttons[i].transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(cell, 48) * unit;
        rect.anchoredPosition = new Vector2(-width * .5f + cell * .5f + (i % columns) * (cell + 8), (rows - 1) * 28 - (i / columns) * 56) * unit;
        var label = buttons[i].GetComponentInChildren<Text>();
        MobileUILayout.FitButtonLabel(label, unit);
        label.resizeTextForBestFit = false; label.fontSize = Mathf.RoundToInt(18 * unit);
        if (buttons[i].name == "ButtonBack") {
          var localized = label as Ballance2.UI.Core.Controls.I18NText;
          if (localized != null) localized.resourceKey = "core.ui.Continue";
          label.text = Ballance2.Services.I18N.I18N.Tr("core.ui.Continue");
        }
      }
    }
  }
}
