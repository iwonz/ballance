using Ballance2.Services;
using UnityEngine;

/*
 * Copyright (c) 2020  mengyu
 * 
 * Module:
 * SpeedMeter.cs
 * 
 * Purpose:
 * Measure an object's movement speed.
 * 
 * Author:
 * mengyu
 */

namespace Ballance2.Game
{
  /// <summary>
  /// Measure an object's movement speed.
  /// </summary>
  public class SpeedMeter : MonoBehaviour
  {
    [Tooltip("Enable speed measurement.")]
    /// <summary>
    /// Start measuring speed.
    /// </summary>
    /// <value></value>
    public bool Enabled {
      get { return _Enabled; }
      set {
        _Enabled = value;
        if(_Enabled) StartUpdate();
        else StopUpdate();
      }
    }
    [Tooltip("Target to measure; empty means this object.")]
    /// <summary>
    /// Object to measure; null measures this object.
    /// </summary>
    /// <value></value>
    public Transform Target;
    [Tooltip("Maximum speed used for normalization.")]
    /// <summary>
    /// Maximum speed used for normalization.
    /// </summary>
    /// <value></value>
    public float MaxSpeed = 1;
    [Tooltip("Minimum speed used for normalization.")]
    /// <summary>
    /// Minimum speed used for normalization.
    /// </summary>
    /// <value></value>
    public float MinSpeed = 0;
    [Tooltip("Minimum speed used for normalization.")]
    /// <summary>
    /// Invoke Callback when FixedUpdateTick % CallbackDelayMod == 0.
    /// </summary>
    /// <value></value>
    public int CallbackDelayMod = 26;

    [Tooltip("Normalized speed, from 0 to 1.")]
    /// <summary>
    /// Normalized speed, 0-1: (NowAbsoluteSpeed - MinSpeed) / (MaxSpeed - MinSpeed).
    /// </summary>
    /// <value></value>
    public float NowRelativeSpeed;
    [Tooltip("Absolute speed in meters per second.")]
    /// <summary>
    /// Absolute speed in m/s.
    /// /// </summary>
    /// <value></value>
    public float NowAbsoluteSpeed;

    private bool LastPosIsNull = true;
    private Vector3 LastPos;
    private bool CheckOnce = false;
    private SpeedMeterDelegate CheckOnceCallback = null;
    private bool _Enabled = false;

    /// <summary>
    /// Callback receiving the measured speed on updates.
    /// </summary>
    public SpeedMeterDelegate Callback = null;

    private void Start() {
      if(Target == null)
        Target = transform;
      if(_Enabled) 
        StartUpdate();
    }
    private void OnDestroy() {
      StopUpdate();
    }

    private GameTimeMachine.GameTimeMachineTimeTicket fixedUpdateTicket = null;
    
    private void StartUpdate() {
      if (fixedUpdateTicket == null)
        fixedUpdateTicket = GameManager.GameTimeMachine.RegisterFixedUpdate(_FixedUpdate, 30);
    }
    private void StopUpdate() {
      if (fixedUpdateTicket != null)
      {
        fixedUpdateTicket.Unregister();
        fixedUpdateTicket = null;
      }
    }
    private void OnEnable() {
      if (fixedUpdateTicket != null) fixedUpdateTicket.Enable();
    }
    private void OnDisable() {
      if (fixedUpdateTicket != null) fixedUpdateTicket.Disable();
    }

    private void _FixedUpdate() {
      if(Enabled && Target != null) {
        if(LastPosIsNull) {

          var nowPos = Target.position;
          var len = (nowPos - LastPos).sqrMagnitude / Time.fixedDeltaTime;

          NowAbsoluteSpeed = len;
          NowRelativeSpeed = (NowAbsoluteSpeed - MinSpeed) / (MaxSpeed - MinSpeed);

          if(Callback != null && GameTimeMachine.FixedUpdateTick % CallbackDelayMod == 0)
            Callback.Invoke(this);
          if(CheckOnce) {
            CheckOnceCallback?.Invoke(this);
            CheckOnce = false;
            Enabled = false;
          }
          
          LastPos = nowPos;
        } else {
          LastPosIsNull = false;
          LastPos = Target.position;
        }
      }
    }
    
    /// <summary>
    /// Measure speed once manually.
    /// </summary>
    /// <param name="callback">Speed measurement callback.</param>
    public void TestOnce(SpeedMeterDelegate callback) {
      Enabled = true;
      LastPosIsNull = true;
      CheckOnce = true;
      CheckOnceCallback = callback;
    }
  }  

  public delegate void SpeedMeterDelegate(SpeedMeter meter);

}
