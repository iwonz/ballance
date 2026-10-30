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
    /// <summary>Replayable first-level hints driven by the existing authored triggers.</summary>
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

        private GameEventHandler entry;
        private GamePlayManager game;
        private GameUIManager ui;
        private RectTransform card;
        private Text text;
        private Button next, skip;
        private int step, continueKey, skipKey, generation;
        private bool active, subscribed, spawned, busy, modal;
        private float movementHintTime, acceptAfter;

        private void Start()
        {
            game = GamePlayManager.Instance;
            ui = GameUIManager.Instance;
            entry = GameMediator.Instance.RegisterEventHandler(GamePackage.GetSystemPackage(),
                "CoreTutorialLevelEventHandler", "TutorialHandler", (_, args) => {
                    if ((string)args[0] == "beforeStart" && !subscribed) {
                        game.EventBeforeStart.On(BeforeStart);
                        game.EventQuit.On(Quit);
                        game.EventRestart.On(Quit);
                        game.EventFall.On(Fall);
                        subscribed = true;
                    } else if ((string)args[0] == "beforeQuit") Detach();
                    return false;
                });
        }

        private void BeforeStart(params object[] args)
        {
            Cleanup();
            game._ShouldStartByCustom = false;
            game.CanEscPause = true;

            active = true; spawned = false; busy = false; step = 1;
            game._ShouldStartByCustom = true;
            game.GamePhysicsWorld.Simulate = false;
            CreateCard();
            BindTriggers();
            Pfeil_HochHost.transform.position = game.SectorManager.CurrentLevelRestPoints[1].point.transform.position;
            var source = new ConstraintSource { sourceTransform = game.CamManager._CamOrientTransform, weight = 1 };
            if (Pfeil_Runter.sourceCount > 0) Pfeil_Runter.SetSource(0, source); else Pfeil_Runter.AddSource(source);
            if (Pfeil_HochHost.sourceCount > 0) Pfeil_HochHost.SetSource(0, source); else Pfeil_HochHost.AddSource(source);
            Pfeil_Runter.rotationOffset = new Vector3(-90, 0, 0);
            Pfeil_HochHost.SetRotationOffset(0, new Vector3(0, -90, 0));
            StartSeq();
        }

        private void CreateCard()
        {
            card = ui.InitViewToCanvas(GamePackage.GetSystemPackage().GetPrefabAsset("GameTutorialUI.prefab"), "GameTutorialUI", false);
            text = card.GetChild(0).GetChild(0).GetComponent<Text>();
            next = card.GetChild(1).GetChild(0).GetComponent<Button>();
            skip = card.GetChild(1).GetChild(1).GetComponent<Button>();
            skip.GetComponentInChildren<Ballance2.UI.Core.Controls.I18NText>().resourceKey = "core.ui.TutorialSkip";
            next.onClick.RemoveAllListeners(); skip.onClick.RemoveAllListeners();
            next.onClick.AddListener(Continue); skip.onClick.AddListener(Skip);
            next.navigation = skip.navigation = new Navigation { mode = Navigation.Mode.None };
            card.gameObject.AddComponent<TutorialCardLayout>().Initialize(text, next, skip, Ballance2.Menu.Touch.MobileControls.Available);
        }

        public void StartSeq() => Show(1, true);

        private void Show(int value, bool pause)
        {
            if (!active || busy) return;
            ClearKeys(); HideArrows();
            step = value; modal = pause;
            if (pause && spawned && value >= 5) game.PauseLevel(false);
            if (value == 2 || value == 3) {
                game.BallManager.SetControllingStatus(BallControlStatus.LockMode);
                game.CamManager.enabled = true;
            }
            game.CanEscPause = true;
            if (pause) game._IsCountDownPoint = false;
            text.text = HintText(value, Ballance2.Menu.Touch.MobileControls.Available);
            card.gameObject.SetActive(true);
            next.gameObject.SetActive(true);
            skip.gameObject.SetActive(true);
            acceptAfter = Time.unscaledTime + .35f;
            int currentGeneration = generation, currentStep = step;
            continueKey = ui.ListenKey(KeyCode.Return, (_, down) => {
                if (down && generation == currentGeneration && step == currentStep) Continue();
            }, GamepadButton.A);
            skipKey = ui.ListenKey(KeyCode.Q, (_, down) => {
                if (down && generation == currentGeneration && step == currentStep) Skip();
            }, GamepadButton.B);
            if (value == 2) { Pfeil_Rund01.SetActive(true); Pfeil_Rund02.SetActive(true); }
            if (value == 3) Pfeil_Hoch.SetActive(true);
            if (value == 4) {
                var source = new ConstraintSource { sourceTransform = game.BallManager.CurrentBall.transform, weight = 1 };
                if (Tut_Richt_Pfeil.sourceCount > 0) Tut_Richt_Pfeil.SetSource(0, source); else Tut_Richt_Pfeil.AddSource(source);
                Tut_Richt_Pfeil.constraintActive = true; Tut_Richt_Pfeil.rotationAxis = Axis.None;
                Tut_Richt_Pfeil01.SetActive(true); Tut_Richt_Pfeil02.SetActive(true);
                Tut_Richt_Pfeil03.SetActive(true); Tut_Richt_Pfeil04.SetActive(true);
            }
        }

        public static string HintText(int value, bool touch) => I18N.Tr("core.ui." + (touch ? "TutorialTouch" : "TutorialText") + value);

        private bool CanAdvance => active && !busy && card != null && card.gameObject.activeSelf &&
            !game.IsUserPaused && ui.GetCurrentPage() == null && Time.unscaledTime >= acceptAfter;

        private void Continue()
        {
            if (!CanAdvance) return;
            ClearKeys();
            acceptAfter = Time.unscaledTime + .35f;
            if (step == 1) {
                busy = true;
                card.gameObject.SetActive(false);
                game.GamePhysicsWorld.Simulate = true;
                game.BallManager.KeyListener.IsListenKey = true;
                int currentGeneration = generation;
                game._Start(true, () => {
                    if (!active || generation != currentGeneration) return;
                    spawned = true; busy = false;
                    game._ShouldStartByCustom = false;
                    Show(2, true);
                });
            } else if (step == 2) Show(3, true);
            else if (step == 3) {
                HideTutorial();
                game.ResumeLevel();
                movementHintTime = 0;
                Show(4, false);
            } else {
                bool resume = modal, finished = step == 10;
                HideTutorial();
                if (resume) game.ResumeLevel();
                if (finished) Finish();
            }
        }

        private void Skip()
        {
            if (!CanAdvance) return;
            bool restart = !spawned;
            bool resume = modal || restart;
            Finish();
            if (resume) game.ResumeLevel(restart);
        }

        private void Finish()
        {

            active = false;
            HideTutorial(); ClearTriggers(); HideArrows();
            game._ShouldStartByCustom = false;
            game.CanEscPause = true;
            game.BallManager.KeyListener.IsListenKey = true;
        }

        private void Update()
        {
            if (active && step == 4 && !game.IsUserPaused && game.GamePhysicsWorld.Simulate && ui.GetCurrentPage() == null) {
                movementHintTime += Time.deltaTime;
                if (movementHintTime >= 15 && card.gameObject.activeSelf) HideTutorial();
                var ball = game.BallManager;
                Tut_Richt_Pfeil03.transform.localPosition = new Vector3(ball.KeyStateBack ? -4 : -2, .5f, 0);
                Tut_Richt_Pfeil04.transform.localPosition = new Vector3(ball.KeyStateForward ? 6 : 4, 1, 0);
                Tut_Richt_Pfeil01.transform.localPosition = new Vector3(1, 0, ball.KeyStateLeft ? 5 : 3);
                Tut_Richt_Pfeil02.transform.localPosition = new Vector3(1, 0, ball.KeyStateRight ? -5 : -3);
            }
        }

        private void Hint(int value, GameObject target = null)
        {
            if (!active || busy || game.IsUserPaused || value <= step) return;
            Show(value, true);
            if (target != null) {
                var position = target.transform.position + Vector3.up * 3;
                Pfeil_Runter.transform.position = position;
                Pfeil_Runter.gameObject.SetActive(true);
                Pfeil_Runter_Trigger.transform.position = position;
                Pfeil_Runter_Trigger.onTriggerEnter = (_, other) => Pfeil_Runter.gameObject.SetActive(false);
            }
        }

        private void BindTriggers()
        {
            Tut_ExtraLife.onTriggerEnter = (_, other) => Hint(5, Tut_ExtraLifeEnd);
            Tut_StoneTranfo.onTriggerEnter = (_, other) => Hint(6, Tut_StoneTranfoEnd);
            Tut_Rampe.onTriggerEnter = (_, other) => {
                if (other != null && other.gameObject.name == "BallStone") Hint(7, Tut_WoodTranfoEnd);
            };
            Tut_ExtraPoint.onTriggerEnter = (_, other) => Hint(8, Tut_ExtraPointEnd);
            Tut_Checkpoint.onTriggerEnter = (_, other) => Hint(9, Tut_CheckpointEnd);
            Tut_End.onTriggerEnter = (_, other) => Hint(10);
            Tut_KeyEnd.onTriggerEnter = (_, other) => { if (step == 4) HideTutorial(); };
        }

        public void HideTutorial()
        {
            ClearKeys(); HideArrows();
            if (card != null) card.gameObject.SetActive(false);
            game.CanEscPause = true;
            modal = false;
        }
        private void Fall(params object[] args) { if (step == 4) HideTutorial(); else HideArrows(); }
        private void ClearKeys()
        {
            if (ui != null) {
                if (continueKey != 0) ui.DeleteKeyListen(continueKey);
                if (skipKey != 0) ui.DeleteKeyListen(skipKey);
            }
            continueKey = skipKey = 0;
        }
        private void HideArrows()
        {
            foreach (var arrow in new[] { Tut_Richt_Pfeil01, Tut_Richt_Pfeil02, Tut_Richt_Pfeil03, Tut_Richt_Pfeil04,
                Pfeil_Rund01, Pfeil_Rund02, Pfeil_Hoch, Pfeil_Runter != null ? Pfeil_Runter.gameObject : null })
                if (arrow != null) arrow.SetActive(false);
        }
        private void ClearTriggers()
        {
            foreach (var trigger in new[] { Tut_ExtraLife, Tut_StoneTranfo, Tut_Rampe, Tut_ExtraPoint, Tut_Checkpoint, Tut_End, Tut_KeyEnd, Pfeil_Runter_Trigger })
                if (trigger != null) trigger.onTriggerEnter = null;
        }
        private void Cleanup()
        {
            generation++;
            active = false; busy = false; ClearKeys(); ClearTriggers(); HideArrows();
            if (card != null) { card.gameObject.SetActive(false); Destroy(card.gameObject); }
            card = null;
        }
        private void Quit(params object[] args) => Cleanup();
        private void Detach()
        {
            Cleanup();
            if (subscribed && game != null) {
                game.EventBeforeStart.Off(BeforeStart); game.EventQuit.Off(Quit);
                game.EventRestart.Off(Quit); game.EventFall.Off(Fall);
            }
            subscribed = false;
            if (entry != null) {
                if (GameMediator.Instance != null) GameMediator.Instance.UnRegisterEventHandler("CoreTutorialLevelEventHandler", entry);
                entry = null;
            }
        }
        private void OnDestroy() => Detach();
    }
}
