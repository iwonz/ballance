using System;
using Ballance2.Menu.Touch;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Ballance2.Editor
{
  public static class MobileInputChecks
  {
    public static void Run()
    {
      Require(SimpleTouchController.Normalize(new Vector2(1, 1), 100) == Vector2.zero, "Joystick neutral dead zone");
      Require(SimpleTouchController.Normalize(new Vector2(300, 400), 100).magnitude <= 1.0001f, "Joystick diagonal bound");
      Require(MobileTilt.Steering(new Vector2(.2f, -.4f), new Vector2(.2f, -.4f)) == Vector2.zero, "Tilt calibration");
      Require(MobileTilt.Steering(new Vector2(.21f, -.4f), new Vector2(.2f, -.4f)) == Vector2.zero, "Tilt jitter dead zone");
      Require(MobileTilt.Steering(Vector2.one, Vector2.zero).magnitude <= 1.0001f, "Tilt diagonal bound");
      Require(MobileTilt.Steering(Vector2.left, Vector2.zero).x == -1, "Tilt direction");
      Require(MobileTilt.Steering(new Vector2(float.NaN, 0), Vector2.zero) == Vector2.zero, "Invalid tilt sample is neutral");
      Require(MobileTilt.Steering(new Vector2(float.PositiveInfinity, 0), Vector2.zero) == Vector2.zero, "Infinite tilt sample is neutral");
      var events = new GameObject("Input check events", typeof(EventSystem));
      var go = new GameObject("Input check joystick", typeof(RectTransform));
      try {
        ((RectTransform)go.transform).sizeDelta = new Vector2(200, 200);
        var joystick = go.AddComponent<SimpleTouchController>();
        var first = new PointerEventData(events.GetComponent<EventSystem>()) { pointerId = 10, position = new Vector2(70, 0) };
        var second = new PointerEventData(events.GetComponent<EventSystem>()) { pointerId = 20, position = new Vector2(-70, 0) };
        joystick.OnPointerDown(first);
        Require(joystick.IsHeld && joystick.GetTouchPosition.x > .9f, "Joystick starts without a drag threshold");
        joystick.OnPointerDown(second); joystick.OnDrag(second); joystick.OnPointerUp(second);
        Require(joystick.IsHeld && joystick.GetTouchPosition.x > .9f, "Second finger cannot steal or release joystick");
        joystick.OnPointerUp(first);
        Require(!joystick.IsHeld && joystick.GetTouchPosition == Vector2.zero, "Owner release resets joystick");
        joystick.OnPointerDown(first);
        // Edit-mode checks invoke the player lifecycle message explicitly.
        joystick.SendMessage("OnDisable");
        Require(!joystick.IsHeld && joystick.GetTouchPosition == Vector2.zero, "Disabled joystick resets");
      } finally { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(events); }
      Debug.Log("Mobile input checks passed: pointer ownership, immediate touch, cancellation, tilt calibration and bounded analog input.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
  }
}
