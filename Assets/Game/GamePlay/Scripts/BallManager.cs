using System;
using System.Collections.Generic;
using Ballance2.Base;
using Ballance2.Game.GamePlay.Balls;
using Ballance2.Game.Utils;
using Ballance2.Menu;
using Ballance2.Services;
using Ballance2.Services.Debug;
using Ballance2.Services.InputManager;
using Ballance2.Utils;
using BallancePhysics.Wapper;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem.LowLevel;
using static Ballance2.Services.GameManager;

namespace Ballance2.Game.GamePlay
{
    /// <summary>
    /// Manage ball registration, movement, and special effects.
    /// </summary>
    public class BallManager : MonoBehaviour
    {
        private const string TAG = "BallManager";
        // Previously controlled ball type.
        public BallLightningSphere _BallLightningSphere;
        public GameObject _BallWood;
        public GameObject _BallStone;
        public GameObject _BallPaper;
        public GameObject _BallSmoke;
        public ParentConstraint _BallShadowProjector;

        public BallPiecesControllManager BallPiecesControllManager;

        /// <summary>
        /// Current ball name [R].
        /// </summary>
        public string CurrentBallName { get; private set; }
        /// <summary>
        /// Current ball [R].
        /// </summary>
        public Ball CurrentBall { get; private set; }
        /// <summary>
        /// Current ball force direction [RW].
        /// </summary>
        public BallPushType PushType = BallPushType.None;
        /// <summary>
        /// Whether the player can control the ball [RW].
        /// </summary>
        public bool CanControll = false;
        /// <summary>
        /// Whether the player can control the camera [RW].
        /// </summary>
        public bool CanControllCamera = false;
        /// <summary>
        /// Whether Shift is held [R].
        /// </summary>
        public bool ShiftPressed = false;
        /// <summary>
        /// Upward movement key state [RW].
        /// </summary>
        public bool KeyStateUp = false;
        /// <summary>
        /// Downward movement key state [RW].
        /// </summary>
        public bool KeyStateDown = false;
        /// <summary>
        /// Forward movement key state [RW].
        /// </summary>
        public bool KeyStateForward = false;
        /// <summary>
        /// Backward movement key state [RW].
        /// </summary>
        public bool KeyStateBack = false;
        /// <summary>
        /// Left movement key state [RW].
        /// </summary>
        public bool KeyStateLeft = false;
        /// <summary>
        /// Right movement key state [RW].
        /// </summary>
        public bool KeyStateRight = false;
        /// <summary>
        /// Ball speed multiplier, default 1 [RW].
        /// </summary>
        public float BallSpeedFactor = 1;
        /// <summary>
        /// Current ball transform [R].
        /// </summary>
        public Transform PosFrame
        {
            get
            {
                return GamePlayManager.Instance.CamManager._CamOrientTransform;
            }
        }

        private void Awake()
        {
            _AddCommands();
            _InitEvents();
            _InitSettings();
            _InitKeys();

            // Register built-in ball types.
            RegisterBall("BallWood", _BallWood);
            RegisterBall("BallStone", _BallStone);
            RegisterBall("BallPaper", _BallPaper);

        }
        private void OnDestroy()
        {
            _DeleteCommands();
            _DeleteEvents();
            _UnInitSettings();
            _UnInitKeys();
            _CollectRegisterBalls();
        }

        #region Keyboard

        private int settingsCallbackId = 0;
        private KeyListener keyListener = null;
        private KeyCode keyFront = KeyCode.None;
        private KeyCode keyBack = KeyCode.None;
        private KeyCode keyUp = KeyCode.None;
        private KeyCode keyUpCamera = KeyCode.None;
        private KeyCode keyDown = KeyCode.None;
        private KeyCode keyRoateCamera = KeyCode.None;
        private KeyCode keyRoateCamera2 = KeyCode.None;
        private KeyCode keyLeft = KeyCode.None;
        private KeyCode keyRight = KeyCode.None;
        private bool reverseRotation = false;
        private bool _LeftPressed = false;
        private bool _RightPressed = false;

        /// <summary>
        /// Get the main key listener.
        /// </summary>
        /// <value></value>
        public KeyListener KeyListener { get => keyListener; }

        private void _InitSettings()
        {
            // Initialize control settings.
            var GameSettings = GameSettingsManager.GetSettings(GamePackageManager.SYSTEM_PACKAGE_NAME);
            settingsCallbackId = GameSettings.RegisterSettingsUpdateCallback(SettingConstants.SettingsControl, (groupName, action) =>
            {
                _OnControlSettingsChanged();
                return false;
            });
            // Request a settings update.
            GameSettings.RequireSettingsLoad(SettingConstants.SettingsControl);
        }
        private void _UnInitSettings()
        {
            var GameSettings = GameSettingsManager.GetSettings(GamePackageManager.SYSTEM_PACKAGE_NAME);
            GameSettings.UnRegisterSettingsUpdateCallback(settingsCallbackId);
        }
        private void _OnControlSettingsChanged()
        {
            // Apply settings to the current fields on load or change.
            var GameSettings = GameSettingsManager.GetSettings(GamePackageManager.SYSTEM_PACKAGE_NAME);
            keyFront = GameSettings.GetKeyCode(SettingConstants.SettingsControlKeyFront);
            keyUp = GameSettings.GetKeyCode(SettingConstants.SettingsControlKeyUp);
            keyDown = GameSettings.GetKeyCode(SettingConstants.SettingsControlKeyDown);
            keyBack = GameSettings.GetKeyCode(SettingConstants.SettingsControlKeyBack);
            keyLeft = GameSettings.GetKeyCode(SettingConstants.SettingsControlKeyLeft);
            keyRight = GameSettings.GetKeyCode(SettingConstants.SettingsControlKeyRight);
            keyRoateCamera = GameSettings.GetKeyCode(SettingConstants.SettingsControlKeyRoate);
            keyRoateCamera2 = GameSettings.GetKeyCode(SettingConstants.SettingsControlKeyRoate2);
            keyUpCamera = GameSettings.GetKeyCode(SettingConstants.SettingsControlKeyUpCam);
            reverseRotation = GameSettings.GetBool(SettingConstants.SettingsControlKeyReverse);
            _InitKeys();
        }

