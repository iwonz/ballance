using Ballance2.Base;
using Ballance2.Menu;
using Ballance2.Services;
using UnityEngine;

namespace Ballance2.Game.GamePlay
{
  /// <summary>
  /// Manage camera movement during gameplay.
  /// </summary>
  public class CamManager : MonoBehaviour
  {
    private const string TAG = "CamManager";

    public GameObject _CamOrient;
    public Transform _CamTarget;
    public Transform _CamOrientTransform;
    public Skybox _SkyBox;
    public AnimationCurve _CamRotateSpeedCurve;
    public AnimationCurve _CamUpSpeedCurve;
    public AnimationCurve _CamDownSpeedCurve;
    public Transform _CamPosFrame;
    public CamFollow _CamFollow;

    [SerializeField]
    private float _CameraRotateTime = 0.3f;
    [SerializeField]
    private float _CameraRotateUpTime = 0.8f;
    [SerializeField]
    private float _CameraRotateDownTime = 1.3f;
    [SerializeField]
    private float _CameraNormalZ = 17;
    [SerializeField]
    private float _CameraNormalY = 30;
    [SerializeField]
    private float _CameraSpaceY = 55;
    [SerializeField]
    private float _CameraSpaceZ = 8;
    [SerializeField]
    private float _CamRotateValueNow = 0;
    [SerializeField]
    private bool _CamIsRotateing = false;
    [SerializeField]
    private float _CamRotateTick = 0;
    [SerializeField]
    private bool _CamIsRotateingUp = false;
    [SerializeField]
    private float _CamRotateUpTick = 0;
    [SerializeField]
    private Vector3 _CamRotateUpStart = Vector3.zero;
    [SerializeField]
    private Vector3 _CamRotateUpTarget = Vector3.zero;
    [SerializeField]
    private Vector3 _CamOutSpeed = Vector3.zero;
    [SerializeField]
    private float _CamRotateStartDegree = 0;
    [SerializeField]
    private float _CamRotateTargetDegree = 0;

    #region Public properties

    /// <summary>
    /// Camera orientation transform used as the ball's reference [R].
    /// </summary>
    public Transform CamDirectionRef { get; private set; }
    /// <summary>
    /// Camera right vector [R].
    /// </summary>
    public Vector3 CamRightVector { get; private set; } = Vector3.right;
    /// <summary>
    /// Camera left vector [R].
    /// </summary>
    public Vector3 CamLeftVector { get; private set; } = Vector3.left;
    /// <summary>
    /// Camera forward vector [R].
    /// </summary>
    public Vector3 CamForwerdVector  { get; private set; } = Vector3.forward;
    /// <summary>
    /// Camera backward vector [R].
    /// </summary>
    public Vector3 CamBackVector  { get; private set; } = Vector3.back;
    /// <summary>
    /// Camera follow speed [RW].
    /// </summary>
    public float CamFollowSpeed  { get; set; } = 0.05f;
    /// <summary>
    /// Whether Space has raised the camera [R].
    /// </summary>
    public bool CamIsSpaced { get; private set; } = false;
    /// <summary>
    /// Current camera direction [R]; use RotateTo to change it.
    /// </summary>
    public float CamRotateValue  { get; private set; } = 0;
    /// <summary>
    /// Camera follow component [R].
    /// </summary>
    public CamFollow CamFollow  { get { return _CamFollow;  } }
        
    #endregion

    #region Initialization

    private void Start() {
      InitCommand();
      InitEvents();
          
      this._CamPosFrame.localPosition = new Vector3(0, this._CameraNormalY, -this._CameraNormalZ);
      this.transform.position = this._CamPosFrame.position;
      this.transform.LookAt(this._CamTarget);
      this.CamDirectionRef = this._CamOrient.transform;
    }
    private void OnDestroy() {
      DestroyCommand();
      DestroyEvent();
    }
    private void Update() {
      // Horizontal camera rotation.
      if (this._CamIsRotateing) {
        this._CamRotateTick += Time.deltaTime;

        var v = this._CamRotateSpeedCurve.Evaluate(this._CamRotateTick / this._CameraRotateTime);
        
        this._CamRotateValueNow = this._CamRotateStartDegree + v * this._CamRotateTargetDegree;
        this._CamOrientTransform.localEulerAngles = new Vector3(0, this._CamRotateValueNow, 0);

        if (v >= 1) {
          this._CamRotateValueNow = this._CamRotateStartDegree + this._CamRotateTargetDegree;
          this._CamOrientTransform.localEulerAngles = new Vector3(0, this._CamRotateValueNow, 0);
          this._CamIsRotateing = false;
          this.ResetVector();
        }
      }
      // Raise the camera vertically.
      if (this._CamIsRotateingUp) {
        this._CamRotateUpTick += Time.deltaTime;
        
        float v = 0;
        if (this.CamIsSpaced)
          v = this._CamUpSpeedCurve.Evaluate(this._CamRotateUpTick / this._CameraRotateUpTime);
        else
          v = this._CamDownSpeedCurve.Evaluate(this._CamRotateUpTick / this._CameraRotateDownTime);

        this._CamPosFrame.localPosition = new Vector3(0, this._CamRotateUpStart.y + v * this._CamRotateUpTarget.y, this._CamRotateUpStart.z + v * this._CamRotateUpTarget.z);
        if (v >= 1)
          this._CamIsRotateingUp = false;
      }
    }

