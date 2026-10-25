using UnityEngine;
using UnityEngine.UI;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* TextValueBinder.cs
* 
* Purpose:
* Text data binder.
*
* Author:
* mengyu
*/

namespace Ballance2.UI.Core.ValueBinder
{
  [AddComponentMenu("Ballance/UI/ValueBinder/TextValueBinder")]
  [RequireComponent(typeof(Text))]
  public class TextValueBinder : GameUIControlValueBinder
  {
    private Text text = null;
    protected override bool OnBinderSupplierHandle(object value)
    {
      text.text = (string)value;
      return true;
    }
    protected override void BinderBegin()
    {
      text = GetComponent<Text>();
    }
  }
}