        private void _InitKeys()
        {
            // Initialize keyboard listeners.
            keyListener = KeyListener.Get(gameObject);
            keyListener.DisableWhenUIFocused = false;
            keyListener.ClearKeyListen();
            keyListener.AddKeyListen(keyFront, _UpArrow_Key, GamepadButton.DpadUp, GamepadButton.A);
            keyListener.AddKeyListen(keyBack, _DownArrow_Key, GamepadButton.DpadDown, GamepadButton.B);
            keyListener.AddKeyListen(keyUp, _Up_Key, GamepadButton.LeftTrigger);
            keyListener.AddKeyListen(keyDown, _Down_Key, GamepadButton.RightTrigger);
            keyListener.AddKeyListen(keyUpCamera, _Space_Key, GamepadButton.Y);
            keyListener.AddKeyListen(keyRoateCamera, _Shift_Key);
            keyListener.AddKeyListen(keyRoateCamera2, _Shift_Key);
            keyListener.AddKeyListen(KeyCode.PageUp, (key, down) =>
            {
                if (down && CanControllCamera)
                {
                    if (reverseRotation)
                        GamePlayManager.Instance.CamManager.RotateLeft();
                    else
                        GamePlayManager.Instance.CamManager.RotateRight();
                }
            }, GamepadButton.LeftShoulder);
            keyListener.AddKeyListen(KeyCode.PageDown, (key, down) =>
            {
                if (down && CanControllCamera)
                {
                    if (reverseRotation)
                        GamePlayManager.Instance.CamManager.RotateRight();
                    else
                        GamePlayManager.Instance.CamManager.RotateLeft();
                }
            }, GamepadButton.RightShoulder);
            keyListener.AddKeyListen(keyLeft, _LeftArrow_Key, GamepadButton.DpadLeft);
            keyListener.AddKeyListen(keyRight, _RightArrow_Key, GamepadButton.DpadRight);

            // Test button.
            if (GameManager.DebugMode)
            {
                keyListener.AddKeyListen(KeyCode.Alpha1, (key, downed) =>
                {
                    if (downed)
                    {
                        SetNextRecoverPosToNowPos();
                        SetCurrentBall("BallWood", BallControlStatus.Control);
                    }
                });
                keyListener.AddKeyListen(KeyCode.Alpha2, (key, downed) =>
                {
                    if (downed)
                    {
                        SetNextRecoverPosToNowPos();
                        SetCurrentBall("BallStone", BallControlStatus.Control);
                    }
                });
                keyListener.AddKeyListen(KeyCode.Alpha3, (key, downed) =>
                {
                    if (downed)
                    {
                        SetNextRecoverPosToNowPos();
                        SetCurrentBall("BallPaper", BallControlStatus.Control);
                    }
                });
                keyListener.AddKeyListen(KeyCode.Alpha5, (key, downed) =>
                {
                    if (downed)
                    {
                        GamePlayManager.Instance.AddLife();
                    }
                });
                keyListener.AddKeyListen(KeyCode.Alpha6, (key, downed) =>
                {
                    if (downed)
                    {
                        GamePlayManager.Instance.Fall();
                    }
                });
                keyListener.AddKeyListen(KeyCode.Alpha7, (key, downed) =>
                {
                    if (downed)
                    {
                        if (downed)
                        {
                            SetControllingStatus(BallControlStatus.NoControl);
                            SetNextRecoverPos(Vector3.zero);
                        }
                    }
                });
            }
        }
        private void _UnInitKeys()
        {
            keyListener.ClearKeyListen();
        }

        #endregion

        #region Commands

        private int _CommandId = 0;

        private void _AddCommands()
        {
            // Register console commands.
            _CommandId = GameManager.Instance.GameDebugCommandServer.RegisterCommand("balls", (keyword, fullCmd, argsCount, args) =>
            {
                var type = args[1];
                if (type == "play-lighting")
                    PlayLighting(nextRecoverPos, true, true, null);
                else if (type == "set-recover-pos")
                {
                    if (!DebugUtils.CheckIntDebugParam(1, args, out var nx, true, 0))
                        return false;
                    if (!DebugUtils.CheckIntDebugParam(2, args, out var ny, true, 0))
                        return false;
                    if (!DebugUtils.CheckIntDebugParam(3, args, out var nz, true, 0))
                        return false;

                    SetNextRecoverPos(new Vector3(nx, ny, nz));
                    Log.D(TAG, "NextRecoverPos now is : {0}", new Vector3(nx, ny, nz));
                }
                else if (type == "set-ball")
                {
                    if (!DebugUtils.CheckDebugParam(1, args, out var n, true, ""))
                        return false;

                    SetCurrentBall(n);
                    Log.D(TAG, "Set ball type to : {0}", n);
                }
                else if (type == "set-control-status")
                {
                    if (!DebugUtils.CheckIntDebugParam(1, args, out var n, true, 1))
                        return false;

                    SetControllingStatus((BallControlStatus)n);
                    Log.D(TAG, "Set control status to : {0}", n);
                }
                return true;
            }, 0, "balls <set-recover-pos/set-ball/set-control-status/play-lighting> Ball manager commands" +
                "  set-recover-pos <x:number> <y:number> <z:number> ▶ Set the next ball activation position" +
                "  set-ball <ballName:string>                       ▶ Set the active ball" +
                "  set-control-status <status:number>               ▶ Set control mode: 0 none, 1 normal, 2 release, 3 locked, 4 release 2" +
                "  play-lighting                                    ▶ Play the spawn animation");
        }
        private void _DeleteCommands()
        {
            GameManager.Instance.GameDebugCommandServer.UnRegisterCommand(_CommandId);
        }

