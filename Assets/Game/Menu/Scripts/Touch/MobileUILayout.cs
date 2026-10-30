using UnityEngine;
using UnityEngine.UI;

namespace Ballance2.Menu.Touch
{
  public static class MobileUILayout
  {
#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")] private static extern float BallanceCanvasCSSHeight();
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
    private static float density;
    private static float AndroidDensity()
    {
      if (density > 0) return density;
      using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
      using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
      using (var resources = activity.Call<AndroidJavaObject>("getResources"))
      using (var metrics = resources.Call<AndroidJavaObject>("getDisplayMetrics"))
        density = metrics.Get<float>("density");
      return Mathf.Max(1, density);
    }
#endif
    public static float Unit(Transform target)
    {
      var canvas = target.GetComponentInParent<Canvas>();
      float pixels = 1;
#if UNITY_WEBGL && !UNITY_EDITOR
      pixels = Screen.height / Mathf.Max(1, BallanceCanvasCSSHeight());
#elif UNITY_ANDROID && !UNITY_EDITOR
      pixels = AndroidDensity();
#endif
      return pixels / (canvas != null ? canvas.scaleFactor : 1);
    }

    public static void FitMenuHost(Transform content)
    {
      var page = content.GetComponentInParent<Ballance2.UI.Core.GameUIPage>();
      if (page == null || page.ContentHost == null) return;
      var host = page.ContentHost;
      SafeArea(host);
      float unit = Unit(content);
      float inset = Mathf.Max(12 * unit, (host.rect.width - 600 * unit) * .5f);
      host.offsetMin = new Vector2(inset, 12 * unit);
      host.offsetMax = new Vector2(-inset, -12 * unit);
    }

    public static void FitButtonLabel(Text label, float unit)
    {
      label.rectTransform.anchorMin = Vector2.zero;
      label.rectTransform.anchorMax = Vector2.one;
      label.rectTransform.offsetMin = new Vector2(24, 2) * unit;
      label.rectTransform.offsetMax = new Vector2(-8, -2) * unit;
      label.horizontalOverflow = HorizontalWrapMode.Wrap;
    }

    public static void SafeArea(RectTransform rect)
    {
      var safe = Screen.safeArea;
      rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
      rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
      rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
  }
}
