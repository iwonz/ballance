using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using Ballance2.Services;

namespace Ballance2.Game.Remaster
{
  public sealed class RemasterGraphics : MonoBehaviour
  {
    public static Vector2Int ActualResolution { get; private set; }
    private RenderTexture scene;
    private RawImage output;
    private GameObject display;
    private Camera presentation;
    private CanvasScaler ui;
    private Vector2 originalReference;
    private ReflectionProbe reflection;
    private Material originalSky, capturedSky, visibleSky;
    private Vector4 capturedWeather, capturedTheme;
    private int reflectionRender = -1;
    private float reflectionTime;
    private readonly HashSet<Camera> redirected = new HashSet<Camera>();
    private static readonly int[] scales = { 50, 67, 75, 100 };
    private static readonly float[] uiScales = { 0.85f, 1f, 1.15f, 1.3f };

    public static Vector2Int Calculate(int width, int height, int resolution, int scale)
    {
      if (width < 1 || height < 1 || resolution < 0 || resolution > 3 || scale < 0 || scale > 3)
        throw new ArgumentOutOfRangeException("Invalid render dimensions or setting");
      int capHeight = resolution <= 1 ? 1080 : resolution == 2 ? 1440 : 2160;
      float fit = Mathf.Min((capHeight * 16f / 9) / width, (float)capHeight / height);
      if (resolution == 0) fit = Mathf.Min(fit, 1);
      fit *= scales[scale] / 100f;
      return new Vector2Int(Mathf.Max(1, Mathf.RoundToInt(width * fit)), Mathf.Max(1, Mathf.RoundToInt(height * fit)));
    }

    public static void ApplyQuality()
    {
      int quality = Mathf.Clamp(RemasterPreferences.GetInt("graphics.quality", 1), 0, 2);
      QualitySettings.SetQualityLevel(new[] { 1, 2, 3 }[quality], true);
      int shadows = Mathf.Clamp(RemasterPreferences.GetInt("graphics.shadows", 1), 0, 2);
      QualitySettings.shadows = shadows == 0 ? ShadowQuality.Disable : shadows == 1 ? ShadowQuality.HardOnly : ShadowQuality.All;
      QualitySettings.shadowDistance = new[] { 0f, 100f, 180f }[shadows];
      QualitySettings.antiAliasing = new[] { 0, 2, 4 }[Mathf.Clamp(RemasterPreferences.GetInt("graphics.aa", 1), 0, 2)];
      QualitySettings.realtimeReflectionProbes = true;
      if (GameManager.Instance != null && GameManager.Instance.GameSettings != null)
        QualitySettings.vSyncCount = GameManager.Instance.GameSettings.GetInt(SettingConstants.SettingsVideoVsync, 0);
    }

    private void OnEnable()
    {
      display = new GameObject("RemasterSceneDisplay", typeof(RectTransform), typeof(Canvas));
      display.transform.SetParent(transform, false);
      // Metal still needs a screen-targeted camera when the scene cameras render to textures.
      presentation = display.AddComponent<Camera>();
      presentation.depth = 1000;
      presentation.cullingMask = 0;
      presentation.clearFlags = CameraClearFlags.SolidColor;
      presentation.backgroundColor = Color.black;
      var canvas = display.GetComponent<Canvas>();
      canvas.renderMode = RenderMode.ScreenSpaceOverlay;
      canvas.sortingOrder = -1000;
      var image = new GameObject("Scene", typeof(RectTransform), typeof(RawImage));
      image.transform.SetParent(display.transform, false);
      output = image.GetComponent<RawImage>();
      output.raycastTarget = false;
      var rect = output.rectTransform;
      rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
      rect.offsetMin = rect.offsetMax = Vector2.zero;
      originalSky = RenderSettings.skybox;
      var probe = new GameObject("Remaster sky reflection");
      probe.transform.SetParent(transform, false);
      reflection = probe.AddComponent<ReflectionProbe>();
      reflection.mode = ReflectionProbeMode.Realtime;
      reflection.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
      reflection.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
      reflection.cullingMask = 0;
      reflection.size = Vector3.one * 100000;
      reflection.boxProjection = false;
      reflection.clearFlags = ReflectionProbeClearFlags.Skybox;
      ApplyQuality();
    }