        #endregion

        #region Events

        /// <summary>
        /// Ball registered.
        /// </summary>
        [HideInInspector]
        public GameEventEmitterStorage EventBallRegistered;
        /// <summary>
        /// Ball unregistered.
        /// </summary>
        [HideInInspector]
        public GameEventEmitterStorage EventBallUnRegister;
        /// <summary>
        /// Current ball changed.
        /// </summary>
        [HideInInspector]
        public GameEventEmitterStorage EventCurrentBallChanged;
        /// <summary>
        /// Next ball spawn position changed.
        /// </summary>
        [HideInInspector]
        public GameEventEmitterStorage EventNextRecoverPosChanged;
        /// <summary>
        /// Ball control state changed.
        /// </summary>
        [HideInInspector]
        public GameEventEmitterStorage EventControllingStatusChanged;
        /// <summary>
        /// Play smoke event.
        /// </summary>
        [HideInInspector]
        public GameEventEmitterStorage EventPlaySmoke;
        /// <summary>
        /// Play lightning event.
        /// </summary>
        [HideInInspector]
        public GameEventEmitterStorage EventPlayLighting;
        /// <summary>
        /// Refresh ball force state.
        /// </summary>
        [HideInInspector]
        public GameEventEmitterStorage EventFlushBallPush;
        /// <summary>
        /// Ball force value updated manually.
        /// </summary>
        [HideInInspector]
        public GameEventEmitterStorage EventSetBallPushValue;
        /// <summary>
        /// Clear ball forces.
        /// </summary>
        [HideInInspector]
        public GameEventEmitterStorage EventRemoveAllBallPush;

        private void _InitEvents()
        {
            var events = GameManager.GameMediator.RegisterEventEmitter("BallManager");
            this.EventBallRegistered = events.RegisterEvent("BallRegistered");
            this.EventBallUnRegister = events.RegisterEvent("BallUnRegister");
            this.EventCurrentBallChanged = events.RegisterEvent("CurrentBallChanged");
            this.EventNextRecoverPosChanged = events.RegisterEvent("NextRecoverPosChanged");
            this.EventControllingStatusChanged = events.RegisterEvent("ControllingStatusChanged");
            this.EventPlaySmoke = events.RegisterEvent("PlaySmoke");
            this.EventPlayLighting = events.RegisterEvent("PlayLighting");
            this.EventFlushBallPush = events.RegisterEvent("FlushBallPush");
            this.EventSetBallPushValue = events.RegisterEvent("SetBallPushValue");
            this.EventRemoveAllBallPush = events.RegisterEvent("RemoveAllBallPush");
        }
        private void _DeleteEvents()
        {
            GameManager.GameMediator.UnRegisterEventEmitter("BallManager");
        }

        #endregion

        #region Ball registration

        private Dictionary<string, BallRegStorage> registerBalls = new Dictionary<string, BallRegStorage>();

        private void _CollectRegisterBalls()
        {
            // Fragment cleanup timer.
            foreach (var value in registerBalls.Values)
            {
                if (value.ball._PiecesData != null)
                {
                    var data = value.ball._PiecesData;
                    if (data.delayHideTimerID > 0)
                    {
                        GameTimer.DeleteDelay(data.delayHideTimerID);
                        data.delayHideTimerID = 0;
                    }
                    if (data.fadeOutTimerID > 0)
                    {
                        GameTimer.DeleteDelay(data.fadeOutTimerID);
                        data.fadeOutTimerID = 0;
                    }
                }
            }
            // Clear.
            registerBalls.Clear();
        }

