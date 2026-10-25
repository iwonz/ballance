using System.Collections.Generic;
using System.IO;
using Ballance2;
using Ballance2.Base;
using Ballance2.Package;
using Ballance2.Res;
using Ballance2.Services;
using Ballance2.Services.Debug;
using Ballance2.Services.I18N;
using Ballance2.UI.Core;
using Ballance2.UI.Core.Controls;
using Ballance2.UI.Core.ValueBinder;
using UnityEngine;
using UnityEngine.UI;

/*
* Copyright(c) 2022 mengyu
*
* Module:
* PackageManagerContentUIControl.cs
* 
* Purpose:
* Package manager window dialog logic.
*
* Author:
* mengyu
*/


namespace Ballance2.Menu
{
  public class PackageManagerUIControl : GameSingletonBehavior<PackageManagerUIControl> {
    
    private const string TAG = "PackageManagerUIControl";
    private const string PagePackageSettingUIPagePrefixName = "PackageManagerUIControl:";
    private const string PagePackageSettingUIPageDefaultContent = "PagePackageSettingUI.prefab";

    public ScrollView ScrollView;
    public Button ButtonSave;
    public Button ButtonCancel;
    public Button ButtonEnableAll;
    public Button ButtonDisableAll;
    public Text TextStatus;

    public GameObject PrefabButton;
    public GameObject PrefabKeyChoose;
    public GameObject PrefabToogle;
    public GameObject PrefabUpdown;
    public GameObject PrefabInput;
    public GameObject PrefabSlider;
    public GameObject PrefabSubTitle;
    public GameObject PrefabText;

    private GamePackageManager gamePackageManager;
    private GameUIManager gameUIManager;
    private GameUIPage page;
    private bool firstShow = true;

    private void Start() {
      gamePackageManager = GameSystem.GetSystemService<GamePackageManager>();
      gameUIManager = GameSystem.GetSystemService<GameUIManager>();

      ScrollView.SetItemCountFunc(() => packageItems.Count);
      ScrollView.SetUpdateFunc(ItemUpdateFn);
      ScrollView.defaultItemSize = new Vector2((transform as RectTransform).rect.width - 250, 80);

      ButtonEnableAll.onClick.AddListener(() => EnableAll());
      ButtonDisableAll.onClick.AddListener(() => DisableAll());
      ButtonCancel.onClick.AddListener(() => Back());
      ButtonSave.onClick.AddListener(() => Save());

      page = gameUIManager.FindPage("PagePackageManager");
      page.OnShow = (options) => {
        if(firstShow) {
          LoadList();
          firstShow = false;
        }
        if(options.ContainsKey("LocatePackage")) 
          SelectPackage(options["LocatePackage"]);
      };
    }

    private class PackageStateChange {
      public string name;
      public bool newState;
      public PackageStateChange(string name, bool newState) {
        this.name = name;
        this.newState = newState;
      }
    }
    private class PackageInfoItem {
      public string packageName;
      public bool enableLoad;
      public bool notUnLoadable;
      public bool systemPackage;
      public bool hasSettingsUI;
      public string name;
      public string introduction;
      public Sprite logo;
    }
    private Dictionary<string, PackageStateChange> packageStateChanges = new Dictionary<string, PackageStateChange>();
    private List<PackageInfoItem> packageItems = new List<PackageInfoItem>();

