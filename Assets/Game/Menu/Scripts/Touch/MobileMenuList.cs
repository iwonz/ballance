using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ballance2.Menu.Touch
{
  /// <summary>Scales original menu buttons for touch and scrolls long lists without replacing their artwork.</summary>
  public sealed class MobileMenuList : MonoBehaviour
  {
    private readonly List<RectTransform> rows = new List<RectTransform>();
    private RectTransform viewport, content;
    private ScrollRect scroll;
    private Vector2 screen;
    private float unit;
    private int visibleCount;

    private void LateUpdate()
    {
      if (!MobileControls.Available) return;
      if (viewport == null) Initialize();
      int count = 0;
      foreach (var row in rows) if (row.gameObject.activeSelf) count++;
      if (screen != new Vector2(Screen.width, Screen.height) || visibleCount != count ||
          !Mathf.Approximately(unit, MobileUILayout.Unit(transform))) Fit(count);
    }

    private void Initialize()
    {
      content = (RectTransform)transform;
      var layout = GetComponent<LayoutGroup>();
      if (layout != null) layout.enabled = false;
      foreach (Transform child in transform) {
        if (child.GetComponent<Text>() != null || child.GetComponentInChildren<Button>(true) != null) rows.Add((RectTransform)child);
        else if (child.name == "Space") child.gameObject.SetActive(false);
      }
      viewport = new GameObject("Touch menu viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect)).GetComponent<RectTransform>();
      viewport.SetParent(content.parent, false);
      viewport.GetComponent<Image>().color = Color.clear;
      viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
      content.SetParent(viewport, false);
      content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
      content.pivot = new Vector2(.5f, 1);
      scroll = viewport.GetComponent<ScrollRect>();
      scroll.viewport = viewport; scroll.content = content;
      scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
    }

    private void Fit(int count)
    {
      screen = new Vector2(Screen.width, Screen.height); visibleCount = count;
      unit = MobileUILayout.Unit(transform);
      MobileUILayout.FitMenuHost(transform);
      viewport.offsetMin = Vector2.one * 12 * unit;
      viewport.offsetMax = -Vector2.one * 12 * unit;
      float total = 0;
      foreach (var row in rows) if (row.gameObject.activeSelf) total += (row.GetComponent<Text>() != null ? 80 : 56) * unit;
      float height = Mathf.Max(total, viewport.rect.height);
      content.sizeDelta = new Vector2(0, height); content.anchoredPosition = Vector2.zero;
      float y = (height - total) * .5f;
      foreach (var rect in rows) {
        if (!rect.gameObject.activeSelf) continue;
        bool title = rect.GetComponent<Text>() != null;
        float rowHeight = (title ? 80 : 56) * unit;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1); rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(Mathf.Min(320 * unit, viewport.rect.width), (title ? 80 : 48) * unit);
        rect.anchoredPosition = new Vector2(0, -y - rowHeight * .5f); y += rowHeight;
        if (rect.GetComponent<Button>() == null && !title) {
          var nestedLayout = rect.GetComponent<LayoutGroup>();
          if (nestedLayout != null) nestedLayout.enabled = false;
          var actions = rect.GetComponentsInChildren<Button>(true);
          for (int i = 0; i < actions.Length; i++) {
            var action = (RectTransform)actions[i].transform;
            action.anchorMin = new Vector2(i / (float)actions.Length, 0);
            action.anchorMax = new Vector2((i + 1) / (float)actions.Length, 1);
            action.offsetMin = new Vector2(2 * unit, 0); action.offsetMax = new Vector2(-2 * unit, 0);
          }
        }
        foreach (var label in rect.GetComponentsInChildren<Text>(true)) {
          label.resizeTextForBestFit = false; label.fontSize = Mathf.RoundToInt(16 * unit);
          if (label.transform.parent.GetComponent<Button>() != null) MobileUILayout.FitButtonLabel(label, unit);
          label.horizontalOverflow = HorizontalWrapMode.Wrap;
        }
      }
      scroll.StopMovement(); scroll.verticalNormalizedPosition = 1;
    }
  }
}
