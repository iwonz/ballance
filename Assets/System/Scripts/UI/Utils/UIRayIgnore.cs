using UnityEngine;
using UnityEngine.EventSystems;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* SkyBoxUtils.cs
* 
* Purpose:
* Allow pointer events to pass through a UI control.
*
* Author:
* mengyu
*/

namespace Ballance2.UI.Utils
{
  /// <summary>
  /// Allow events to pass through a UI control.
  /// </summary>
  [AddComponentMenu("Ballance/UI/UIRayIgnore")]
  [Tooltip("UI raycast passthrough. Add this component to let mouse events pass through the control.")]
  public class UIRayIgnore : UIBehaviour, ICanvasRaycastFilter
  {
    private bool isEnabled = true;

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
      return !isEnabled;
    }

    protected override void OnDisable()
    {
      isEnabled = false;
    }
    protected override void OnEnable()
    {
      isEnabled = true;
    }
  }
}
