using UnityEngine;
using UnityEngine.UI;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* DropdownValueBinder.cs
* 
* Purpose:
* Dropdown data binder.
*
* Author:
* mengyu
*/

namespace Ballance2.UI.Core.ValueBinder
{
  [AddComponentMenu("Ballance/UI/ValueBinder/DropdownValueBinder")]
  [RequireComponent(typeof(Dropdown))]
  public class DropdownValueBinder : GameUIControlValueBinder
  {
    private Dropdown dropdown = null;

    protected override bool OnBinderSupplierHandle(object value) {
      dropdown.value = (int)value;
      return true;
    }

    protected override void BinderBegin()
    {
      dropdown = GetComponent<Dropdown>();
      dropdown.onValueChanged.AddListener((v) =>
      {
        NotifyUserUpdate(v);
      });
    }
  }
}