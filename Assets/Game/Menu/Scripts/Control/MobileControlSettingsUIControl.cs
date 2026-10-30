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
        text.resizeTextForBestFit = true; text.resizeTextMinSize = 14; text.resizeTextMaxSize = 22;

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
