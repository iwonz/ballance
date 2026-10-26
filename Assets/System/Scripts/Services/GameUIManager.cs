using Ballance2.Base;
using Ballance2.Package;
using Ballance2.Res;
using Ballance2.Services.Debug;
using Ballance2.Services.InputManager;
using Ballance2.UI.Core;
using Ballance2.UI.Utils;
using Ballance2.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using static Ballance2.Services.GameManager;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* GameUIManager.cs
* 
* Purpose:
* Manage shared UI functionality.
*
* Author:
* mengyu
*/

namespace Ballance2.Services
{
  /// <summary>
  /// Manage shared UI functionality.
  /// </summary>
  /// <Remarks>
  /// Shared UI functionality:
  /// * Legacy window APIs are not part of this manager; use registered pages for current navigation.
  /// * Page is an exclusive view rather than a concurrently open window; create pages with GameUIManager.RegisterPage.
  /// * MaskBlack provides a global black transition overlay.
  /// * MaskWhite provides a global white transition overlay.
  /// * Global dialogs provide exclusive confirmation/error interactions.
  /// </Remarks>
  public class GameUIManager : GameService<GameUIManager>
  {
    #region Basics

    private static readonly string TAG = "GameUIManager";

    public GameUIManager() : base(TAG) { }

    private GameObject GameUICommonHost;
 
    public override void Destroy()
    {
      Object.Destroy(uiManagerGameObject);

      if(UIRoot != null) {
        GameObject go = null;
        int count = 0;
        for (int i = 0, c = UIRoot.transform.childCount; i < c; i++) {
          go = UIRoot.transform.GetChild(i).gameObject;
          if(go.tag != "Speical" && go.tag != "GameController") {
            Object.Destroy(go);
            count ++;
          }
        }
        Log.D(TAG, "Destroy {0} ui objects", count);
      }
    } 
    
    public override bool Initialize()
    {
      UIRoot = GameManager.Instance.GameCanvas;
      UIFadeManager = UIRoot.gameObject.AddComponent<UIFadeManager>();

      GameManager.GameMediator.RegisterGlobalEvent(GameEventNames.EVENT_UI_MANAGER_INIT_FINISHED);
      // Wait for base initialization.
      GameManager.GameMediator.RegisterEventHandler(GamePackage.GetSystemPackage(),
          GameEventNames.EVENT_BASE_INIT_FINISHED, TAG, (evtName, param) =>
          {
            var GameGlobalMask = UIRoot.transform.Find("GameGlobalMask");
            var GameGlobalDialog = UIRoot.transform.Find("GameGlobalDialog");
            GlobalFadeMaskWhite = GameGlobalMask.Find("GlobalFadeMaskWhite").gameObject.GetComponent<Image>();
            GlobalFadeMaskBlack = GameGlobalMask.Find("GlobalFadeMaskBlack").gameObject.GetComponent<Image>();

            // Hide the initialization overlay.
            var GlobalBlackMask = GameGlobalMask.Find("GlobalBlackMask");
            if (GlobalBlackMask != null) GlobalBlackMask.gameObject.SetActive(false);

            // Black overlay.
            GlobalFadeMaskBlack.gameObject.SetActive(true);
            MaskBlackSet(true);

            //Init all
            InitAllObects();
            InitCommands();
            InitF9CaptureScreenshot();
            InitF8SwitchViews();

            // Update the main manager's Canvas reference.
            GameManager.Instance.GameCanvas = ViewsRectTransform;
            GameGlobalMask.SetAsLastSibling();
            GameGlobalDialog.SetAsLastSibling();
            TopViewsRectTransform.SetAsLastSibling();
            TopWindowsRectTransform.SetAsLastSibling();
            GlobalWindowRectTransform.SetAsLastSibling();
            UIToast.SetAsLastSibling();
            if(Entry.GameEntry.Instance != null && Entry.GameEntry.Instance.GameGlobalIngameLoading != null)
              Entry.GameEntry.Instance.GameGlobalIngameLoading.transform.SetAsLastSibling();
            InitF10FpsSwitch();
            InitGlobalPage();

            // Emit the ready event.
            GameManager.GameMediator.DispatchGlobalEvent(GameEventNames.EVENT_UI_MANAGER_INIT_FINISHED);
            return false;
          });
      // Black overlay on quit.
      GameManager.GameMediator.RegisterEventHandler(GamePackage.GetSystemPackage(),
          GameEventNames.EVENT_BEFORE_GAME_QUIT, TAG, (evtName, param) =>
      {
        MaskBlackFadeIn(0.25f);
        return false;
      });
      return true;
    }

