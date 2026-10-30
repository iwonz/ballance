using System.Collections.Generic;
using Ballance2.Game;
using Ballance2.Game.GamePlay;
using Ballance2.Package;
using Ballance2.Services;
using Ballance2.Services.I18N;
using Ballance2.Services.InputManager;
using Ballance2.UI.Core;
using Ballance2.UI.Core.Controls;
using UnityEngine;
using UnityEngine.UI;

namespace Ballance2.Menu
{
  public class MobileControlSettingsUIControl : MonoBehaviour {
    public ScrollView ScrollView;

    private class MobileControlItem {
      public string name;
      public string explain;
      public Sprite image;
    }
    private List<MobileControlItem> items = new List<MobileControlItem>();
    private Vector2 touchScreen;
    private float touchUnit;

    private void LateUpdate() {
      if (Touch.MobileControls.Available && (touchScreen != new Vector2(Screen.width, Screen.height) ||
          !Mathf.Approximately(touchUnit, Touch.MobileUILayout.Unit(transform)))) FitTouch();
    }

    private static void Rect(RectTransform rect, float left, float bottom, float right, float top) {
      rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
      rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top);
    }

    private void FitTouch() {
      touchScreen = new Vector2(Screen.width, Screen.height); touchUnit = Touch.MobileUILayout.Unit(transform);
      Touch.MobileUILayout.FitMenuHost(transform);
      var layout = GetComponent<LayoutGroup>(); if (layout != null) layout.enabled = false;
      foreach (Transform child in transform) if (child.name == "Space") child.gameObject.SetActive(false);
      var bounds = ((RectTransform)transform).rect.size / touchUnit;
      bool landscape = Screen.width > Screen.height;
      float side = landscape ? bounds.x * .55f : 0;
      var list = (RectTransform)ScrollView.transform;
      Rect(list, 0, (landscape ? 56 : 208) * touchUnit, landscape ? (bounds.x - side + 8) * touchUnit : 0, 48 * touchUnit);
      if (ScrollView.verticalScrollbar != null) ScrollView.verticalScrollbar.gameObject.SetActive(false);
      Rect(ScrollView.viewport, 0, 0, 0, 0);
      ScrollView.defaultItemSize = new Vector2(list.rect.width, 80 * touchUnit);
      Rect((RectTransform)transform.Find("Text"), 0, (bounds.y - 40) * touchUnit, 0, 0);
      Rect((RectTransform)transform.Find("ButtonBack"), 0, 0, 0, (bounds.y - 48) * touchUnit);
      var reverse = transform.Find("ButtonToogleControlReverse").GetComponent<ToggleEx>();
      var size = transform.Find("ButtonControlUISize").GetComponent<Updown>();
      float reverseBottom = landscape ? Mathf.Max(136, bounds.y - 128) : 128;
      Rect((RectTransform)reverse.transform, side * touchUnit, reverseBottom * touchUnit, 0, (bounds.y - reverseBottom - 72) * touchUnit);
      Rect((RectTransform)size.transform, side * touchUnit, 56 * touchUnit, 0, (bounds.y - 128) * touchUnit);
      Rect(reverse.Text.rectTransform, 0, 48 * touchUnit, 0, 0);
      float half = ((RectTransform)reverse.transform).rect.width * .5f;
      Rect(reverse.CheckedButton.rectTransform, 0, 0, half + 2 * touchUnit, 24 * touchUnit);
      Rect(reverse.UnCheckedButton.rectTransform, half + 2 * touchUnit, 0, 0, 24 * touchUnit);
      Rect(size.TextTitle.rectTransform, 0, 48 * touchUnit, 0, 0);
      float width = ((RectTransform)size.transform).rect.width;
      Rect((RectTransform)size.DownBtn.transform, 0, 0, width - 48 * touchUnit, 24 * touchUnit);
      Rect((RectTransform)size.UpBtn.transform, width - 48 * touchUnit, 0, 0, 24 * touchUnit);
      Rect(size.TextValue.rectTransform, 48 * touchUnit, 0, 48 * touchUnit, 24 * touchUnit);
      foreach (var label in GetComponentsInChildren<Text>(true)) {
        label.resizeTextForBestFit = false; label.fontSize = Mathf.RoundToInt(16 * touchUnit);
      }
      reverse.Text.fontSize = size.TextTitle.fontSize = Mathf.RoundToInt((landscape ? 12 : 14) * touchUnit);
      if (ScrollView.updateFunc != null) ScrollView.UpdateData(false);
    }

    private void Start() {
      var settings = GameSettingsManager.GetSettings(GamePackageManager.SYSTEM_PACKAGE_NAME);
      var page = GameUIManager.Instance.FindPage("PageSettingsControlsMobile");
      var controlKeypadSettting = "";

      ScrollView.updateFunc = (index, item) => {
        var data = items[index];
        // EventTrigger swallows drag events before the list can scroll.
        item.GetComponent<EventTriggerListener>().enabled = false;
        var button = item.GetComponent<Button>();
        if (button == null) button = item.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.RemoveAllListeners();
        var ImageBg = item.Find("ImageBg");
        var text = item.Find("Text").GetComponent<Text>();
        var ImageBgActive = item.Find("ImageBgActive");
        var image = item.Find("Image").GetComponent<Image>();

        if (controlKeypadSettting == data.name) {
          ImageBg.gameObject.SetActive(false);
          ImageBgActive.gameObject.SetActive(true);
        } else {
          ImageBg.gameObject.SetActive(true);
          ImageBgActive.gameObject.SetActive(false);
        }

        text.text = data.explain;
        image.sprite = data.image;
        image.rectTransform.anchorMin = new Vector2(.03f, .1f);
        image.rectTransform.anchorMax = new Vector2(.35f, .9f);
        image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
        text.rectTransform.anchorMin = new Vector2(.38f, .1f);
        text.rectTransform.anchorMax = new Vector2(.97f, .9f);
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        image.preserveAspect = true;
        float unit = Touch.MobileControls.Available ? Touch.MobileUILayout.Unit(item) : 1;
        text.resizeTextForBestFit = true; text.resizeTextMinSize = Mathf.RoundToInt(16 * unit); text.resizeTextMaxSize = Mathf.RoundToInt(20 * unit);

        button.onClick.AddListener(() => {
          controlKeypadSettting = data.name;
          settings.SetString(SettingConstants.SettingsControlKeypad, data.name);
          PlayerPrefs.Save();

          // Refresh the list.
          ScrollView.UpdateData(false);

          // Replace the touch keyboard dynamically if the game is running.
          if (GamePlayUIControl.Instance != null)
            GamePlayUIControl.Instance.ReBuildMobileKeyPad();
          if (data.name == "Tilt") Touch.MobileTilt.Start(true); else Touch.MobileTilt.Stop();
        });
      };
      ScrollView.itemCountFunc = () => {
        return items.Count;
      };

      page.OnShow = (o) => {
        controlKeypadSettting = settings.GetString(SettingConstants.SettingsControlKeypad, "BaseJoyLeft");
        LoadList();
        ScrollView.UpdateData(false);
      };
    }
    private void LoadList() {
      items.Clear();
      foreach (var item in KeypadUIManager.GetAllKeypad())
      {
        items.Add(new MobileControlItem() {
          name = item.Key,
          image = item.Value.image,
          explain = item.Value.explain,
        });
      }
    }
  }
}
