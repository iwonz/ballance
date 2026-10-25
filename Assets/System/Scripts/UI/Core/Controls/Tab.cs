using System.Collections.Generic;
using Ballance2.Services.InputManager;
using Ballance2.UI.Utils;
using Ballance2.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* Tab.cs
* 
* Purpose:
* Tab control.
* 
* Author:
* mengyu
*/

namespace Ballance2.UI.Core.Controls
{
  /// <summary>
  /// Tab control.
  /// </summary>
  [ExecuteInEditMode]
  [AddComponentMenu("Ballance/UI/Controls/Tab")]
  public class Tab : UIBehaviour
  {   
    public Dictionary<string, TabContent> tabs = new Dictionary<string, TabContent>();
    public TabContent tabActive = null;

    public Sprite TabImageActive;
    public Sprite TabImageNormal;
    public RectTransform PanelArea;
    public RectTransform PanelPrefab;
    public RectTransform TabPrefab;
    public RectTransform TabArea;
    
    protected override void Start() {
      for (var i = 0; i < TabArea.childCount; i++)
      {
        var child = TabArea.GetChild(i);
        if(child.gameObject.activeSelf) {
          var tab = child.GetComponent<TabContent>();
          if(tab != null) {
            InitTab(tab);
          }
        }
      }
    }

    private void InitTab(TabContent t) {
      if(tabs.ContainsKey(t.Name)) tabs[t.Name] = t;
      else tabs.Add(t.Name, t);
      EventTriggerListener.Get(t.Tab.gameObject).onClick += (go) => {
        ActiveTab(t.Name);
      };
      if(tabActive == null)
        ActiveTab(t.Name);
    }

    /// <summary>
    /// Get tab content by name.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <returns>Tab content, or null if not found.</returns>
    public RectTransform GetTabContent(string name) {
      if(tabs.TryGetValue(name, out var tab)) 
        return tab.TabContentRect;
      return null;
    }
    /// <summary>
    /// Get the number of tabs.
    /// </summary>
    /// <returns></returns>
    public int GetTabCount() { return tabs.Count; }  
    /// <summary>
    /// Add a tab.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <param name="title">Title.</param>
    /// <param name="content">Content.</param>
    public bool AddTab(string tabName, string title, RectTransform content) {
      if(tabs.ContainsKey(tabName))
        return false;
      var tabRect = CloneUtils.CloneNewObjectWithParent(TabPrefab.gameObject, TabArea, "Tab_" + tabName).transform as RectTransform;
      var tab = tabRect.gameObject.AddComponent<TabContent>();
      tab.Name = tabName;
      tab.Title = title;
      tab.Tab = tabRect;
      tab.TabImage = tab.Tab.GetComponent<Image>();

      var text = tab.Tab.Find("Text");
      if(text != null)
        text.GetComponent<Text>().text = title;

      tab.TabContentArea = CloneUtils.CloneNewObjectWithParent(PanelPrefab.gameObject, PanelArea, "TabPanel_" + tabName).transform as RectTransform;
      tab.TabContentRect = content;
      if(content != null) {
        content.SetParent(tab.TabContentArea);
        content.localScale = Vector3.one;
        UIAnchorPosUtils.SetUIPos(content, 0, 0, 0, 0);
      }
      InitTab(tab);
      return true;
    }    
    /// <summary>
    /// Remove a tab.
    /// </summary>
    /// <param name="name">Name.</param>
    public bool DeleteTab(string tabName) {
      if(tabs.ContainsKey(tabName)) {
        var tab = tabs[tabName];
        tabs.Remove(tabName);

        if(tabActive == tab) 
          if(tabs.Count > 0)
            ActiveTab(tabs.Keys.GetEnumerator().Current);
          else
            tabActive = null;
        return true;
      } else {
        Log.W("Tab", "Not found tab {0}", tabName);
      }
      return false;
    } 
    /// <summary>
    /// Activate a named tab.
    /// </summary>
    /// <param name="name">Name.</param>
    public bool ActiveTab(string tabName) {
      if(tabs.TryGetValue(tabName, out var tab)) {
        if(tabActive != tab) {
          if(tabActive != null) {
            tabActive.TabContentArea.gameObject.SetActive(false);
            tabActive.TabImage.sprite = TabImageNormal;
          }

          tabActive = tab;
          tabActive.TabContentArea.gameObject.SetActive(true);
          tabActive.TabImage.sprite = TabImageActive;
          return true;
        }
      } else {
        Log.W("Tab", "Not found tab {0}", tabName);
      }
      return false;
    }
  }
}
