using UnityEngine;

/*
 * Copyright (c) 2021  mengyu
 * 
 * Module:
 * LookAt.cs
 * 
 * Purpose:
 * Make an object look at another object.
 * 
 * Author:
 * mengyu
 */

namespace Ballance2.Game
{
  /// <summary>
  /// Make an object look at another object with selectable rotation axes.
  /// </summary>
  public class LookAt : MonoBehaviour {
    
    /// <summary>
    /// Target object to look at.
    /// </summary>
    [Tooltip("Object to look at.")]
    public Transform Target;
    /// <summary>
    /// Update rotation around X.
    /// </summary>
    [Tooltip("Update rotation around the X axis.")]
    public bool EnableX = true;
    /// <summary>
    /// Update rotation around Y.
    /// </summary>
    [Tooltip("Update rotation around the Y axis.")]
    public bool EnableY = true;
    /// <summary>
    /// Update rotation around Z.
    /// </summary>
    [Tooltip("Update rotation around the Z axis.")]
    public bool EnableZ = true;

    private void Update() {
      if(Target != null) {
        if(EnableX && EnableY && EnableZ)
          transform.LookAt(Target);
        else {
          Vector3 old = transform.eulerAngles;
          transform.LookAt(Target);

          if(EnableX) old.x = transform.eulerAngles.x;
          if(EnableY) old.y = transform.eulerAngles.y;
          if(EnableZ) old.z = transform.eulerAngles.z;

          transform.eulerAngles = old;
        }
      }
    }
  }
}