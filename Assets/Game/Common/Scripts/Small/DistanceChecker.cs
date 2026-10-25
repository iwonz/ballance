using System.Collections;
using System.Collections.Generic;
using Ballance2.Services.InputManager;
using UnityEngine;

/*
 * Copyright (c) 2020  mengyu
 * 
 * Module:
 * DistanceChecker.cs
 * 
 * Purpose:
 * Measure the distance between two objects and trigger events within a configured range.
 * 
 * Author:
 * mengyu
 */

namespace Ballance2.Game
{
  /// <summary>
  /// Measure the distance between two objects and trigger events within a configured range.
  /// </summary>
  public class DistanceChecker : MonoBehaviour
  {
    [Tooltip("Object 1.")]
    /// <summary>
    /// Object 1.
    /// </summary>
    public Transform Object1;

    [Tooltip("Object 2.")]
    /// <summary>
    /// Object 2.
    /// </summary>
    public Transform Object2;

    [Tooltip("Distance threshold.")]
    /// <summary>
    /// Distance threshold.
    /// </summary>
    public float Diatance;

    /// <summary>
    /// Enable distance checks.
    /// </summary>
    public bool CheckEnabled = false;
    /// <summary>
    /// Number of ticks between checks.
    /// </summary>
    public int CheckTickMin = 60;
    /// <summary>
    /// Maximum check tick.
    /// </summary>
    public int CheckTickMax = 300;

    /// <summary>
    /// Entered the range.
    /// </summary>
    public GameObjectDelegate OnEnterRange;
    /// <summary>
    /// Left the range.
    /// </summary>
    public GameObjectDelegate OnLeaveRange;

    private int CheckTickNow = 0;
    private bool LastEnter = false;

    void FixedUpdate()
    {
      if(CheckTickNow > 0) 
        CheckTickNow--;
      else {
        if(CheckEnabled && Object1 != null && Object2 != null) {
          var distance = (Object1.position - Object2.position).sqrMagnitude;
          if(distance <= Diatance) {
            if(!LastEnter) {
              OnEnterRange?.Invoke(gameObject);
              LastEnter = true;
            }
            CheckTickNow = CheckTickMin;
          } else {
            if(LastEnter) {
              OnLeaveRange?.Invoke(gameObject);
              LastEnter = false;
            }
            CheckTickNow = (int)Mathf.Lerp((float)CheckTickMin, (float)CheckTickMax, ((distance - Diatance) / (float)Diatance));
          }
        }
      }
    }
  }

}
