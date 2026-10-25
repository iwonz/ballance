using Ballance2.Services;
using UnityEngine;
using UnityEngine.Animations;

/*
 * Copyright (c) 2020  mengyu
 * 
 * Module:
 * CamFollow.cs
 * 
 * Purpose:
 * Camera-follow behavior used by the Ballance ball camera.
 * 
 * Author:
 * mengyu
 */

namespace Ballance2.Game
{
  /// <summary>
  /// Camera-follow behavior used by the Ballance ball camera.
  /// </summary>
  public class CamFollow : MonoBehaviour
  {
    #region Properties

    /// <summary>
    /// Enable camera following.
    /// </summary>
    /// <value></value>
    public bool Follow { 
      get { return _Follow; } 
      set { 
        _Follow = value;
      } 
    }
    /// <summary>
    /// Enable looking at the ball.
    /// </summary>
    /// <value></value>
    public bool Look { get { return _Look; } set { _Look = value; } }
    /// <summary>
    /// Current follow target.
    /// </summary>
    /// <value></value>
    public Transform Target {
      get { return _Target; }
      set {
        _Target = value;
        if(value != null) {
          CamTarget.position = value.position;
          CamLookTarget.position = value.position;
        }
      }
    }

    /// <summary>
    /// Set the follow target without updating the position.
    /// </summary>
    /// <param name="t"></param>
    public void SetTargetWithoutUpdatePos(Transform t) {
      _Target = t;
    }

    [Tooltip("Target to follow.")]
    [SerializeField]
    private Transform _Target;
    [Tooltip("Enable looking at the ball.")]
    [SerializeField]
    private bool _Look = false;
    [Tooltip("Enable following the ball.")]
    [SerializeField]
    private bool _Follow = false;

    #endregion

    public Vector3 SmoothTime = new Vector3();
    public Vector3 CamTargetSmoothTime = new Vector3();
    public Vector3 CamLookTargetSmoothTime = new Vector3();

    public Transform CamLookTarget = null;
    public Transform CamTarget = null;
    public Transform CamOrient = null;
    public Transform CamPos = null;
    
    public Camera InGameCam = null;

    private Vector3 smoothyVelocityCamTarget = Vector3.zero;
    private Vector3 smoothyVelocityCamLookTarget = Vector3.zero;
    private Vector3 smoothyVelocityCam = Vector3.zero;
    public float cameraMovePosGetPosTime = 0f;
    private Vector3 cameraMovePos = Vector3.zero;

    private GameTimeMachine.GameTimeMachineTimeTicket fixedUpdateTicket = null;
    
    private void Start() 
    {
      fixedUpdateTicket = GameManager.GameTimeMachine.RegisterFixedUpdate(_FixedUpdate, 100);
    }
    private void OnDestroy() {
      if (fixedUpdateTicket != null)
      {
        fixedUpdateTicket.Unregister();
        fixedUpdateTicket = null;
      }
    }

    private void _FixedUpdate()
    {
      if (Target != null)
      {
        if (Look && InGameCam != null) {
          
          var x = Mathf.SmoothDamp(CamLookTarget.position.x, _Target.position.x, ref smoothyVelocityCamLookTarget.x, CamLookTargetSmoothTime.x);
          var y = Mathf.SmoothDamp(CamLookTarget.position.y, _Target.position.y, ref smoothyVelocityCamLookTarget.y, CamLookTargetSmoothTime.y);
          var z = Mathf.SmoothDamp(CamLookTarget.position.z, _Target.position.z, ref smoothyVelocityCamLookTarget.z, CamLookTargetSmoothTime.z);

          CamLookTarget.position = new Vector3(x, y, z);
          InGameCam.transform.LookAt(CamLookTarget);
        }
        if (Follow)
        {
          var x = Mathf.SmoothDamp(CamTarget.position.x, Target.position.x, ref smoothyVelocityCamTarget.x, CamTargetSmoothTime.x);
          var y = Mathf.SmoothDamp(CamTarget.position.y, Target.position.y, ref smoothyVelocityCamTarget.y, CamTargetSmoothTime.y);
          var z = Mathf.SmoothDamp(CamTarget.position.z, Target.position.z, ref smoothyVelocityCamTarget.z, CamTargetSmoothTime.z);
          CamTarget.position = new Vector3(x, y, z);
        
          x = Mathf.SmoothDamp(InGameCam.transform.position.x, CamPos.position.x, ref smoothyVelocityCam.x, SmoothTime.x);
          y = Mathf.SmoothDamp(InGameCam.transform.position.y, CamPos.position.y, ref smoothyVelocityCam.y, SmoothTime.y);
          z = Mathf.SmoothDamp(InGameCam.transform.position.z, CamPos.position.z, ref smoothyVelocityCam.z, SmoothTime.z);
          InGameCam.transform.position = new Vector3(x, y, z);
        }
      }
    }
  }
}