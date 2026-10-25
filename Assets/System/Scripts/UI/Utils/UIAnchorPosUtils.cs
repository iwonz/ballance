using Ballance2.Services;
using UnityEngine;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* UIAnchorPosUtils.cs
* 
* Purpose:
* UI anchor and position helpers.
*
* Author:
* mengyu
*/

/*

Convenience setters for
RectTransform anchorMin, anchorMax, offsetMax, and offsetMin,
using parameters similar to the Unity inspector.

The helpers cover Pivot, Anchor, and Left/Right/Top/Bottom values.

See the individual method comments for details.

*/

namespace Ballance2.UI.Utils
{

  /// <summary>
  /// UI anchor and position helpers.
  /// </summary>
  public static class UIAnchorPosUtils
  {
    /// <summary>
    /// Set UI anchors.
    /// </summary>
    /// <param name="rectTransform">UI transform.</param>
    /// <param name="x">X-axis anchor.</param>
    /// <param name="y">Y-axis anchor.</param>
    public static void SetUIAnchor(RectTransform rectTransform, UIAnchor x, UIAnchor y)
    {
      float x1 = 0.5f, x2 = 0.5f, y1 = 0.5f, y2 = 0.5f;

      switch (x)
      {
        case UIAnchor.Left:
          x1 = 0;
          x2 = 0;
          break;
        case UIAnchor.Center:
          x1 = 0.5f;
          x2 = 0.5f;
          break;
        case UIAnchor.Right:
          x1 = 1;
          x2 = 1;
          break;
        case UIAnchor.Stretch:
          x1 = 0;
          x2 = 1;
          break;
      }
      switch (y)
      {
        case UIAnchor.Top:
          y1 = 1;
          y2 = 1;
          break;
        case UIAnchor.Center:
          y1 = 0.5f;
          y2 = 0.5f;
          break;
        case UIAnchor.Bottom:
          y1 = 0;
          y2 = 0;
          break;
        case UIAnchor.Stretch:
          y1 = 0;
          y2 = 1;
          break;
      }

      rectTransform.anchorMin = new Vector2(x1, y1);
      rectTransform.anchorMax = new Vector2(x2, y2);
    }
    /// <summary>
    /// Get a UI anchor.
    /// </summary>
    /// <param name="rectTransform">UI transform.</param>
    public static UIAnchor[] GetUIAnchor(RectTransform rectTransform)
    {
      UIAnchor x = UIAnchor.None, y = UIAnchor.None;
      float x1 = rectTransform.anchorMin.x, x2 = rectTransform.anchorMax.x,
          y1 = rectTransform.anchorMin.y, y2 = rectTransform.anchorMax.y;

      if (x1 == 0 && x2 == 0) x = UIAnchor.Left;
      else if (x1 == 0.5f && x2 == 0.5f) x = UIAnchor.Center;
      else if (x1 == 1 && x2 == 1) x = UIAnchor.Right;
      else if (x1 == 0 && x2 == 1) x = UIAnchor.Stretch;

      if (y1 == 0 && y2 == 0) y = UIAnchor.Bottom;
      else if (y1 == 0.5f && y2 == 0.5f) y = UIAnchor.Center;
      else if (y1 == 1 && y2 == 1) y = UIAnchor.Top;
      else if (y1 == 0 && y2 == 1) y = UIAnchor.Stretch;

      return new UIAnchor[2] { x, y };
    }
    /// <summary>
    /// Get a UI anchor.
    /// </summary>
    /// <param name="rectTransform">UI transform.</param>
    /// <param name="axis">Axis to retrieve.</param>
    public static UIAnchor GetUIAnchor(RectTransform rectTransform, RectTransform.Axis axis)
    {
      UIAnchor anchor = UIAnchor.None;
      if (axis == RectTransform.Axis.Horizontal)
      {
        float x1 = rectTransform.anchorMin.x, x2 = rectTransform.anchorMax.x;
        if (x1 == 0 && x2 == 0) anchor = UIAnchor.Left;
        else if (x1 == 0.5f && x2 == 0.5f) anchor = UIAnchor.Center;
        else if (x1 == 1 && x2 == 1) anchor = UIAnchor.Right;
        else if (x1 == 0 && x2 == 1) anchor = UIAnchor.Stretch;
      }
      else if (axis == RectTransform.Axis.Vertical)
      {
        float y1 = rectTransform.anchorMin.y, y2 = rectTransform.anchorMax.y;
        if (y1 == 0 && y2 == 0) anchor = UIAnchor.Left;
        else if (y1 == 0.5f && y2 == 0.5f) anchor = UIAnchor.Center;
        else if (y1 == 1 && y2 == 1) anchor = UIAnchor.Right;
        else if (y1 == 0 && y2 == 1) anchor = UIAnchor.Stretch;
      }
      return anchor;
    }

