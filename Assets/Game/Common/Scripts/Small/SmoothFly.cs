using UnityEngine;

/*
 * Copyright (c) 2020  mengyu
 * 
 * Module:
 * SmoothFly.cs
 * 
 * Purpose:
 * Move an object smoothly toward a target.
 * 
 * Author:
 * mengyu
 */

namespace Ballance2.Game.Utils
{    
  /// <summary>
  /// Interpolation mode used by SmoothFly.
  /// </summary>
  public enum SmoothFlyType {
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    SmoothDamp,
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Lerp
  }
  /// <summary>
  /// Move an object smoothly toward a target.
  /// </summary>
  /// <remarks>
  /// Attach this component, set the target, enable movement, and handle completion in ArrivalCallback. Example:
  /// ```csharp
  /// var smoothFly = gameObject.AddComponent(SmoothFly);
  /// smoothFly.TargetTransform = target;
  /// smoothFly.Fly = true; // Start movement.
  /// smoothFly.ArrivalDiatance = () => {
  ///   // Perform work when movement finishes.
  /// };
  /// ```
  /// </remarks>
  public class SmoothFly : MonoBehaviour {

    public delegate void CallbackDelegate(SmoothFly fly);

    /// <summary>
    /// Set the target transform.
    /// </summary>
    [Tooltip("Target transform for smooth movement.")]
    public Transform TargetTransform;
    /// <summary>
    /// Set the target position.
    /// </summary>
    [Tooltip("Target position for smooth movement.")]
    public Vector3 TargetPos = Vector3.zero;
    /// <summary>
    /// Enable smooth movement.
    /// </summary>
    [Tooltip("Enable smooth movement.")]
    public bool Fly = false;
    /// <summary>
    /// Set movement duration in seconds.
    /// </summary>
    [Tooltip("Smooth movement time, in seconds.")]
    public float Time = 1.0f;
    /// <summary>
    /// Set maximum speed.
    /// </summary>
    [Tooltip("Maximum speed.")]
    public float MaxSpeed = 0;
    /// <summary>
    /// Get current speed.
    /// </summary>
    [Tooltip("Current velocity.")]
    public Vector3 CurrentVelocity = Vector3.zero;
    /// <summary>
    /// Stop when the target is reached.
    /// </summary>
    [Tooltip("Stop when the target is reached.")]
    public bool StopWhenArrival = false;
    /// <summary>
    /// Set the arrival threshold.
    /// </summary>
    [Tooltip("Threshold for considering the target reached.")]
    public float ArrivalDiatance = 0.1f;
    /// <summary>
    /// Set the arrival callback.
    /// </summary>
    public CallbackDelegate ArrivalCallback;
    /// <summary>
    /// Set the interpolation mode.
    /// </summary>
    [Tooltip("Smooth movement mode.")]
    public SmoothFlyType Type = SmoothFlyType.SmoothDamp;

    private float LerpTick = 0;
    private Vector3 LerpStart = Vector3.zero;
    private Vector3 LerpEnd = Vector3.zero;
    
    private void FixedUpdate() {
      if(Fly) {
        switch (Type) {
          case SmoothFlyType.SmoothDamp: {
            var targetPos = GetPos();
            if(MaxSpeed <= 0)
              transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref CurrentVelocity, Time);
            else
              transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref CurrentVelocity, Time, MaxSpeed);

            if((transform.position - targetPos).sqrMagnitude < ArrivalDiatance) 
              Arrival();
            break;
          } 
          case SmoothFlyType.Lerp: {
            if(LerpTick == 0) {
              LerpStart = transform.position;
            }
            if(LerpTick <= Time){
              transform.position = Vector3.Lerp(LerpStart, GetPos(), LerpTick);
              LerpTick += UnityEngine.Time.deltaTime;
            } else {
              LerpTick = 0;
              Arrival();
            }
            break;
          }
        }
      }
    }
    private Vector3 GetPos() {
      return TargetTransform != null ? TargetTransform.position : TargetPos;
    }
    private void Arrival() {
      if(StopWhenArrival) Fly = false;
      ArrivalCallback?.Invoke(this);
    }
  }
}