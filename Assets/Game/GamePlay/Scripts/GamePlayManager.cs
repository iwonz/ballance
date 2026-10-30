using System.Collections;
using System.Collections.Generic;
using Ballance2.Base;
using Ballance2.Game.GamePlay.Balls;
using Ballance2.Game.GamePlay.Other;
using Ballance2.Game.GamePlay.Tranfo;
using Ballance2.Game.Utils;
using Ballance2.Menu;
using Ballance2.Package;
using Ballance2.Services;
using Ballance2.Utils;
using BallancePhysics.Wapper;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace Ballance2.Game.GamePlay
{
    public class GamePlayManager : GameSingletonBehavior<GamePlayManager>
    {
        private const string TAG = "GamePlayManager";

        #region Public properties

        /// <summary>
        /// Starting lives for the current level.
        /// </summary>
        public int StartLife = 3;
        /// <summary>
        /// Starting time points for the current level.
        /// </summary>
        public int StartPoint = 1000;
        /// <summary>
        /// Base score for the current level.
        /// </summary>
        public int LevelScore = 100;
        /// <summary>
        /// Starting ball name for the current level.
        /// </summary>
        public string StartBall = "BallWood";
        /// <summary>
        /// Next level name; empty means no next level.
        /// </summary>
        public string NextLevelName = "";
        /// <summary>
        /// Physics environment.
        /// </summary>
        public PhysicsEnvironment GamePhysicsWorld { get; private set; }
        /// <summary>
        /// Camera manager.
        /// </summary>
        public CamManager CamManager;
        /// <summary>
        /// Ball manager.
        /// </summary>
        public BallManager BallManager;
        /// <summary>
        /// Sector manager.
        /// </summary>
        public SectorManager SectorManager;
        /// <summary>
        /// Background music manager.
        /// </summary>
        public MusicManager MusicManager;
        /// <summary>
        /// Ball sound manager.
        /// </summary>
        public BallSoundManager BallSoundManager;
        /// <summary>
        /// Ball sound manager.
        /// </summary>
        public LevelBuilder.LevelBuilder LevelBuilderRef
        {
            get { return LevelBuilder.LevelBuilder.Instance; }
        }

        /// <summary>
        /// Current level name.
        /// </summary>
        public string CurrentLevelName = "";
        /// <summary>
        /// Current time points.
        /// </summary>
        public int CurrentPoint = 0;
        /// <summary>
        /// Current lives.
        /// </summary>
        public int CurrentLife = 0;
        /// <summary>
        /// Current sector.
        /// </summary>
        public int CurrentSector = 0;
        /// <summary>
        /// Whether the level has been completed.
        /// </summary>
        public bool CurrentLevelPass { get; private set; } = false;
        public bool CurrentDisableStart = false;
        public bool CurrentEndWithUFO = false;
        public bool CanEscPause = true;

        #endregion

        internal bool _IsGamePlaying = false;
        public bool IsUserPaused { get; private set; }
        public bool IsSessionOpen { get; private set; }
        private bool pausedPlaying, pausedCountdown, pausedSimulation, pausedMovement, pausedCamera, pausedCamEnabled;
        private BallControlStatus pausedControl;
        private int sessionGeneration;

        private void CancelPendingStart()
        {
            sessionGeneration++;
            _DethLock = false;
            IsUserPaused = false;
            BallManager.CancelLighting();
            CamManager.enabled = true;
        }

        private IEnumerator StartWhenReady(float delay, int generation, GameManager.VoidDelegate callback)
        {
            while (generation == sessionGeneration && (delay > 0 || IsUserPaused)) {
                if (!IsUserPaused) delay -= Time.deltaTime;
                yield return null;
            }
            if (generation == sessionGeneration && IsSessionOpen) callback();
        }

        internal bool _IsCountDownPoint = false;
        internal bool _BallBirthed = false;
        internal bool _FirstStart = false;

        private float _UpdateTick = 0;
        private List<int> _CommandIds = new List<int>();
        private int _HideBalloonEndTimerID = 0;
        //Used by Tutorial
        internal bool _ShouldStartByCustom = false;
        private int _EscKeyIds = 0;

        private void Awake()
        {
            var Mediator = GameManager.GameMediator;

            _InitPhysicsWorld();
            _InitSounds();
            _InitKeyEvents();
            _InitSettings();
            _InitEvents();

            // Register global events.
            Mediator.SubscribeSingleEvent(GamePackage.GetSystemPackage(), "CoreGamePlayManagerInitAndStart", "GamePlayManager", (evtName, param) =>
            {
                this._InitAndStart();
                return false;
            });

            // Register console commands.
            this._AddCommands();
        }
        protected override void OnDestroy()
        {
            Log.D(TAG, "Destroy");
            if (GameUIManager.Instance != null)
                GameUIManager.Instance.DeleteKeyListen(_EscKeyIds);
            GameManager.GameMediator?.UnRegisterSingleEvent("CoreGamePlayManagerInitAndStart"); // Unregister global events.
            this._DeleteEvents();
            this._DeleteCommands(); // Delete commands.
        }
        private void Update()
        {
            if (IsUserPaused) return;
            this._UpdateTick = this._UpdateTick + Time.deltaTime;
            if (this._UpdateTick >= 0.5)
            {
                // Reset counters.
                this._UpdateTick = 0;
                // Subtract one point every half-second.
                if (this._IsCountDownPoint && this.CurrentPoint > 0)
                {
                    this.CurrentPoint = this.CurrentPoint - 1;
                    GamePlayUIControl.Instance.SetPointText(this.CurrentPoint);
                }
            }
        }

        #region Command

        private void _AddCommands()
        {
            var GameDebugCommandServer = GameManager.Instance.GameDebugCommandServer;
            // Register console commands.

            this._CommandIds.Add(GameDebugCommandServer.RegisterCommand("pass", (keyword, fullCmd, argsCount, args) =>
            {
                this.Pass();
                return true;
            }, 0, "win > Complete the level"));
            this._CommandIds.Add(GameDebugCommandServer.RegisterCommand("debug0", (keyword, fullCmd, argsCount, args) =>
            {
                HighscoreManager.Instance.AddItem(CurrentLevelName, "debug0", 5000);
                return true;
            }, 0, "debug0 > Add highscroll"));
            this._CommandIds.Add(GameDebugCommandServer.RegisterCommand("fall", (keyword, fullCmd, argsCount, args) =>
            {
                this.Fall();
                return true;
            }, 0, "fall > Trigger a fatal fall"));
            this._CommandIds.Add(GameDebugCommandServer.RegisterCommand("restart", (keyword, fullCmd, argsCount, args) =>
            {
                this.RestartLevel();
                return true;
            }, 0, "restart > Restart the level"));
            this._CommandIds.Add(GameDebugCommandServer.RegisterCommand("pause", (keyword, fullCmd, argsCount, args) =>
            {
                this.PauseLevel(false);
                return true;
            }, 0, "pause > Pause"));
            this._CommandIds.Add(GameDebugCommandServer.RegisterCommand("resume", (keyword, fullCmd, argsCount, args) =>
            {
                this.ResumeLevel();
                return true;
            }, 0, "resume > Resume"));
            this._CommandIds.Add(GameDebugCommandServer.RegisterCommand("unload", (keyword, fullCmd, argsCount, args) =>
            {
                this.QuitLevel();
                return true;
            }, 0, "unload > Unload the level"));
            this._CommandIds.Add(GameDebugCommandServer.RegisterCommand("nextlev", (keyword, fullCmd, argsCount, args) =>
            {
                this.Fall();
                return true;
            }, 0, "nextlev > Load the next level"));
            this._CommandIds.Add(GameDebugCommandServer.RegisterCommand("gos", (keyword, fullCmd, argsCount, args) =>
            {
                var ox = DebugUtils.CheckIntDebugParam(0, args, out var nx, true, 0);
                if (!ox)
                    return false;
                BallManager.SetControllingStatus(BallControlStatus.NoControl);
                SectorManager.SetCurrentSector(nx);
                this._SetCamPos();
                this._Start(true, null);
                return true;
            }, 1, "gos <count:number> > Go to the specified sector"));
            this._CommandIds.Add(GameDebugCommandServer.RegisterCommand("rebirth", (keyword, fullCmd, argsCount, args) =>
            {
                this._Rebirth();
                return true;
            }, 0, "rebirth > Respawn without consuming a life"));
            this._CommandIds.Add(GameDebugCommandServer.RegisterCommand("addlife", (keyword, fullCmd, argsCount, args) =>
            {
                this.AddLife();
                return true;
            }, 0, "addlife > Add one life"));
            this._CommandIds.Add(GameDebugCommandServer.RegisterCommand("addtime", (keyword, fullCmd, argsCount, args) =>
            {
                var ox = DebugUtils.CheckIntDebugParam(0, args, out var nx, true, 0);
                if (!ox)
                    return false;
                this.AddPoint(nx);
                return true;
            }, 1, "addtime <count:number> > Add the specified number of time points"));
            this._CommandIds.Add(GameDebugCommandServer.RegisterCommand("settime", (keyword, fullCmd, argsCount, args) =>
            {
                var ox = DebugUtils.CheckIntDebugParam(0, args, out var nx, true, 0);
                if (!ox)
                    return false;
                if (nx < 0) nx = 0;
                this.CurrentPoint = nx;
                GamePlayUIControl.Instance.SetPointText(nx);
                return true;
            }, 1, "settime <count:number> > Set the current number of time points"));
            this._CommandIds.Add(GameDebugCommandServer.RegisterCommand("set-physics-speed", (keyword, fullCmd, argsCount, args) =>
            {
                var ox = DebugUtils.CheckFloatDebugParam(0, args, out var nx, true, 0);
                if (!ox)
                    return false;
                if (nx < 0.5) nx = 0.5f;
                if (nx > 5) nx = 5;
                GamePhysicsWorld.TimeFactor = nx;
                return true;
            }, 1, "set-physics-speed <speed:number> > Set simulation speed (default 1, range 0.5-5.0)"));
            this._CommandIds.Add(GameDebugCommandServer.RegisterCommand("set-ball-speed", (keyword, fullCmd, argsCount, args) =>
            {
                var ox = DebugUtils.CheckFloatDebugParam(0, args, out var nx, true, 0);
                if (!ox)
                    return false;
                if (nx < 0.1f) nx = 0.1f;
                if (nx > 10) nx = 10;
                BallManager.BallSpeedFactor = nx;
                BallManager.FlushBallPush();
                return true;
            }, 1, "set-ball-speed <count:number> > Set ball speed multiplier (default 1, range 0.1-10.0)"));
        }
        private void _DeleteCommands()
        {
            var GameDebugCommandServer = GameManager.Instance.GameDebugCommandServer;
            _CommandIds.ForEach((id) => GameDebugCommandServer.UnRegisterCommand(id));
            _CommandIds.Clear();
        }

        #endregion

        #region Initialization

        private AudioSource _SoundBallFall;
        private AudioSource _SoundAddLife;
        private AudioSource _SoundLastSector;
        private AudioSource _SoundFinnal;
        private AudioSource _SoundLastFinnal;

        internal AudioSource GetSoundLastSector() { return _SoundLastSector; }

        private void _InitSounds()
        {
            var SoundManager = GameSoundManager.Instance;
            this._SoundBallFall = SoundManager.RegisterSoundPlayer(GameSoundType.Normal, SoundManager.LoadAudioResource("core.sounds:Misc_Fall.wav"), false, true, "Misc_Fall");
            this._SoundAddLife = SoundManager.RegisterSoundPlayer(GameSoundType.UI, SoundManager.LoadAudioResource("core.sounds:Misc_extraball.wav"), false, true, "Misc_extraball");
            this._SoundLastSector = SoundManager.RegisterSoundPlayer(GameSoundType.Background, SoundManager.LoadAudioResource("core.sounds.music:Music_EndCheckpoint.wav"), false, true, "Music_EndCheckpoint");
            this._SoundFinnal = SoundManager.RegisterSoundPlayer(GameSoundType.Normal, SoundManager.LoadAudioResource("core.sounds.music:Music_Final.wav"), false, true, "Music_Final");
            this._SoundLastFinnal = SoundManager.RegisterSoundPlayer(GameSoundType.Normal, SoundManager.LoadAudioResource("core.sounds.music:Music_LastFinal.wav"), false, true, "Music_LastFinal");
            this._SoundLastSector.loop = true;
            this._SoundLastSector.dopplerLevel = 0;
            this._SoundLastSector.rolloffMode = AudioRolloffMode.Linear;
            this._SoundLastSector.minDistance = 95;
            this._SoundLastSector.maxDistance = 160;
        }
        private void _InitKeyEvents()
        {
            // Escape key.
            _EscKeyIds = GameUIManager.Instance.ListenKey(KeyCode.Escape, (key, down) =>
            {
                if (down
                  && this.CanEscPause
                  && this.IsSessionOpen
                  && !this.CurrentLevelPass)
                {
                    if (IsUserPaused) ResumeLevel();
                    else if (GameUIManager.Instance.GetCurrentPage() == null) PauseLevel(true);
                }
            }, GamepadButton.Select, GamepadButton.Start);
        }
        private void _InitSettings()
        {
            var GameSettings = GameSettingsManager.GetSettings(GamePackageManager.SYSTEM_PACKAGE_NAME);
            var GameLevelBuilder = LevelBuilderRef;
            GameSettings.RegisterSettingsUpdateCallback(SettingConstants.SettingsVideoCloud, (groupName, action) =>
            {
                if (GameLevelBuilder.CurrentLevelSkyLayer != null)
                {
                    if (GameSettings.GetBool(SettingConstants.SettingsVideoCloud))
                        GameLevelBuilder.CurrentLevelSkyLayer.SetActive(true);
                    else
                        GameLevelBuilder.CurrentLevelSkyLayer.SetActive(false);
                }
                return false;
            });
        }
        private void _InitPhysicsWorld()
        {
            if (GamePhysicsWorld == null)
            {
                GamePhysicsWorld = gameObject.AddComponent<PhysicsEnvironment>();
                GamePhysicsWorld.AutoCreate = false;

                // Read physics environment configuration.
                var dataAsset = Resources.Load<TextAsset>("GamePhysData");
                if (dataAsset != null)
                {
                    JObject o = (JObject)JToken.Parse(dataAsset.text);
                    JToken Gravity = o["Gravity"];
                    JToken SimulationRate = o["SimulationRate"];
                    JToken TimeFactor = o["TimeFactor"];
                    if (Gravity != null)
                        GamePhysicsWorld.Gravity = Gravity.ToObject<Vector3>();
                    if (SimulationRate != null)
                        GamePhysicsWorld.SimulationRate = (SimulationRate.Value<int>());
                    if (TimeFactor != null)
                        GamePhysicsWorld.TimeFactor = (TimeFactor.Value<float>());
                }
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Before the level starts.
        /// </summary>
        [HideInInspector]
        public GameEventEmitterStorage EventBeforeStart;
        /// <summary>
        /// Level started.
        /// </summary>
        [HideInInspector]
        public GameEventEmitterStorage EventStart;
        /// <summary>
        /// Level exited.
        /// </summary>
        [HideInInspector]
        public GameEventEmitterStorage EventQuit;
        /// <summary>
        /// Player ball fell.
        /// </summary>
        [HideInInspector]
        public GameEventEmitterStorage EventFall;
        /// <summary>
        /// Ball fell with no lives left; game over is emitted instead of Fall.
        /// </summary>
        [HideInInspector]
        public GameEventEmitterStorage EventDeath;
        /// <summary>
        /// Resumed.
        /// </summary>
        [HideInInspector]
        public GameEventEmitterStorage EventResume;
        /// <summary>
        /// Paused.
        /// </summary>
        [HideInInspector]
        public GameEventEmitterStorage EventPause;
        /// <summary>
        /// Level restarted.
        /// </summary>
        [HideInInspector]
        public GameEventEmitterStorage EventRestart;
        /// <summary>
        /// UFO animation completed.
        /// </summary>
        [HideInInspector]
        public GameEventEmitterStorage EventUfoAnimFinish;
        /// <summary>
        /// Level completed.
        /// </summary>
        [HideInInspector]
        public GameEventEmitterStorage EventPass;
        /// <summary>
        /// Completion balloon hidden.
        /// </summary>
        [HideInInspector]
        public GameEventEmitterStorage EventHideBalloonEnd;
        /// <summary>
        /// Life added.
        /// </summary>
        [HideInInspector]
        public GameEventEmitterStorage EventAddLife;
        /// <summary>
        /// Points added.
        /// </summary>
        [HideInInspector]
        public GameEventEmitterStorage EventAddPoint;

        private void _InitEvents()
        {
            var events = GameManager.GameMediator.RegisterEventEmitter("GamePlay");
            this.EventBeforeStart = events.RegisterEvent("BeforeStart");
            this.EventStart = events.RegisterEvent("Start");
            this.EventQuit = events.RegisterEvent("Quit");
            this.EventFall = events.RegisterEvent("Fall");
            this.EventDeath = events.RegisterEvent("Death");
            this.EventResume = events.RegisterEvent("Resume");
            this.EventPause = events.RegisterEvent("Pause");
            this.EventRestart = events.RegisterEvent("Restart");
            this.EventUfoAnimFinish = events.RegisterEvent("UfoAnimFinish");
            this.EventPass = events.RegisterEvent("Pass");
            this.EventHideBalloonEnd = events.RegisterEvent("HideBalloonEnd");
            this.EventAddLife = events.RegisterEvent("AddLife");
            this.EventAddPoint = events.RegisterEvent("AddPoint");
        }
        private void _DeleteEvents()
        {
            GameManager.GameMediator?.UnRegisterEventEmitter("GamePlay");
        }

        #endregion

        #region Gameplay flow

        // Stop.
        internal void _Stop(BallControlStatus controlStatus)
        {
            this._IsGamePlaying = false;
            this._IsCountDownPoint = false;
            // Disable control.
            BallManager.SetControllingStatus(controlStatus);
            // Disable music.
            MusicManager.DisableBackgroundMusic();
        }
        // Start.
        internal void _Start(bool isStartBySector, GameManager.VoidDelegate customerFn)
        {
            if (IsUserPaused) {
                StartCoroutine(StartWhenReady(0, sessionGeneration, () => _Start(isStartBySector, customerFn)));
                return;
            }
            this._IsGamePlaying = true;
            int generation = sessionGeneration;

            if (this.CurrentDisableStart)
                return;

            // Start music.
            MusicManager.EnableBackgroundMusic();

            if (!this._BallBirthed)
                this._BallBirthed = true;

            if (isStartBySector)
            {
                var startPos = Vector3.zero;
                if (this.CurrentSector > 0)
                {
                    // Initial position.
                    var startRestPoint = SectorManager.CurrentLevelRestPoints[this.CurrentSector].point;
                    startPos = startRestPoint.transform.position;
                }
                BallManager.SetCanControlCameraWhenStart();
                // Wait for lightning to finish.
                BallManager.PlayLighting(startPos, true, true, () => StartCoroutine(StartWhenReady(0, generation, () =>
                {
                    // Start control.
                    BallManager.SetNextRecoverPos(startPos);
                    // Send the event.
                    this.EventStart.Emit(this._FirstStart);
                    // Set flags.
                    if (this._FirstStart)
                        this._FirstStart = false;

                    if (customerFn != null)
                        customerFn();
                    else
                    {
                        BallManager.SetControllingStatus(BallControlStatus.Control);
                        this._IsCountDownPoint = true;
                    }
                })));
            }
            else
            {
                this._IsCountDownPoint = true;
                BallManager.SetControllingStatus(BallControlStatus.Control);
            }
        }
        // Set camera position.
        internal void _SetCamPos()
        {
            if (this.CurrentSector > 0)
            {
                var startRestPoint = SectorManager.CurrentLevelRestPoints[this.CurrentSector].point;
                CamManager.SetPosAndDirByRestPoint(startRestPoint);
                CamManager.SetTarget(startRestPoint.transform, false);
                CamManager.SetCamLook(true);
            }
        }
        // Respawn without consuming a life.
        internal void _Rebirth()
        {
            BallManager.SetControllingStatus(BallControlStatus.NoControl);
            this._SetCamPos();
            this._Start(true, null);
        }
        // LevelBuilder is ready; initialize GamePlayManager.
        internal void _InitAndStart()
        {
            CancelPendingStart();
            IsSessionOpen = true;
            this.CurrentLevelPass = false;
            this.CurrentDisableStart = false;
            this._IsGamePlaying = false;
            this._IsCountDownPoint = false;

            //UI
            GameUIManager.Instance.CloseAllPage();
            GamePlayUIControl.Instance.gameObject.SetActive(true);
            // Set initial points and lives.
            this.CurrentLife = this.StartLife;
            this.CurrentPoint = this.StartPoint;
            this._BallBirthed = false;
            GamePlayUIControl.Instance.SetLifeBallCount(this.CurrentLife);
            GamePlayUIControl.Instance.SetPointText(this.CurrentPoint);
            // Enter the first sector.
            SectorManager.SetCurrentSector(1);
            // Set the starting ball.
            BallManager.SetCurrentBall(this.StartBall);
            BallManager.CanControllCamera = true;
            CamManager.gameObject.SetActive(true);
            this._SetCamPos();
            GameUIManager.Instance.MaskBlackFadeOut(1);
            // Play the level-start sound.
            GameSoundManager.Instance.PlayFastVoice("core.sounds:Misc_StartLevel.wav", GameSoundType.Normal);
            // Enable camera control.
            BallManager.CanControllCamera = true;
            // Emit the event.
            LevelBuilderRef.CallLevelCustomModEvent("beforeStart");
            // Show clouds again when restarting after level completion, which hid them.
            if (GameManager.Instance.GameSettings.GetBool(SettingConstants.SettingsVideoCloud))
            {
                if (LevelBuilderRef.CurrentLevelSkyLayer && !LevelBuilderRef.CurrentLevelSkyLayer.activeSelf)
                    GameUIManager.Instance.UIFadeManager.AddFadeIn(LevelBuilderRef.CurrentLevelSkyLayer, 1, null);
            }

            this._FirstStart = true;

            Log.D(TAG, "Start");

            this.EventBeforeStart.Emit();

            if (!this._ShouldStartByCustom)
            {
                StartCoroutine(StartWhenReady(0.7f, sessionGeneration, () => {
                    this.GamePhysicsWorld.Simulate = true;
                    this._Start(true, null);
                }));
            }
            else
                Log.D(TAG, "Should Start By Custom");
        }

        private bool _DethLock = false;

        /// <summary>
        /// Trigger a ball fall.
        /// </summary>
        public bool Fall()
        {
            if (this.CurrentLevelPass)
                return false;
            if (this.BallManager.GetControllingStatus() != BallControlStatus.Control)
                return false;
            if (this._DethLock)
                return false;
            this._DethLock = true;
            int generation = sessionGeneration;

            Log.D(TAG, "Fall . CurrentLife: " + this.CurrentLife);

            // Falling sound.
            this._SoundBallFall.volume = 1;
            this._SoundBallFall.Play();

            // Disable keyboard camera control.
            BallManager.CanControllCamera = false;

            if (this.CurrentLife > 0 || this.CurrentLife <= -1)
            {
                // Disable control.
                this._Stop(BallControlStatus.FreeMode);

                if (this.CurrentLife > 0)
                    this.CurrentLife = this.CurrentLife - 1;

                GameUIManager.Instance.MaskWhiteFadeIn(1);

                this.EventFall.Emit();

                StartCoroutine(StartWhenReady(1, generation, () =>
                {
                    // Deactivate mechanisms.
                    SectorManager.DeactiveCurrentSector();
                    // Disable control.
                    this._Stop(BallControlStatus.NoControl);

                    StartCoroutine(StartWhenReady(1, generation, () =>
                    {
                        GameUIManager.Instance.UIFadeManager.AddAudioFadeOut(this._SoundBallFall, 1);

                        /// Reset mechanisms and camera.
                        SectorManager.ActiveCurrentSector(false);
                        this._SetCamPos();
                        this._Start(true, null);
                        GameUIManager.Instance.MaskWhiteFadeOut(1);

                        // Remove the displayed life after a delay.
                        StartCoroutine(StartWhenReady(1, generation, () =>
                        {
                            GamePlayUIControl.Instance.RemoveLifeBall();
                            this._DethLock = false;
                        }));
                    }));
                }));
            }
            else
            {
                Log.D(TAG, "Death");

                // Disable control.
                this._Stop(BallControlStatus.FreeMode);

                this.EventDeath.Emit();

                StartCoroutine(StartWhenReady(1.5f, generation, () =>
                {
                    this._Stop(BallControlStatus.UnleashingMode);
                    MusicManager.DisableBackgroundMusicWithoutAtmo();

                    // Show the failure menu after a delay.
                    StartCoroutine(StartWhenReady(1, generation, () =>
                    {
                        GameUIManager.Instance.GoPage("PageGameFail");
                        this._DethLock = false;
                    }));
                }));
            }
            return true;
        }
        /// <summary>
        /// Complete the level.
        /// </summary>
        public void Pass()
        {
            if (this.CurrentLevelPass)
                return;

            Log.D(TAG, "Pass");

            this.CurrentLevelPass = true;
            this._SoundLastSector.Stop(); // Stop the final sector's music.
            this._Stop(BallControlStatus.FreeMode);

            GameTimer.Delay(0.6f, () => BallManager.SetControllingStatus(BallControlStatus.UnleashingMode));

            // Disable keyboard camera control.
            BallManager.CanControllCamera = false;

            // Fade clouds immediately after completion because the horizontal camera will expose their boundary.
            if (LevelBuilderRef.CurrentLevelSkyLayer && LevelBuilderRef.CurrentLevelSkyLayer.activeSelf)
                GameUIManager.Instance.UIFadeManager.AddFadeOut(LevelBuilderRef.CurrentLevelSkyLayer, 5, true, null);

            // Stop background music.
            MusicManager.DisableBackgroundMusicWithoutAtmo();

            if (this.CurrentEndWithUFO)
            {
                // Play the ending UFO animation.
                this._SoundLastFinnal.Play(); // Play music.
                UFOAnimController.Instance.StartSeq();
            }
            else
            {
                this._SoundFinnal.Play(); // Play music.
                this._HideBalloonEnd(false); // Start hiding the balloon.
                GameTimer.Delay(6, () =>
                {
                    WinScoreUIControl.Instance.StartSeq();
                });
            }
            this.EventPass.Emit();
        }
        /// <summary>
        /// Pause the level.
        /// </summary>
        /// <param name="showPauseUI">Show the pause interface.</param>
        public void PauseLevel(bool showPauseUI)
        {
            if (IsUserPaused) return;
            if (showPauseUI) {
                pausedPlaying = _IsGamePlaying;
                pausedCountdown = _IsCountDownPoint;
                pausedSimulation = GamePhysicsWorld.Simulate;
                pausedControl = BallManager.GetControllingStatus();
                pausedMovement = BallManager.CanControll;
                pausedCamera = BallManager.CanControllCamera;
                pausedCamEnabled = CamManager.enabled;
                IsUserPaused = true;
            }
            // Release forces before changing the permission that guards force updates.
            GamePlayUIControl.Instance.ReleaseMobileInput();
            BallManager.RemoveAllBallPush();
            BallManager.KeyStateForward = BallManager.KeyStateBack = BallManager.KeyStateLeft = BallManager.KeyStateRight = false;
            _IsGamePlaying = false;
            _IsCountDownPoint = false;
            BallManager.CanControll = false;
            BallManager.CanControllCamera = false;
            GamePhysicsWorld.Simulate = false;
            CamManager.enabled = false;
            MusicManager.DisableBackgroundMusic();
            if (showPauseUI) {
                GameSoundManager.Instance.PlayFastVoice("core.sounds:Menu_click.wav", GameSoundType.UI);
                GameUIManager.Instance.GoPage("PageGamePause");
            }
            EventPause.Emit();
        }

        /// <summary>Restore a user pause, or leave a tutorial stop. A forced restart respawns the sector.</summary>
        public void ResumeLevel(bool forceRestart = false)
        {
            GameSoundManager.Instance.PlayFastVoice("core.sounds:Menu_click.wav", GameSoundType.UI);
            GameUIManager.Instance.CloseAllPage();
            if (IsUserPaused && !forceRestart) {
                IsUserPaused = false;
                if (BallManager.GetControllingStatus() != pausedControl) BallManager.SetControllingStatus(pausedControl);
                BallManager.CanControll = pausedMovement;
                BallManager.CanControllCamera = pausedCamera;
                GamePhysicsWorld.Simulate = pausedSimulation;
                CamManager.enabled = pausedCamEnabled;
                _IsGamePlaying = pausedPlaying;
                _IsCountDownPoint = pausedCountdown;
                if (pausedPlaying) MusicManager.EnableBackgroundMusic();
            } else {
                IsUserPaused = false;
                CamManager.enabled = true;
                BallManager.StartCamMove();
                GamePhysicsWorld.Simulate = true;
                _Start(forceRestart, null);
            }
            EventResume.Emit();
        }

        // Hide the balloon after completion.
        private void _HideBalloonEnd(bool fromUfo)
        {
            if (this._HideBalloonEndTimerID > 0)
            {
                GameTimer.DeleteDelay(this._HideBalloonEndTimerID);
                this._HideBalloonEndTimerID = 0;
            }
            // Hide the balloon after 60 seconds.
            this._HideBalloonEndTimerID = GameTimer.Delay(fromUfo ? 40 : 60, () =>
            {
                this._HideBalloonEndTimerID = 0;
                BallManager.SetControllingStatus(BallControlStatus.NoControl);
                SectorManager.CurrentLevelEndBalloon.Deactive();
                this.EventHideBalloonEnd.Emit();
            });
        }
        // UFO animation completion callback.
        internal void UfoAnimFinish()
        {
            this._SoundFinnal.Play();
            this._HideBalloonEnd(true); // Start hiding the balloon.
            BallManager.SetControllingStatus(BallControlStatus.NoControl);
            WinScoreUIControl.Instance.StartSeq();
            this.EventUfoAnimFinish.Emit();
        }

        private bool _IsTranfoIn = false;

        /// <summary>
        /// Start the ball transformation sequence.
        /// </summary>
        /// <param name="tranfo">Transformer.</param>
        /// <param name="targetType">Target ball type.</param>
        public void ActiveTranfo(ITranfoBase tranfo, string targetType)
        {
            if (this._IsTranfoIn)
                return;

            this._IsTranfoIn = true;

            var targetPos = tranfo.GetTransform().TransformPoint(new Vector3(0, 2, 0));
            var oldBallType = BallManager.CurrentBallName;
            var cTranfoAminControl = tranfo.GetTranfoAminControl();

            if (cTranfoAminControl == null)
            {
                Log.E(TAG, "Not found TranfoAminControl on tranfo {0}", tranfo.GetTargetBallType());
                return;
            }

            // Immediately collect fragments of the current ball type.
            BallManager.ResetPeices(BallManager.CurrentBallName);
            BallManager.SetNextRecoverPos(targetPos);
            // Lock the ball and move it quickly to the target.
            BallManager.FastMoveTo(targetPos, 0.1f, (_) =>
            {
                // Play the transformation animation.
                cTranfoAminControl.PlayAnim(tranfo, targetType, () =>
                {

                    // Stop camera following first.
                    CamManager.SetCamFollow(false);
                    CamManager.SetCamLook(false);
                    // Play smoke.
                    BallManager.PlaySmoke(targetPos);
                    // Clear the current ball first.
                    BallManager.SetNoCurrentBall();
                    // Switch balls and scatter fragments.
                    BallManager.ThrowPeices(oldBallType, targetPos);
                    // Activate the new ball.
                    BallManager.SetCurrentBall(targetType, BallControlStatus.Control);
                    // Resume camera following.
                    CamManager.SetCamFollow(true);
                    CamManager.SetCamLook(true);

                    // Reset state.
                    tranfo.ResetTranfo();
                    this._IsTranfoIn = false;
                });
            });
        }

        #endregion

        #region Level loading, unloading, and progression

        private void _QuitOrLoadNextLevel(bool loadNext)
        {
            CancelPendingStart();
            IsSessionOpen = false;
            Log.D(TAG, "Start Quit Level");

            GameManager.VoidDelegate callBack = null;
            if (loadNext)
            {
                callBack = () => LevelBuilder.LevelBuilder.Instance.LoadLevel(this.NextLevelName);
            }

            // Cancel the balloon hide timer.
            if (this._HideBalloonEndTimerID > 0)
            {
                GameTimer.DeleteDelay(this._HideBalloonEndTimerID);
                this._HideBalloonEndTimerID = 0;
            }

            // Send the event.
            this.EventQuit.Emit();

            // Stop background music.
            MusicManager.DisableBackgroundMusic(true);

            GameUIManager.Instance.CloseAllPage();
            GameUIManager.Instance.MaskBlackFadeIn(0.7f);
            GameSoundManager.Instance.PlayFastVoice("core.sounds:Menu_load.wav", GameSoundType.Normal);

            GameTimer.Delay(0.5f, () =>
            {
                // Stop simulation.
                this.GamePhysicsWorld.Simulate = false;
                // Disable the ball.
                this._Stop(BallControlStatus.NoControl);
                this.CurrentSector = 0;
                // Hide UI.
                GamePlayUIControl.Instance.gameObject.SetActive(false);
                // Emit the event.
                LevelBuilderRef.CallLevelCustomModEvent("beforeQuit");
                LevelBuilderRef.UnLoadLevel(callBack);
            });
        }

        // Load the next level.
        public void NextLevel()
        {
            if (this.NextLevelName == "")
                return;
            this._QuitOrLoadNextLevel(true);
        }
        // Restart the level.
        public void RestartLevel()
        {
            CancelPendingStart();
            IsSessionOpen = false;
            // Fade to black.
            GameUIManager.Instance.MaskBlackFadeIn(1);

            Log.D(TAG, "Restart Level");

            this._Stop(BallControlStatus.NoControl);

            this.EventRestart.Emit();

            // Reset the ball manager.
            BallManager.ResetAllPeices();

            GameTimer.Delay(0.8f, () =>
            {
                // Reset all sectors.
                SectorManager.ResetAllSector(false);
                this.CurrentSector = 0;
                GameTimer.Delay(0.5f, () =>
                {
                    // Start.
                    this._InitAndStart();
                });
            });
        }
        // Exit the level.
        public void QuitLevel()
        {
            this._QuitOrLoadNextLevel(false);
        }

        #endregion

        #region Score operations

        /// <summary>
        /// Add a life.
        /// </summary>
        /// <returns></returns>
        public void AddLife()
        {
            this.CurrentLife = this.CurrentLife + 1;

            GameTimer.Delay(0.317f, () =>
            {
                this._SoundAddLife.Play();
                GamePlayUIControl.Instance.AddLifeBall();
            });

            this.EventAddLife.Emit();
        }
        /// <summary>
        /// Add time points.
        /// </summary>
        /// <param name="count">Number of time points; default 10.</param>
        public void AddPoint(int count = 10)
        {
            if (count < 0)
            {
                Log.E(TAG, "AddPoint count can not be negative!");
                return;
            }
            this.CurrentPoint = this.CurrentPoint + count;
            GamePlayUIControl.Instance.SetPointText(this.CurrentPoint);
            GamePlayUIControl.Instance.TwinklePoint();
            this.EventAddPoint.Emit(count);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Initialize lighting and skybox.
        /// </summary>
        /// <param name="skyBoxPre">A-K, or empty to use customSkyMat.</param>
        /// <param name="customSkyMat">Custom skybox material.</param>
        /// <param name="lightColor">Light color.</param>
        public void CreateSkyAndLight(string skyBoxPre, Material customSkyMat, Color lightColor)
        {
            CamManager.SetSkyBox(customSkyMat != null ? customSkyMat : SkyBoxUtils.MakeSkyBox(skyBoxPre));
            GameManager.GameLight.color = lightColor;
        }
        /// <summary>
        /// Hide the skybox and level lighting.
        /// </summary>
        public void HideSkyAndLight()
        {
            CamManager.SetSkyBox(null);
        }

        #endregion

    }
}