    /// <summary>
    /// Set UI top and right offsets.
    /// </summary>
    /// <param name="rectTransform"></param>
    /// <param name="right"></param>
    /// <param name="top"></param>
    public static void SetUIRightTop(RectTransform rectTransform, float right, float top)
    {
      rectTransform.offsetMax = new Vector2(-right, -top);
    }
    /// <summary>
    /// Set UI left and bottom offsets.
    /// </summary>
    /// <param name="rectTransform"></param>
    /// <param name="left"></param>
    /// <param name="bottom"></param>
    public static void SetUILeftBottom(RectTransform rectTransform, float left, float bottom)
    {
      rectTransform.offsetMin = new Vector2(left, bottom);
    }
    /// <summary>
    /// Set UI left, top, right, and bottom offsets.
    /// </summary>
    /// <param name="rectTransform"></param>
    /// <param name="left"></param>
    /// <param name="top"></param>
    /// <param name="right"></param>
    /// <param name="bottom"></param>
    public static void SetUIPos(RectTransform rectTransform, float left, float top, float right, float bottom)
    {
      rectTransform.offsetMin = new Vector2(left, bottom);
      rectTransform.offsetMax = new Vector2(-right, -top);
    }

    /// <summary>
    /// Set UI right offset.
    /// </summary>
    /// <param name="rectTransform"></param>
    /// <param name="top"></param>
    public static void SetUIRight(RectTransform rectTransform, float right)
    {
      rectTransform.offsetMax = new Vector2(-right, rectTransform.offsetMax.y);
    }
    /// <summary>
    /// Set UI top offset.
    /// </summary>
    /// </summary>
    /// <param name="rectTransform"></param>
    /// <param name="top"></param>
    public static void SetUITop(RectTransform rectTransform, float top)
    {
      rectTransform.offsetMax = new Vector2(rectTransform.offsetMax.x, -top);
    }
    /// <summary>
    /// Set UI left offset.
    /// </summary>
    /// <param name="rectTransform"></param>
    /// <param name="left"></param>
    public static void SetUILeft(RectTransform rectTransform, float left)
    {
      rectTransform.offsetMin = new Vector2(left, rectTransform.offsetMin.y);
    }
    /// <summary>
    /// Set UI bottom offset.
    /// </summary>
    /// <param name="rectTransform"></param>
    /// <param name="bottom"></param>
    public static void SetUIBottom(RectTransform rectTransform, float bottom)
    {
      rectTransform.offsetMin = new Vector2(rectTransform.offsetMin.x, bottom);
    }

    /// <summary>
    /// Get the right offset.
    /// </summary>
    /// <param name="rectTransform">UI transform.</param>
    /// <returns></returns>
    public static float GetUIRight(RectTransform rectTransform)
    {
      return -rectTransform.offsetMax.x;
    }
    /// <summary>
    /// Get the top offset.
    /// </summary>
    /// <param name="rectTransform">UI transform.</param>
    /// <returns></returns>
    public static float GetUITop(RectTransform rectTransform)
    {
      return -rectTransform.offsetMax.y;
    }
    /// <summary>
    /// Get the left offset.
    /// </summary>
    /// <param name="rectTransform">UI transform.</param>
    /// <returns></returns>
    public static float GetUILeft(RectTransform rectTransform)
    {
      return rectTransform.offsetMin.x;
    }
    /// <summary>
    /// Get the bottom offset.
    /// </summary>
    /// <param name="rectTransform">UI transform.</param>
    /// <returns></returns>
    public static float GetUIBottom(RectTransform rectTransform)
    {
      return rectTransform.offsetMin.y;
    }

