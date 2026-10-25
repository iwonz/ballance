using System;
using Ballance2.Services;
using UnityEngine;

namespace Ballance2.Game.Remaster
{
  public sealed class EnvironmentAudio : IDisposable
  {
    private readonly AudioClip clip;
    private readonly AudioSource source;
    private float previousTime;

    public EnvironmentAudio()
    {
      var samples = CreateRainSamples();
      clip = AudioClip.Create("Remaster rain", samples.Length / 2, 2, 22050, false);
      clip.SetData(samples, 0);
      source = GameSoundManager.Instance.RegisterSoundPlayer(GameSoundType.ModulEffect, clip, false, true, "Remaster rain");
      source.loop = true; source.spatialBlend = 0; source.volume = 0;
    }

    public static float[] CreateRainSamples()
    {
      const int frames = 22050 * 8;
      var samples = new float[frames * 2];
      var random = new System.Random(4107);
      for (int channel = 0; channel < 2; channel++) {
        float low = 0;
        for (int frame = 0; frame < frames; frame++) {
          float noise = (float)random.NextDouble() * 2 - 1;
          low += (noise - low) * .06f;
          float swell = .9f + .1f * Mathf.Sin(frame * (2 * Mathf.PI / frames));
          samples[frame * 2 + channel] = (noise * .28f + low * .5f) * swell;
        }
      }
      return samples;
    }

    public void Apply(float rain, float time, bool running)
    {
      source.volume = rain * .18f;
      if (time < previousTime || time - previousTime > 1) source.time = Mathf.Repeat(time, clip.length);
      previousTime = time;
      if (running && rain > .001f) {
        if (!source.isPlaying) { source.UnPause(); if (!source.isPlaying) source.Play(); }
      } else if (source.isPlaying) source.Pause();
    }

    public void Dispose()
    {
      if (GameSoundManager.Instance != null) GameSoundManager.Instance.DestroySoundPlayer(source);
      UnityEngine.Object.Destroy(clip);
    }
  }
}
