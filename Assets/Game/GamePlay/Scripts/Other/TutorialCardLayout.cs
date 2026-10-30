using Ballance2.Game.GamePlay;
using Ballance2.Menu.Touch;
using Ballance2.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Ballance2.Game.GamePlay.Other
{
    /// <summary>Adapts the authored tutorial panel without replacing its visual theme.</summary>
    public sealed class TutorialCardLayout : MonoBehaviour
    {
        private RectTransform panel, buttons, viewport;
        private Text text;
        private Button next, skip;
        private CanvasGroup group;
        private ScrollRect scroll;
        private bool mobile;
        private Rect safe;
        private Vector2 screen;
        private float unit;
        private string previousText;
        private float controlTop, controlSideWidth;

        public void Initialize(Text body, Button continueButton, Button skipButton, bool touch)
        {
            text = body; next = continueButton; skip = skipButton; mobile = touch;
            panel = (RectTransform)transform.GetChild(0);
            buttons = (RectTransform)transform.GetChild(1);
            group = gameObject.AddComponent<CanvasGroup>();
            if (!mobile) return;
            var passthrough = panel.GetComponent<Ballance2.UI.Utils.UIRayIgnore>();
            if (passthrough != null) passthrough.enabled = false;
            foreach (var layout in GetComponentsInChildren<LayoutGroup>(true)) layout.enabled = false;
            viewport = new GameObject("Tutorial text viewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            viewport.SetParent(panel, false);
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
            text.transform.SetParent(viewport, false);
            text.resizeTextForBestFit = false;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = true;
            text.rectTransform.anchorMin = new Vector2(0, 1); text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.pivot = new Vector2(.5f, 1); text.rectTransform.anchoredPosition = Vector2.zero;
            scroll = panel.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = text.rectTransform;
            scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
            Fit();
        }

        private void Fit()
        {
            safe = Screen.safeArea; screen = new Vector2(Screen.width, Screen.height);
            MobileUILayout.SafeArea((RectTransform)transform);
            unit = MobileUILayout.Unit(transform);
            Canvas.ForceUpdateCanvases();
            var bounds = ((RectTransform)transform).rect.size / unit;
            bool portrait = bounds.y > bounds.x;
            var controls = transform.parent.GetComponentInChildren<MobileControls>(true);
            controlTop = controls != null ? controls.ControlTop : 200;
            controlSideWidth = controls != null ? controls.ControlSideWidth : 200;
            float width = Mathf.Min(540, bounds.x - (portrait ? 24 : (controlSideWidth + 12) * 2));
            bool stacked = !portrait && width < 220;
            float actionsHeight = stacked ? 104 : 56;
            float bottom = portrait ? Mathf.Max(controlTop, bounds.y - 316) : Mathf.Max(52, controls != null ? controls.CenterControlTop + 8 : 52);
            float height = Mathf.Clamp(bounds.y - bottom - actionsHeight - 72, 64, portrait ? 180 : 220);
            Place(buttons, width, actionsHeight, bottom);
            Place(panel, width, height, bottom + actionsHeight + 8);
            viewport.offsetMin = Vector2.one * 12 * unit;
            viewport.offsetMax = -Vector2.one * 12 * unit;
            text.fontSize = Mathf.RoundToInt(18 * unit);
            PlaceButton(next, stacked ? 0 : .51f, 1); PlaceButton(skip, 0, stacked ? 1 : .49f);
            if (stacked) {
                ((RectTransform)next.transform).anchorMax = new Vector2(1, .46f);
                ((RectTransform)skip.transform).anchorMin = new Vector2(0, .54f);
            }
            previousText = null;
        }
        private void Place(RectTransform rect, float width, float height, float bottom)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
            rect.pivot = new Vector2(.5f, 0);
            rect.sizeDelta = new Vector2(width, height) * unit;
            rect.anchoredPosition = new Vector2(0, bottom * unit);
        }
        private void PlaceButton(Button button, float left, float right)
        {
            var rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(left, 0); rect.anchorMax = new Vector2(right, 1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var label = button.GetComponentInChildren<Text>();
            label.resizeTextForBestFit = false; label.fontSize = Mathf.RoundToInt(16 * unit);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(6, 2) * unit;
            label.rectTransform.offsetMax = new Vector2(-6, -2) * unit;
        }
        private void LateUpdate()
        {
            if (group == null) return;
            bool visible = !GamePlayManager.Instance.IsUserPaused && GameUIManager.Instance.GetCurrentPage() == null;
            group.alpha = visible ? 1 : 0;
            group.interactable = group.blocksRaycasts = visible;
            if (!mobile) return;
            var controls = transform.parent.GetComponentInChildren<MobileControls>(true);
            if ((controls != null && (controlTop != controls.ControlTop || controlSideWidth != controls.ControlSideWidth)) || safe != Screen.safeArea || screen != new Vector2(Screen.width, Screen.height) || !Mathf.Approximately(unit, MobileUILayout.Unit(transform))) Fit();
            if (previousText != text.text) {
                previousText = text.text;
                Canvas.ForceUpdateCanvases();
                text.rectTransform.sizeDelta = new Vector2(0, text.preferredHeight);
                scroll.StopMovement(); scroll.verticalNormalizedPosition = 1;
            }
        }
    }
}
