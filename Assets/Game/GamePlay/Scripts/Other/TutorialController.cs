using Ballance2.Base;
using Ballance2.Package;
using Ballance2.Services;
using Ballance2.Services.I18N;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace Ballance2.Game.GamePlay.Other
{
    /// <summary>
    /// First-level tutorial controller.
    /// </summary>
    public class TutorialController : MonoBehaviour
    {
        public ParentConstraint Pfeil_HochHost;
        public GameObject Pfeil_Rund01;
        public GameObject Pfeil_Rund02;
        public GameObject Pfeil_Hoch;
        public ParentConstraint Tut_Richt_Pfeil;
        public GameObject Tut_Richt_Pfeil01;
        public GameObject Tut_Richt_Pfeil02;
        public GameObject Tut_Richt_Pfeil03;
        public GameObject Tut_Richt_Pfeil04;
        public TiggerTester Pfeil_Runter_Trigger;
        public LookAtConstraint Pfeil_Runter;
        public TiggerTester Tut_ExtraLife;
        public TiggerTester Tut_StoneTranfo;
        public TiggerTester Tut_Rampe;
        public TiggerTester Tut_WoodTranfo;
        public TiggerTester Tut_KeyEnd;
        public TiggerTester Tut_ExtraPoint;
        public TiggerTester Tut_Checkpoint;
        public TiggerTester Tut_End;
        public GameObject Tut_ExtraLifeEnd;
        public GameObject Tut_StoneTranfoEnd;
        public GameObject Tut_WoodTranfoEnd;
        public GameObject Tut_ExtraPointEnd;
        public GameObject Tut_CheckpointEnd;

        private const string TAG = "TutorialController";

        private bool _Tutorial = false;
        private int _TutorialStep = 1;
        private bool _TutorialShouldDisablePointDown = false;
        private bool _TutorialBallFinded = false;
        private bool _TutorialCamFinded = false;
        private int _TutorialCurrWaitkey;
        private RectTransform _TutorialUI = null;
        private Text _TutorialUIText = null;
        private Image _TutorialUIBg = null;
        private Button _TutorialUIButtonContinue = null;
        private Button _TutorialUIButtonQuit = null;

        private GameSoundManager GameSoundManager;
        private GameUIManager GameUIManager;
        private GamePlayManager GamePlayManager;
        private GameEventHandler _EventEntery = null;

        private void Start()
        {
            GameSoundManager = Services.GameSoundManager.Instance;
            GameUIManager = Services.GameUIManager.Instance;
            GamePlayManager = GamePlay.GamePlayManager.Instance;

            GameEventEmitterDelegate startFun = (_) =>
            {
                Log.D(TAG, "Init Tutorial");

                GamePlayManager._ShouldStartByCustom = true;
                GamePlayManager.CanEscPause = false;

                _TutorialStep = 1;
                _Tutorial = true;
                _TutorialShouldDisablePointDown = true;

                // Hide arrows first.
                Tut_Richt_Pfeil01.SetActive(false);
                Tut_Richt_Pfeil02.SetActive(false);
                Tut_Richt_Pfeil03.SetActive(false);
                Tut_Richt_Pfeil04.SetActive(false);
                Pfeil_Rund02.SetActive(false);
                Pfeil_Rund01.SetActive(false);
                Pfeil_Hoch.SetActive(false);
                // Initialize tutorial UI.
                _TutorialUI = GameUIManager.InitViewToCanvas(GamePackage.GetSystemPackage().GetPrefabAsset("GameTutorialUI.prefab"), "GameTutorialUI", false);
                var view = _TutorialUI.transform.GetChild(0);
                var buttonView = _TutorialUI.transform.GetChild(1);
                _TutorialUIBg = view.GetComponent<Image>();
                _TutorialUIText = view.GetChild(0).GetComponent<Text>();
                _TutorialUIButtonContinue = buttonView.GetChild(0).GetComponent<Button>();
                _TutorialUIButtonQuit = buttonView.GetChild(1).GetComponent<Button>();
                _TutorialUI.gameObject.SetActive(false);
                StartSeq();

                // Move to the ball spawn position.
                Pfeil_HochHost.transform.position = GamePlayManager.SectorManager.CurrentLevelRestPoints[1].point.transform.position;
                // Make arrows follow camera rotation.
                var constraintSource = new ConstraintSource();
                constraintSource.sourceTransform = GamePlayManager.CamManager._CamOrientTransform;
                constraintSource.weight = 1;

                if (_TutorialCamFinded)
                    Pfeil_Runter.SetSource(0, constraintSource);
                else
                    Pfeil_Runter.AddSource(constraintSource);

                Pfeil_Runter.rotationOffset = new Vector3(-90, 0, 0);
                if (_TutorialCamFinded)
                    Pfeil_HochHost.SetSource(0, constraintSource);
                else
                    Pfeil_HochHost.AddSource(constraintSource);

                Pfeil_HochHost.SetRotationOffset(0, new Vector3(0, -90, 0));
                _TutorialCamFinded = true;
            };
            GameEventEmitterDelegate fallFun = (_) =>
            {
                Log.D(TAG, "Fall handler");

                _Tutorial = true;
                Tut_Richt_Pfeil01.SetActive(false);
                Tut_Richt_Pfeil02.SetActive(false);
                Tut_Richt_Pfeil03.SetActive(false);
                Tut_Richt_Pfeil04.SetActive(false);
            };
            GameEventEmitterDelegate quitFun = (_) =>
            {
                Log.D(TAG, "Start quit");

                _Tutorial = false;
                GameTimer.Delay(1, () =>
                {
                    // Remove key listeners.
                    if (_TutorialCurrWaitkey > 0)
                    {
                        GameUIManager.DeleteKeyListen(_TutorialCurrWaitkey);
                    }

                    // Reset and restore.
                    GamePlayManager._ShouldStartByCustom = false;

                    // Delete UI.
                    if (_TutorialUI != null)
                    {
                        UnityEngine.Object.Destroy(_TutorialUI.gameObject);
                        _TutorialUI = null;
                    }
                });

            };

            _EventEntery = GameMediator.Instance.RegisterEventHandler(
              GamePackage.GetSystemPackage(),
              "CoreTutorialLevelEventHandler",
              "TutorialHandler",
              (evtName, param) =>
            {
                var type = param[0] as string;
                if (type == "beforeStart")
                {
                    GamePlayManager.EventBeforeStart.On(startFun);
                    GamePlayManager.EventFall.On(fallFun);
                    GamePlayManager.EventQuit.On(quitFun);
                }
                else if (type == "beforeQuit")
                {
                    GamePlayManager.EventBeforeStart.Off(startFun);
                    GamePlayManager.EventFall.Off(fallFun);
                    GamePlayManager.EventQuit.Off(quitFun);
                    // Unregister events.
                    if (_EventEntery != null)
                    {
                        GameMediator.Instance.UnRegisterEventHandler("CoreTutorialLevelEventHandler", _EventEntery);
                        _EventEntery = null;
                    }
                }
                return false;
            });
        }

        /// <summary>
        /// Start the sequence.
        /// </summary>
        public void StartSeq()
        {
            Log.D(TAG, "Tutorial StartSeq");

            if (!_Tutorial)
                return;

            int step1KeyReturn = 0;
            int step1KeyQ = 0;

            var funStepLock = false;
            var funQuitLock = false;
            GameManager.VoidDelegate funQuit = () =>
            {
                if (!funQuitLock)
                {
                    funQuitLock = true;
                    GameSoundManager.PlayFastVoice("core.sounds:Menu_click.wav", GameSoundType.Normal);
                    // Press Q to exit.
                    GameUIManager.DeleteKeyListen(step1KeyReturn);
                    HideTutorial();
                    // Restore ball movement keys.
                    GamePlayManager.BallManager.KeyListener.IsListenKey = true;
                    // Resume gameplay.
                    GamePlayManager._ShouldStartByCustom = false;
                    GamePlayManager.CanEscPause = true;
                    GamePlayManager.ResumeLevel(true);

                    Tut_ExtraLife.onTriggerEnter = null;
                    Tut_StoneTranfo.onTriggerEnter = null;
                    Tut_Rampe.onTriggerEnter = null;
                    Tut_ExtraPoint.onTriggerEnter = null;
                    Tut_Checkpoint.onTriggerEnter = null;
                    Tut_End.onTriggerEnter = null;
                }
            };
            GameManager.VoidDelegate funStep2 = () =>
            {
                var BallManager = GamePlayManager.BallManager;

                GameSoundManager.PlayFastVoice("core.sounds:Menu_click.wav", GameSoundType.Normal);

                // Enable control at this point.
                BallManager.SetControllingStatus(BallControlStatus.Control);
                GamePlayManager._IsCountDownPoint = true;
                GamePlayManager.CanEscPause = true;
                _TutorialShouldDisablePointDown = false;

                // Bind the current ball.
                var ballWoodConstraintSource = new ConstraintSource();
                ballWoodConstraintSource.sourceTransform = BallManager.CurrentBall.transform;
                ballWoodConstraintSource.weight = 1;
                if (_TutorialBallFinded)
                    Tut_Richt_Pfeil.SetSource(0, ballWoodConstraintSource);
                else
                    Tut_Richt_Pfeil.AddSource(ballWoodConstraintSource);
                Tut_Richt_Pfeil.constraintActive = true;
                Tut_Richt_Pfeil.rotationAxis = Axis.None;
                _TutorialBallFinded = true;

                // Next step: directional key navigation.
                GameUIManager.UIFadeManager.AddFadeOut(Pfeil_Hoch, 1, true, null);
                GameUIManager.UIFadeManager.AddFadeIn(Tut_Richt_Pfeil01, 1, null);
                GameUIManager.UIFadeManager.AddFadeIn(Tut_Richt_Pfeil02, 1, null);
                GameUIManager.UIFadeManager.AddFadeIn(Tut_Richt_Pfeil03, 1, null);
                GameUIManager.UIFadeManager.AddFadeIn(Tut_Richt_Pfeil04, 1, null);

                _TutorialUIButtonContinue.gameObject.SetActive(false);

                // Stretch arrows when their keys are pressed.
                GameEventEmitterDelegate FlushBallPushListener = (_) =>
                {
                    if (BallManager.KeyStateBack)
                        Tut_Richt_Pfeil03.transform.localPosition = new Vector3(-4, 0.5f, 0);
                    else
                        Tut_Richt_Pfeil03.transform.localPosition = new Vector3(-2, 0.5f, 0);
                    if (BallManager.KeyStateForward)
                        Tut_Richt_Pfeil04.transform.localPosition = new Vector3(6, 1, 0);
                    else
                        Tut_Richt_Pfeil04.transform.localPosition = new Vector3(4, 1, 0);
                    if (BallManager.KeyStateLeft)
                        Tut_Richt_Pfeil01.transform.localPosition = new Vector3(1, 0, 5);
                    else
                        Tut_Richt_Pfeil01.transform.localPosition = new Vector3(1, 0, 3);
                    if (BallManager.KeyStateRight)
                        Tut_Richt_Pfeil02.transform.localPosition = new Vector3(1, 0, -5);
                    else
                        Tut_Richt_Pfeil02.transform.localPosition = new Vector3(1, 0, -3);
                };
                BallManager.EventFlushBallPush.On(FlushBallPushListener);

                // Hide the four directional arrows.
                GameManager.VoidDelegate hideRichtPfeil = () =>
                {
                    GameUIManager.UIFadeManager.AddFadeOut(Tut_Richt_Pfeil01, 1, true, null);
                    GameUIManager.UIFadeManager.AddFadeOut(Tut_Richt_Pfeil02, 1, true, null);
                    GameUIManager.UIFadeManager.AddFadeOut(Tut_Richt_Pfeil03, 1, true, null);
                    GameUIManager.UIFadeManager.AddFadeOut(Tut_Richt_Pfeil04, 1, true, null);
                    GameTimer.Delay(1, () =>
                    {
                        Tut_Richt_Pfeil.constraintActive = false;
                        _TutorialUIButtonContinue.gameObject.SetActive(true);
                        BallManager.EventFlushBallPush.Off(FlushBallPushListener);
                    });
                };

                GameTimer.Delay(1, () =>
                {
                    _TutorialStep = 4;
                    ShowTutorialText();
                });

                // Hide the key arrow after at most 15 seconds.
                GameTimer.Delay(15.000f, () =>
                {
                    if (_TutorialStep == 4)
                    {
                        _TutorialStep = 0;
                        HideTutorial();
                        hideRichtPfeil();
                    }
                });
                // Or hide it when the ball reaches the wooden bridge.
                Tut_KeyEnd.onTriggerEnter = (e, v) =>
                {
                    if (_TutorialStep == 4)
                    {
                        _TutorialStep = 0;
                        HideTutorial();
                        hideRichtPfeil();
                    }
                };
            };
            GameManager.VoidDelegate funStep1 = () =>
            {
                GameSoundManager.PlayFastVoice("core.sounds:Menu_click.wav", GameSoundType.Normal);

                // Next step: overhead view with Space.
                GameUIManager.UIFadeManager.AddFadeOut(Pfeil_Rund01, 1, true, null);
                GameUIManager.UIFadeManager.AddFadeOut(Pfeil_Rund02, 1, true, null);

                GameTimer.Delay(1, () =>
                {
                    GameUIManager.UIFadeManager.AddFadeIn(Pfeil_Hoch, 1, null);
                    _TutorialStep = 3;
                    ShowTutorialText();
                });

                _TutorialCurrWaitkey = GameUIManager.WaitKey(KeyCode.Return, true, funStep2, GamepadButton.A);
            };
            GameManager.VoidDelegate commonTurHide = null;
            GameManager.VoidDelegate funSeq = () =>
            {
                GameSoundManager.PlayFastVoice("core.sounds:Menu_click.wav", GameSoundType.Normal);
                _TutorialUIButtonQuit.gameObject.SetActive(false);
                GameUIManager.DeleteKeyListen(step1KeyQ);
                // Restore ball movement keys.
                GamePlayManager.BallManager.KeyListener.IsListenKey = true;

                // Advance to the next step.
                HideTutorial();

                // Ball spawn sequence.
                GamePlayManager._Start(true, () =>
                {

                    // Spawn the ball without enabling control.
                    GamePlayManager.BallManager.SetControllingStatus(BallControlStatus.LockMode);

                    // Show arrows.
                    GameUIManager.UIFadeManager.AddFadeIn(Pfeil_Rund01, 1, null);
                    GameUIManager.UIFadeManager.AddFadeIn(Pfeil_Rund02, 1, null);

                    _TutorialStep = 2;
                    ShowTutorialText();

                    _TutorialCurrWaitkey = GameUIManager.WaitKey(KeyCode.Return, true, funStep1,GamepadButton.A);
                });

                // Move arrows to the specified position.
                commonTurHide = () =>
                {
                    GameSoundManager.PlayFastVoice("core.sounds:Menu_click.wav", GameSoundType.Normal);
                    GamePlayManager.ResumeLevel(false);
                    HideTutorial();
                };
                GameManager.VoidDelegate commonTurReturn = () =>
                {
                    GamePlayManager.PauseLevel(false);
                    _TutorialCurrWaitkey = GameUIManager.WaitKey(KeyCode.Return, true, commonTurHide, GamepadButton.A);
                };
                GameManager.VoidDelegate commonTurTipSound = () =>
                {
                    GameSoundManager.PlayFastVoice("core.sounds:Hit_Stone_Kuppel.wav", GameSoundType.Normal);
                };

                // Configure tutorials activated by triggers.
                Tut_ExtraLife.onTriggerEnter = (s, o) =>
                {
                    if (_TutorialStep < 5)
                    {
                        _TutorialStep = 5;
                        ShowTutorialText();
                        commonTurTipSound();
                        commonTurReturn();
                        MoveArrowToObject(Tut_ExtraLifeEnd);
                    }
                };
                Tut_StoneTranfo.onTriggerEnter = (s, o) =>
                {
                    if (_TutorialStep < 6)
                    {
                        _TutorialStep = 6;
                        ShowTutorialText();
                        commonTurTipSound();
                        commonTurReturn();
                        MoveArrowToObject(Tut_StoneTranfoEnd);
                    }
                };
                Tut_Rampe.onTriggerEnter = (body, other) =>
                {
                    // Stone ball uphill hint.
                    if (other != null && other.gameObject.tag == "Ball" && other.gameObject.name == "BallStone")
                    {
                        if (_TutorialStep < 7)
                        {
                            _TutorialStep = 7;
                            ShowTutorialText();
                            commonTurTipSound();
                            commonTurReturn();
                            MoveArrowToObject(Tut_WoodTranfoEnd);
                        }
                    }
                };
                Tut_ExtraPoint.onTriggerEnter = (s, o) =>
                {
                    if (_TutorialStep < 8)
                    {
                        _TutorialStep = 8;
                        ShowTutorialText();
                        commonTurTipSound();
                        commonTurReturn();
                        MoveArrowToObject(Tut_ExtraPointEnd);
                    }
                };
                Tut_Checkpoint.onTriggerEnter = (s, o) =>
                {
                    if (_TutorialStep < 9)
                    {
                        _TutorialStep = 9;
                        ShowTutorialText();
                        commonTurTipSound();
                        commonTurReturn();
                        MoveArrowToObject(Tut_CheckpointEnd);
                    }
                };
                Tut_End.onTriggerEnter = (s, o) =>
                {
                    if (_TutorialStep < 10)
                    {
                        _TutorialStep = 10;
                        ShowTutorialText();
                        commonTurTipSound();
                        commonTurReturn();
                    }
                };
            };

            _TutorialUIButtonContinue.onClick.RemoveAllListeners();
            _TutorialUIButtonQuit.onClick.RemoveAllListeners();
            _TutorialUIButtonContinue.onClick.AddListener(() =>
            {

                // Prevent advancing too quickly through key presses.
                if (funStepLock)
                    return;
                funStepLock = true;
                GameTimer.Delay(1, () => funStepLock = false);

                if (_TutorialStep == 1)
                    funSeq();
                else if (_TutorialStep == 2)
                    funStep1();
                else if (_TutorialStep == 3)
                    funStep2();
                else if (_TutorialStep >= 5)
                    commonTurHide();
            });
            _TutorialUIButtonQuit.onClick.AddListener(() => funQuit());

            // Temporarily disable ball movement keys.
            GamePlayManager.BallManager.KeyListener.IsListenKey = false;

            GameTimer.Delay(0.5f, () =>
            {
                ShowTutorialText();
                // Step 1: Q exits; Enter continues.
                step1KeyReturn = GameUIManager.WaitKey(KeyCode.Return, true, () => funSeq(), GamepadButton.A);
                step1KeyQ = GameUIManager.WaitKey(KeyCode.Q, true, () => funQuit(), GamepadButton.B);
            });
        }

        private void MoveArrowToObject(GameObject obj)
        {
            var arrowPosition = obj.transform.position;
            arrowPosition.y = arrowPosition.y + 3;
            Pfeil_Runter.transform.position = arrowPosition;
            Pfeil_Runter.gameObject.SetActive(true);
            Pfeil_Runter_Trigger.transform.position = arrowPosition;
            Pfeil_Runter_Trigger.onTriggerEnter = (s, g) =>
            {
                GameUIManager.UIFadeManager.AddFadeOut(Pfeil_Runter.gameObject, 0.5f, true, null);
            };
        }
        private void ShowTutorialText()
        {
            if (_TutorialUI.gameObject.activeSelf)
            {
                GameUIManager.UIFadeManager.AddFadeOut(_TutorialUIText, 0.3f, false);
                GameTimer.Delay(0.350f, () =>
                {
                    _TutorialUIText.text = I18N.Tr("core.ui.TutorialText" + _TutorialStep);
                    _TutorialUIText.gameObject.SetActive(true);
                    GameUIManager.UIFadeManager.AddFadeIn(_TutorialUIText, 0.3f);
                });
            }
            else
            {
                _TutorialUIText.text = I18N.Tr("core.ui.TutorialText" + _TutorialStep);
                _TutorialUI.gameObject.SetActive(true);
                _TutorialUIText.gameObject.SetActive(true);
                GameUIManager.UIFadeManager.AddFadeIn(_TutorialUIText, 0.5f);
                GameUIManager.UIFadeManager.AddFadeIn(_TutorialUIBg, 0.5f);
            }
            GamePlayManager._IsCountDownPoint = false;
        }

        /// <summary>
        /// Hide the tutorial.
        /// </summary>
        public void HideTutorial()
        {
            GameUIManager.UIFadeManager.AddFadeOut(_TutorialUIText, 0.6f, true);
            GameUIManager.UIFadeManager.AddFadeOut(_TutorialUIBg, 0.6f, true);

            GameTimer.Delay(0.6f, () =>
            {
                _TutorialUI.gameObject.SetActive(false);
            });

            if (!_TutorialShouldDisablePointDown)
                GamePlayManager._IsCountDownPoint = true;
        }
    }
}