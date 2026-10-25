using UnityEngine;

/*
* Copyright(c) 2021 imengyu
*
* Module:
* ActiveInTime.cs
* 
* Purpose:
* Deactivate an object automatically after a configured active duration.
*
* Author:
* mengyu
*/

namespace Ballance2.Sys.Tools
{
  /// <summary>
  /// Deactivate an object automatically after a configured active duration.
  /// </summary>
  public class ActiveInTime : MonoBehaviour
  {
    public float ActiveTime = 1;

    private float mActiveTimeInUpdate = 1;
    private bool mActiveTesting = false;

    void Start()
    {
      mActiveTimeInUpdate = ActiveTime;
      mActiveTesting = true;
    }
    private void OnEnable()
    {
      mActiveTimeInUpdate = ActiveTime;
      mActiveTesting = true;
    }
    private void Update()
    {
      if (mActiveTesting)
      {
        if (mActiveTimeInUpdate > 0)
          mActiveTimeInUpdate -= Time.deltaTime;
        else
        {
          gameObject.SetActive(false);
          mActiveTesting = false;
        }
      }
    }
  }
}