    private void ConfigureCamera(Camera camera)
    {
      if (camera == presentation || camera.cameraType != CameraType.Game || camera.targetDisplay != 0 ||
          (camera.targetTexture != null && !redirected.Contains(camera))) return;
      var skybox = camera.GetComponent<Skybox>();
      if (skybox != null && skybox.enabled && skybox.material != null) {
        visibleSky = skybox.material;
        RenderSettings.skybox = visibleSky;
        if (visibleSky.shader.name == "Ballance/Remaster/LayeredSky" && camera.GetComponent<RemasterSkyRenderer>() == null)
          camera.gameObject.AddComponent<RemasterSkyRenderer>();
      }
      camera.targetTexture = scene;
      redirected.Add(camera);
    }

    private void PrepareTarget()
    {
      var size = Calculate(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height),
        Mathf.Clamp(RemasterPreferences.GetInt("graphics.resolution"), 0, 3),
        Mathf.Clamp(RemasterPreferences.GetInt("graphics.scale", 3), 0, 3));
      int samples = Mathf.Max(1, QualitySettings.antiAliasing);
      if (scene == null || scene.width != size.x || scene.height != size.y || scene.antiAliasing != samples) {
        var previous = scene;
        scene = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32) {
          name = "Remaster 3D", antiAliasing = samples, filterMode = FilterMode.Bilinear
        };
        scene.Create();
        foreach (var camera in redirected) if (camera != null) camera.targetTexture = scene;
        output.texture = scene;
        ActualResolution = size;
        if (previous != null) { previous.Release(); Destroy(previous); }
      }
    }

    private void LateUpdate()
    {
      // Keep targets bound through Metal's multisample resolve and presentation.
      PrepareTarget();
      foreach (var camera in Camera.allCameras) ConfigureCamera(camera);
      if (visibleSky != null && (reflectionRender < 0 || reflection.IsFinishedRendering(reflectionRender))) {
        int quality = Mathf.Clamp(RemasterPreferences.GetInt("graphics.quality", 1), 0, 2);
        int size = 64 << quality;
        var weather = visibleSky.HasProperty("_Weather") ? visibleSky.GetVector("_Weather") : Vector4.zero;
        var theme = visibleSky.HasProperty("_ThemeSky") ? visibleSky.GetVector("_ThemeSky") : Vector4.zero;
        if (capturedSky != visibleSky || reflection.resolution != size ||
            (((weather - capturedWeather).sqrMagnitude + (theme - capturedTheme).sqrMagnitude > .01f) && Time.unscaledTime - reflectionTime > 2)) {
          reflection.resolution = size;
          capturedSky = visibleSky; capturedWeather = weather; capturedTheme = theme; reflectionTime = Time.unscaledTime;
          reflectionRender = reflection.RenderProbe();
        }
      }
      if (ui == null && GameManager.Instance != null && GameManager.Instance.GameCanvas != null) {
        ui = GameManager.Instance.GameCanvas.GetComponent<CanvasScaler>();
        if (ui != null) originalReference = ui.referenceResolution;
      }
      if (ui != null) {
        float scale = uiScales[Mathf.Clamp(RemasterPreferences.GetInt("graphics.uiScale", 1), 0, 3)];
        ui.referenceResolution = originalReference / scale;
      }
    }

    private void OnDisable()
    {
      foreach (var camera in redirected) if (camera != null) {
        camera.targetTexture = null;
        var sky = camera.GetComponent<RemasterSkyRenderer>();
        if (sky != null) Destroy(sky);
      }
      redirected.Clear();
      if (scene != null) { scene.Release(); Destroy(scene); scene = null; }
      if (display != null) Destroy(display);
      if (reflection != null) Destroy(reflection.gameObject);
      RenderSettings.skybox = originalSky;
    }
  }
}
