using UnityEngine;
using UnityEngine.UI;
using Ballance2.Services.I18N;
#if UNITY_EDITOR
using UnityEditor;
#endif

/*
* Copyright(c) 2021  mengyu
*
* Module:
* Progress.cs
* 
* Purpose:
* Text component that automatically loads a localized string.
*
* Author:
* mengyu
*/

namespace Ballance2.UI.Core.Controls
{
  /// <summary>
  /// Text component that automatically loads a localized string.
  /// </summary>
  [ExecuteInEditMode]
  [AddComponentMenu("Ballance/UI/Controls/I18NText")]
  public class I18NText : Text
  {
    [SerializeField]
    private string m_ResourceKey = "";
    [SerializeField]
    private string m_DefaultString = "";
    [SerializeField]
    private bool m_I18N = true;

    public string resourceKey
    {
      get
      {
        return m_ResourceKey;
      }
      set
      {
        m_ResourceKey = value;
        UpdateText();
      }
    }
    public string defaultString
    {
      get
      {
        return m_DefaultString;
      }
      set
      {
        m_DefaultString = value;
        UpdateText();
      }
    }
    public bool i18N
    {
      get
      {
        return m_I18N;
      }
      set
      {
        m_I18N = value;
        UpdateText();
      }
    }

    private new void Start()
    {
      base.Start();
      UpdateText();
    }
    private string GetDefaultString()
    {
      return string.IsNullOrEmpty(defaultString) ? resourceKey : defaultString;
    }
    public void UpdateText()
    {
      // Plain Text prefabs may opt into localization only in their instances.
      // Keep runtime values and serialized m_Text intact when not opted in.
      if (!i18N && string.IsNullOrEmpty(defaultString))
        return;
#if UNITY_EDITOR
      if (EditorApplication.isPlaying)
        text = (i18N && m_ResourceKey != "") ?
            I18N.Tr(resourceKey, defaultString) : GetDefaultString();
      else
        text = defaultString;
#else
            text = (i18N && m_ResourceKey != "") ? 
                I18N.Tr(resourceKey, defaultString) : GetDefaultString();
#endif
    }
  }
}