    #endregion

    #region Events

    /// <summary>
    /// Space-key camera elevation changed.
    /// </summary>
    [HideInInspector]
    public GameEventEmitterStorage EventRotateUpStateChanged;
    /// <summary>
    /// Camera rotation direction changed.
    /// </summary>
    [HideInInspector]
    public GameEventEmitterStorage EventRotateDirectionChanged;
    /// <summary>
    /// Camera following state changed.
    /// </summary>
    [HideInInspector]
    public GameEventEmitterStorage EventCamFollowChanged;
    /// <summary>
    /// Camera look-at state changed.
    /// </summary>
    [HideInInspector]
    public GameEventEmitterStorage EventCamLookChanged;
    /// <summary>
    /// Camera follow target changed.
    /// </summary>
    [HideInInspector]
    public GameEventEmitterStorage EventCamFollowTargetChanged;

    private void InitEvents() {
      var events = GameMediator.Instance.RegisterEventEmitter("CamManager");
      this.EventRotateUpStateChanged = events.RegisterEvent("RotateUpStateChanged");
      this.EventRotateDirectionChanged = events.RegisterEvent("RotateDirectionChanged");
      this.EventCamFollowChanged = events.RegisterEvent("CamFollowChanged");
      this.EventCamLookChanged = events.RegisterEvent("CamLookChanged");
      this.EventCamFollowTargetChanged = events.RegisterEvent("CamFollowTargetChanged");
    }
    private void DestroyEvent() {
      GameMediator.Instance.UnRegisterEventEmitter("CamManager");
    }

    #endregion

    #region Commands

    private int _CommandId = 0;
    private void InitCommand() {
      this._CommandId = GameManager.Instance.GameDebugCommandServer.RegisterCommand("cam", (eyword, fullCmd, argsCount, args) => {
        var type = args[0];
        if (type == "left")
          this.RotateLeft();
        else if (type == "right")
          this.RotateLeft();
        else if (type == "up")
          this.RotateUp(true);
        else if (type == "down")
          this.RotateUp(false);
        else if (type == "follow")
          this.SetCamFollow(true);
        else if (type == "no-follow")
          this.SetCamFollow(false);
        else if( type == "look")
          this.SetCamLook(true);
        else if (type == "no-look")
          this.SetCamLook(false);
        return true;
      }, 1, "cam <left/right/up/down/follow/no-follow/look/no-look> Camera commands" +
              "  left      ▶ Rotate the camera left" +
              "  right     ▶ Rotate the camera right" +
              "  up        ▶ Raise the camera as when holding Space" +
              "  down      ▶ Lower the camera as when releasing Space" +
              "  follow    ▶ Enable camera follow" +
              "  no-follow ▶ Disable camera follow" +
              "  look      ▶ Enable looking at the ball" +
              "  no-look   ▶ Disable looking at the ball"
      );
    }
    private void DestroyCommand() {
      GameManager.Instance.GameDebugCommandServer.UnRegisterCommand(this._CommandId);
    }
  
    #endregion

    #region Public methods

    /// <summary>
    /// Reset camera-facing vectors.
    /// </summary>
    /// <returns></returns>
    public void ResetVector()
    {
      // Recalculate ball movement vectors from the camera orientation.
      var y = -this._CamOrientTransform.localEulerAngles.y - 90;
      this.CamRightVector = Quaternion.AngleAxis(-y, Vector3.up) * Vector3.right;
      this.CamLeftVector = Quaternion.AngleAxis(-y, Vector3.up) * Vector3.left;
      this.CamForwerdVector = Quaternion.AngleAxis(-y, Vector3.up) * Vector3.forward;
      this.CamBackVector = Quaternion.AngleAxis(-y, Vector3.up) * Vector3.back;
    }
    private void _UpdateStateForDebugStats()
    {
      if (GameManager.DebugMode && GamePlayUIControl.Instance.DebugStatValues.ContainsKey("CamDirection")) {
        GamePlayUIControl.Instance.DebugStatValues["CamDirection"].Value = this._CamOrientTransform.localEulerAngles.y.ToString("F2");
        GamePlayUIControl.Instance.DebugStatValues["CamState"].Value = $"IsSpaced: {this.CamIsSpaced} Follow: {this.CamFollow.Follow} Look: {this.CamFollow.Look}";
      }
    }