    // Entry update callback.
    private void ItemUpdateFn(int index, RectTransform item) {
      
      var data = packageItems[index];

      var Button = item.gameObject.GetComponent<Button>();
      var ImageLogo = item.Find("ImageLogo").GetComponent<Image>();
      var TextTitle = item.Find("TextTitle").GetComponent<Text>();
      var TextIntrod = item.Find("TextIntrod").GetComponent<Text>();
      var TextNotUnloadableText = item.Find("TextNotUnloadableText").GetComponent<Text>();
      var ToggleEnable = item.Find("ToggleEnable").GetComponent<ToggleEx>();
      var ButtonUnload = item.Find("ButtonUnload").GetComponent<Button>();
      var ButtonSettings = item.Find("ButtonSettings").GetComponent<Button>();

      Button.onClick.RemoveAllListeners();
      Button.onClick.AddListener(() => {
        Dictionary<string, string> options = new Dictionary<string, string>();
        options.Add("PackageName", data.packageName);
        gameUIManager.GoPageWithOptions("PagePackageManagerInfo", options);
      });

      TextTitle.text = data.name;
      TextIntrod.text = data.introduction;
      ImageLogo.sprite = data.logo;
      Ballance2.Game.Remaster.RemasterPackages.Row(item,data.packageName);

      // Open settings button.
      if(data.hasSettingsUI) {
        ButtonSettings.gameObject.SetActive(true);
        ButtonSettings.onClick.RemoveAllListeners();
        ButtonSettings.onClick.AddListener(() => {
          gameUIManager.GoPage(PagePackageSettingUIPagePrefixName + data.packageName);
        });
      } else {
        ButtonSettings.gameObject.SetActive(false);
      }

      if(!data.systemPackage) {
        TextNotUnloadableText.gameObject.SetActive(false);

        // Unload button.
        if(data.notUnLoadable)
          ButtonUnload.gameObject.SetActive(false);
        else {
          ButtonUnload.gameObject.SetActive(true);
          ButtonUnload.onClick.RemoveAllListeners();
          ButtonUnload.onClick.AddListener(() => {
            UnloadPackage(data.packageName);
          });
        }

        if(packageStateChanges.TryGetValue(data.packageName, out var pack))
          ToggleEnable.isOn = pack.newState;// Cached state.
        else
          ToggleEnable.isOn = data.enableLoad;

        ToggleEnable.gameObject.SetActive(true);
        ToggleEnable.onValueChanged.RemoveAllListeners();
        ToggleEnable.onValueChanged.AddListener((on) => {
          AddPackageStateChange(data.packageName, on, data.enableLoad);
        });
      }
      else {
        ButtonUnload.gameObject.SetActive(false);
        TextNotUnloadableText.gameObject.SetActive(true);
        TextNotUnloadableText.text = I18N.Tr("core.ui.PackageSystemPackage");
        ToggleEnable.onValueChanged.RemoveAllListeners();
        ToggleEnable.gameObject.SetActive(false);
      }
    }

    // Update status text.
    private void UpdateStatusText() {
      TextStatus.text = I18N.TrF("core.ui.PackageManagerStatus", "", packageItems.Count, gamePackageManager.loadedPackages.Count);
    }

    // Select a package to inspect its information.
    private void SelectPackage(string name) {
      var pack = gamePackageManager.FindRegisteredPackage(name);
      if(pack != null) {
        Dictionary<string, string> options = new Dictionary<string, string>();
        options.Add("PackageName", name);
        gameUIManager.GoPageWithOptions("PagePackageSettings", options);
      }
    }

    private void AddPackageStateChange(string name, bool state, bool intitalState) {
      if(!packageStateChanges.TryGetValue(name, out var pack)) {
        if(state != intitalState) {
          pack = new PackageStateChange(name, state);
          packageStateChanges.Add(name, pack); 
        }
      } else {
        if(state == intitalState)
          packageStateChanges.Remove(name);
        else {
          pack.name = name;
          pack.newState = state;
        }
      }
    }
    private void AddPackageToList(GamePackageManager.GamePackageRegisterInfo info) {
      if(info.package != null) {
        
        if(packageItems.Find((i) => i.packageName == info.packageName) != null)
          return;

        var package = info.package;
        PackageInfoItem newItem = new PackageInfoItem();
        newItem.packageName = info.packageName;
        newItem.logo = package.BaseInfo.LogoTexture != null ? package.BaseInfo.LogoTexture : GameStaticResourcesPool.FindStaticAssets<Sprite>("ModIconSuccess");
        newItem.name = package.BaseInfo.Name;
        newItem.introduction = package.BaseInfo.Introduction;
        newItem.notUnLoadable = package.IsNotUnLoadable() || package.IsSystemPackage();
        newItem.systemPackage = package.IsSystemPackage();
        newItem.hasSettingsUI = packageSettingsUIs.ContainsKey(info.packageName);
        newItem.enableLoad = info.enableLoad;

        packageItems.Add(newItem);
      }
    }
    private void RemovePackageFromList(string name) {
      var item = packageItems.Find((i) => i.packageName == name);
      if(item != null)
        packageItems.Remove(item);
    }

