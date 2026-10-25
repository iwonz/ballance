using System.Collections.Generic;
using Ballance2.Base;
using Ballance2.Game;
using Ballance2.Game.GamePlay;
using Ballance2.Services;
using Ballance2.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Ballance2.Menu
{
  /// <summary>
  /// Main gameplay menu controller.
  /// </summary>
  public class GamePlayUIControl : GameSingletonBehavior<GamePlayUIControl> 
  {
    public Image _ScoreBoardActive ;
    public Text _ScoreText;
    public Image _LifeBoardLeftBaffle;
    public GameObject _LifeBoardBallPrefab;
    public GameObject _LifeBoardBallInfPrefab;
    public RectTransform _LifeBalls;
    public GameObject _TextDebugMode;
    public GuiStats GuiStats;

    private int _CurrentShowLifeBallCount = 0;
    private float _CurrentMoveBaffleTick = 0.3f;
    private float _MoveBaffleSec = 0.3f;
    private float _CurrentMoveBaffleStart = 0;
    private float _CurrentMoveBaffleTarget = 0;
    private RectTransform _CurrentMobileKeyPad = null;
    private bool _CurrentMobileKeyPadShow = false;

    public Dictionary<string, GuiStatsValue> DebugStatValues = new Dictionary<string, GuiStatsValue>();

    private void Start() {
      _ScoreBoardActive.gameObject.SetActive(false);
      Ballance2.Game.Remaster.RemasterHud.Install(this);

        // Create a touch keyboard on mobile.
        #if (UNITY_ANDROID || UNITY_IOS)
        // Show the on-screen keyboard only when the device has a touchscreen.
        if (UnityEngine.InputSystem.Touchscreen.current != null)
        {
            _CurrentMobileKeyPadShow = true;
            ReBuildMobileKeyPad();
            GamePlayManager.Instance.EventBeforeStart.On((evt) =>
            {
                _CurrentMobileKeyPadShow = true;
                if (_CurrentMobileKeyPad != null)
                    _CurrentMobileKeyPad.gameObject.SetActive(true);
            });
            GamePlayManager.Instance.EventQuit.On((evt) =>
            {
                _CurrentMobileKeyPadShow = false;
                if (_CurrentMobileKeyPad != null)
                    _CurrentMobileKeyPad.gameObject.SetActive(false);
            });
        }
      #endif

      // Create debug information.
      if (GameManager.DebugMode) {
        DebugStatValues["CurrentBall"] = GuiStats.AddStat("CurrentBall");
        DebugStatValues["CurrentStatus"] = GuiStats.AddStat("CurrentStatus");
        DebugStatValues["Position"] = GuiStats.AddStat("Position");
        DebugStatValues["Rotation"] = GuiStats.AddStat("Rotation");
        DebugStatValues["PhysicsState"] = GuiStats.AddStat("PhysicsState");
        DebugStatValues["CamDirection"] = GuiStats.AddStat("CamDirection");
        DebugStatValues["CamState"] = GuiStats.AddStat("CamState");
        DebugStatValues["Velocity"] = GuiStats.AddStat("Velocity");
        DebugStatValues["PushValue"] = GuiStats.AddStat("PushValue");
        DebugStatValues["Sector"] = GuiStats.AddStat("Sector");
        DebugStatValues["Moduls"] = GuiStats.AddStat("Moduls");
        DebugStatValues["PhysicsTime"] = GuiStats.AddStat("PhysicsTime");
        DebugStatValues["PhysicsObjects"] = GuiStats.AddStat("PhysicsObjects");
        _TextDebugMode.SetActive(true);
      } else {
        _TextDebugMode.SetActive(false);
      }
    }

    private void Update()
    {
      if (_CurrentMoveBaffleTick < _MoveBaffleSec)
      {
        _CurrentMoveBaffleTick = _CurrentMoveBaffleTick + Time.deltaTime;
        _LifeBoardLeftBaffle.rectTransform.anchoredPosition = new Vector2(
          Mathf.Lerp(_CurrentMoveBaffleStart, _CurrentMoveBaffleTarget, _CurrentMoveBaffleTick / _MoveBaffleSec),
          0
        );
      }
    }

    protected override void OnDestroy()
    {
      base.OnDestroy();
      this.DestroyMobileKeyPad();
    }

    private void DestroyMobileKeyPad() {
      // Destroy the keyboard.
      if (_CurrentMobileKeyPad != null) {
        Destroy(_CurrentMobileKeyPad.gameObject);
        _CurrentMobileKeyPad = null;
      }
    }
    internal void ReBuildMobileKeyPad() {
      // Destroy the keyboard.
      DestroyMobileKeyPad();
      // Read keyboard settings.
      var settings = GameSettingsManager.GetSettings(GamePackageManager.SYSTEM_PACKAGE_NAME);
      var controlKeypadSettting = settings.GetString(SettingConstants.SettingsControlKeypad);

      KeypadUIManager.KeypadUIInfo keyPad;
      if (!KeypadUIManager.GetKeypadRegistered(controlKeypadSettting)) 
      {
        Log.E("GamePlayUIControl", "Keypad in setting \"" + controlKeypadSettting + "\" not found, use default keypad insted.");
        keyPad = KeypadUIManager.GetKeypad("BaseCenter");
      } 
      else 
      {
        keyPad = KeypadUIManager.GetKeypad(controlKeypadSettting);
      }

      // Create the keyboard.
      _CurrentMobileKeyPad = GameUIManager.Instance.InitViewToCanvas(keyPad.prefab, "GameMobileKeypad", false);
      // When the on-screen keyboard is hidden.
      if (!_CurrentMobileKeyPadShow)
        _CurrentMobileKeyPad.gameObject.SetActive(false);
    }

    /// <summary>
    /// Flash the points display.
    /// </summary>
    public void TwinklePoint() 
    {
      _ScoreBoardActive.gameObject.SetActive(true);
      GameUIManager.Instance.UIFadeManager.AddFadeOut(_ScoreBoardActive, 1, true);
    }
    /// <summary>
    /// Set the points display text.
    /// </summary>
    /// <param name="score">Score.</param>
    public void SetPointText(int score) 
    {
      _ScoreText.text = score.ToString();
    }
    /// <summary>
    /// Add one displayed reserve life.
    /// </summary>
    public void AddLifeBall()
    {
      if (_CurrentShowLifeBallCount != -1) {
        _CurrentShowLifeBallCount++;

        var ball = CloneUtils.CloneNewObjectWithParent(_LifeBoardBallPrefab, _LifeBalls).GetComponent<Image>();
        ball.rectTransform.SetAsFirstSibling();
        ball.color = new Color(1,1,1,0);
        _MoveLifeLeftBaffle();
        GameTimer.Delay(0.4f, () => {
          GameUIManager.Instance.UIFadeManager.AddFadeIn(ball, 0.4f);
        });
      }
    }
    /// <summary>
    /// Remove one displayed reserve life.
    /// </summary>
    public void RemoveLifeBall()
    {
      if (_CurrentShowLifeBallCount <= -1) 
        return;
      if (_CurrentShowLifeBallCount > 0) {
        _CurrentShowLifeBallCount--;
        var ballIndex = _LifeBalls.childCount - 1 - _CurrentShowLifeBallCount;
        if (ballIndex < 0) ballIndex = 0;
        if (ballIndex == 0 && _LifeBalls.childCount == 0) {
          _MoveLifeLeftBaffle();
          return;
        }

        var ball = _LifeBalls.GetChild(ballIndex);
        GameUIManager.Instance.UIFadeManager.AddFadeOut(ball.GetComponent<Image>(), 0.4f, true);
        GameTimer.Delay(0.4f, () => {
          _MoveLifeLeftBaffle();
          UnityEngine.Object.Destroy(ball.gameObject);
        });
      }
    }
    /// <summary>
    /// Set displayed reserve lives directly without animation.
    /// </summary>
    /// <param name="count">Count.</param>
    public void SetLifeBallCount(int count)
    {
      if (count != _CurrentShowLifeBallCount) {
        _CurrentShowLifeBallCount = count;
        if (count == -1) {
          // Display the infinity symbol for -1.
          for (var i = _LifeBalls.childCount - 1; i >= 0; i--)
            Destroy(_LifeBalls.GetChild(i).gameObject);
          CloneUtils.CloneNewObjectWithParent(_LifeBoardBallInfPrefab, _LifeBalls);
          _MoveLifeLeftBaffle();
        }
        else {
          if (_LifeBalls.childCount > count) {
            // Remove excess icons when the displayed count is too high.
            for (var i = _LifeBalls.childCount - 1; i >= count; i--)
              UnityEngine.Object.Destroy(_LifeBalls.GetChild(i).gameObject);
            _MoveLifeLeftBaffle();
          }
          else if (_LifeBalls.childCount < count) {
            // Add icons when the displayed count is too low.
            for (var i = count - _LifeBalls.childCount; i > 0; i--)
              CloneUtils.CloneNewObjectWithParent(_LifeBoardBallPrefab, _LifeBalls);
            _MoveLifeLeftBaffle();
          }
        }
      }
    }

    private void _MoveLifeLeftBaffle() 
    {
      if (_CurrentShowLifeBallCount < 0)
        _CurrentMoveBaffleTarget =  -27;
      else
        _CurrentMoveBaffleTarget =  -(_CurrentShowLifeBallCount * 27);
      _CurrentMoveBaffleTick = 0;
      _CurrentMoveBaffleStart = _LifeBoardLeftBaffle.rectTransform.anchoredPosition.x;
    }
  }
}
