using UnityEngine;
using Ballance2.Services;

/*
* Copyright(c) 2021 imengyu
*
* Module:
* DyamicLoadAudioResource.cs
* 
* Purpose:
* Load audio resources dynamically through GameSoundManager.
*
* Author:
* mengyu
*/

namespace Ballance2.Sys.Tools
{
  /// <summary>
  /// Load audio resources dynamically through GameSoundManager.
  /// </summary>
  [RequireComponent(typeof(AudioSource))]
  public class DyamicLoadAudioResource : MonoBehaviour
  {
    [Tooltip("Sound resource path in the format accepted by GameSoundManager.")]
    public string AssetString = "";
    [Tooltip("Sound category.")]
    public GameSoundType SoundType = GameSoundType.Normal;

    private AudioSource AudioSource;

    private void Awake()
    {
      AudioSource = GetComponent<AudioSource>();
      if (GameManager.Instance == null)
      {
        UnityEngine.Debug.LogError("LoadAudioResource failed: GameSystem not init!");
        return;
      }

      var SoundManager = GameManager.GetSystemService<GameSoundManager>();
      AudioSource.clip = SoundManager.LoadAudioResource(AssetString);
      SoundManager.RegisterSoundPlayer(SoundType, AudioSource);
    }
  }
}