    private void UnloadPackage(string name) {
      gameUIManager.GlobalConfirmWindow(I18N.Tr("core.ui.PackageUnaloadTip")  + '\n' + I18N.TrF("core.ui.PackageWantUnaload", packageStateChanges.Count), () => {
        packageStateChanges.Clear();
        gameUIManager.BackPreviusPage();
      }, () => {
        if(gamePackageManager.registeredPackages.TryGetValue(name, out var pack)) {
          if(gamePackageManager.UnLoadPackage(name, true)) {
            if(File.Exists(pack.package.PackageFilePath))
              File.Delete(pack.package.PackageFilePath);
          }
        }
      });
    }
    private void EnableAll() {
      for (var i = 0; i < packageItems.Count; i++)
      {
        var item = packageItems[i];
        if(!item.systemPackage) {
          if(packageStateChanges.ContainsKey(item.name))
            packageStateChanges[item.name].newState = true;
          else
            packageStateChanges.Add(item.name, new PackageStateChange(item.name, true));
        }
      }
      ScrollView.UpdateData(false);
    }
    private void DisableAll() {
      for (var i = 0; i < packageItems.Count; i++)
      {
        var item = packageItems[i];
        if(!item.systemPackage) {
          if(packageStateChanges.ContainsKey(item.name))
            packageStateChanges[item.name].newState = false;
          else
            packageStateChanges.Add(item.name, new PackageStateChange(item.name, false));
        }
      }
      ScrollView.UpdateData(false);
    }

    // Load the list.
    private void LoadList() {
      // Load the list.
      foreach(var pack in gamePackageManager.registeredPackages.Values) 
        AddPackageToList(pack);
      ScrollView.UpdateData(false);
      UpdateStatusText();

      // Listen for package registration and removal.
      GameManager.GameMediator.RegisterEventHandler(GamePackage.GetSystemPackage(), GameEventNames.EVENT_PACKAGE_REGISTERED, TAG, (evtName, param) => {
        var packageName = param[0] as string;
        if(gamePackageManager.registeredPackages.TryGetValue(packageName, out var pack)) {
          AddPackageToList(pack);
          UpdateStatusText();
          if(ScrollView.gameObject.activeSelf)
            ScrollView.UpdateData(false);
        }
        return false;
      });
      GameManager.GameMediator.RegisterEventHandler(GamePackage.GetSystemPackage(), GameEventNames.EVENT_PACKAGE_UNREGISTERED, TAG, (evtName, param) => {
        RemovePackageFromList(param[0] as string);
        UpdateStatusText();
        if(ScrollView.gameObject.activeSelf)
          ScrollView.UpdateData(false);
        return false;
      });
    }

    // Save and return.
    private void Save() { 
      // Remove entries whose state is unchanged.
      List<string> sameNames = new List<string>();
      foreach(var pack in packageItems) {
        if(packageStateChanges.ContainsKey(pack.name) && packageStateChanges[pack.name].newState == pack.enableLoad) 
          sameNames.Add(pack.name);
      }
      foreach(var name in sameNames)
        packageStateChanges.Remove(name);
      sameNames.Clear();

      if(packageStateChanges.Count > 0) {
        // Apply states to the package manager.
        foreach(var change in packageStateChanges) {
          if(gamePackageManager.registeredPackages.ContainsKey(change.Value.name)) 
            gamePackageManager.registeredPackages[change.Value.name].enableLoad = change.Value.newState;
        }
        packageStateChanges.Clear();
        // Save state.
        gamePackageManager.SavePackageRegisterInfo();
        // Restart the game.
        GameManager.Instance.RestartGame();
      } else {
        gameUIManager.BackPreviusPage();
      }
    }
    private void Back() {
      if(packageStateChanges.Count > 0) {
        gameUIManager.GlobalConfirmWindow(I18N.Tr("core.ui.PackageManagerTip")  + '\n' + I18N.TrF("core.ui.PackageManagerChangeTip", packageStateChanges.Count), () => {
          packageStateChanges.Clear();
          gameUIManager.BackPreviusPage();
        }, () => {
        }, I18N.Tr("core.ui.PackageManagerGiveUpSave"), I18N.Tr("core.ui.Back"));
      } else {
        gameUIManager.BackPreviusPage();
      }
    }
  
