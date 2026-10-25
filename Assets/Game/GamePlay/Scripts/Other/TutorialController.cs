using Ballance2.Base;
using Ballance2.Game.Remaster;
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
        private int step, skipKey;
        private bool active, subscribed, spawned, busy, modal;
        private float movementHintTime;

        private void Start()
        {
            game = GamePlayManager.Instance;
            ui = GameUIManager.Instance;
            entry = GameMediator.Instance.RegisterEventHandler(GamePackage.GetSystemPackage(),
                "CoreTutorialLevelEventHandler", "TutorialHandler", (_, args) => {
                    if ((string)args[0] == "beforeStart" && !subscribed) {
                        game.EventBeforeStart.On(BeforeStart);
                        game.EventQuit.On(Quit);
                        subscribed = true;
                    } else if ((string)args[0] == "beforeQuit") Detach();
                    return false;
                });
        }

        private void BeforeStart(params object[] args)
        {
            Cleanup();
            HideArrows();
            game._ShouldStartByCustom = false;
            game.CanEscPause = true;
            if (RemasterPreferences.GetInt("tutorial.dismissed") != 0) return;
            active = true; spawned = false; busy = false; step = 1;
            game._ShouldStartByCustom = true;
            game.GamePhysicsWorld.Simulate = false;
            CreateCard();
            BindTriggers();
            StartSeq();
        }

        private void CreateCard()
        {
            card = ui.InitViewToCanvas(GamePackage.GetSystemPackage().GetPrefabAsset("GameTutorialUI.prefab"), "GameTutorialUI", false);
            var panel = (RectTransform)card.GetChild(0);
            var buttons = (RectTransform)card.GetChild(1);
            foreach (var layout in card.GetComponentsInChildren<LayoutGroup>()) layout.enabled = false;
            RemasterTheme.Place(panel, .06f, .58f, .94f, .94f);
            panel.GetComponent<Image>().sprite = null;
            panel.GetComponent<Image>().color = new Color(.035f, .085f, .11f, .96f);
            text = panel.GetChild(0).GetComponent<Text>();
            RemasterTheme.Place(text.rectTransform, .035f, .07f, .965f, .93f);
            text.font = RemasterTheme.Font; text.fontSize = 24; text.lineSpacing = 1.2f;
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 16; text.resizeTextMaxSize = 24;
            RemasterTheme.Place(buttons, .06f, .49f, .94f, .56f);
            next = buttons.GetChild(0).GetComponent<Button>();
            skip = buttons.GetChild(1).GetComponent<Button>();
            RemasterTheme.Place((RectTransform)next.transform, .51f, 0, 1, 1);
            RemasterTheme.Place((RectTransform)skip.transform, 0, 0, .49f, 1);
            next.onClick.RemoveAllListeners(); skip.onClick.RemoveAllListeners();
            next.onClick.AddListener(Continue);
            skip.onClick.AddListener(Skip);
            RemasterTheme.Apply(card.gameObject);
            foreach (var label in new[] { next.GetComponentInChildren<Text>(), skip.GetComponentInChildren<Text>() }) label.fontSize = 20;
            skip.GetComponentInChildren<Ballance2.UI.Core.Controls.I18NText>().resourceKey = "core.ui.Remaster.SkipTutorial";
        }

        public void StartSeq() => Show(1, true);

        private void Show(int value, bool pause)
        {
            if (!active || busy) return;
            ClearKeys();
            step = value; modal = pause;
            if (pause && spawned) game.PauseLevel(false);
            game.CanEscPause = !pause;
            if (pause) game._IsCountDownPoint = false;
            text.text = HintText(value, Application.isMobilePlatform);
            card.gameObject.SetActive(true);
            next.gameObject.SetActive(value != 4);
            skip.gameObject.SetActive(true);
            if (value != 4) next.Select();
            // Escape is reserved by the card while modal; movement bindings remain untouched.
            if (pause) skipKey = ui.WaitKey(KeyCode.Escape, true, Skip, GamepadButton.B);
        }

        public static string HintText(int value, bool touch)
        {
            string result = I18N.Tr("core.ui.Remaster.Tutorial" + value);
            var settings = GameManager.Instance.GameSettings;
            string Key(string key) => settings.GetKeyCode(key).ToString();
            string movement = touch ? I18N.Tr("core.ui.Remaster.TouchMove") :
                Key(SettingConstants.SettingsControlKeyFront) + " / " + Key(SettingConstants.SettingsControlKeyBack) + " / " +
                Key(SettingConstants.SettingsControlKeyLeft) + " / " + Key(SettingConstants.SettingsControlKeyRight);
            string rotation = touch ? I18N.Tr("core.ui.Remaster.TouchRotate") :
                Key(SettingConstants.SettingsControlKeyRoate) + " + " + Key(SettingConstants.SettingsControlKeyLeft) + " / " + Key(SettingConstants.SettingsControlKeyRight);
            string overhead = touch ? I18N.Tr("core.ui.Remaster.TouchLook") : Key(SettingConstants.SettingsControlKeyUpCam);
            return result.Replace("{move}", movement).Replace("{rotate}", rotation).Replace("{look}", overhead);
        }

        private void Continue()
        {
            if (!active || busy) return;
            ClearKeys();
            if (step == 1) {
                busy = true;
                card.gameObject.SetActive(false);
                game.GamePhysicsWorld.Simulate = true;
                game.BallManager.KeyListener.IsListenKey = true;
                game._Start(true, () => {
                    if (!active) return;
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
                bool finished = step == 10;
                HideTutorial();
                game.ResumeLevel();
                if (finished) Finish();
            }
        }

        private void Skip()
        {
            if (!active || busy) return;
            bool restart = !spawned;
            bool resume = modal || restart;
            Finish();
            if (resume) game.ResumeLevel(restart);
        }

        private void Finish()
        {
            RemasterPreferences.SetInt("tutorial.dismissed", 1);
            active = false;
            HideTutorial();
            ClearTriggers();
            HideArrows();
            game._ShouldStartByCustom = false;
            game.CanEscPause = true;
            game.BallManager.KeyListener.IsListenKey = true;
        }

        private void Update()
        {
            if (active && step == 4 && game.GamePhysicsWorld.Simulate) {
                movementHintTime += Time.deltaTime;
                if (movementHintTime >= 12) HideTutorial();
            }
        }

        private void Hint(int value, GameObject target = null)
        {
            if (!active || busy || value <= step) return;
            Show(value, true);
            if (target != null) {
                var position = target.transform.position + Vector3.up * 3;
                Pfeil_Runter.transform.position = position;
                Pfeil_Runter.gameObject.SetActive(true);
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
            ClearKeys();
            if (card != null) card.gameObject.SetActive(false);
            if (Pfeil_Runter != null) Pfeil_Runter.gameObject.SetActive(false);
            game.CanEscPause = true;
            modal = false;
        }

        private void ClearKeys()
        {
            if (skipKey != 0 && ui != null) ui.DeleteKeyListen(skipKey);
            skipKey = 0;
        }
        private void HideArrows()
        {
            foreach (var arrow in new[] { Tut_Richt_Pfeil01, Tut_Richt_Pfeil02, Tut_Richt_Pfeil03, Tut_Richt_Pfeil04,
                Pfeil_Rund01, Pfeil_Rund02, Pfeil_Hoch, Pfeil_Runter != null ? Pfeil_Runter.gameObject : null })
                if (arrow != null) arrow.SetActive(false);
        }
        private void ClearTriggers()
        {
            foreach (var trigger in new[] { Tut_ExtraLife, Tut_StoneTranfo, Tut_Rampe, Tut_ExtraPoint, Tut_Checkpoint, Tut_End, Tut_KeyEnd })
                if (trigger != null) trigger.onTriggerEnter = null;
        }
        private void Cleanup()
        {
            active = false; ClearKeys(); ClearTriggers();
            if (card != null) Destroy(card.gameObject);
            card = null;
        }
        private void Quit(params object[] args) => Cleanup();
        private void Detach()
        {
            Cleanup();
            if (subscribed && game != null) { game.EventBeforeStart.Off(BeforeStart); game.EventQuit.Off(Quit); }
            subscribed = false;
            if (entry != null) {
                if (GameMediator.Instance != null) GameMediator.Instance.UnRegisterEventHandler("CoreTutorialLevelEventHandler", entry);
                entry = null;
            }
        }
        private void OnDestroy() => Detach();
    }
}