    #region Special key controls

    // F10 toggles the FPS display.
    private void InitF10FpsSwitch() {
      var GameFpsStat = UIRoot.transform.Find("GameFpsStat");
      if(GameFpsStat != null) {
        GameFpsStat.SetAsLastSibling();
        if(!DebugMode) {
          ListenKey(KeyCode.F10, (k, d) => {
            if(d)
              GameFpsStat.gameObject.SetActive(!GameFpsStat.gameObject.activeSelf);
          });
        }
      } 
    }
    // F9 captures a screenshot.
    private void InitF9CaptureScreenshot() {
      ListenKey(KeyCode.F9, (k, d) => { if(d) GameManager.Instance.CaptureScreenshot(); });
    }
    // F8 toggles the top overlay view.
    private void InitF8SwitchViews() {
      ListenKey(KeyCode.F8, (k, d) => {
        if(d) SetUIOverlayVisible(!TopViewsRectTransform.gameObject.activeSelf);
      });
    }

    /// <summary>
    /// Toggle overlay UI visibility.
    /// </summary>
    /// <param name="visible">Whether to show it.</param>
    public void SetUIOverlayVisible(bool visible) {
      if(visible) {
        TopViewsRectTransform.gameObject.SetActive(true);
        ViewsRectTransform.gameObject.SetActive(true);
      } else {
        TopViewsRectTransform.gameObject.SetActive(false);
        ViewsRectTransform.gameObject.SetActive(false);
      }
    }

    #endregion

    /// <summary>
    /// Root Canvas RectTransform.
    /// </summary>
    public RectTransform UIRoot;
    /// <summary>
    /// Fade manager.
    /// </summary>
    public UIFadeManager UIFadeManager;

    private GameObject uiManagerGameObject = null;

    protected override void Update()
    {
      UpdateToastShow();
    }

    // Root management.
    private RectTransform TemporarilyRectTransform;
    private RectTransform GlobalWindowRectTransform;
    private RectTransform PagesRectTransform;
    private RectTransform WindowsRectTransform;
    private RectTransform TopWindowsRectTransform;
    private RectTransform ViewsRectTransform;
    private RectTransform TopViewsRectTransform;
    private RectTransform OthersRectTransform;

    /// <summary>
    /// Root UI RectTransform.
    /// </summary>
    public RectTransform UIRootRectTransform { get; private set; }

