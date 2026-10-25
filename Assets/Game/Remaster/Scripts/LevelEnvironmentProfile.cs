using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Ballance2.Game.Remaster
{
  [Serializable]
  public sealed class LevelEnvironmentProfile
  {
    public string level;
    public string[] sectors;
    public float intensity = 1;
    public float fogDistance = 140;
    public float coverage = 0.25f;
    public string[] coverings = Array.Empty<string>();
    public string[] mechanisms = Array.Empty<string>();
    public Vector3 wind;

    public EnvironmentState Resolve(EnvironmentPreset preset, int sector)
    {
      EnvironmentState state;
      if (preset == EnvironmentPreset.Weather)
      {
        string condition = sectors[Mathf.Clamp(sector - 1, 0, sectors.Length - 1)];
        switch (condition)
        {
          case "clear": state = EnvironmentState.For(EnvironmentPreset.Standard); break;
          case "sun": state = EnvironmentState.For(EnvironmentPreset.California); break;
          case "rain": state = EnvironmentState.For(EnvironmentPreset.London); state.fog = 0.15f; break;
          case "fog": state = new EnvironmentState { fog = 0.6f, cloud = 0.65f }; break;
          case "snow": state = EnvironmentState.For(EnvironmentPreset.Moscow); break;
          default: throw new InvalidOperationException("Unknown authored weather: " + condition);
        }
        state = EnvironmentState.Lerp(new EnvironmentState(), state, intensity);
      }
      else state = EnvironmentState.For(preset);
      if (preset != EnvironmentPreset.Standard) state.wind = wind;
      return state;
    }

    public void Validate()
    {
      if (string.IsNullOrEmpty(level) || sectors == null || sectors.Length == 0 || sectors.Length > 32 ||
          !FiniteRange(intensity, 0, 1) || !FiniteRange(coverage, 0, 0.5f) || !FiniteRange(fogDistance, 80, 1000))
        throw new InvalidOperationException("Invalid environment profile: " + level);
      if (coverings == null || mechanisms == null || coverings.Length > 512 || mechanisms.Length > 512 || !FiniteRange(wind.sqrMagnitude, 0, 4))
        throw new InvalidOperationException("Invalid surface placement or wind: " + level);
      var objects = new HashSet<string>(StringComparer.Ordinal);
      foreach (string name in coverings)
        if (string.IsNullOrWhiteSpace(name) || !objects.Add(name))
          throw new InvalidOperationException("Invalid covering object: " + level);
      foreach (string name in mechanisms)
        if (string.IsNullOrWhiteSpace(name) || !objects.Add(name))
          throw new InvalidOperationException("Invalid covering mechanism: " + level);
      for (int sector = 1; sector <= sectors.Length; sector++) Resolve(EnvironmentPreset.Weather, sector);
    }

    private static bool FiniteRange(float value, float min, float max) => !float.IsNaN(value)
      && !float.IsInfinity(value) && value >= min && value <= max;

    public static Dictionary<string, LevelEnvironmentProfile> Parse(string json)
    {
      var profiles = JsonConvert.DeserializeObject<LevelEnvironmentProfile[]>(json);
      if (profiles == null) throw new InvalidOperationException("Missing environment profiles");
      var result = new Dictionary<string, LevelEnvironmentProfile>(StringComparer.Ordinal);
      foreach (var profile in profiles)
      {
        if (profile == null) throw new InvalidOperationException("Null environment profile");
        profile.Validate();
        if (result.ContainsKey(profile.level)) throw new InvalidOperationException("Duplicate environment profile: " + profile.level);
        result.Add(profile.level, profile);
      }
      return result;
    }
  }
}
