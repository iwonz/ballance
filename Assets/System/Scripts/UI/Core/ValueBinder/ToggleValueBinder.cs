using UnityEngine;
using UnityEngine.UI;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* ToggleExValueBinder.cs
* 
* Purpose:
* ToggleEx data binder.
*
* Author:
* mengyu
*/

namespace Ballance2.UI.Core.ValueBinder
{
  [AddComponentMenu("Ballance/UI/ValueBinder/ToggleValueBinder")]
  [RequireComponent(typeof(Toggle))]
  public class ToggleValueBinder : GameUIControlValueBinder
  {
    private Toggle toggle = null;
    
    protected override bool OnBinderSupplierHandle(object value) {
      toggle.isOn = (bool)value;
      return true;
    }

    protected override void BinderBegin()
    {
      toggle = GetComponent<Toggle>();
      toggle.onValueChanged.AddListener((on) =>
      {
        NotifyUserUpdate(on);
      });
    }
  }
}