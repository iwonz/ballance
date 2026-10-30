using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Ballance2.UI.Utils;
using Ballance2.Services;
using Ballance2.Game;

namespace Ballance2.Menu.Touch
{
  /// <summary>
  /// Mobile keyboard UI controller.
  /// </summary>
  [RequireComponent(typeof(KeypadGridLayout))]
  public class SimpleTouchDirectionKeyController : UIBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
  {
    public Image ImageForward;
    public Image ImageForwardLeft;
    public Image ImageForwardRight;
    public Image ImageBack;
    public Image ImageBackLeft;
    public Image ImageBackRight;
    public Image ImageLeft;
    public Image ImageRight;
    public Vector2 SplitCount = new Vector2(3, 3);
    public bool RectInRight = false;
    public Vector2 SplitStart = Vector2.zero;
    public KeyPadJoystickDirectionChanged DirectionChanged;

    private void CallKeyPadJoystickDirectionChanged(bool stat, KeyPadJoystickDirection dir) {
      if(stat)
        currentState |= dir;
      else
        currentState &= ~dir;

      ImageForwardLeft.enabled = (currentState & KeyPadJoystickDirection.LeftForward) == KeyPadJoystickDirection.LeftForward;
      ImageForwardRight.enabled = (currentState & KeyPadJoystickDirection.RightForward) == KeyPadJoystickDirection.RightForward;
      ImageBackLeft.enabled = (currentState & KeyPadJoystickDirection.LeftBack) == KeyPadJoystickDirection.LeftBack;
      ImageBackRight.enabled = (currentState & KeyPadJoystickDirection.RightBack) == KeyPadJoystickDirection.RightBack;

      DirectionChanged?.Invoke(stat, dir);
    }
    private void ClearState(KeyPadJoystickDirection without = KeyPadJoystickDirection.None) {
      if((without & KeyPadJoystickDirection.Back) != KeyPadJoystickDirection.Back && (currentState & KeyPadJoystickDirection.Back) != KeyPadJoystickDirection.None) CallKeyPadJoystickDirectionChanged(false, KeyPadJoystickDirection.Back);
      if((without & KeyPadJoystickDirection.Forward) != KeyPadJoystickDirection.Forward && (currentState & KeyPadJoystickDirection.Forward) != KeyPadJoystickDirection.None) CallKeyPadJoystickDirectionChanged(false, KeyPadJoystickDirection.Forward);
      if((without & KeyPadJoystickDirection.Left) != KeyPadJoystickDirection.Left && (currentState & KeyPadJoystickDirection.Left) != KeyPadJoystickDirection.None) CallKeyPadJoystickDirectionChanged(false, KeyPadJoystickDirection.Left);
      if((without & KeyPadJoystickDirection.Right) != KeyPadJoystickDirection.Right && (currentState & KeyPadJoystickDirection.Right) != KeyPadJoystickDirection.None) CallKeyPadJoystickDirectionChanged(false, KeyPadJoystickDirection.Right);
    }
    private void HandleTouchPos(Vector2 pos) {
      float spx = (rect.width / SplitCount.x);
      float spy = (rect.height / SplitCount.y);
      // Calculate the button directly from the nine-cell grid.
      float x = pos.x - rect.x - SplitStart.x * spx;
      float y = pos.y - rect.y - SplitStart.y * spy;
      int xg = (int)Mathf.Floor(x / spx);
      int yg = (int)Mathf.Floor(y / spy);
      switch(yg) {// Y: top, middle, bottom.
        case 2: // Top.
          if(xg == 0) { // X: left and right.
            ClearState(KeyPadJoystickDirection.LeftForward);
            if((currentState & KeyPadJoystickDirection.Left) == KeyPadJoystickDirection.None)
              CallKeyPadJoystickDirectionChanged(true, KeyPadJoystickDirection.Left);
          }
          else if(xg == 2) {
            ClearState(KeyPadJoystickDirection.RightForward);
            if((currentState & KeyPadJoystickDirection.Right) == KeyPadJoystickDirection.None)
              CallKeyPadJoystickDirectionChanged(true, KeyPadJoystickDirection.Right);
          } else {
            ClearState(KeyPadJoystickDirection.Forward);
          }
          if((currentState & KeyPadJoystickDirection.Forward) == KeyPadJoystickDirection.None)
            CallKeyPadJoystickDirectionChanged(true, KeyPadJoystickDirection.Forward);
          break;
        case 1: // Middle.
          if(xg == 0) { // X: left and right.
            ClearState(KeyPadJoystickDirection.Left);
            if((currentState & KeyPadJoystickDirection.Left) == KeyPadJoystickDirection.None)
              CallKeyPadJoystickDirectionChanged(true, KeyPadJoystickDirection.Left);
          }
          else if(xg == 2) {
            ClearState(KeyPadJoystickDirection.Right);
            if((currentState & KeyPadJoystickDirection.Right) == KeyPadJoystickDirection.None)
              CallKeyPadJoystickDirectionChanged(true, KeyPadJoystickDirection.Right);
          } else {
            ClearState();
          }
          break;
        case 0: // Bottom.
          if(xg == 0) { // X: left and right.
            ClearState(KeyPadJoystickDirection.LeftBack);
            if((currentState & KeyPadJoystickDirection.Left) == KeyPadJoystickDirection.None)
              CallKeyPadJoystickDirectionChanged(true, KeyPadJoystickDirection.Left);
          }
          else if(xg == 2) {
            ClearState(KeyPadJoystickDirection.RightBack);
            if((currentState & KeyPadJoystickDirection.Right) == KeyPadJoystickDirection.None)
              CallKeyPadJoystickDirectionChanged(true, KeyPadJoystickDirection.Right);
          } else {
            ClearState(KeyPadJoystickDirection.Back);
          }
          if((currentState & KeyPadJoystickDirection.Back) == KeyPadJoystickDirection.None)
            CallKeyPadJoystickDirectionChanged(true, KeyPadJoystickDirection.Back);
          break;
      }
    }