    private void InitAllObects()
    {
      UIRootRectTransform = UIRoot.GetComponent<RectTransform>();
      TemporarilyRectTransform = CloneUtils.CreateEmptyUIObjectWithParent(UIRoot.transform, "GameUITemporarily").GetComponent<RectTransform>();
      TemporarilyRectTransform.gameObject.SetActive(false);
      GlobalWindowRectTransform = CloneUtils.CreateEmptyUIObjectWithParent(UIRoot.transform, "GameUIGlobalWindow").GetComponent<RectTransform>();
      PagesRectTransform = CloneUtils.CreateEmptyUIObjectWithParent(UIRoot.transform, "GameUIPages").GetComponent<RectTransform>();
      ViewsRectTransform = CloneUtils.CreateEmptyUIObjectWithParent(UIRoot.transform, "GameViewsRectTransform").GetComponent<RectTransform>();
      TopViewsRectTransform = CloneUtils.CreateEmptyUIObjectWithParent(UIRoot.transform, "GameTopViewsRectTransform").GetComponent<RectTransform>();
      WindowsRectTransform = CloneUtils.CreateEmptyUIObjectWithParent(UIRoot.transform, "GameUIWindow").GetComponent<RectTransform>();
      TopWindowsRectTransform = CloneUtils.CreateEmptyUIObjectWithParent(UIRoot.transform, "GameTopUIWindow").GetComponent<RectTransform>();
      OthersRectTransform = CloneUtils.CreateEmptyUIObjectWithParent(UIRoot.transform, "GameUIOthers").GetComponent<RectTransform>();

      InitAllPrefabs();

      UIAnchorPosUtils.SetUIAnchor(TopViewsRectTransform, UIAnchor.Stretch, UIAnchor.Stretch);
      UIAnchorPosUtils.SetUIPos(TopViewsRectTransform, 0, 0, 0, 0);
      UIAnchorPosUtils.SetUIAnchor(ViewsRectTransform, UIAnchor.Stretch, UIAnchor.Stretch);
      UIAnchorPosUtils.SetUIPos(ViewsRectTransform, 0, 0, 0, 0);
      UIAnchorPosUtils.SetUIAnchor(PagesRectTransform, UIAnchor.Stretch, UIAnchor.Stretch);
      UIAnchorPosUtils.SetUIPos(PagesRectTransform, 0, 0, 0, 0);
      UIAnchorPosUtils.SetUIAnchor(GlobalWindowRectTransform, UIAnchor.Stretch, UIAnchor.Stretch);
      UIAnchorPosUtils.SetUIPos(GlobalWindowRectTransform, 0, 0, 0, 0);
      UIAnchorPosUtils.SetUIAnchor(WindowsRectTransform, UIAnchor.Stretch, UIAnchor.Stretch);
      UIAnchorPosUtils.SetUIPos(WindowsRectTransform, 0, 0, 0, 0);
      UIAnchorPosUtils.SetUIAnchor(TopWindowsRectTransform, UIAnchor.Stretch, UIAnchor.Stretch);
      UIAnchorPosUtils.SetUIPos(TopWindowsRectTransform, 0, 0, 0, 0);

      UIToast = CloneUtils.CloneNewObjectWithParent(GameStaticResourcesPool.FindStaticPrefabs("PrefabToast"), UIRoot.transform, "GlobalUIToast").GetComponent<RectTransform>();
      UIToastImage = UIToast.GetComponent<Image>();
      UIToastText = UIToast.Find("Text").GetComponent<Text>();
      UIToast.gameObject.SetActive(false);
      UIToast.SetAsLastSibling();
      EventTriggerListener.Get(UIToast.gameObject).onClick = (g) => { toastTimeTick = 1; };

      keyListener = KeyListener.Get(UIRoot.gameObject);
      keyListener.DisableWhenUIFocused = false;
    }

    private KeyListener keyListener = null;

    /// <summary>
    /// Listen for a keyboard key once.
    /// </summary>
    /// <param name="code">Key code.</param>
    /// <param name="pressedOrReleased">True listens for press; false listens for release.</param>
    /// <param name="callback">Callback.</param>
    /// <returns>Listener ID; remove it with DeleteKeyListen.</returns>
    public int WaitKey(KeyCode code, bool pressedOrReleased, VoidDelegate callback,params GamepadButton[] gamepadButtons)
    {
      int id = 0;
      id = keyListener.AddKeyListen(code, (key, down) =>
      {
        if (down == pressedOrReleased)
        {
          keyListener.DeleteKeyListen(id);
          if(callback != null) 
            callback();
        }
      }, gamepadButtons);
      return id;
    }

