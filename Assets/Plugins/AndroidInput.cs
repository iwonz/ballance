using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Ballance2.InputCompatibility
{
  // Android cannot use Both backends; retain saved KeyCodes and the existing control scripts.
  public static class AndroidInput
  {
    private static readonly Dictionary<KeyCode, Key> keys = new Dictionary<KeyCode, Key>();

#if UNITY_ANDROID && !UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
    public static void EnableTouch() => EnhancedTouchSupport.Enable();

    public static Key ToKey(KeyCode code)
    {
      if (keys.TryGetValue(code, out var key)) return key;
      string name = code.ToString();
      switch (code)
      {
        case KeyCode.Return: name = "Enter"; break;
        case KeyCode.LeftControl: name = "LeftCtrl"; break;
        case KeyCode.RightControl: name = "RightCtrl"; break;
        case KeyCode.LeftCommand: case KeyCode.LeftWindows: name = "LeftMeta"; break;
        case KeyCode.RightCommand: case KeyCode.RightWindows: name = "RightMeta"; break;
        case KeyCode.AltGr: name = "RightAlt"; break;
        case KeyCode.Print: name = "PrintScreen"; break;
        case KeyCode.Break: name = "Pause"; break;
        case KeyCode.Menu: name = "ContextMenu"; break;
        default:
          if (name.StartsWith("Alpha")) name = "Digit" + name.Substring(5);
          else if (name.StartsWith("Keypad")) name = "Numpad" + name.Substring(6);
          break;
      }
      if (!Enum.TryParse(name, true, out key)) key = Key.None;
      keys[code] = key;
      return key;
    }

    private static ButtonControl KeyButton(KeyCode code)
    {
      if (code >= KeyCode.Mouse0 && code <= KeyCode.Mouse6) return MouseButton(code - KeyCode.Mouse0);
      var key = ToKey(code);
      return key != Key.None && Keyboard.current != null ? Keyboard.current[key] : null;
    }

    private static ButtonControl MouseButton(int button)
    {
      if (button == 0 && Touchscreen.current != null &&
          (Touchscreen.current.primaryTouch.press.isPressed || Touchscreen.current.primaryTouch.press.wasReleasedThisFrame))
        return Touchscreen.current.primaryTouch.press;
      var mouse = Mouse.current;
      if (mouse == null) return null;
      switch (button)
      {
        case 0: return mouse.leftButton;
        case 1: return mouse.rightButton;
        case 2: return mouse.middleButton;
        case 3: return mouse.backButton;
        case 4: return mouse.forwardButton;
        default: return null;
      }
    }

    public static bool GetKey(KeyCode key) => KeyButton(key)?.isPressed == true;
    public static bool GetKeyDown(KeyCode key) => KeyButton(key)?.wasPressedThisFrame == true;
    public static bool GetMouseButton(int button) => MouseButton(button)?.isPressed == true;
    public static bool GetMouseButtonDown(int button) => MouseButton(button)?.wasPressedThisFrame == true;
    public static bool GetMouseButtonUp(int button) => MouseButton(button)?.wasReleasedThisFrame == true;
    public static bool anyKeyDown => Keyboard.current?.anyKey.wasPressedThisFrame == true;
    public static Vector3 mousePosition => Touchscreen.current?.primaryTouch.press.isPressed == true
      ? Touchscreen.current.primaryTouch.position.ReadValue() : Mouse.current?.position.ReadValue() ?? Vector2.zero;

    public static float GetAxis(string name)
    {
      // shortcut: Only the game's three mouse axes are mapped; extend when adding an axis.
      // Match Mouse X/Y sensitivity 0.1 in ProjectSettings/InputManager.asset.
      var delta = Touchscreen.current?.primaryTouch.press.isPressed == true
        ? Touchscreen.current.primaryTouch.delta.ReadValue() : Mouse.current?.delta.ReadValue() ?? Vector2.zero;
      switch (name)
      {
        case "Mouse X": return delta.x * 0.1f;
        case "Mouse Y": return delta.y * 0.1f;
        case "Mouse ScrollWheel": return (Mouse.current?.scroll.y.ReadValue() ?? 0) / 1200f;
        default: throw new ArgumentException("Unmapped input axis: " + name);
      }
    }

    public static float GetAxisRaw(string name) => GetAxis(name);
    public static int touchCount => Touch.activeTouches.Count;
    public static UnityEngine.Touch GetTouch(int index)
    {
      var touch = Touch.activeTouches[index];
      var phase = UnityEngine.TouchPhase.Stationary;
      switch (touch.phase)
      {
        case UnityEngine.InputSystem.TouchPhase.Began: phase = UnityEngine.TouchPhase.Began; break;
        case UnityEngine.InputSystem.TouchPhase.Moved: phase = UnityEngine.TouchPhase.Moved; break;
        case UnityEngine.InputSystem.TouchPhase.Ended: phase = UnityEngine.TouchPhase.Ended; break;
        case UnityEngine.InputSystem.TouchPhase.Canceled: phase = UnityEngine.TouchPhase.Canceled; break;
      }
      return new UnityEngine.Touch { fingerId = touch.finger.index, position = touch.screenPosition,
        deltaPosition = touch.delta, phase = phase };
    }

    public static UnityEngine.Touch[] touches
    {
      get
      {
        var result = new UnityEngine.Touch[touchCount];
        for (int i = 0; i < result.Length; i++) result[i] = GetTouch(i);
        return result;
      }
    }
  }
}