        /// <summary>
        /// Register a ball.
        /// </summary>
        /// <param name="name">Ball name.</param>
        /// <param name="gameObject">Ball GameObject, already added to the scene.</param>
        public void RegisterBall(string name, GameObject gameObject)
        {
            // Check registration.
            if (GetRegisterBall(name) != null)
            {
                GameErrorChecker.SetLastErrorAndLog(GameError.AlreadyRegistered, TAG, "Ball {0} already registered", name);
                return;
            }

            // Add the rigid-body component.
            PhysicsObject body = gameObject.AddComponent<PhysicsObject>();
            // Find physics parameters.
            if (!GamePhysBall.Data.TryGetValue(name, out var physicsData))
            {
                GameErrorChecker.SetLastErrorAndLog(GameError.ParamNotFound, TAG, "Not fuoud GamePhysBall data for Ball {0} , please add it before call RegisterBall", name);
                return;
            }
            body.DoNotAutoCreateAtAwake = true;
            // Set physics parameters.
            body.Fixed = false;
            body.Mass = physicsData.Mass;
            if (physicsData.BallRadius > 0)
            {
                body.UseBall = true;
                body.BallRadius = physicsData.BallRadius;
            }
            else
            {
                body.UseBall = false;
            }
            body.Friction = physicsData.Friction;
            body.Elasticity = physicsData.Elasticity;
            body.RotSpeedDamping = physicsData.RotDamp;
            body.LinearSpeedDamping = physicsData.LinearDamp;
            body.Layer = physicsData.Layer;
            body.EnableCollision = true;
            body.AutoControlActive = false;
            body.AutoMassCenter = false;
            body.UseExistsSurface = true;
            body.CustomDePhysicsFall = true;
            body.OnFallCallback = (body) =>
            {
                if (!GamePlayManager.Instance.Fall())
                {
                    body.UnPhysicalize(true);
                    body.gameObject.SetActive(false);
                }
            };

            // Set the constant force.
            body.EnableConstantForce = true;

            // Add a speed meter.
            var speedMeter = gameObject.GetComponent<SpeedMeter>();
            if (speedMeter == null)
            {
                speedMeter = gameObject.AddComponent<SpeedMeter>();
                speedMeter.Enabled = true;
            }

            Ball ball = gameObject.GetComponent<Ball>();
            if (ball == null)
            {
                GameErrorChecker.SetLastErrorAndLog(GameError.ClassNotFound, TAG, "Not found Ball class on {0} !", name);
                return;
            }

            var pieces = ball.GetPieces();
            // Set ball physics parameters.
            ball._PiecesPhysicsData = physicsData.PiecesPhysicsData;
            // Set movement force parameters.
            ball._PiecesMinForce = physicsData.PiecesMinForce;
            ball._PiecesMaxForce = physicsData.PiecesMaxForce;
            ball._Force = physicsData.Force;
            ball._UpForce = physicsData.UpForce * 1.5f;
            ball._DownForce = physicsData.DownForce;
            if (pieces != null)
                ObjectStateBackupUtils.BackUpObjectAndChilds(pieces); // Back up fragment state.

            // A Unity collider is also needed for death-zone detection.
            var collder = gameObject.AddComponent<SphereCollider>();
            collder.radius = physicsData.TiggerBallRadius;
            var rigidbody = gameObject.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            // Set the name.
            if (gameObject.name != name)
                gameObject.name = name;

            EventBallRegistered.Emit(ball, body);

            // Add metadata.
            registerBalls[name] = new BallRegStorage()
            {
                name = name,
                ball = ball,
                rigidbody = body,
                speedMeter = speedMeter,
            };
        }
        /// <summary>
        /// Unregister a ball.
        /// </summary>
        /// <param name="name">Ball name.</param>
        public bool UnRegisterBall(string name)
        {
            if (registerBalls.TryGetValue(name, out var ball))
            {
                if (ball == currentActiveBall)
                    _DeactiveCurrentBall();

                registerBalls.Remove(name);
                EventBallUnRegister.Emit(name);
                return true;
            }
            GameErrorChecker.SetLastErrorAndLog(GameError.NotRegister, TAG, "Ball {0} not register", name);
            return false;
        }
        /// <summary>
        /// Get a registered ball.
        /// </summary>
        /// <param name="name">Ball name.</param>
        public BallRegStorage GetRegisterBall(string name)
        {
            if (registerBalls.TryGetValue(name, out var ball))
                return ball;
            return null;
        }

        #endregion

        private BallRegStorage currentBall = null;
        private BallRegStorage currentActiveBall = null;
        private BallControlStatus controllingStatus = BallControlStatus.NoControl;
        private Vector3 nextRecoverPos = Vector3.zero;

        /// <summary>
        /// Get the active ball.
        /// </summary>
        public BallRegStorage GetCurrentBall() { return currentBall; }
        /// <summary>
        /// Set the controlled ball.
        /// </summary>
        /// <param name="name">Nonempty ball name.</param>
        /// <param name="status">New control state; null preserves the previous state.</param>
        public void SetCurrentBall(string name, BallControlStatus status = BallControlStatus.KeepOldStatus)
        {
            if (string.IsNullOrEmpty(name))
            {
                GameErrorChecker.SetLastErrorAndLog(GameError.NotRegister, TAG, "You must provide a name for the ball");
                return;
            }
            var ball = GetRegisterBall(name);
            if (ball == null)
            {
                GameErrorChecker.SetLastErrorAndLog(GameError.NotRegister, TAG, "Ball {0} not register", name);
                return;
            }
            if (currentBall != ball)
            {
                _DeactiveCurrentBall();
                currentBall = ball;
                CurrentBall = ball.ball;
                CurrentBallName = ball.name;
                EventCurrentBallChanged.Emit(name);
                SetControllingStatus(status);
            }
        }
        /// <summary>
        /// Disable the currently controlled ball.
        /// </summary>
        public void SetNoCurrentBall()
        {
            EventCurrentBallChanged.Emit("");
            _DeactiveCurrentBall();
        }
        /// <summary>
        /// Set the current ball's control state.
        /// </summary>
        /// <param name="status">New state; KeepOldStatus refreshes without changing it.</param>
        public void SetControllingStatus(BallControlStatus status = BallControlStatus.KeepOldStatus)
        {
            if (status != BallControlStatus.KeepOldStatus)
            {
                controllingStatus = status;
                _FlushCurrentBallAllStatus();
            }
            else if (status != controllingStatus)
            {
                _FlushCurrentBallAllStatus();
            }
        }
        /// <summary>
        /// Get the current ball's control state.
        /// </summary>
        public BallControlStatus GetControllingStatus()
        {
            return controllingStatus;
        }
        /// <summary>
        /// Set the next ball spawn position.
        /// </summary>
        /// <param name="pos">Spawn position.</param>
        public void SetNextRecoverPos(Vector3 pos)
        {
            EventNextRecoverPosChanged.Emit(pos);
            nextRecoverPos = pos;
        }
        /// <summary>
        /// Use the current ball position as the next spawn position.
        /// </summary>
        public void SetNextRecoverPosToNowPos()
        {
            var current = currentActiveBall;
            if (current != null)
                SetNextRecoverPos(current.ball.transform.position);
        }
        /// <summary>
        /// Reset a ball's fragments.
        /// </summary>
        /// <param name="typeName">Nonempty ball name.</param>
        public void ResetPeices(string typeName)
        {
            var ball = GetRegisterBall(typeName);
            if (ball != null)
                ball.ball.ResetPieces();
            else
                Log.W(TAG, "Ball type {0} not found", typeName);
        }
        /// <summary>
        /// Scatter the ball's fragments at NextRecoverPos.
        /// </summary>
        /// <param name="typeName">Nonempty ball name.</param>
        public void ThrowPeices(string typeName)
        {
            ThrowPeices(typeName, nextRecoverPos);
        }
        /// <summary>
        /// Scatter the ball's fragments.
        /// </summary>
        /// <param name="typeName">Nonempty ball name.</param>
        /// <param name="pos">Scatter position.</param>
        public void ThrowPeices(string typeName, Vector3 pos)
        {
            var ball = GetRegisterBall(typeName);
            if (ball != null)
                ball.ball.ThrowPieces(pos);
            else
                Log.W(TAG, "Ball type {0} not found", typeName);
        }
        /// <summary>
        /// Resume camera movement.
        /// </summary>
        public void StartCamMove()
        {
            if (GamePlayManager.Instance.CamManager.Target == null)
                GamePlayManager.Instance.CamManager.SetTarget(currentBall.ball.transform, true);
        }
        /// <summary>
        /// Stop camera movement.
        /// </summary>
        public void StopCamMove()
        {
            if (GamePlayManager.Instance.CamManager.Target == null)
                GamePlayManager.Instance.CamManager.SetTarget(null, false);
        }
        /// <summary>
        /// Reset all fragment states.
        /// </summary>
        public void ResetAllPeices()
        {
            foreach (var item in registerBalls)
                item.Value.ball.ResetPieces();
        }

