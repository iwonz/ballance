using Ballance2.UI.Utils;
using UnityEngine;
using UnityEngine.EventSystems;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* Toggle.cs
* 
* Purpose:
* Split two views with a user-draggable size divider.
* 
* Author:
* mengyu
*
*/

namespace Ballance2.UI.Core.Controls
{
  /// <summary>
  /// Split two views with a user-draggable size divider.
  /// </summary>
  [ExecuteInEditMode]
  [AddComponentMenu("Ballance/UI/Controls/SplitView")]
  public class SplitView : UIBehaviour
  {
    protected override void Start()
    {
      self = GetComponent<RectTransform>();
      if (splitViewDragger != null)
        BindDragger();
      UpdateVal();
    }

    [SerializeField, HideInInspector]
    private float _value = 0;
    [SerializeField, HideInInspector]
    private float _min = 0;
    [SerializeField, HideInInspector]
    private float _max = 1;
    [SerializeField, HideInInspector]
    private SplitViewDirection _direction = SplitViewDirection.LeftRight;

    public RectTransform self;
    public RectTransform dragger;
    public RectTransform panelOne;
    public RectTransform panelTwo;

    public SplitViewDragger splitViewDragger;
    public void BindDragger()
    {
      splitViewDragger.onValueChanged.AddListener((v) =>
      {
        value = v;
      });
    }

    protected override void OnRectTransformDimensionsChange()
    {
      UpdateVal();
    }

    public void UpdateVal()
    {
      float val = value;
      if (val < _min) val = _min;
      else if (val > _max) val = _max;

      if (splitViewDragger.direction != _direction)
        splitViewDragger.direction = _direction;

      if (_direction == SplitViewDirection.LeftRight)
      {
        float leftWidth = val * self.rect.width;
        float rightWidth = (1 - val) * self.rect.width;


        dragger.anchoredPosition = new Vector2(leftWidth - dragger.rect.width / 2, -20);

        UIAnchorPosUtils.SetUIPos(panelOne, 0, 0, rightWidth, 0);
        UIAnchorPosUtils.SetUIPos(panelTwo, leftWidth, 0, 0, 0);
      }
      else if (_direction == SplitViewDirection.TopBottom)
      {
        float topWidth = val * self.rect.height;
        float bottomWidth = (1 - val) * self.rect.height;

        dragger.anchoredPosition = new Vector2(20, -topWidth + dragger.rect.height / 2);

        UIAnchorPosUtils.SetUIPos(panelOne, 0, 0, 0, bottomWidth);
        UIAnchorPosUtils.SetUIPos(panelTwo, 0, topWidth, 0, 0);
      }
    }

    /// <summary>
    /// Minimum split ratio, limiting the left or top view.
    /// </summary>
    public float min
    {
      get { return _min; }
      set
      {
        _min = value;
        UpdateVal();
      }
    }
    /// <summary>
    /// Maximum split ratio, limiting the right or bottom view.
    /// </summary>
    public float max
    {
      get { return _max; }
      set
      {
        _max = value;
        UpdateVal();
      }
    }
    /// <summary>
    /// Split direction.
    /// </summary>
    public SplitViewDirection direction
    {
      get
      {
        return _direction;
      }
      set
      {
        _direction = value;
        UpdateVal();
      }
    }
    /// <summary>
    /// Split ratio from 0 to 1.
    /// </summary>
    public float value
    {
      get { return _value; }
      set
      {
        _value = value;
        UpdateVal();
      }
    }
  }
  /// <summary>
  /// Split view direction.
  /// </summary>
  public enum SplitViewDirection
  {
    /// <summary>
    /// Left and right.
    /// </summary>
    LeftRight,
    /// <summary>
    /// Top and bottom.
    /// </summary>
    TopBottom
  }
}
