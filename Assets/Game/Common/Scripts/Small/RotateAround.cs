using Ballance2.Services;
using UnityEngine;
using UnityEngine.Animations;

/*
 * Copyright (c) 2023  mengyu
 * 
 * Module:
 * CamFollow.cs
 * 
 * Purpose:
 * Rotate an object around a specified target axis.
 * 
 * Author:
 * mengyu
 */

namespace Ballance2.Game
{
  /// <summary>
  /// Rotate an object around a specified target axis.
  /// </summary>
  public class RotateAround : MonoBehaviour
  {
    public float Speed = 0.1f;
    public Vector3 Axis = Vector3.up;
    public Transform Around = null;
    public bool Rotate = true;

    private void Update() {
      if (Rotate && Around != null)
        transform.RotateAround(Around.position, Axis, Time.deltaTime * this.Speed);
    }
  } 
}