        #region Ball state operations

        private int debugFlushInfoTimer = 0;

        private void _SetStateDebugStatus(string stat)
        {
            if (GameManager.DebugMode && GamePlayUIControl.Instance.DebugStatValues.ContainsKey("CurrentStatus"))
            {
                GamePlayUIControl.Instance.DebugStatValues["CurrentStatus"].Value = stat;
            }
        }
        private void _FlushCurrentBallAllStatus()
        {
            var CamManager = GamePlayManager.Instance.CamManager;

            switch (controllingStatus)
            {
                case BallControlStatus.NoControl:
                    {
                        _SetCanControl(false);
                        CanControllCamera = false;
                        _DeactiveCurrentBall();
                        CamManager.DisbleAll();
                        _SetStateDebugStatus("NoControl");
                        break;
                    }
                case BallControlStatus.Control:
                    {
                        _SetCanControl(true);
                        CanControllCamera = true;
                        _ActiveCurrentBall();
                        _PhysicsOrDePhysicsCurrentBall(true);
                        CamManager.SetCamLook(true);
                        CamManager.SetCamFollow(true);
                        _SetStateDebugStatus("Control");
                        break;
                    }
                case BallControlStatus.LockMode:
                    {
                        _SetCanControl(false);
                        CanControllCamera = true;
                        _ActiveCurrentBall();
                        _PhysicsOrDePhysicsCurrentBall(false);
                        CamManager.SetCamLook(true);
                        CamManager.SetCamFollow(true);
                        _SetStateDebugStatus("LockMode");
                        break;
                    }
                case BallControlStatus.UnleashingMode:
                    {
                        _SetCanControl(false);
                        CanControllCamera = true;
                        _ActiveCurrentBall();
                        _PhysicsOrDePhysicsCurrentBall(true);
                        CamManager.SetCamLook(true);
                        CamManager.SetCamFollow(false);
                        _SetStateDebugStatus("UnleashingMode");
                        break;
                    }
                case BallControlStatus.FreeMode:
                    {
                        _SetCanControl(false);
                        CanControllCamera = true;
                        _ActiveCurrentBall();
                        _PhysicsOrDePhysicsCurrentBall(true);
                        CamManager.SetCamLook(true);
                        CamManager.SetCamFollow(true); ;
                        _SetStateDebugStatus("FreeMode");
                        break;
                    }
                case BallControlStatus.LockLookMode:
                    {
                        _SetCanControl(false);
                        CanControllCamera = false;
                        _ActiveCurrentBall();
                        _PhysicsOrDePhysicsCurrentBall(false);
                        CamManager.SetCamLook(true);
                        CamManager.SetCamFollow(false);
                        _SetStateDebugStatus("LockLookMode");
                        break;
                    }
                default:
                    {
                        Log.W(TAG, "Bad ball control state {0}", controllingStatus);
                        break;
                    }
            }
            EventControllingStatusChanged.Emit(null);
        }
        private void _SetCanControl(bool can) { CanControll = can; }
        /// <summary>
        /// Deactivate the current ball.
        /// </summary>
        private void _DeactiveCurrentBall()
        {
            var current = currentActiveBall;
            if (current != null)
            {
                // Hide the shadow.
                _BallShadowProjector.constraintActive = false;
                if (_BallShadowProjector.gameObject.activeSelf)
                    _BallShadowProjector.gameObject.SetActive(false);
                // Clear forces.
                RemoveAllBallPush();
                // Deactivate.
                if (current.rigidbody.IsPhysicalized)
                    _PhysicsOrDePhysicsCurrentBall(false);
                current.ball.gameObject.SetActive(false);
                // Clear the camera follow target.
                GamePlayManager.Instance.CamManager.SetTarget(null, false);
                currentActiveBall = null;

                if (GameManager.DebugMode)
                    GamePlayUIControl.Instance.DebugStatValues["CurrentBall"].Value = "None";
            }
            if (GameManager.DebugMode)
            {
                // Delete the timer.
                if (debugFlushInfoTimer > 0)
                {
                    GameTimer.DeleteTimer(debugFlushInfoTimer);
                    debugFlushInfoTimer = 0;
                }
            }
        }
        private void _ActiveCurrentBall()
        {
            var current = currentActiveBall;
            if (current == null && currentBall != null)
            {
                current = currentBall;
                var currentTransform = current.ball.transform;
                currentActiveBall = currentBall;
                // Set shadow position and parent constraint.
                var constraintSource = new ConstraintSource();
                constraintSource.sourceTransform = currentTransform;
                constraintSource.weight = 1;
                _BallShadowProjector.SetSource(0, constraintSource);
                _BallShadowProjector.constraintActive = true;
                // Show the shadow.
                if (!_BallShadowProjector.gameObject.activeSelf)
                    _BallShadowProjector.gameObject.SetActive(true);
                // Set position.
                currentTransform.position = nextRecoverPos;
                // Set the camera follow target.
                GamePlayManager.Instance.CamManager.SetTarget(currentTransform, false);
                // Clear forces.
                RemoveAllBallPush();
                // Debug information.
                if (GameManager.DebugMode)
                {
                    var _DebugStatValues = GamePlayUIControl.Instance.DebugStatValues;
                    var Position = _DebugStatValues["Position"];
                    var Rotation = _DebugStatValues["Rotation"];
                    var Velocity = _DebugStatValues["Velocity"];
                    var PushValue = _DebugStatValues["PushValue"];
                    var PhysicsTime = _DebugStatValues["PhysicsTime"];
                    var PhysicsObjects = _DebugStatValues["PhysicsObjects"];
                    var PhysicsState = _DebugStatValues["PhysicsState"];
                    var GamePhysicsWorld = GamePlayManager.Instance.GamePhysicsWorld;
                    _DebugStatValues["CurrentBall"].Value = current.name;

                    // Delete the timer.
                    if (debugFlushInfoTimer > 0)
                    {
                        GameTimer.DeleteTimer(debugFlushInfoTimer);
                        debugFlushInfoTimer = 0;
                    }
                    // Update the ball position debug display once per second.
                    debugFlushInfoTimer = GameTimer.Timer(0.5f, () =>
                    {

                        if (!GameManager.Instance.GameSettings.GetBool("debugDisableBallInfo", false))
                        {
                            // Ball position.
                            Position.SetVector3Value(currentTransform.position);
                            Rotation.SetVector3Value(currentTransform.eulerAngles);
                        }
                        if (!GameManager.Instance.GameSettings.GetBool("debugDisablePhysicsInfo", false))
                        {
                            // Physics time.
                            PhysicsTime.Value = string.Format("{0:F2} ms", GamePhysicsWorld.PhysicsTime * 1000);
                            // Physics object information.
                            PhysicsObjects.Value = string.Format("All/Active/Update {0}/{1}/{2}\nFixed/FallCollect/Push {3}/{4}/{5}",
                              GamePhysicsWorld.PhysicsBodies,
                              GamePhysicsWorld.PhysicsActiveBodies,
                              GamePhysicsWorld.PhysicsUpdateBodies,
                              GamePhysicsWorld.PhysicsFixedBodies,
                              GamePhysicsWorld.PhysicsFallCollectBodies,
                              GamePhysicsWorld.PhysicsConstantPushBodies
                            );
                        }

                        if (!GameManager.Instance.GameSettings.GetBool("debugDisableBallInfo", false))
                        {
                            // Ball speed and force data.
                            if (current.rigidbody.IsPhysicalized)
                            {
                                PhysicsState.Value = string.Format("{0:F2} ContractState: {1}\n{2}",
                                  current.speedMeter.NowAbsoluteSpeed,
                                  current.rigidbody.ContractCacheString,
                                  current.ball._SoundConfig._SoundManagerDebugString.ToString()
                                );

                                Velocity.SetVector3Value(current.rigidbody.SpeedVector);
                                PushValue.Value = string.Format("{0:F2}, {1:F2}, {2:F2})",
                                  current.pushForceX.Force,
                                  current.pushForceY.Force,
                                  current.pushForceZ.Force
                                );
                            }
                            else
                            {
                                PhysicsState.Value = "";
                                PushValue.Value = "";
                                Velocity.Value = "";
                            }
                        }
                    });
                }
            }
        }
        private void _PhysicsOrDePhysicsCurrentBall(bool physics)
        {
            var CamManager = GamePlayManager.Instance.CamManager;
            var current = currentActiveBall;
            if (current != null)
            {
                // Activate.
                var physicsed = current.rigidbody.IsPhysicalized;
                current.ball.gameObject.SetActive(true);
                if (physics && !physicsed)
                {
                    current.rigidbody.Physicalize();
                    current.rigidbody.ClearConstantForce();
                    current.rigidbody.WakeUp();
                    // Add the ball's constant forces along X, Y, and Z.
                    current.pushForceX = current.rigidbody.AddConstantForceWithPositionAndRef(0, Vector3.right, Vector3.zero, CamManager.CamDirectionRef, current.ball.transform);
                    current.pushForceZ = current.rigidbody.AddConstantForceWithPositionAndRef(0, Vector3.forward, Vector3.zero, CamManager.CamDirectionRef, current.ball.transform);
                    current.pushForceY = current.rigidbody.AddConstantForceWithPositionAndRef(0, Vector3.up, Vector3.zero, CamManager.CamDirectionRef, current.ball.transform);
                    // Activate.
                    current.ball.Active();
                    // Start ball sounds.
                    GamePlayManager.Instance.BallSoundManager.AddSoundableBall(current.rigidbody, current.ball._SoundConfig);
                    // Stop applying movement forces.
                    RemoveAllBallPush();
                    // Resend held-key states after switching balls so the new ball receives the correct constant forces.
                    keyListener.ReSendPressingKey();
                }
                if (!physics)
                {
                    // Stop applying movement forces.
                    RemoveAllBallPush();
                    // Stop playing sounds.
                    GamePlayManager.Instance.BallSoundManager.RemoveSoundableBall(current.rigidbody, current.ball._SoundConfig);
                    GamePlayManager.Instance.BallSoundManager.StopSoundableBallAllSound(current.rigidbody, current.ball._SoundConfig);
                    // Remove the ball's movement forces.
                    if (current.pushForceX != null)
                    {
                        current.pushForceX.Delete();
                        current.pushForceX = null;
                    }
                    if (current.pushForceZ != null)
                    {
                        current.pushForceZ.Delete();
                        current.pushForceZ = null;
                    }
                    if (current.pushForceY != null)
                    {
                        current.pushForceY.Delete();
                        current.pushForceY = null;
                    }
                    // Deactivate.
                    current.ball.Deactive();
                    if (physicsed)
                        current.rigidbody.UnPhysicalize(true);
                }
            }
        }
        internal void SetCanControlCameraWhenStart()
        {
            CanControllCamera = true;
            GamePlayManager.Instance.CamManager.SetCamFollow(true);
        }

