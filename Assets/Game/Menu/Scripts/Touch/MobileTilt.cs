using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ballance2.Menu.Touch
{
  public static class MobileTilt
  {
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void BallanceTiltStart(int permission, string allow, string cancel);
    [DllImport("__Internal")] private static extern float BallanceTiltRead(int axis);
    [DllImport("__Internal")] private static extern void BallanceTiltStop();
#else
    private static Sensor sensor;
    private static bool enabledHere;
    private static double sampleTime;
    private static float receivedAt;
#endif
    private static Vector2 neutral, filtered;
    private static bool calibrated;
    public static void Start(bool requestPermission)
    {
      Calibrate();
#if UNITY_WEBGL && !UNITY_EDITOR
      BallanceTiltStart(requestPermission ? 1 : 0,
        Services.I18N.I18N.Tr("core.ui.MobileSensorAllow"), Services.I18N.I18N.Tr("core.ui.Cancel"));
#else
      sensor = (Sensor)GravitySensor.current ?? Accelerometer.current;
      if (sensor != null) {
        enabledHere |= !sensor.enabled;
        if (enabledHere) InputSystem.EnableDevice(sensor);
        try { sensor.samplingFrequency = 30; }
        catch (System.NotSupportedException) { /* Keep the device default when frequency control is unavailable. */ }
        sampleTime = sensor.lastUpdateTime;
        receivedAt = float.NegativeInfinity;
      }
#endif
    }
    public static void Stop()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
      BallanceTiltStop();
#else
      if (sensor != null && enabledHere) InputSystem.DisableDevice(sensor);
      sensor = null; enabledHere = false;
#endif
      Calibrate();
    }
    public static void Calibrate() { calibrated = false; filtered = Vector2.zero; }
    public static Vector2 Steering(Vector2 sample, Vector2 center)
    {
      var delta = (sample - center) / .45f;
      float length = delta.magnitude;
      if (float.IsNaN(length) || float.IsInfinity(length)) return Vector2.zero;
      return length <= .08f ? Vector2.zero : delta.normalized * Mathf.Clamp01((length - .08f) / .92f);
    }
    public static bool Read(out Vector2 value)
    {
      value = Vector2.zero;
      Vector3 gravity;
#if UNITY_WEBGL && !UNITY_EDITOR
      if (BallanceTiltRead(3) < 1) { Calibrate(); return false; }
      gravity = new Vector3(BallanceTiltRead(0), BallanceTiltRead(1), BallanceTiltRead(2));
#else
      if (sensor == null || !sensor.enabled) return false;
      if (sensor.lastUpdateTime != sampleTime) { sampleTime = sensor.lastUpdateTime; receivedAt = Time.realtimeSinceStartup; }
      if (Time.realtimeSinceStartup - receivedAt > 1) { Calibrate(); return false; }
      gravity = sensor is GravitySensor g ? g.gravity.ReadValue() : ((Accelerometer)sensor).acceleration.ReadValue();
#endif
      float magnitude = gravity.sqrMagnitude;
      if (float.IsNaN(magnitude) || float.IsInfinity(magnitude) || magnitude < .01f) { Calibrate(); return false; }
      var sample = (Vector2)gravity.normalized;
      if (!calibrated) { neutral = sample; calibrated = true; }
      filtered = Vector2.Lerp(filtered, Steering(sample, neutral), 1 - Mathf.Exp(-12 * Time.unscaledDeltaTime));
      value = filtered;
      return true;
    }
  }
}
