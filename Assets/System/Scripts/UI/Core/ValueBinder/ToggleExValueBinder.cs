using UnityEngine;
using Ballance2.UI.Core.Controls;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* ToggleValueBinder.cs
* 
* Purpose:
* Toggle data binder.
*
* Author:
* mengyu
*/

namespace Ballance2.UI.Core.ValueBinder
{
  [AddComponentMenu("Ballance/UI/ValueBinder/ToggleExValueBinder")]
  [RequireComponent(typeof(ToggleEx))]
  public class ToggleExValueBinder : GameUIControlValueBinder
  {
    private ToggleEx toggle = null;
    protected override bool OnBinderSupplierHandle(object value)
    {
      toggle.isOn = (bool)value;
      return true;
    }
    protected override void BinderBegin()
    {
      toggle = GetComponent<ToggleEx>();
      toggle.onValueChanged.AddListener((on) => NotifyUserUpdate(on));
    }
  }
}