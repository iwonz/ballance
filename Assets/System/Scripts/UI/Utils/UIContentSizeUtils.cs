using UnityEngine;
using UnityEngine.UI;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* UIContentSizeUtils.cs
* 
* Purpose:
* UI content size calculation helpers.
*
* Author:
* mengyu
*/

namespace Ballance2.UI.Utils
{
  /// <summary>
  /// UI content size calculation helpers.
  /// </summary>
  public static class UIContentSizeUtils
  {
    /// <summary>
    /// Calculate a suitable size for a ContentSizeFitter.
    /// </summary>
    /// <param name="rect">RectTransform</param>
    /// <param name="contentSizeFitter">ContentSizeFitter</param>
    /// <returns>Calculated size.</returns>
    public static Vector2 GetContentSizeFitterPreferredSize(this RectTransform rect, ContentSizeFitter contentSizeFitter)
    {
      LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
      return new Vector2(HandleSelfFittingAlongAxis(0, rect, contentSizeFitter), HandleSelfFittingAlongAxis(1, rect, contentSizeFitter));
    }

    private static float HandleSelfFittingAlongAxis(int axis, RectTransform rect, ContentSizeFitter contentSizeFitter)
    {
      ContentSizeFitter.FitMode fitting = (axis == 0 ? contentSizeFitter.horizontalFit : contentSizeFitter.verticalFit);
      if (fitting == ContentSizeFitter.FitMode.MinSize)
      {
        return LayoutUtility.GetMinSize(rect, axis);
      }
      else
      {
        return LayoutUtility.GetPreferredSize(rect, axis);
      }
    }
  }
}