        #endregion

        #region Ball helpers

        /// <summary>
        /// Play ball spawn smoke.
        /// </summary>
        /// <param name="pos">Placement position.</param>
        public void PlaySmoke(Vector3 pos)
        {
            _BallSmoke.transform.position = pos;
            _BallSmoke.SetActive(true);
            EventPlaySmoke.Emit(null);
        }
        /// <summary>
        /// Play the ball spawn lightning effect.
        /// </summary>
        /// <param name="pos">Placement position.</param>
        /// <param name="smallToBig">Grow from small to large.</param>
        /// <param name="lightAnim">Also play the light effect.</param>
        /// <param name="callback">Called when playback finishes.</param>
        public void PlayLighting(Vector3 pos, bool smallToBig, bool lightAnim, VoidDelegate callback)
        {
            EventPlayLighting.Emit(null);
            _BallLightningSphere.PlayLighting(pos, smallToBig, callback, lightAnim);
        }
        /// <summary>
        /// Whether the spawn lightning effect is running.
        /// </summary>
        public bool IsLighting() { return _BallLightningSphere.IsLighting(); }
        /// <summary>
        /// Lock the ball and move it quickly to a target.
        /// </summary>
        /// <param name="pos">Target position.</param>
        /// <param name="time">Duration in seconds.</param>
        /// <param name="callback">Called when movement finishes.</param>
        public void FastMoveTo(Vector3 pos, float time, SmoothFly.CallbackDelegate callback)
        {
            // Lock.
            SetControllingStatus(BallControlStatus.LockMode);

            if (currentActiveBall != null)
            {
                var ball = currentActiveBall.ball;
                SmoothFly mover = ball.gameObject.GetComponent<SmoothFly>();
                if (mover == null)
                    mover = ball.gameObject.AddComponent<SmoothFly>();

                mover.TargetPos = pos;
                mover.Time = time;
                mover.ArrivalDiatance = 0.02f;
                mover.StopWhenArrival = true;
                mover.ArrivalCallback = callback;
                mover.Fly = true;
            }
        }

