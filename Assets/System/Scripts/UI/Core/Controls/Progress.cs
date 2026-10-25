using Ballance2.UI.Utils;
using UnityEngine;
using UnityEngine.UI;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* Progress.cs
* 
* Purpose:
* Progress bar component.
*
* Author:
* mengyu
*/
namespace Ballance2.UI.Core.Controls
{
  /// <summary>
  /// Progress bar component.
  /// </summary>
  [ExecuteInEditMode]
  [AddComponentMenu("Ballance/UI/Controls/Progress")]
  public class Progress : MonoBehaviour
  {
    [SerializeField, HideInInspector]
    private float val = 0;
    public bool lightByVal = false;

    public RectTransform fillArea;
    public RectTransform fillRect;
    public Image fillImage;

    public void UpdateVal()
    {
      UIAnchorPosUtils.SetUIRightTop(fillRect, ((1.0f - val) * fillArea.rect.size.x), 0);
      if (lightByVal)
        fillImage.color = new Color(fillImage.color.r, fillImage.color.g, fillImage.color.b, val);
    }

    /// <summary>
    /// Progress value from 0 to 1.
    /// </summary>
    /// <value></value>
    public float value
    {
      get { return val; }
      set
      {
        val = value;
        UpdateVal();
      }
    }
  }
}