    // Package settings UI controller.
    public class PackageSettingsUIControlInfo<T> {
      public PackageSettingsUIControlInfo(GameUIControlValueBinderSupplierCallback valueBinderSupplierCallback, T control) {
        ValueBinderSupplierCallback = valueBinderSupplierCallback;
        Control = control;
      }
      public GameUIControlValueBinderSupplierCallback ValueBinderSupplierCallback { private set; get; }
      public T Control { private set; get; }
    }
    public class PackageSettingsUI {
      private GamePackage package;
      private PackageManagerUIControl control;
      private GameUIMessageCenter MessageCenter;
      private RectTransform PageContent;
      private bool isCustomControl;

      /// <summary>
      /// Get the page containing these settings.
      /// </summary>
      /// <value></value>
      public GameUIPage Page { get; private set; }

      internal PackageSettingsUI(GamePackage package, PackageManagerUIControl control, string customPagePrefab, string customPageTemplatePrefab) {
        this.control = control;
        this.package = package;
        isCustomControl = customPagePrefab != null;
        MessageCenter = MenuManager.MessageCenter;
        Page = GameUIManager.Instance.RegisterPage(
          PagePackageSettingUIPagePrefixName + package.PackageName, 
          customPageTemplatePrefab == null ? "PageCommon" : customPageTemplatePrefab
        );
        if (customPagePrefab != null) 
          Page.CreateContent(package, customPagePrefab);
        else {
          Page.CreateContent(GamePackage.GetSystemPackage(), PagePackageSettingUIPageDefaultContent);
          PageContent = Page.Content.Find("ScrollView/Viewport/Content").GetComponent<RectTransform>();
          Page.Content.Find("Title").GetComponent<Text>().text = I18N.TrF("core.ui.PackageManagerSettingUITitle", "{0}", package.BaseInfo.Name);
        }
      } 
      internal void Delete() {
        GameUIManager.Instance.UnRegisterPage(Page.PageName);
      } 

      private bool CheckIsCustomControl() {
        if (isCustomControl) {
          GameErrorChecker.SetLastErrorAndLog(GameError.NotImplemented, TAG, "PackageSettingsUI fail: can not use auto make ui when set customPagePrefab!");
          return true;
        }
        return false;
      }