    /// <summary>
    /// Add a keyboard listener.
    /// </summary>
    /// <param name="key">Key code.</param>
    /// <param name="callBack">Callback.</param>
    /// <returns>Listener ID; remove it with DeleteKeyListen.</returns>
    public int ListenKey(KeyCode key, KeyListener.KeyDelegate callBack, params GamepadButton[] gamepadButtons)
    {
      return keyListener.AddKeyListen(key, callBack, gamepadButtons);
    }

    /// <summary>
    /// Remove a key listener.
    /// </summary>
    /// <param name="id">ID returned by the key listener registration.</param>
    public void DeleteKeyListen(int id)
    {
      keyListener.DeleteKeyListen(id);
    }

    /// <summary>
    /// Whether the pointer is over UI.
    /// </summary>
    /// <returns></returns>
    public bool IsUiFocus()
    {
      return EventSystem.current.IsPointerOverGameObject() || GUIUtility.hotControl != 0;
    }

    #endregion

    #region UI Prefab

    private struct UIPrefab
    {
      public GameObject prefab;
      public GameUIPrefabType type;
    }
    private Dictionary<string, UIPrefab> uIPrefabs = new Dictionary<string, UIPrefab>();

    /// <summary>
    /// Get a UI control prefab.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <returns>The named prefab, or null if not found.</returns>
    public GameObject GetUIPrefab(string name, GameUIPrefabType type)
    {
      if (uIPrefabs.ContainsKey(name))
      {
        uIPrefabs.TryGetValue(name, out UIPrefab prefab);
        return prefab.type == type ? prefab.prefab : null;
      }
      return null;
    }

    /// <summary>
    /// Register a UI control prefab.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <param name="perfab">Prefab.</param>
    /// <returns>Whether registration succeeded.</returns>
    public bool RegisterUIPrefab(string name, GameUIPrefabType type, GameObject perfab)
    {
      if (uIPrefabs.ContainsKey(name))
      {
        GameErrorChecker.SetLastErrorAndLog(GameError.AlreadyRegistered, TAG, "UI prefab {0} is already registered.", name);
        return false;
      }
      UIPrefab uiprefab = new UIPrefab();
      uiprefab.type = type;
      uiprefab.prefab = perfab;
      uIPrefabs[name] = uiprefab;
      return true;
    }

    /// <summary>
    /// Remove a registered UI control prefab.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <returns>Whether the operation succeeded.</returns>
    public bool RemoveUIPrefab(string name)
    {
      if (uIPrefabs.ContainsKey(name))
      {
        uIPrefabs.Remove(name);
        return true;
      }
      else
      {
        GameErrorChecker.SetLastErrorAndLog(GameError.NotRegister, TAG, "UI control prefab {0} is not registered.", name);
        return false;
      }
    }

    private void InitAllPrefabs()
    {
    }

    #endregion

    #region Page management

    private Dictionary<string, GameUIPage> pages = new Dictionary<string, GameUIPage>();
    private List<GameUIPage> pageStack = new List<GameUIPage>();
    private GameUIPage currentPage = null;

    /// <summary>
    /// Find a page.
    /// </summary>
    /// <param name="name">Page name.</param>
    /// <returns>Page instance, or null if not found.</returns>
    public GameUIPage FindPage(string name)
    {
      pages.TryGetValue(name, out GameUIPage w);
      return w;
    }

    /// <summary>
    /// Register a page.
    /// </summary>
    /// <param name="name">Page name.</param>
    /// <param name="prefabName">Page template name.</param>
    /// <returns>New page instance, or null on failure; inspect LastError.</returns>
    public GameUIPage RegisterPage(string name, string prefabName)
    {
      if (pages.TryGetValue(name, out GameUIPage pageOld))
      {
        GameErrorChecker.SetLastErrorAndLog(GameError.AlreadyRegistered, TAG, "Page {0} is already registered.", name);
        return pageOld;
      }
      GameObject prefab = GetUIPrefab(prefabName, GameUIPrefabType.Page);
      if (prefab == null)
      {
        GameErrorChecker.SetLastErrorAndLog(GameError.PrefabNotFound, TAG, "Page template {0} was not found.", prefabName);
        return null;
      }
      GameObject go = CloneUtils.CloneNewObjectWithParent(prefab, PagesRectTransform, name);
      GameUIPage page = go.GetComponent<GameUIPage>();
      if (page == null)
      {
        GameErrorChecker.SetLastErrorAndLog(GameError.ClassNotFound, TAG, "Page template has no GameUIPage component.");
        return null;
      }
      page.PageName = name;
      pages.Add(name, page);
      GameManager.Instance.StartCoroutine(LateHidePage(go));
      return page;
    }

