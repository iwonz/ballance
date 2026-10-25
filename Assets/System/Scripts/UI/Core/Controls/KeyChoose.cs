#if UNITY_ANDROID && !UNITY_EDITOR
using Input = Ballance2.InputCompatibility.AndroidInput;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* KeyChoose.cs
* 
* Purpose:
* Keyboard key selection component.
*
* Author:
* mengyu
*/

namespace Ballance2.UI.Core.Controls
{
  /// <summary>
  /// Keyboard key selection component.
  /// </summary>
  [AddComponentMenu("Ballance/UI/Controls/KeyChoose")]
  public class KeyChoose : MonoBehaviour
  {
    public I18NText Text;
    public Text TextValue;

    [SerializeField, SetProperty("value")]
    private KeyCode _value = KeyCode.None;

    public class KeyChooseEvent : UnityEvent<KeyCode>
    {
      public KeyChooseEvent() { }
    }

    public KeyChooseEvent onValueChanged = new KeyChooseEvent();

    /// <summary>
    /// Get or set the selected key.
    /// </summary>
    public KeyCode value
    {
      get { return _value; }
      set
      {
        _value = value;
        UpdateValue();
        if (onValueChanged != null)
          onValueChanged.Invoke(value);
      }
    }
    public void UpdateValue()
    {
      TextValue.text = Ballance2.Services.I18N.I18N.Tr("core.key." + _value, _value.ToString());
    }

    private bool waitForKey = false;

    public void StartWaitKey() {
      waitForKey = true;
    }

    void Start()
    {
      UpdateValue();
    }
#if UNITY_ANDROID && !UNITY_EDITOR
    void Update()
    {
      if (!waitForKey || !Input.anyKeyDown) return;
      foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
      {
        if (!Input.GetKeyDown(key)) continue;
        value = key;
        waitForKey = false;
        break;
      }
    }
#else
    void OnGUI()
    {
      if (Input.anyKeyDown && waitForKey)
      {
        Event e = Event.current;
        if (e.isKey) {
          value = e.keyCode;
          waitForKey = false;
        }
      }
    }
#endif
  }
}
