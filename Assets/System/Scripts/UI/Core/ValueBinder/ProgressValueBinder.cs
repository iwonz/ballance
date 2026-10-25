using UnityEngine;
using Ballance2.UI.Core.Controls;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* ProgressValueBinder.cs
* 
* Purpose:
* Progress data binder.
*
* Author:
* mengyu
*/
namespace Ballance2.UI.Core.ValueBinder
{
  [AddComponentMenu("Ballance/UI/ValueBinder/ProgressValueBinder")]
  [RequireComponent(typeof(Progress))]
  public class ProgressValueBinder : GameUIControlValueBinder
  {
    private Progress progress = null;

    protected override bool OnBinderSupplierHandle(object value)
    {
      if (value.GetType() == typeof(int))
        progress.value = (float)value;
      else if (value.GetType() == typeof(float))
        progress.value = (float)(int)value;
      else if (value.GetType() == typeof(double))
        progress.value = (float)(double)value;
      else
        UnityEngine.Debug.Log("Unknow type:  " + value.GetType().Name + " " + value);
      return true;
    }
    protected override void BinderBegin()
    {
      progress = GetComponent<Progress>();
    }
  }
}