    private IEnumerator LateHidePage(GameObject go)
    {
      yield return new WaitForSeconds(0.5f);
      go.SetActive(false);
    }

    /// <summary>
    /// Navigate to a page.
    /// </summary>
    /// <param name="name">Page name.</param>
    /// <returns>Whether navigation succeeded.</returns>
    public bool GoPage(string name)
    {
      return GoPageWithOptions(name, new Dictionary<string, string>());
    }

    /// <summary>
    /// Navigate to a page with options.
    /// </summary>
    /// <param name="name">Page name.</param>
    /// <param name="options">Options passed to the page, available through GameUIPage.LastOption.</param>
    /// <returns>Whether navigation succeeded.</returns>
    public bool GoPageWithOptions(string name, Dictionary<string, string> options)
    {
      if (currentPage != null && currentPage.name == name)
        return true;
      if (!pages.TryGetValue(name, out GameUIPage page))
      {
        GameErrorChecker.SetLastErrorAndLog(GameError.NotRegister, TAG, "Page {0} is not registered.", name);
        return false;
      }

      //Hide old
      if (pageStack.Count > 0) pageStack[pageStack.Count - 1].Hide();
      if (!pageStack.Contains(page)) pageStack.Add(page);

      page.LastOptions = options;
      page.Show();
      currentPage = page;
      return true;
    }

    /// <summary>
    /// Get the currently displayed page.
    /// </summary>
    /// <returns></returns>
    public GameUIPage GetCurrentPage() { return currentPage; }

    /// <summary>
    /// Hide the current page.
    /// </summary>
    /// <returns></returns>
    public void HideCurrentPage()
    {
      if (currentPage != null)
        currentPage.Hide();
    }

    /// <summary>
    /// Close all displayed pages.
    /// </summary>
    public void CloseAllPage()
    {
      foreach (var p in pageStack)
        p.Hide();
      currentPage = null;
      pageStack.Clear();
    }

    /// <summary>
    /// Return to the previous page.
    /// </summary>
    /// <returns>True if back navigation is possible; otherwise false.</returns>
    public bool BackPreviusPage()
    {
      return BackPreviusPageWithOptions(new Dictionary<string, string>());
    }

