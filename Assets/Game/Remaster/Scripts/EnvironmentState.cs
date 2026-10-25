using System;
using BallancePhysics.Api;
using UnityEngine;

namespace Ballance2.Game.Remaster
{
  // Values are persisted; append new presets rather than reordering them.
  public enum EnvironmentPreset { Standard, Weather, Moscow, London, Sahara, HongKong, California }

  [Serializable]
  public struct EnvironmentState
  {
    public SurfaceState surface;
    public float rain, snowfall, fog, cloud, warmth;
    public Vector4 skyTheme;
    public Vector3 wind;

    public static EnvironmentState Lerp(EnvironmentState a, EnvironmentState b, float t) => new EnvironmentState {
      surface = SurfaceState.Lerp(a.surface, b.surface, t),
      rain = Mathf.Lerp(a.rain, b.rain, t), snowfall = Mathf.Lerp(a.snowfall, b.snowfall, t),
      fog = Mathf.Lerp(a.fog, b.fog, t), cloud = Mathf.Lerp(a.cloud, b.cloud, t),
      warmth = Mathf.Lerp(a.warmth, b.warmth, t), wind = Vector3.Lerp(a.wind, b.wind, t),
      skyTheme = Vector4.Lerp(a.skyTheme, b.skyTheme, t)
    };

    public static EnvironmentState For(EnvironmentPreset preset)
    {
      switch (preset)
      {
        case EnvironmentPreset.Standard: return new EnvironmentState();
        case EnvironmentPreset.Moscow: return new EnvironmentState {
          surface = new SurfaceState { snow = 0.8f, ice = 0.7f }, snowfall = 0.55f,
          cloud = 0.35f, fog = 0.12f, warmth = -0.6f, skyTheme = new Vector4(.5f, .66f, .82f, 1)
        };
        case EnvironmentPreset.London: return new EnvironmentState {
          surface = new SurfaceState { wetness = 0.9f }, rain = 0.65f, fog = 0.55f,
          cloud = 0.85f, warmth = -0.25f, skyTheme = new Vector4(.35f, .44f, .52f, 1)
        };
        case EnvironmentPreset.Sahara: return new EnvironmentState {
          surface = new SurfaceState { sand = 0.7f }, fog = 0.18f, cloud = 0.15f, warmth = 0.8f,
          skyTheme = new Vector4(.83f, .58f, .28f, 1)
        };
        case EnvironmentPreset.HongKong: return new EnvironmentState {
          surface = new SurfaceState { wetness = 0.55f, vegetation = 1 },
          fog = 0.22f, cloud = 0.45f, warmth = 0.15f, skyTheme = new Vector4(.48f, .66f, .64f, 1)
        };
        case EnvironmentPreset.California: return new EnvironmentState {
          cloud = 0.2f, warmth = 0.4f, skyTheme = new Vector4(.35f, .65f, .92f, 1)
        };
        default: throw new ArgumentOutOfRangeException(nameof(preset), "Weather requires an authored level profile");
      }
    }
  }

  public sealed class EnvironmentRun
  {
    public const float TransitionSeconds = 8;
    public EnvironmentPreset Preset { get; private set; }
    public int Sector { get; private set; }
    public EnvironmentState Current { get; private set; }
    public float Time { get; private set; }
    private EnvironmentState from, target;
    private float elapsed;
    private readonly LevelEnvironmentProfile profile;
    private EnvironmentState checkpointFrom, checkpointTarget, checkpointCurrent;
    private float checkpointElapsed;
    private float checkpointTime;

    public EnvironmentRun(LevelEnvironmentProfile profile, EnvironmentPreset preset)
    {
      this.profile = profile;
      Restart(preset);
    }

    public void Restart(EnvironmentPreset preset)
    {
      Validate(preset);
      Preset = preset;
      Sector = 1;
      Time = 0;
      Current = from = target = Resolve();
      elapsed = TransitionSeconds;
      CaptureCheckpoint();
    }

    public void Select(EnvironmentPreset preset)
    {
      Validate(preset);
      if (preset == Preset) return;
      Preset = preset;
      BeginTransition();
      CaptureCheckpoint();
    }

    public void EnterSector(int sector)
    {
      if (sector < 1) throw new ArgumentOutOfRangeException(nameof(sector));
      if (Sector == sector) return;
      Sector = sector;
      if (Preset == EnvironmentPreset.Weather) BeginTransition();
      CaptureCheckpoint();
    }

    public void Advance(float seconds)
    {
      if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0)
        throw new ArgumentOutOfRangeException(nameof(seconds));
      elapsed = Mathf.Min(elapsed + seconds, TransitionSeconds);
      Time += seconds;
      float t = elapsed / TransitionSeconds;
      Current = EnvironmentState.Lerp(from, target, t * t * (3 - 2 * t));
    }

    public void RestoreCheckpoint()
    {
      from = checkpointFrom; target = checkpointTarget;
      Current = checkpointCurrent; elapsed = checkpointElapsed;
      Time = checkpointTime;
    }

    private void CaptureCheckpoint()
    {
      checkpointFrom = from; checkpointTarget = target;
      checkpointCurrent = Current; checkpointElapsed = elapsed;
      checkpointTime = Time;
    }

    private void BeginTransition()
    {
      from = Current; target = Resolve(); elapsed = 0;
    }

    private EnvironmentState Resolve() => profile == null ? EnvironmentState.For(EnvironmentPreset.Standard)
      : profile.Resolve(Preset, Sector);

    private static void Validate(EnvironmentPreset preset)
    {
      if (!Enum.IsDefined(typeof(EnvironmentPreset), preset)) throw new ArgumentOutOfRangeException(nameof(preset));
    }
  }
}
