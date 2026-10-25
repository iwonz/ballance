using UnityEngine;
using UnityEngine.UI;
using Ballance2.Services;
using static Ballance2.Services.GameManager;
using Ballance2.Package;
using Ballance2.Utils;
using Ballance2.Services.Debug;
using Ballance2.UI.Utils;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.LowLevel;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* GameUIPage.cs
* 
* Purpose:
* UI page instance.
* Pages are exclusive views, unlike multiple concurrently open windows.
* Create pages through GameUIManager.RegisterPage.
* 
* Author:
* mengyu
*/

namespace Ballance2.UI.Core
{
  /// <summary>
  /// UI page instance.
  /// </summary>
  public class GameUIPage : MonoBehaviour
  {
    public RectTransform Content;
    public RectTransform ContentHost;
    public VerticalLayoutGroup VerticalLayoutGroup;
    public HorizontalLayoutGroup HorizontalLayoutGroup;

    /// <summary>
    /// Page name.
    /// </summary>
    [Tooltip("Page name.")]
    public string PageName;
    /// <summary>
    /// Allow Escape to return to the previous page.
    /// </summary>
    [Tooltip("Allow Escape to return to the previous page.")]
    public bool CanEscBack = true;
    /// <summary>
    /// Page shown.
    /// </summary>
    public OptionsDelegate OnShow;
    /// <summary>
    /// Page revisited through back navigation.
    /// </summary>
    public OptionsDelegate OnBackFromChild;
    /// <summary>
    /// Page hidden.
    /// </summary>
    public VoidDelegate OnHide;

    private DefaultSelection defaultSelection = null;

    /// <summary>
    /// Options from the last forward navigation.
    /// </summary>
    public Dictionary<string, string> LastOptions;
    /// <summary>
    /// Options returned from a child page.
    /// </summary>
    public Dictionary<string, string> LastBackOptions;

    public delegate void OptionsDelegate(Dictionary<string, string> options);

    private GameUIManager uIManager;
    private int escBackId = 0;

    /// <summary>
    /// Show directly, bypassing GameUIManager's navigation stack; prefer using the manager.
    /// </summary>
    public void Show()
    {
      uIManager = GameManager.GetSystemService<GameUIManager>();
      gameObject.SetActive(true);
      OnShow?.Invoke(LastOptions);

      // Select the default button.
      if(defaultSelection != null) {
        // Do not play a sound for automatic selection.
        var sound = defaultSelection.select.GetComponent<ClickSound>();
        if(sound != null)
          sound.enabled = false;
        EventSystem.current.SetSelectedGameObject(defaultSelection.select);
        if(sound != null)
          sound.enabled = true;
      }

      if (CanEscBack)
      {
        escBackId = uIManager.WaitKey(KeyCode.Escape, false, () =>
        {
          uIManager.BackPreviusPage();
        }, GamepadButton.B);
      }
    }

    /// <summary>
    /// Hide directly, bypassing GameUIManager's navigation stack; prefer using the manager.
    /// </summary>
    public void Hide()
    {
      gameObject.SetActive(false);
      OnHide?.Invoke();
      if (escBackId != 0)
      {
        uIManager.DeleteKeyListen(escBackId);
        escBackId = 0;
      }
    }

    /// <summary>
    /// Create content from a resource.
    /// </summary>
    /// <param name="package">Package containing the resource.</param>
    /// <param name="resourceName">Content template resource path.</param>
    public void CreateContent(GamePackage package, string resourceName)
    {
      var go = CloneUtils.CloneNewObjectWithParent(package.GetPrefabAsset(resourceName), ContentHost, PageName);
      if (go == null)
      {
        GameErrorChecker.SetLastErrorAndLog(GameError.PrefabNotFound, "GameUIPage", "Can not create page content for {0}, The prefab {1} not found", PageName, resourceName);
        return;
      }
      var content = go.GetComponent<RectTransform>();
      if (content != null)
      {
        Content = content;
        UIAnchorPosUtils.SetUIAnchor(content, UIAnchor.Stretch, UIAnchor.Stretch);
        UIAnchorPosUtils.SetUIPos(content, 0, 0, 0, 0);

        defaultSelection = content.GetComponent<DefaultSelection>();
        Ballance2.Game.Remaster.RemasterTheme.Apply(go, PageName != "PageEndScore");
      }
    }

    /// <summary>
    /// Create content automatically from the page name.
    /// </summary>
    /// <param name="package">Package containing the resource.</param>
    public void CreateContent(GamePackage package)
    {
      CreateContent(package, PageName + ".prefab");
    }
    /// <summary>
    /// Create content from the page prefab.
    /// </summary>
    /// <param name="prefab">Prefab.</param>
    public void CreateContent(GameObject prefab)
    {
      var go = CloneUtils.CloneNewObjectWithParent(prefab, ContentHost, PageName);
      var content = go.GetComponent<RectTransform>();
      if (content != null)
      {
        Content = content;
        UIAnchorPosUtils.SetUIAnchor(content, UIAnchor.Stretch, UIAnchor.Stretch);
        UIAnchorPosUtils.SetUIPos(content, 0, 0, 0, 0);
        Ballance2.Game.Remaster.RemasterTheme.Apply(go);
      }
    }

    /// <summary>
    /// Set the page's content container.
    /// </summary>
    /// <param name="content">Content RectTransform.</param>
    public void SetContent(RectTransform content)
    {
      content.transform.SetParent(ContentHost);
      UIAnchorPosUtils.SetUIAnchor(content, UIAnchor.Stretch, UIAnchor.Stretch);
      UIAnchorPosUtils.SetUIPos(content, 0, 0, 0, 0);
    }
  }
}