    /// <summary>
    /// Set the UI pivot.
    /// </summary>
    /// <param name="rectTransform">UI transform.</param>
    /// <param name="pivot">Pivot position.</param>
    public static void SetUIPivot(RectTransform rectTransform, UIPivot pivot)
    {
      float x = 0.5f, y = 0.5f;

      switch (pivot)
      {
        case UIPivot.TopLeft:
          x = 0;
          y = 1;
          break;
        case UIPivot.TopCenter:
          x = 0.5f;
          y = 1;
          break;
        case UIPivot.TopRight:
          x = 1;
          y = 1;
          break;
        case UIPivot.CenterLeft:
          x = 0;
          y = 0.5f;
          break;
        case UIPivot.Center:
          x = 0.5f;
          y = 0.5f;
          break;
        case UIPivot.CenterRight:
          x = 1;
          y = 0.5f;
          break;
        case UIPivot.BottomCenter:
          x = 0.5f;
          y = 0;
          break;
        case UIPivot.BottomLeft:
          x = 0;
          y = 0;
          break;
        case UIPivot.BottomRight:
          x = 1;
          y = 0;
          break;
      }

      rectTransform.pivot = new Vector2(x, y);
    }
    /// <summary>
    /// Set the UI pivot.
    /// </summary>
    /// <param name="rectTransform">UI transform.</param>
    public static void SetUIPivot(RectTransform rectTransform, UIPivot pivot, RectTransform.Axis axis)
    {
      float x = rectTransform.pivot.x, y = rectTransform.pivot.y;

      if (axis == RectTransform.Axis.Vertical)
      {
        if ((pivot & UIPivot.Top) == UIPivot.Top)
          y = 1;
        else if ((pivot & UIPivot.Center) == UIPivot.Center)
          y = 0.5f;
        else if ((pivot & UIPivot.Bottom) == UIPivot.Bottom)
          y = 0;
      }
      else
      {
        if ((pivot & UIPivot.Left) == UIPivot.Left)
          x = 0;
        else if ((pivot & UIPivot.Center) == UIPivot.Center)
          x = 0.5f;
        else if ((pivot & UIPivot.Right) == UIPivot.Right)
          x = 1;
      }

      rectTransform.pivot = new Vector2(x, y);
    }
    /// <summary>
    /// Get the UI pivot.
    /// </summary>
    /// <param name="rectTransform">UI transform.</param>
    public static UIPivot GetUIPivot(RectTransform rectTransform)
    {
      UIPivot uIPivot = UIPivot.None;
      float x = rectTransform.pivot.x, y = rectTransform.pivot.y;

      if (x == 0 && y == 1) uIPivot = UIPivot.TopLeft;
      else if (x == 0 && y == 1) uIPivot = UIPivot.TopCenter;
      else if (x == 0.5f && y == 1) uIPivot = UIPivot.TopRight;
      else if (x == 1 && y == 1) uIPivot = UIPivot.TopLeft;
      else if (x == 0 && y == 1) uIPivot = UIPivot.TopLeft;
      else if (x == 0 && y == 1) uIPivot = UIPivot.TopLeft;
      else if (x == 0 && y == 1) uIPivot = UIPivot.TopLeft;
      else if (x == 0 && y == 1) uIPivot = UIPivot.TopLeft;

      return uIPivot;
    }

    /// <summary>
    /// Calculate UI coordinate offset from the pivot.
    /// </summary>
    /// <param name="rectTransform"></param>
    /// <param name="value"></param>
    /// <param name="axis"></param>
    /// <returns></returns>
    public static float GetUIPivotLocationOffest(RectTransform rectTransform, float value, RectTransform.Axis axis)
    {
      if (axis == RectTransform.Axis.Horizontal)
        return value - ((1 - rectTransform.pivot.x) / 1 * rectTransform.rect.width);
      else if (axis == RectTransform.Axis.Vertical)
        return value - ((1 - rectTransform.pivot.y) / 1 * rectTransform.rect.height);

      return value;
    }

    /// <summary>
    /// Get UI coordinates.
    /// </summary>
    /// <param name="targetWordPos"></param>
    /// <returns></returns>
    public static Vector2 GetUIForScreenPos(Vector3 targetWordPos)
    {
      Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, targetWordPos);
      Vector2 localPoint;
      RectTransformUtility.ScreenPointToLocalPointInRectangle(GameManager.Instance.GameCanvas, screenPoint, Camera.main, out localPoint);
      return localPoint;
    }
  }

  /// <summary>
  /// UI pivot.
  /// </summary>
  public enum UIPivot
  {
    None,
    Top = 0x1,
    Center = 0x2,
    Bottom = 0x4,
    Left = 0x8,
    Right = 0x10,

    TopLeft = Top | Left,
    TopCenter = Top | Center,
    TopRight = Top | Right,
    CenterLeft = Center | Left,
    CenterRight = Center | Right,
    BottomCenter = Bottom | Center,
    BottomLeft = Bottom | Left,
    BottomRight = Bottom | Right,
  }
  /// <summary>
  /// UI anchor.
  /// </summary>
  public enum UIAnchor
  {
    None,
    Top,
    Center,
    Bottom,
    Left,
    Right,
    Stretch,
  }
}