        #endregion

        #region Keyboard event handling

        private void _UpArrow_Key(KeyCode key, bool down)
        {
            KeyStateForward = down;
            FlushBallPush();
        }
        private void _DownArrow_Key(KeyCode key, bool down)
        {
            KeyStateBack = down;
            FlushBallPush();
        }
        private void _RightArrow_Key(KeyCode key, bool down)
        {
            _RightPressed = down;
            if (down)
            {
                // Rotate the camera.
                if (CanControllCamera && ShiftPressed)
                {
                    if (reverseRotation)
                        GamePlayManager.Instance.CamManager.RotateLeft();
                    else
                        GamePlayManager.Instance.CamManager.RotateRight();
                    // Do not move the ball while Shift is held.
                    KeyStateRight = false;
                }
                else
                    KeyStateRight = true;
                FlushBallPush();
            }
            else
            {
                KeyStateRight = false;
                FlushBallPush();
            }
        }
        private void _LeftArrow_Key(KeyCode key, bool down)
        {
            _LeftPressed = down;
            if (down)
            {
                /// Rotate the camera.
                if (CanControllCamera && ShiftPressed)
                {
                    if (reverseRotation)
                        GamePlayManager.Instance.CamManager.RotateRight();
                    else
                        GamePlayManager.Instance.CamManager.RotateLeft();
                    // Do not move the ball while Shift is held.
                    KeyStateLeft = false;
                }
                else
                    KeyStateLeft = true;
                FlushBallPush();
            }
            else
            {
                KeyStateLeft = false;
                FlushBallPush();
            }
        }
        private void _Down_Key(KeyCode key, bool down)
        {
            if (GameManager.DebugMode)
            {
                KeyStateDown = down;
                FlushBallPush();
            }
        }
        private void _Up_Key(KeyCode key, bool down)
        {
            if (GameManager.DebugMode)
            {
                KeyStateUp = down;
                FlushBallPush();
            }
        }
        private void _Space_Key(KeyCode key, bool down)
        {
            if (CanControllCamera)
            {
                GamePlayManager.Instance.CamManager.RotateUp(down);
            }
        }
        private void _Shift_Key(KeyCode key, bool down)
        {
            ShiftPressed = down;
            if (_LeftPressed)
            {
                // Rotate the camera.
                if (down && CanControllCamera)
                {
                    if (reverseRotation)
                        GamePlayManager.Instance.CamManager.RotateRight();
                    else
                        GamePlayManager.Instance.CamManager.RotateLeft();
                    // Disable leftward force.
                    KeyStateLeft = false;
                }
                else
                {
                    // Restore leftward force on release.
                    KeyStateLeft = true;
                }
                FlushBallPush();
            }
            else if (_RightPressed)
            {
                // Rotate the camera.
                if (down && CanControllCamera)
                {
                    if (reverseRotation)
                        GamePlayManager.Instance.CamManager.RotateLeft();
                    else
                        GamePlayManager.Instance.CamManager.RotateRight();
                    // Disable rightward force.
                    KeyStateRight = false;
                }
                else
                {
                    // Restore rightward force on release.
                    KeyStateRight = true;
                }
                FlushBallPush();
            }
        }

