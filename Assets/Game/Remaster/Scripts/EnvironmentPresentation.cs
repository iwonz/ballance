using System;
using BallancePhysics.Api;
using System.Collections.Generic;
using Ballance2.Game.GamePlay;
using Ballance2.Services;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ballance2.Game.Remaster
{
  internal sealed class EnvironmentPresentation : IDisposable
  {
    private readonly EnvironmentController owner;
    private readonly bool preview;
    private readonly Skybox skybox;
    private readonly Material originalSky, sky, rainMaterial, snowMaterial;
    private readonly Camera camera;
    private readonly DepthTextureMode depthMode;
    private readonly Color lightColor, ambientSky, ambientEquator, ambientGround, fogColor;
    private readonly float intensity, fogStart, fogEnd;
    private readonly AmbientMode ambientMode;
    private readonly bool fog;
    private readonly FogMode fogMode;
    private readonly LightShadows shadows;
    private readonly ParticleSystem rain, snow;
    private readonly EnvironmentVegetation vegetation;
    private readonly EnvironmentAudio audio;
    private readonly List<Renderer> surfaces = new List<Renderer>();
    private readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
    private SurfaceState applied = new SurfaceState { wetness = -1 };

    public EnvironmentPresentation(EnvironmentController owner, bool preview)
    {
      this.owner = owner; this.preview = preview;
      camera = preview ? GamePreviewManager.Instance.GamePreviewCamera : GamePlayManager.Instance.CamManager.GetComponent<Camera>();
      depthMode = camera.depthTextureMode; camera.depthTextureMode |= DepthTextureMode.Depth;
      skybox = preview ? GamePreviewManager.Instance.GamePreviewCameraSkyBox : GamePlayManager.Instance.CamManager._SkyBox;
      originalSky = skybox.material;
      sky = new Material(originalSky) { shader = Resources.Load<Shader>("Remaster/LayeredSky") };
      if (sky.GetTexture("_UpTex") == null) sky.SetTexture("_UpTex", sky.GetTexture("_FrontTex"));
      sky.SetTexture("_CloudVolume", Resources.Load<Texture3D>("Remaster/CloudDensity"));
      sky.SetTexture("_CloudGroups", Resources.Load<Texture2D>("Remaster/CloudGroups"));
      skybox.material = sky;
      var light = GameManager.GameLight;
      lightColor = light.color; intensity = light.intensity; shadows = light.shadows;
      ambientMode = RenderSettings.ambientMode; ambientSky = RenderSettings.ambientSkyColor;
      ambientEquator = RenderSettings.ambientEquatorColor; ambientGround = RenderSettings.ambientGroundColor;
      fog = RenderSettings.fog; fogMode = RenderSettings.fogMode; fogColor = RenderSettings.fogColor;
      fogStart = RenderSettings.fogStartDistance; fogEnd = RenderSettings.fogEndDistance;
      foreach (var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        foreach (var material in renderer.sharedMaterials)
          if (material != null && material.shader.name == "Ballance/Remaster/Surface") { surfaces.Add(renderer); break; }
      float lowest = float.PositiveInfinity;
      var floors = new HashSet<string>(owner.Profile.coverings, StringComparer.Ordinal);
      foreach (var renderer in surfaces)
        if (floors.Contains(renderer.name)) lowest = Mathf.Min(lowest,renderer.bounds.min.y);
      // The lower volume extends up to 408 units above its base.
      sky.SetFloat("_CloudBase", float.IsInfinity(lowest) ? -600 : lowest-600);
      rainMaterial = new Material(Resources.Load<Shader>("Remaster/Precipitation"));
      snowMaterial = new Material(rainMaterial); rainMaterial.SetFloat("_Streak",1);
      rain = MakeParticles("Rain", true); snow = MakeParticles("Snow", false);
      vegetation = new EnvironmentVegetation(owner.Profile);
      if (!preview) audio = new EnvironmentAudio();
      Apply(false);
    }

    private ParticleSystem MakeParticles(string name, bool streak)
    {
      var go = new GameObject(name);
      go.transform.SetParent(owner.transform, false);
      var system = go.AddComponent<ParticleSystem>();
      system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
      var main = system.main;
      main.playOnAwake = false; main.loop = true; main.maxParticles = 2400;
      main.simulationSpace = ParticleSystemSimulationSpace.World;
      main.startLifetime = streak ? 1.9f : 12;
      main.startSpeed = 0; main.startSize = new ParticleSystem.MinMaxCurve(streak ? .025f : .08f, streak ? .055f : .20f);
      main.startRotation = new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);
      main.startColor = streak ? new Color(0.75f, 0.86f, 0.92f, 0.35f) : new Color(0.94f, 0.97f, 1, 0.8f);
      var shape = system.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(70, 1, 70);
      var velocity = system.velocityOverLifetime; velocity.enabled = true;
      velocity.space = ParticleSystemSimulationSpace.World; velocity.y = streak ? -42 : -3;
      velocity.x = streak ? 1.2f : 0.35f;
      var emission = system.emission; emission.rateOverTime = 0;
      var color = system.colorOverLifetime; color.enabled = true;
      var gradient = new Gradient();
      gradient.SetKeys(new[] { new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1) },
        new[] { new GradientAlphaKey(0,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(1,.75f),new GradientAlphaKey(0,1) });
      color.color = gradient;
      if (!streak) {
        var noise = system.noise; noise.enabled = true; noise.separateAxes = true;
        noise.strengthX = .8f; noise.strengthY = .15f; noise.strengthZ = .8f;
        noise.frequency = .25f; noise.scrollSpeed = .35f; noise.octaveCount = 2;
        noise.quality = ParticleSystemNoiseQuality.Low;
      }
      var renderer = system.GetComponent<ParticleSystemRenderer>();
      renderer.sharedMaterial = streak ? rainMaterial : snowMaterial;
      renderer.renderMode = streak ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
      renderer.velocityScale = streak ? 0.035f : 0;
      renderer.lengthScale = streak ? 2 : 1;
      return system;
    }

    public void Apply(bool running)
    {
      var state = owner.Run.Current;
      vegetation.Apply(state.surface.vegetation, owner.Run.Time, state.surface.wetness);
      Shader.SetGlobalFloat("_RemasterWeatherTime",owner.Run.Time);
      Shader.SetGlobalFloat("_RemasterRain",state.rain);
      audio?.Apply(state.rain, owner.Run.Time, running);
      sky.SetVector("_Weather", new Vector4(state.cloud, state.fog, state.warmth, state.rain));
      sky.SetVector("_ThemeSky", state.skyTheme);
      sky.SetFloat("_WeatherTime", owner.Run.Time);
      bool clouds = GameSettingsManager.GetSettings(GamePackageManager.SYSTEM_PACKAGE_NAME).GetBool(SettingConstants.SettingsVideoCloud);
      sky.SetFloat("_CloudQuality", !clouds ? 0 : Mathf.Clamp(RemasterPreferences.GetInt("graphics.quality", 1),0,2)+1);
      sky.SetVector("_CloudSun", -GameManager.GameLight.transform.forward);
      var warm = new Color(1, 0.89f, 0.72f);
      var cold = new Color(0.72f, 0.85f, 1);
      Color tint = state.warmth >= 0 ? Color.Lerp(Color.white, warm, state.warmth) : Color.Lerp(Color.white, cold, -state.warmth);
      GameManager.GameLight.color = lightColor * tint;
      GameManager.GameLight.intensity = intensity * Mathf.Lerp(.9f, .64f, state.rain);
      GameManager.GameLight.shadows = (LightShadows)Mathf.Clamp(RemasterPreferences.GetInt("graphics.shadows", 1), 0, 2);
      RenderSettings.ambientMode = AmbientMode.Trilight;
      RenderSettings.ambientSkyColor = new Color(0.34f, 0.39f, 0.46f) * tint;
      RenderSettings.ambientEquatorColor = new Color(0.24f, 0.28f, 0.31f) * tint;
      RenderSettings.ambientGroundColor = new Color(0.16f, 0.17f, 0.17f);
      RenderSettings.fog = state.fog > 0.001f;
      RenderSettings.fogMode = FogMode.Linear;
      RenderSettings.fogColor = new Color(0.64f, 0.72f, 0.75f) * tint;
      RenderSettings.fogEndDistance = Mathf.Lerp(1600, owner.Profile.fogDistance, state.fog);
      RenderSettings.fogStartDistance = RenderSettings.fogEndDistance * 0.45f;
      if (applied.wetness != state.surface.wetness || applied.snow != state.surface.snow || applied.ice != state.surface.ice ||
          applied.sand != state.surface.sand || applied.vegetation != state.surface.vegetation) {
        foreach (var renderer in surfaces) if (renderer != null) {
          renderer.GetPropertyBlock(properties);
          properties.SetVector("_Surface", new Vector4(state.surface.wetness, state.surface.snow, state.surface.ice, state.surface.sand));
          properties.SetFloat("_Vegetation", state.surface.vegetation);
          renderer.SetPropertyBlock(properties);
        }
        applied = state.surface;
      }
      var ball = GamePlayManager.Instance.BallManager.CurrentBall;
      Vector3 focus = !preview && ball != null ? ball.transform.position : camera.transform.position;
      rain.transform.position = snow.transform.position = focus + Vector3.up * 24;
      int effects = Mathf.Clamp(RemasterPreferences.GetInt("graphics.effects", 1), 0, 2);
      UpdateParticles(rain, state.rain * effects * 480, running);
      UpdateParticles(snow, state.snowfall * effects * 180, running);
    }

    private static void UpdateParticles(ParticleSystem system, float rate, bool running)
    {
      var emission = system.emission; emission.rateOverTime = rate;
      if (running && !system.isPlaying) system.Play();
      else if (!running && !system.isPaused) system.Pause();
    }

    public void Dispose()
    {
      if (skybox != null) skybox.material = originalSky;
      if (camera != null) camera.depthTextureMode = depthMode;
      Shader.SetGlobalFloat("_RemasterRain",0);
      audio?.Dispose();
      vegetation.Dispose();
      UnityEngine.Object.Destroy(sky);
      UnityEngine.Object.Destroy(rain.gameObject); UnityEngine.Object.Destroy(snow.gameObject);
      UnityEngine.Object.Destroy(rainMaterial); UnityEngine.Object.Destroy(snowMaterial);
      if (GameManager.GameLight != null) {
        GameManager.GameLight.color = lightColor; GameManager.GameLight.intensity = intensity; GameManager.GameLight.shadows = shadows;
      }
      RenderSettings.ambientMode = ambientMode; RenderSettings.ambientSkyColor = ambientSky;
      RenderSettings.ambientEquatorColor = ambientEquator; RenderSettings.ambientGroundColor = ambientGround;
      RenderSettings.fog = fog; RenderSettings.fogMode = fogMode; RenderSettings.fogColor = fogColor;
      RenderSettings.fogStartDistance = fogStart; RenderSettings.fogEndDistance = fogEnd;
      foreach (var renderer in surfaces) if (renderer != null) {
        renderer.GetPropertyBlock(properties);
        properties.SetVector("_Surface", Vector4.zero); properties.SetFloat("_Vegetation", 0);
        renderer.SetPropertyBlock(properties);
      }
    }
  }
}