      /// <summary>
      /// Add a button to the settings UI.
      /// </summary>
      /// <param name="name">Entry name.</param>
      /// <param name="title">Button text.</param>
      /// <param name="onClick">Button click callback.</param>
      /// <returns></returns>
      public PackageSettingsUIControlInfo<Button> AddButton(string name, string title, GameManager.VoidDelegate onClick) {
        if (CheckIsCustomControl())
          return null;
        var item = GameManager.Instance.InstancePrefab(control.PrefabButton, PageContent, name);
        var cls = item.GetComponent<Button>();
        cls.onClick.AddListener(() => onClick());
        Ballance2.Game.Remaster.RemasterTheme.Apply(item);
        item.transform.SetAsFirstSibling();
        var text = item.transform.Find("Text").GetComponent<Text>();
        if (text != null)
          text.text = title;
        return new PackageSettingsUIControlInfo<Button>(null, cls);
      }
      /// <summary>
      /// Add a subtitle to the settings UI.
      /// </summary>
      /// <param name="name">Entry name.</param>
      /// <param name="title">Subtitle text.</param>
      /// <returns></returns>
      public PackageSettingsUIControlInfo<Text> AddSubTitle(string name, string title) {
        if (CheckIsCustomControl())
          return null;

        var item = GameManager.Instance.InstancePrefab(control.PrefabSubTitle, PageContent, name);
        var cls = item.GetComponent<Text>();
        Ballance2.Game.Remaster.RemasterTheme.Apply(item);
        item.transform.SetAsFirstSibling();
        cls.text = title;
        return new PackageSettingsUIControlInfo<Text>(null, cls);
      }
      /// <summary>
      /// Add text to the settings UI.
      /// </summary>
      /// <param name="name">Entry name.</param>
      /// <param name="title">Text content.</param>
      /// <returns></returns>
      public PackageSettingsUIControlInfo<Text> AddText(string name, string title) {
        if (CheckIsCustomControl())
          return null;

        var item = GameManager.Instance.InstancePrefab(control.PrefabText, PageContent, name);
        var cls = item.GetComponent<Text>();
        cls.text = title;
        Ballance2.Game.Remaster.RemasterTheme.Apply(item);
        item.transform.SetAsFirstSibling();
        return new PackageSettingsUIControlInfo<Text>(null, cls);
      }
      /// <summary>
      /// Add a key selector to the settings UI.
      /// </summary>
      /// <param name="name">Entry name.</param>
      /// <param name="title">Title text.</param>
      /// <param name="valueUpdateCallback">Value change callback.</param>
      /// <returns></returns>
      public PackageSettingsUIControlInfo<KeyChoose> AddKeyChoose(string name, string title, GameUIControlValueBinderUserUpdateCallback valueUpdateCallback) {
        if (CheckIsCustomControl())
          return null;

        var item = GameManager.Instance.InstancePrefab(control.PrefabKeyChoose, PageContent, name);
        var valueBinder = item.AddComponent<KeyChooseValueBinder>();
        var cls = item.GetComponent<KeyChoose>();
        cls.Text.defaultString = title;
        valueBinder.MessageCenter = MessageCenter;
        valueBinder.Name = name; 
        Ballance2.Game.Remaster.RemasterTheme.Apply(item);
        item.transform.SetAsFirstSibling();
        return new PackageSettingsUIControlInfo<KeyChoose>(MessageCenter.SubscribeValueBinder(valueBinder, valueUpdateCallback), cls);
      }
      /// <summary>
      /// Add an option selector to the settings UI.
      /// </summary>
      /// <param name="name">Entry name.</param>
      /// <param name="title">Title text.</param>
      /// <param name="valueUpdateCallback">Value change callback.</param>
      /// <returns></returns>
      public PackageSettingsUIControlInfo<Updown> AddUpdown(string name, string title, GameUIControlValueBinderUserUpdateCallback valueUpdateCallback) {
        if (CheckIsCustomControl())
          return null;

        var item = GameManager.Instance.InstancePrefab(control.PrefabUpdown, PageContent, name);
        var cls = item.GetComponent<Updown>();
        var valueBinder = item.AddComponent<UpdownValueBinder>();
        Ballance2.Game.Remaster.RemasterTheme.Apply(item);
        item.transform.SetAsFirstSibling();
        valueBinder.MessageCenter = MessageCenter;
        valueBinder.Name = name; 
        cls.TextTitle.defaultString = title;
        return new PackageSettingsUIControlInfo<Updown>(MessageCenter.SubscribeValueBinder(valueBinder, valueUpdateCallback), cls);
      }
      /// <summary>
      /// Add a toggle to the settings UI.
      /// </summary>
      /// <param name="name">Entry name.</param>
      /// <param name="title">Title text.</param>
      /// <param name="valueUpdateCallback">Value change callback.</param>
      /// <returns></returns>
      public PackageSettingsUIControlInfo<ToggleEx> AddToggle(string name, string title, GameUIControlValueBinderUserUpdateCallback valueUpdateCallback) {
        if (CheckIsCustomControl())
          return null;

        var item = GameManager.Instance.InstancePrefab(control.PrefabToogle, PageContent, name);
        var cls = item.GetComponent<ToggleEx>();
        var valueBinder = item.AddComponent<ToggleExValueBinder>();
        Ballance2.Game.Remaster.RemasterTheme.Apply(item);
        item.transform.SetAsFirstSibling();
        valueBinder.MessageCenter = MessageCenter;
        valueBinder.Name = name; 
        cls.Text.defaultString = title;
        return new PackageSettingsUIControlInfo<ToggleEx>(MessageCenter.SubscribeValueBinder(valueBinder, valueUpdateCallback), cls);
      }
      /// <summary>
      /// Add an input field to the settings UI.
      /// </summary>
      /// <param name="name">Entry name.</param>
      /// <param name="title">Title text.</param>
      /// <param name="valueUpdateCallback">Value change callback.</param>
      /// <returns></returns>
      public PackageSettingsUIControlInfo<InputField> AddInput(string name, GameUIControlValueBinderUserUpdateCallback valueUpdateCallback) {
        if (CheckIsCustomControl())
          return null;

        var item = GameManager.Instance.InstancePrefab(control.PrefabInput, PageContent, name);
        var cls = item.GetComponent<InputField>();
        var valueBinder = item.AddComponent<InputFieldValueBinder>();
        Ballance2.Game.Remaster.RemasterTheme.Apply(item);
        item.transform.SetAsFirstSibling();
        valueBinder.MessageCenter = MessageCenter;
        valueBinder.Name = name; 
        return new PackageSettingsUIControlInfo<InputField>(MessageCenter.SubscribeValueBinder(valueBinder, valueUpdateCallback), cls);
      }
      /// <summary>
      /// Add a slider to the settings UI.
      /// </summary>
      /// <param name="name">Entry name.</param>
      /// <param name="title">Title text.</param>
      /// <param name="valueUpdateCallback">Value change callback.</param>
      /// <returns></returns>
      public PackageSettingsUIControlInfo<Slider> AddSlider(string name, GameUIControlValueBinderUserUpdateCallback valueUpdateCallback) {
        if (CheckIsCustomControl())
          return null;

        var item = GameManager.Instance.InstancePrefab(control.PrefabSlider, PageContent, name);
        var cls = item.GetComponent<Slider>();
        var valueBinder = item.AddComponent<SliderValueBinder>();
        Ballance2.Game.Remaster.RemasterTheme.Apply(item);
        item.transform.SetAsFirstSibling();
        valueBinder.MessageCenter = MessageCenter;
        valueBinder.Name = name; 
        return new PackageSettingsUIControlInfo<Slider>(MessageCenter.SubscribeValueBinder(valueBinder, valueUpdateCallback), cls);
      }
    }
    private Dictionary<string, PackageSettingsUI> packageSettingsUIs = new Dictionary<string, PackageSettingsUI>();