        #endregion

        #region Force operations

        /// <summary>
        /// Refresh directional ball input.
        /// </summary>
        public void FlushBallPush()
        {
            if (CurrentBall == null || currentActiveBall == null || !CanControll)
                return;

            var currentBall = currentActiveBall;
            var force = CurrentBall._Force * BallSpeedFactor;

            // Forward and backward.
            if (KeyStateForward && KeyStateBack)
                currentBall.pushForceZ.Force = 0;
            else if (KeyStateForward)
                currentBall.pushForceZ.Force = force;
            else if (KeyStateBack)
                currentBall.pushForceZ.Force = -force;
            else
                currentBall.pushForceZ.Force = 0;
            // Left and right.
            if (KeyStateLeft && KeyStateRight)
                currentBall.pushForceX.Force = 0;
            else if (KeyStateLeft)
                currentBall.pushForceX.Force = -force;
            else if (KeyStateRight)
                currentBall.pushForceX.Force = force;
            else
                currentBall.pushForceX.Force = 0;
            /// Up and down.
            if (KeyStateUp && KeyStateDown)
                currentBall.pushForceY.Force = 0;
            else if (KeyStateUp)
                currentBall.pushForceY.Force = currentBall.ball._UpForce;
            else if (KeyStateDown)
                currentBall.pushForceY.Force = -currentBall.ball._DownForce;
            else
                currentBall.pushForceY.Force = 0;

            EventFlushBallPush.Emit(null);
        }

        /// <summary>
        /// Set the ball force direction values, for example from a joystick.
        /// </summary>
        /// <param name="x">X-axis force fraction in [-1, 1].</param>
        /// <param name="y">Y-axis force fraction in [-1, 1].</param>
        public void SetBallPushValue(float x, float y)
        {
            if (CurrentBall == null || currentActiveBall == null || !CanControll)
                return;
            var currentBall = currentActiveBall;
            var force = CurrentBall._Force;
            EventSetBallPushValue.Emit(x, y);
            currentBall.pushForceX.Force = x * force;
            currentBall.pushForceZ.Force = y * force;
        }
        /// <summary>
        /// Clear all movement directions for the current ball.
        /// </summary>
        public void RemoveAllBallPush()
        {
            if (CurrentBall == null || currentActiveBall == null || !CanControll)
                return;
            var currentBall = currentActiveBall;
            EventRemoveAllBallPush.Emit(null);

            if (currentBall.pushForceX != null) currentBall.pushForceX.Force = 0;
            if (currentBall.pushForceY != null) currentBall.pushForceY.Force = 0;
            if (currentBall.pushForceZ != null) currentBall.pushForceZ.Force = 0;
        }

        #endregion
    }

    /// <summary>
    /// Ball control state.
    /// </summary>
    public enum BallPushType
    {
        /// <summary>
        /// No movement force.
        /// </summary>
        None = 0,
        /// <summary>
        /// Forward.
        /// </summary>
        Forward = 0x2,
        /// <summary>
        /// Backward.
        /// </summary>
        Back = 0x4,
        /// <summary>
        /// Left.
        /// </summary>
        Left = 0x8,
        /// <summary>
        /// Right.
        /// </summary>
        Right = 0x10,
        /// <summary>
        /// Upward.
        /// </summary>
        Up = 0x20,
        /// <summary>
        /// Downward.
        /// </summary>
        Down = 0x40,
    }
    /// <summary>
    /// Ball control state.
    /// </summary>
    public enum BallControlStatus
    {
        /// <summary>
        /// Preserve the previous state when passed as a parameter.
        /// </summary>
        KeepOldStatus = -1,
        /// <summary>
        /// No control, physics, or camera following.
        /// </summary>
        NoControl = 0,
        /// <summary>
        /// Normal control with physics and camera following.
        /// </summary>
        Control = 1,
        /// <summary>
        /// Released state, such as falling: physics remains active, control and camera following stop, but the camera looks at the ball.
        /// </summary>
        UnleashingMode = 2,
        /// <summary>
        /// Locked state, such as transformation: no physics or control, with camera following.
        /// </summary>
        LockMode = 3,
        /// <summary>
        /// Second released state: physics remains active without control; the camera follows and looks at the ball.
        /// </summary>
        FreeMode = 4,
        /// <summary>
        /// No physics or control; the camera looks at the ball without following.
        /// </summary>
        LockLookMode = 5,
    }
    /// <summary>
    /// Ball registration data.
    /// </summary>
    public class BallRegStorage
    {
        /// <summary>
        /// Ball name.
        /// </summary>
        public string name = "";
        /// <summary>
        /// Ball class instance.
        /// </summary>
        public Ball ball = null;
        /// <summary>
        /// Ball rigid body, available only while this ball is active.
        /// </summary>
        public PhysicsObject rigidbody = null;
        /// <summary>
        /// Speed meter.
        /// </summary>
        public SpeedMeter speedMeter = null;
        /// <summary>
        /// X-axis force, available only while this ball is active.
        /// </summary>
        public PhysicsConstantForceData pushForceX = null;
        /// <summary>
        /// Y-axis force, available only while this ball is active.
        /// </summary>
        public PhysicsConstantForceData pushForceY = null;
        /// <summary>
        /// Z-axis force, available only while this ball is active.
        /// </summary>
        public PhysicsConstantForceData pushForceZ = null;
    }
}