    /// <summary>
    /// Set camera position and orientation from a RestPoint placeholder.
    /// </summary>
    /// <param name="go">RestPoint placeholder.</param>
    public void SetPosAndDirByRestPoint(GameObject go) 
    {
      var rot = go.transform.eulerAngles.y;
      rot = rot % 360;
      if (rot < 0) rot += 360;
      else if (rot > 315) rot -= 360;

      this._CamOrientTransform.localEulerAngles = new Vector3(0, rot - 90, 0);
      this._CamTarget.position = go.transform.position;
      this.transform.position = this._CamPosFrame.position;
      this.CamRotateValue = rot - 90;
      this._CamRotateValueNow = rot - 90;
      this.ResetVector();
      this._UpdateStateForDebugStats();
    }
    /// <summary>
    /// Tilt upward with Space.
    /// </summary>
    /// <param name="enable">Enabled state.</param>
    public void RotateUp(bool enable)
    {
      this.CamIsSpaced = enable;
      this._CamRotateUpStart.y = this._CamPosFrame.localPosition.y;
      this._CamRotateUpStart.z = this._CamPosFrame.localPosition.z;
      if (enable) 
      {
        this._CamRotateUpTarget.y = this._CameraSpaceY - this._CamRotateUpStart.y;
        this._CamRotateUpTarget.z = -this._CameraSpaceZ - this._CamRotateUpStart.z;
      }
      else
      {
        this._CamRotateUpTarget.y = this._CameraNormalY - this._CamRotateUpStart.y;
        this._CamRotateUpTarget.z = -this._CameraNormalZ - this._CamRotateUpStart.z;
      }
      this._CamRotateUpTick = 0;
      this._CamIsRotateingUp = true;
      this.EventRotateUpStateChanged.Emit(enable);
      this._UpdateStateForDebugStats();
    }
    /// <summary>
    /// Rotate the camera right.
    /// </summary>
    public void RotateRight()
    {
      this.RotateDregree(-90);
    }
    /// <summary>
    /// Rotate the camera left.
    /// </summary>
    public void RotateLeft()
    {
      this.RotateDregree(90);
    }
    /// <summary>
    /// Rotate the camera by an angle.
    /// </summary>
    /// <param name="deg">Angle in degrees; positive rotates right, negative left.</param>
    public void RotateDregree(float deg)
    {
      this.CamRotateValue = this.CamRotateValue + deg;
      this._CamRotateStartDegree = this._CamRotateValueNow;
      this._CamRotateTargetDegree = this.CamRotateValue - this._CamRotateStartDegree;
      this._CamRotateTick = 0;
      this._CamIsRotateing = true;
      this.EventRotateDirectionChanged.Emit(this._CamRotateTargetDegree);
      this._UpdateStateForDebugStats();
    }
    /// <summary>
    /// Set the main camera skybox material.
    /// </summary>
    /// <param name="mat"></param>
    public void SetSkyBox(Material mat)
    {
      this._SkyBox.material = mat;
    }
    /// <summary>
    /// Enable following the ball.
    /// </summary>
    /// <param name="enable"></param>
    public void SetCamFollow(bool enable)
    {
      this.CamFollow.Follow = enable;
      this.EventCamFollowChanged.Emit(enable);
      this._UpdateStateForDebugStats();
    }
    /// <summary>
    /// Enable looking at the ball.
    /// </summary>
    /// <param name="enable"></param>
    public void SetCamLook(bool enable)
    {
      this.CamFollow.Look = enable;
      this.EventCamLookChanged.Emit(enable);
      this._UpdateStateForDebugStats();
    }
    /// <summary>
    /// Set the camera follow target.
    /// </summary>
    /// <param name="target">Target.</param>
    /// <param name="noUpdatePos">Skip position synchronization when setting the target.</param>
    public void SetTarget(Transform target, bool noUpdatePos)
    {
      if (noUpdatePos)
        this.CamFollow.SetTargetWithoutUpdatePos(target);
      else
        this.CamFollow.Target = target;
      this.EventCamFollowTargetChanged.Emit(target);
    }

    /// <summary>
    /// Get the camera follow target.
    /// </summary>
    /// <value></value>
    public Transform Target {
      get { return this.CamFollow.Target; }
    }

    /// <summary>
    /// Disable all camera behavior.
    /// </summary>
    public void DisbleAll()
    {
      this.CamFollow.Follow = false;
      this.CamFollow.Look = false;
      this.CamFollow.Target = null;
      this.EventCamFollowChanged.Emit(false);
      this.EventCamLookChanged.Emit(false);
      this.EventCamFollowTargetChanged.Emit(null);
      this._UpdateStateForDebugStats();
    }

    #endregion
  }
}