    /// <summary>
    /// Register a package settings page.
    /// </summary>
    /// <param name="package">Package.</param>
    /// <param name="customPagePrefab">Custom content prefab name, resolved in this package when nonempty. Using it disables PackageSettingsUI automatic control generation.</param>
    /// <param name="customPageTemplatePrefab">Custom page template; null uses PageCommon.</param>
    /// <returns>Settings page controller for customizing the package settings UI.</returns>
    public PackageSettingsUI RegisterPackageSettingsUI(GamePackage package, string customPagePrefab = null, string customPageTemplatePrefab = null) {
      if (packageSettingsUIs.ContainsKey(package.PackageName)) 
        return packageSettingsUIs[package.PackageName];
      var ui = new PackageSettingsUI(package, this, customPagePrefab, customPageTemplatePrefab);
      packageSettingsUIs.Add(package.PackageName, ui);
      var packageInfo = packageItems.Find((k) => k.packageName == package.PackageName);
      if (packageInfo != null)
        packageInfo.hasSettingsUI = true;
      return ui;
    }
    /// <summary>
    /// Delete a package settings page.
    /// </summary>
    /// <param name="package">Package.</param>
    public bool DeletePackageSettingsUI(GamePackage package) {
      if (!packageSettingsUIs.ContainsKey(package.PackageName)) {
        GameErrorChecker.SetLastErrorAndLog(GameError.AlreadyRegistered, TAG, "DeletePackageSettingsUI " + package.PackageName + "  fail: not register");
        return false;
      }
      var ui = packageSettingsUIs[package.PackageName];
      ui.Delete();
      packageSettingsUIs.Remove(package.PackageName);
      return true;
    }
  }
}
