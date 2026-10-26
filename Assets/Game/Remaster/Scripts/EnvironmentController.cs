using System;
using Ballance2.Game.GamePlay;
using Ballance2.Game.LevelBuilder;
using Ballance2.Res;
using Ballance2.Utils;
using BallancePhysics.Wapper;
using BallancePhysics;
using BallancePhysics.Api;
using UnityEngine;

namespace Ballance2.Game.Remaster
{
  public sealed class EnvironmentController : MonoBehaviour
  {
    public static EnvironmentController Active { get; private set; }
    public EnvironmentRun Run { get; private set; }
    public LevelEnvironmentProfile Profile { get; private set; }
    private PhysicsEnvironment world;
    private bool preview;
    private EnvironmentPresentation presentation;
    private EnvironmentSurfaces surfaces;

    public static void Attach(GameObject root, LevelAssets asset, string level, bool preview)
    {
      var controller = root.AddComponent<EnvironmentController>();
      Active = controller;
      controller.preview = preview;
      var profiles = LevelEnvironmentProfile.Parse(Resources.Load<TextAsset>("Remaster/EnvironmentProfiles").text);
      string path = asset.Path.Replace('\\', '/');
      string expected = asset.LoadInEditor ? GamePathManager.DEBUG_LEVEL_FOLDER + "/" + level
        : PathUtils.ToRequestUrl(GamePathManager.GetLevelRealPath(level, false));
      // A user level with the same JSON name must not inherit a built-in weather profile.
      if (string.Equals(path, expected, StringComparison.OrdinalIgnoreCase) && profiles.TryGetValue(level, out var profile))
        controller.Profile = profile;
      controller.Run = new EnvironmentRun(controller.Profile, RemasterPreferences.Preset);
      if (!preview) {
        controller.world = GamePlayManager.Instance.GamePhysicsWorld;
        controller.world.BeforeSimulation += controller.Advance;
      }
      if (controller.Profile != null) {
        var builder = LevelBuilder.LevelBuilder.Instance;
        if (builder.CurrentLevelSkyLayer != null) {
          Destroy(builder.CurrentLevelSkyLayer);
          builder.CurrentLevelSkyLayer = null;
        }
        controller.presentation = new EnvironmentPresentation(controller, preview);
        controller.surfaces = new EnvironmentSurfaces(controller.Profile, preview);
      }
    }

    public static void Select(EnvironmentPreset preset)
    {
      RemasterPreferences.Preset = preset;
      if (Active != null) {
        Active.Run.Select(preset);
      }
    }

    private void Advance(float seconds)
    {
      Run.Advance(seconds);
      var state = Run.Current.surface;
      var wind = Run.Current.wind;
      if (PhysicsApi.API.environment_set_surface_state(world.Handle, ref state, wind.x, wind.y, wind.z) == 0)
        throw new InvalidOperationException("Invalid authored physics environment");
    }
    private void Update()
    {
      bool running = preview ? GamePreviewManager.Instance.IsPlaying : world != null && world.Simulate;
      if (preview && running) Run.Advance(Time.deltaTime);
      presentation?.Apply(running);
      surfaces?.Apply();

    }

    public void Release()
    {
      if (world != null) world.BeforeSimulation -= Advance;
      if (world != null && world.Handle != IntPtr.Zero) {
        var dry = new SurfaceState();
        PhysicsApi.API.environment_set_surface_state(world.Handle, ref dry, 0, 0, 0);
      }
      world = null;
      surfaces?.Dispose(); surfaces = null;
      presentation?.Dispose();
      presentation = null;
      if (Active == this) Active = null;
      enabled = false;
    }

    private void OnDestroy() => Release();
  }
}