    private KeyPadJoystickDirection currentState;
    private KeypadGridLayout keypadGridLayout;
    private int? pointer;
    private Rect rect = new Rect();
    private int settingsUpdateCallbackId = 0;

    private void InitKeys() {
      var GameSettings = GameManager.Instance.GameSettings;
      settingsUpdateCallbackId = GameSettings.RegisterSettingsUpdateCallback(SettingConstants.SettingsControl, (groupName, action) => {
        SetKeysSize(GameSettings.GetFloat(SettingConstants.SettingsControlKeySize));
        return false;
      });
      SetKeysSize(GameSettings.GetFloat(SettingConstants.SettingsControlKeySize));
    }
    private void SetKeysSize(float newKeySize) {
      keypadGridLayout.DoLayout(newKeySize);
    }

    protected override void Start()
    {
      keypadGridLayout = GetComponent<KeypadGridLayout>();
      currentState = KeyPadJoystickDirection.None;
      InitKeys();
      base.Start();
    }
    protected override void OnDestroy()
    {
      base.OnDestroy();
      if (GameManager.Instance != null)
        GameManager.Instance.GameSettings.UnRegisterSettingsUpdateCallback(settingsUpdateCallbackId);
    }

    public void OnPointerDown(PointerEventData data)
    {
      if (pointer.HasValue || data.button != PointerEventData.InputButton.Left) return;
      pointer = data.pointerId;
      OnDrag(data);
    }
    public void OnDrag(PointerEventData data)
    {
      if (pointer != data.pointerId) return;
      var area = (RectTransform)transform;
      rect = area.rect;
      if (RectTransformUtility.ScreenPointToLocalPointInRectangle(area, data.position, data.pressEventCamera, out var point) && rect.Contains(point))
        HandleTouchPos(point);
      else ClearState();
    }
    public void OnPointerUp(PointerEventData data) { if (pointer == data.pointerId) Release(); }
    public void Release() { pointer = null; ClearState(); }
    protected override void OnDisable() { Release(); base.OnDisable(); }
    private void OnApplicationFocus(bool focused) { if (!focused) Release(); }
    private void OnApplicationPause(bool paused) { if (paused) Release(); }
  }
}
