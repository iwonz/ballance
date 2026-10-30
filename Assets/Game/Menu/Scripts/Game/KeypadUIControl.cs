using Ballance2.Game;
using Ballance2.Menu.Touch;
using Ballance2.Services;
using Ballance2.Services.InputManager;
using UnityEngine;

namespace Ballance2.Menu
{
  
  public class KeypadUIControl : MonoBehaviour {
    
    private int shiftPressCount = 0;
    //private bool shiftPressOne = false;
    private bool lastCameraSpaceByShift = false;

    public KeypadGridLayout[] KeypadActionButtons;
    private int settingsUpdateCallbackId = 0;

    private void Start() {
      InitKeys();
      InitKeySize();
    }
    private void OnDestroy() {
     if (GameManager.Instance != null)
       GameManager.Instance.GameSettings.UnRegisterSettingsUpdateCallback(settingsUpdateCallbackId);
    }

    private void OnDisable() { Release(); }
    public void Release() {
      shiftPressCount = 0;
      lastCameraSpaceByShift = false;
      foreach (var joystick in GetComponentsInChildren<SimpleTouchController>(true)) joystick.EndDrag();
      foreach (var direction in GetComponentsInChildren<SimpleTouchDirectionKeyController>(true)) direction.Release();
      var play = Game.GamePlay.GamePlayManager.Instance;
      if (play == null || play.BallManager == null) return;
      var ball = play.BallManager;
      ball.KeyStateForward = ball.KeyStateBack = ball.KeyStateLeft = ball.KeyStateRight = false;
      ball.KeyStateUp = ball.KeyStateDown = false;
      ball.RemoveAllBallPush();
      if (play.CamManager != null) play.CamManager.RotateUp(false);
    }
    
    // Keyboard size settings.
    private void InitKeySize() {
      var GameSettings = GameManager.Instance.GameSettings;
      settingsUpdateCallbackId = GameSettings.RegisterSettingsUpdateCallback(SettingConstants.SettingsControl, (groupName, action) => {
        SetKeysSize(GameSettings.GetFloat(SettingConstants.SettingsControlKeySize));
        return false;
      });
      SetKeysSize(GameSettings.GetFloat(SettingConstants.SettingsControlKeySize));
    }
    private void SetKeysSize(float newKeySize) {
      if (KeypadActionButtons != null)
        foreach(var layout in KeypadActionButtons)
          layout.DoLayout(newKeySize);
    }
   
    // Discover Button-prefixed objects up to two hierarchy levels deep.
    private void InitKeys() {
      for (int i = 0; i < transform.childCount; i++) {

        var child = transform.GetChild(i);
        if(
          child.gameObject.name.StartsWith("Button") 
          || child.gameObject.name.StartsWith("Joystick")
          || child.gameObject.name.StartsWith("DirectionKey")
        ) {
          AddButton(child.gameObject);
        }
        if (child.childCount > 0) {
          for (int j = 0; j < child.childCount; j++) {
            var child2 = child.GetChild(j).gameObject;
            if(child2.name.StartsWith("Button"))
              AddButton(child2);
          }
        }
      }
      
    }
    
    // Add buttons.
    private void AddButton(GameObject go) {
      var name = go.name;
      var GamePlayManager = Game.GamePlay.GamePlayManager.Instance;
      var CamManager = GamePlayManager.CamManager;
      var BallManager = GamePlayManager.BallManager;

      if (name.StartsWith("Joystick")) {

        var Joystick = go.GetComponent<KeyPadJoystickController>();
        Joystick.ValueChanged = (x, y) => BallManager.SetBallPushValue(x, y);

      }
      else if (name.StartsWith("DirectionKey")) {

        var DirectionKey = go.GetComponent<SimpleTouchDirectionKeyController>();
        DirectionKey.DirectionChanged = (state, dir) => {
          switch(dir) {
            case KeyPadJoystickDirection.Back:
              BallManager.KeyStateBack = state;
              BallManager.FlushBallPush();
              break;
            case KeyPadJoystickDirection.Forward:
              BallManager.KeyStateForward = state;
              BallManager.FlushBallPush();
              break;
            case KeyPadJoystickDirection.Left:
              BallManager.KeyStateLeft = state;
              BallManager.FlushBallPush();
              break;
            case KeyPadJoystickDirection.Right:
              BallManager.KeyStateRight = state;
              BallManager.FlushBallPush();
              break;
          }
        };
      } 
      else {
        var listener = EventTriggerListener.Get(go);
        if (name == "ButtonUp") {

          // Show only in debug mode.
          if (!GameManager.DebugMode)
            go.SetActive(false);
          else {
            // Upward movement key.
            listener.onDown = (_) => {
              BallManager.KeyStateUp = true;
              BallManager.FlushBallPush();
            };
            listener.onUp = (_) => {
              BallManager.KeyStateUp = false;
              BallManager.FlushBallPush();
            };
          }

        }
        else if (name == "ButtonDown") {

          // Show only in debug mode.
          if (!GameManager.DebugMode)
            go.SetActive(false);
          else {
            // Downward movement key.
            listener.onDown = (_) => {
              BallManager.KeyStateDown = true;
              BallManager.FlushBallPush();
            };
            listener.onUp = (_) => {
              BallManager.KeyStateDown = false;
              BallManager.FlushBallPush();
            };
          }

        }
        else if (name == "ButtonSpaceShift") {

          // Combined Shift and Space key.
          listener.onDown = (_) => {
            shiftPressCount++;
            if (shiftPressCount >= 2 && GamePlayManager.BallManager.CanControllCamera) {
              lastCameraSpaceByShift = true;
              CamManager.RotateUp(true);
            }
          };
          listener.onUp = (_) => {
            shiftPressCount = Mathf.Max(0, shiftPressCount - 1);
            if (lastCameraSpaceByShift && GamePlayManager.BallManager.CanControllCamera) {
              lastCameraSpaceByShift = false;
              CamManager.RotateUp(false);
            }
          };
        }
        else if (name == "ButtonSpace") {

          // Space key.
          listener.onDown = (_) => {
            if (GamePlayManager.BallManager.CanControllCamera) 
              CamManager.RotateUp(true);
          };
          listener.onUp = (_) => CamManager.RotateUp(false);

        }
        else if (name == "ButtonShift") {

          // Shift key.
          //listener.onDown = (_) => shiftPressOne = true;
          //listener.onUp = (_) => shiftPressOne = false;

        }
        else if (name == "ButtonCamLeft") {

          // Rotate camera left key.
          listener.onClick = (_) => {
            if (GamePlayManager.BallManager.CanControllCamera)
              CamManager.RotateLeft();
          };

        }
        else if (name == "ButtonCamRight") {

          // Rotate camera right key.
          listener.onClick = (_) => {
            if (GamePlayManager.BallManager.CanControllCamera)
              CamManager.RotateRight();
          };

        }
      }
    }

  }
}
