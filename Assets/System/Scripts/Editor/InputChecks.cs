using System;
using AndroidInput = Ballance2.InputCompatibility.AndroidInput;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.LowLevel;

namespace Ballance2.Editor
{
  public static class InputChecks
  {
    public static void Run()
    {
      MobileInputChecks.Run();
      var keyboard = InputSystem.AddDevice<Keyboard>();
      var mouse = InputSystem.AddDevice<Mouse>();
      var screen = InputSystem.AddDevice<Touchscreen>();
      AndroidInput.EnableTouch();
      try
      {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.UpArrow, Key.LeftCtrl, Key.Digit0, Key.Enter));
        InputSystem.Update();
        foreach (var key in new[] { KeyCode.UpArrow, KeyCode.LeftControl, KeyCode.Alpha0, KeyCode.Return })
          Require(AndroidInput.GetKey(key), "Saved key binding: " + key);
        Require(!AndroidInput.GetKey(KeyCode.None), "Unassigned key");
        Require(AndroidInput.ToKey(KeyCode.KeypadEnter) == Key.NumpadEnter &&
          AndroidInput.ToKey(KeyCode.Print) == Key.PrintScreen, "Named key aliases");
        // Edit-mode updates exercise state; frame-edge flags require the player loop.
        Require(!AndroidInput.GetMouseButton(0), "Initial mouse state");
        InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1, delta = new Vector2(10, -20) });
        InputSystem.Update();
        Require(AndroidInput.GetMouseButton(0), "Mouse press");
        Require(Mathf.Approximately(AndroidInput.GetAxis("Mouse X"), 1) &&
          Mathf.Approximately(AndroidInput.GetAxis("Mouse Y"), -2), "Mouse sensitivity changed");
        InputSystem.QueueStateEvent(mouse, new MouseState());
        InputSystem.Update();
        Require(!AndroidInput.GetMouseButton(0), "Mouse release");
        InputSystem.QueueStateEvent(screen, new TouchState { touchId = 1,
          phase = UnityEngine.InputSystem.TouchPhase.Began, position = new Vector2(40, 50) });
        InputSystem.Update();
        Require(AndroidInput.touchCount == 1 && AndroidInput.GetTouch(0).position == new Vector2(40, 50), "Touch position");
        InputSystem.QueueStateEvent(screen, new TouchState { touchId = 1,
          phase = UnityEngine.InputSystem.TouchPhase.Ended, position = new Vector2(40, 50) });
        InputSystem.Update();
        InputSystem.Update();
        Require(AndroidInput.touchCount == 0 && !AndroidInput.GetMouseButton(0), "Touch remained pressed");
        Debug.Log("Android input checks passed: saved keys, mouse sensitivity, touch press and release.");
      }
      finally
      {
        EnhancedTouchSupport.Disable();
        InputSystem.RemoveDevice(screen);
        InputSystem.RemoveDevice(mouse);
        InputSystem.RemoveDevice(keyboard);
      }
    }

    private static void Require(bool valid, string message)
    {
      if (!valid) throw new InvalidOperationException(message);
    }
  }
}