    /// <summary>
    /// Return to the previous page with options.
    /// </summary>
    /// <param name="options">Return options, available through GameUIPage.LastBackOptions.</param>
    /// <returns>True if back navigation is possible; otherwise false.</returns>
    public bool BackPreviusPageWithOptions(Dictionary<string, string> options)
    {
      if (pageStack.Count > 0)
      {
        // Hide the current page.
        var page = pageStack[pageStack.Count - 1];
        pageStack.RemoveAt(pageStack.Count - 1);

        if(page != null) {
          page.Hide();
          if (pageStack.Count > 0)
          {
            // Show the previous page.
            page = pageStack[pageStack.Count - 1];
            page.LastBackOptions = options;
            page.OnBackFromChild?.Invoke(options);
            page.Show();
            currentPage = page;
          }
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Unregister a page.
    /// </summary>
    /// <param name="name">Page name.</param>
    /// <returns>Whether the operation succeeded.</returns>
    public bool UnRegisterPage(string name)
    {
      if (pages.TryGetValue(name, out GameUIPage page))
      {
        if (page == currentPage && pageStack.Count > 1) BackPreviusPage();
        if (pageStack.Contains(page)) pageStack.Remove(page);
        if(page != null) Object.Destroy(page.gameObject);

        pages.Remove(name);
        return true;
      }
      GameErrorChecker.LastError = GameError.NotRegister;
      return false;
    }

    #endregion

    #region Global dialogs

    private RectTransform UIToast;
    private Image UIToastImage;
    private Text UIToastText;
    private FadeObject UIToastImageFadeObject;
    private FadeObject UIToastTextFadeObject;

    private float toastTimeTick = 0;

    /// <summary>
    /// Show a global toast.
    /// </summary>
    /// <param name="text">Notification text.</param>
    public void GlobalToast(string text)
    {
      GlobalToast(text, text.Length / 30.0f);
    }

    /// <summary>
    /// Show a global toast.
    /// </summary>
    /// <param name="text">Notification text.</param>
    /// <param name="showSec">Display duration in seconds.</param>
    public void GlobalToast(string text, float showSec)
    {
      if (showSec <= 0.5f) showSec = 0.5f;
      ShowToast(text, showSec);
    }
    
    private void ShowToast(string text, float time)
    {
      UIToastText.text = text;
      float h = UIToastText.preferredHeight;
      UIToast.sizeDelta = new Vector2(UIToast.sizeDelta.x, h > 50 ? h : 50);
      UIToast.gameObject.SetActive(true);
      UIToast.SetAsLastSibling();
      toastTimeTick = time + 0.25f;

      if(UIToastTextFadeObject != null) {
        UIToastTextFadeObject.ResetTo(1);
        UIToastTextFadeObject.Delete();
        UIToastTextFadeObject = null;
      } else {
        UIToastTextFadeObject = UIFadeManager.AddFadeIn(UIToastText, 0.26f);
      }
      if(UIToastImageFadeObject != null) {
        UIToastImageFadeObject.ResetTo(1);
        UIToastImageFadeObject.Delete();
        UIToastImageFadeObject = null;
      } else {
        UIToastImageFadeObject = UIFadeManager.AddFadeIn(UIToastImage, 0.26f);
      }
    }
    private void UpdateToastShow()
    {
      if (toastTimeTick > 0)
      {
        toastTimeTick -= Time.deltaTime;
        if (toastTimeTick <= 0)
        {
          if(UIToastTextFadeObject != null) {
            UIToastTextFadeObject.ResetTo(0);
            UIToastTextFadeObject.Delete();
            UIToastTextFadeObject = null;
          } else {
            UIToastTextFadeObject = UIFadeManager.AddFadeOut(UIToastText, 0.4f, false);
          }
          if(UIToastImageFadeObject != null) {
            UIToastImageFadeObject.ResetTo(0);
            UIToastImageFadeObject.Delete();
            UIToastImageFadeObject = null;
          } else {
            UIToastImageFadeObject = UIFadeManager.AddFadeOut(UIToastImage, 0.4f, false);
          }
        }
      }
    }

    private VoidDelegate GlobalConfirmWindoOnConfirm = null;
    private VoidDelegate GlobalConfirmWindoOnCancel = null;

    private void InitGlobalPage() {

    }

    internal void GlobalConfirmWindowCallback(bool isConfirm) {
      if (isConfirm) {
        if (GlobalConfirmWindoOnConfirm != null) {
          GlobalConfirmWindoOnConfirm.Invoke();
          GlobalConfirmWindoOnConfirm = null;
        }
      } else {
        if (GlobalConfirmWindoOnCancel != null) {
          GlobalConfirmWindoOnCancel.Invoke();
          GlobalConfirmWindoOnCancel = null;
        }
      }
    }

    /// <summary>
    /// Show an exclusive global confirmation dialog.
    /// </summary>
    /// <param name="text">Content.</param>
    /// <param name="okText">OK button text.</param>
    /// <param name="cancelText">Cancel button text.</param>
    /// <returns></returns>
    public void GlobalConfirmWindow(string text, VoidDelegate onConfirm, VoidDelegate onCancel, string okText = null, string cancelText = null)
    {
      GlobalConfirmWindoOnConfirm = onConfirm;
      GlobalConfirmWindoOnCancel = onCancel;
      var options = new Dictionary<string, string>();
      options.Add("text", text);
      options.Add("okText", okText ?? I18N.I18N.Tr("core.ui.Ok"));
      options.Add("cancelText", cancelText ??  I18N.I18N.Tr("core.ui.Cancel"));
      GoPageWithOptions("PageGlobalConfirm", options);
    }



    #endregion

    #region Global transition overlays

    private Image GlobalFadeMaskWhite;
    private Image GlobalFadeMaskBlack;

    /// <summary>
    /// Set the global black overlay without animation.
    /// </summary>
    /// <param name="show">True shows the overlay; false hides it.</param>
    public void MaskBlackSet(bool show)
    {
      GlobalFadeMaskBlack.color = new Color(GlobalFadeMaskBlack.color.r, GlobalFadeMaskBlack.color.g, GlobalFadeMaskBlack.color.b, show ? 1.0f : 0f);
      GlobalFadeMaskBlack.gameObject.SetActive(show);
      GlobalFadeMaskBlack.transform.SetAsLastSibling();
    }

    /// <summary>
    /// Set the global white overlay without animation.
    /// </summary>
    /// <param name="show">True shows the overlay; false hides it.</param>
    public void MaskWhiteSet(bool show)
    {
      GlobalFadeMaskWhite.color = new Color(GlobalFadeMaskWhite.color.r, GlobalFadeMaskWhite.color.g, GlobalFadeMaskWhite.color.b, show ? 1.0f : 0f);
      GlobalFadeMaskWhite.gameObject.SetActive(show);
      GlobalFadeMaskWhite.transform.SetAsLastSibling();
    }

    /// <summary>
    /// Fade in the global black overlay.
    /// </summary>
    /// <param name="second">Duration in seconds.</param>
    public void MaskBlackFadeIn(float second)
    {
      UIFadeManager.AddFadeIn(GlobalFadeMaskBlack, second);
      GlobalFadeMaskBlack.transform.SetAsLastSibling();
    }

    /// <summary>
    /// Fade in the global white overlay.
    /// </summary>
    /// <param name="second">Duration in seconds.</param>
    public void MaskWhiteFadeIn(float second)
    {
      UIFadeManager.AddFadeIn(GlobalFadeMaskWhite, second);
      GlobalFadeMaskWhite.transform.SetAsLastSibling();
    }

    /// <summary>
    /// Fade out the global black overlay.
    /// </summary>
    /// <param name="second">Duration in seconds.</param>
    public void MaskBlackFadeOut(float second)
    {
      UIFadeManager.AddFadeOut(GlobalFadeMaskBlack, second, true);
      GlobalFadeMaskBlack.transform.SetAsLastSibling();
    }

    /// <summary>
    /// Fade out the global white overlay.
    /// </summary>
    /// <param name="second">Duration in seconds.</param>
    public void MaskWhiteFadeOut(float second)
    {
      UIFadeManager.AddFadeOut(GlobalFadeMaskWhite, second, true);
      GlobalFadeMaskWhite.transform.SetAsLastSibling();
    }

    #endregion

    #region Shared management

    /// <summary>
    /// Move UI to the temporary area.
    /// </summary>
    /// <param name="view">UI transform.</param>
    public void SetViewToTemporarily(RectTransform view)
    {
      view.SetParent(TemporarilyRectTransform.gameObject.transform);
    }
    /// <summary>
    /// Attach UI to the main Canvas.
    /// </summary>
    /// <param name="view">UI transform.</param>
    public void AttatchViewToCanvas(RectTransform view)
    {
      view.SetParent(UIRoot.gameObject.transform);
    }
    /// <summary>
    /// Instantiate a prefab and attach it to the main Canvas.
    /// </summary>
    /// <param name="prefab">Prefab</param>
    /// <param name="name">New object name.</param>
    /// <returns>RectTransform of the new object.</returns>
    public RectTransform InitViewToCanvas(GameObject prefab, string name, bool topMost)
    {
      GameObject go = CloneUtils.CloneNewObjectWithParent(prefab, (topMost ? TopViewsRectTransform : ViewsRectTransform).transform, name);
      RectTransform view = go.GetComponent<RectTransform>();
      view.SetParent((topMost ? TopViewsRectTransform : ViewsRectTransform).gameObject.transform);
      return view;
    }

    /// <summary>
    /// Create a UI message center.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <returns>UI message center instance.</returns>
    public GameUIMessageCenter CreateUIMessageCenter(string name)
    {
      var old = GameUIMessageCenter.FindGameUIMessageCenter(name);
      if (old == null)
      {
        var go = CloneUtils.CreateEmptyObjectWithParent(OthersRectTransform.transform, "GameUIMessageCenter:" + name);
        old = go.AddComponent<GameUIMessageCenter>();
        old.Name = name;
      }
      return old;
    }
    /// <summary>
    /// Destroy a UI message center.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <returns>Whether the operation succeeded.</returns>
    public bool DestroyUIMessageCenter(string name)
    {
      var old = GameUIMessageCenter.FindGameUIMessageCenter(name);
      if (old == null)
      {
        Object.Destroy(old);
        return true;
      }
      return false;
    }
    #endregion

    #region Debug commands

    private void InitCommands()
    {
      GameManager.Instance.GameDebugCommandServer.RegisterCommand("um", (keyword, full, argsCount, args) =>
      {
        switch (args[0])
        {
          case "page":
            {
              switch (args[1])
              {
                case "current":
                  Log.V(TAG, "CurrentPage: {0}", currentPage == null ? "null" : currentPage.PageName);
                  break;
                case "stack":
                  {
                    StringBuilder sb = new StringBuilder("CurrentPage: Stack {0}");
                    sb.Append(pageStack.Count);
                    sb.Append("\n");
                    foreach (var p in pageStack)
                      sb.AppendLine(string.Format("{0} => Visible: {1}", p.PageName, p.gameObject.activeInHierarchy));
                    Log.V(TAG, sb.ToString());
                    break;
                  }
                case "hide-cur":
                  HideCurrentPage();
                  Log.V(TAG, "OK");
                  break;
                case "close-all":
                  CloseAllPage();
                  Log.V(TAG, "OK");
                  break;
                case "back":
                  Log.V(TAG, BackPreviusPage() ? "OK" : "No page can back");
                  break;
                case "go":
                  string name = "";
                  if (!DebugUtils.CheckDebugParam(2, args, out name)) break;
                  if (GoPage(name)) Log.V(TAG, "OK");
                  break;
              }
              break;
            }
          case "toast":
            {
              string text; float sec;
              if (!DebugUtils.CheckDebugParam(1, args, out text)) return false;
              DebugUtils.CheckFloatDebugParam(2, args, out sec, false, 1);
              GlobalToast(text, sec);
              return true;
            }
        }
        return false;
      }, 2, "um <page/toast>\n" +
              "  page\n" +
              "    current Show the current page\n" +
              "    stack Show the page stack\n" +
              "    go <name:string> Go to the specified page\n" +
              "    hide-cur Hide the current page\n" +
              "    close-all Close all visible pages\n" +
              "    back Return to the previous page\n" +
              "  toast <text:string> [showSecond:number] Test a global toast notification\n"
      );
    }

    #endregion
  }

  /// <summary>
  /// UI prefab type.
  /// </summary>
  public enum GameUIPrefabType
  {
    /// <summary>
    /// Control.
    /// </summary>
    Control,
    /// <summary>
    /// Page.
    /// </summary>
    Page
  }
}
