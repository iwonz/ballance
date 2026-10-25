using Ballance2.Base;
using Ballance2.Config;
using Ballance2.Game;
using Ballance2.Package;
using Ballance2.Res;
using Ballance2.Services.Debug;
using Ballance2.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* GameSoundManager.cs
* 
* Purpose:
* Manage shared audio functionality.
*
* Author:
* mengyu
*/

namespace Ballance2.Services
{
  /// <summary>
  /// Sound manager.
  /// </summary>
  public class GameSoundManager : GameService<GameSoundManager>
  {
    #region Initialization and definitions

    public const string TAG = "GameSoundManager";

    public GameSoundManager() : base(TAG) { }

    private GameSettingsActuator GameSettings = null;
    private GamePackageManager GamePackageManager;
    private GameObject GameSoundManagerObject;

    public override bool Initialize()
    {

      audioSourcePrefab = GameStaticResourcesPool.FindStaticPrefabs("AudioSource");
      fastPlayVoices = new Dictionary<string, AudioSource>();

      InitGameAudioMixer();

      GameManager.GameMediator.RegisterEventHandler(GamePackage.GetSystemPackage(), GameEventNames.EVENT_BASE_INIT_FINISHED, TAG, (e, p) =>
          {
            GamePackageManager = GameSystem.GetSystemService<GamePackageManager>();
            GameSoundManagerObject = CloneUtils.CreateEmptyObjectWithParent(GameManager.Instance.gameObject.transform, "GameSoundManager");
            InitCommands();
            return false;
          }
      );

      // Settings update event.
      GameSettings = GameSettingsManager.GetSettings(GamePackageManager.SYSTEM_PACKAGE_NAME);
      GameSettings.RegisterSettingsUpdateCallback(SettingConstants.SettingsVoice, OnVoiceSettingsUpdated);
      GameSettings.RequireSettingsLoad(SettingConstants.SettingsVoice);
      return true;
    }
    
    public override void Destroy()
    {
      if (null != fastPlayVoices)
      {
        foreach (var v in fastPlayVoices)
          DestroySoundPlayer(v.Value);
        fastPlayVoices.Clear();
        fastPlayVoices = null;
      }
      if (null != audios)
      {
        for (int i = audios.Count - 1; i >= 0; i--)
          DestroySoundPlayer(audios[i].Audio);
        audios.Clear();
        audios = null;
      }
    }

    private List<AudioGlobalControl> audios = new List<AudioGlobalControl>();
    private GameObject audioSourcePrefab = null;
    private class AudioGlobalControl
    {
      public AudioSource Audio;
      public GameSoundType Type;
    }

    #endregion

    #region AudioMixer

    /// <summary>
    /// Main game AudioMixer.
    /// </summary>
    public AudioMixer GameMainAudioMixer;
    /// <summary>
    /// UI AudioMixer.
    /// </summary>
    public AudioMixer GameUIAudioMixer;

    private AudioMixerGroup GameUIAudioMixerGroupMaster;

    private AudioMixerGroup GameMainAudioMixerGroupMaster;
    private AudioMixerGroup GameMainAudioMixerGroupBallEffect;
    private AudioMixerGroup GameMainAudioMixerGroupModulEffect;
    private AudioMixerGroup GameMainAudioMixerGroupBackgroundMusic;

    private void InitGameAudioMixer()
    {
      GameMainAudioMixer = GameStaticResourcesPool.FindStaticAssets<AudioMixer>("GameMainAudioMixer");
      GameUIAudioMixer = GameStaticResourcesPool.FindStaticAssets<AudioMixer>("GameUIAudioMixer");

      GameUIAudioMixerGroupMaster = GameUIAudioMixer.FindMatchingGroups("Master")[0];
      GameMainAudioMixerGroupMaster = GameUIAudioMixer.FindMatchingGroups("Master")[0];
      GameMainAudioMixerGroupBallEffect = GameMainAudioMixer.FindMatchingGroups("Master/BallEffect")[0];
      GameMainAudioMixerGroupModulEffect = GameMainAudioMixer.FindMatchingGroups("Master/ModulEffect")[0];
      GameMainAudioMixerGroupBackgroundMusic = GameMainAudioMixer.FindMatchingGroups("Master/BackgroundMusic")[0];
    }

    #endregion

    #region Sound Player

    /// <summary>
    /// Load an audio resource from a package.
    /// </summary>
    /// <param name="assets">Resource path in package:audio-path form.</param>
    /// <returns>AudioClip on success; null on failure.</returns>
    public AudioClip LoadAudioResource(string assets)
    {
      string[] names = assets.Split(':');

      GamePackage package = GamePackageManager.FindPackage(names[0]);
      if (package == null)
      {
        Log.W(TAG, "Cannot load sound {0}: package {1} was not found.", assets, names[0]);
        GameErrorChecker.LastError = GameError.NotRegister;
        return null;
      }
      if (package.Status != GamePackageStatus.LoadSuccess)
      {
        Log.W(TAG, "Cannot load sound {0}: package {1} is not initialized.", assets, package.PackageName);
        GameErrorChecker.LastError = GameError.NotLoad;
        return null;
      }

      AudioClip clip = package.GetAsset<AudioClip>(names[1]);
      if (clip != null) clip.name = PathUtils.GetFileNameWithoutExt(names[1]);
      else
      {
        Log.W(TAG, "Sound {0} was not found in package {1}.", assets, names[0]);
        GameErrorChecker.LastError = GameError.AssetNotFound;
      }

      return clip;
    }
    /// <summary>
    /// Load an audio resource from a package.
    /// </summary>
    /// <param name="package">Owning package.</param>
    /// <param name="assets">Audio path.</param>
    /// <returns>AudioClip on success; null on failure.</returns>
    public AudioClip LoadAudioResource(GamePackage package, string assets)
    {
      if (package == null)
      {
        Log.W(TAG, "Cannot load sound {0}: no package was specified.");
        GameErrorChecker.LastError = GameError.ParamNotProvide;
        return null;
      }
      AudioClip clip = package.GetAsset<AudioClip>(assets);
      if (clip != null) clip.name = PathUtils.GetFileNameWithoutExt(assets);
      else
      {
        Log.W(TAG, "Sound {0} was not found in package {1}.", assets, package.PackageName);
        GameErrorChecker.LastError = GameError.AssetNotFound;
      }

      return clip;
    }
    /// <summary>
    /// Register a sound player and its audio resource.
    /// </summary>
    /// <param name="assets">Audio resource identifier.</param>
    /// <param name="playOnAwake">Play on startup.</param>
    /// <param name="activeStart">Activate the player object on startup.</param>
    /// <param name="name">Player object name.</param>
    /// <returns>AudioSource instance.</returns>
    public AudioSource RegisterSoundPlayer(GameSoundType type, string assets, bool playOnAwake = false, bool activeStart = true, string name = "")
    {
      AudioClip audioClip = null;
      if (!string.IsNullOrEmpty(assets))
      {
        audioClip = LoadAudioResource(assets);
        if (audioClip == null)
          return null;
      }

      AudioSource audioSource = Object.Instantiate(audioSourcePrefab, GameSoundManagerObject.transform).GetComponent<AudioSource>();
      audioSource.clip = audioClip;
      audioSource.playOnAwake = playOnAwake;
      audioSource.gameObject.name = "AudioSource_" + type + "_" + (name == "" ?
          (audioClip != null ? PathUtils.GetFileNameWithoutExt(audioClip.name) : "") :
          name);

      if (!activeStart)
        audioSource.gameObject.SetActive(false);

      RegisterAudioSource(type, audioSource);
      return audioSource;
    }

    /// <summary>
    /// Register a sound player and its audio resource.
    /// </summary>
    /// <param name="audioClip">Audio clip.</param>
    /// <param name="playOnAwake">Play on startup.</param>
    /// <param name="activeStart">Activate the player object on startup.</param>
    /// <param name="name">Player object name.</param>
    /// <returns>AudioSource instance.</returns>
    public AudioSource RegisterSoundPlayer(GameSoundType type, AudioClip audioClip, bool playOnAwake = false, bool activeStart = true, string name = "")
    {
      AudioSource audioSource = Object.Instantiate(audioSourcePrefab, GameSoundManagerObject.transform).GetComponent<AudioSource>();
      audioSource.clip = audioClip;
      audioSource.playOnAwake = playOnAwake;
      audioSource.gameObject.name = "AudioSource_" + type + "_" + (name == "" ?
          (audioClip != null ? PathUtils.GetFileNameWithoutExt(audioClip.name) : "") :
          name);

      if (!activeStart)
        audioSource.gameObject.SetActive(false);

      RegisterAudioSource(type, audioSource);
      return audioSource;
    }

    /// <summary>
    /// Register an existing AudioSource with the sound manager.
    /// </summary>
    /// <param name="type">Sound category.</param>
    /// <param name="audioSource">AudioSource</param>
    /// <returns>The original AudioSource instance.</returns>
    public AudioSource RegisterSoundPlayer(GameSoundType type, AudioSource audioSource)
    {
      if (!IsSoundPlayerRegistered(audioSource))
        RegisterAudioSource(type, audioSource);
      else GameErrorChecker.LastError = GameError.AlreadyRegistered;
      return audioSource;
    }

    /// <summary>
    /// Whether an AudioSource is registered with the sound manager.
    /// </summary>
    /// <param name="audioSource">AudioSource</param>
    /// <returns>True if registered; otherwise false.</returns>
    public bool IsSoundPlayerRegistered(AudioSource audioSource)
    {
      foreach (AudioGlobalControl a in audios)
      {
        if (a.Audio == audioSource)
          return true;
      }
      return false;
    }

    /// <summary>
    /// Destroy an AudioSource.
    /// </summary>
    /// <param name="assets">AudioSource instance to destroy.</param>
    /// <returns>Whether destruction succeeded.</returns>
    public bool DestroySoundPlayer(AudioSource audioSource)
    {
      AudioGlobalControl audioGlobalControl = null;
      if (IsSoundPlayerRegistered(audioSource, out audioGlobalControl))
      {
        audios.Remove(audioGlobalControl);
        if (audioGlobalControl.Audio != null && audioGlobalControl.Audio.gameObject != null)
          Object.Destroy(audioGlobalControl.Audio.gameObject);
        return true;
      }
      else
      {
        GameErrorChecker.LastError = GameError.NotRegister;
        return false;
      }

    }

    private void RegisterAudioSource(GameSoundType type, AudioSource audioSource)
    {
      AudioGlobalControl audioGlobalControl = new AudioGlobalControl();
      audioGlobalControl.Audio = audioSource;
      audioGlobalControl.Type = type;

      switch (type)
      {
        case GameSoundType.Background:
          audioSource.outputAudioMixerGroup = GameMainAudioMixerGroupBackgroundMusic;
          break;
        case GameSoundType.BallEffect:
          audioSource.outputAudioMixerGroup = GameMainAudioMixerGroupBallEffect;
          break;
        case GameSoundType.ModulEffect:
          audioSource.outputAudioMixerGroup = GameMainAudioMixerGroupModulEffect;
          break;
        case GameSoundType.Normal:
          audioSource.outputAudioMixerGroup = GameMainAudioMixerGroupMaster;
          break;
        case GameSoundType.UI:
          audioSource.outputAudioMixerGroup = GameUIAudioMixerGroupMaster;
          break;
      }


      audios.Add(audioGlobalControl);
    }
    private bool IsSoundPlayerRegistered(AudioSource audioSource, out AudioGlobalControl audioGlobalControl)
    {
      foreach (AudioGlobalControl a in audios)
      {
        if (a.Audio == audioSource)
        {
          audioGlobalControl = a;
          return true;
        }
      }
      audioGlobalControl = null;
      return false;
    }

    #endregion

    #region Audio settings

    // Load audio settings.
    private bool OnVoiceSettingsUpdated(string groupName, int action)
    {
      float volBackground = GameSettings.GetFloat(SettingConstants.SettingsVoiceBackground);
      float volUI = GameSettings.GetFloat(SettingConstants.SettingsVoiceUI);
      float volMain = GameSettings.GetFloat(SettingConstants.SettingsVoiceMain);

      GameUIAudioMixer.SetFloat("UIMasterVolume", volUI <= 1 ? -60 : (20.0f * Mathf.Log10(volUI / 100.0f)));
      GameMainAudioMixer.SetFloat("MasterVolume", volMain <= 1 ? -60 : (20.0f * Mathf.Log10(volMain / 100.0f)));
      GameMainAudioMixer.SetFloat("BackgroundVolume", volBackground <= 1 ? -60 : (20.0f * Mathf.Log10(volBackground / 100.0f)));

      return true;
    }

    private Dictionary<string, AudioSource> fastPlayVoices = null;

    #endregion

    #region Quick sound helpers

    /// <summary>
    /// Play a short sound through the cached player.
    /// </summary>
    /// <param name="soundName">Sound resource identifier.</param>
    /// <param name="type">Sound category.</param>
    /// <returns>Whether playback succeeded.</returns>
    public bool PlayFastVoice(string soundName, GameSoundType type)
    {
      return PlayFastVoice(null, soundName, type);
    }
    /// <summary>
    /// Play a short sound from a specific package.
    /// </summary>
    /// <param name="package">Owning package.</param>
    /// <param name="soundName">Sound resource path.</param>
    /// <param name="type">Sound category.</param>
    /// <returns>Whether playback succeeded.</returns>
    public bool PlayFastVoice(GamePackage package, string soundName, GameSoundType type)
    {
      string key = soundName + "@" + type;
      AudioSource cache = null;
      if (fastPlayVoices.TryGetValue(key, out cache))
      {
        cache.Play();
        return true;
      }
      AudioClip audioClip = package == null ? LoadAudioResource(soundName) : LoadAudioResource(package, soundName);
      if (audioClip == null)
        return false;

      cache = RegisterSoundPlayer(type, audioClip, false, true, key);
      cache.maxDistance = 2000;
      cache.Play();
      fastPlayVoices[key] = cache;
      return true;
    }

    #endregion

    #region Command

    private void InitCommands()
    {
      var srv = GameManager.Instance.GameDebugCommandServer;
      srv.RegisterCommand("sm", (keyword, fullCmd, argsCount, args) =>
      {
        var type = (string)args[0];
        switch (type)
        {
          case "play":
            {
              string asset = "";
              GameSoundType stype;
              if (!DebugUtils.CheckDebugParam(1, args, out asset)) break;
              DebugUtils.CheckEnumDebugParam<GameSoundType>(2, args, out stype, false, GameSoundType.Normal);

              PlayFastVoice(args[1], stype);
              return true;
            }
          case "list":
            foreach (var i in audios)
              Log.V(TAG, string.Format("{0} => {1}, isPlaying: {2}, loop: {3}", i.Audio.gameObject.name, i.Type, i.Audio.isPlaying, i.Audio.loop));
            break;
        }
        return false;
      }, 1, "sm <play/list> Sound manager commands\n" +
              "  play <asset:string> [soundType:GameSoundType] ▶ Play audio; asset format is 'packageName:audioPath', and soundType defaults to GameSoundType.Normal.\n" +
              "  list                                          ▶ List all managed sound instances");
    }

    #endregion
  }

  /// <summary>
  /// Sound category.
  /// </summary>
  public enum GameSoundType
  {
    /// <summary>
    /// General sound.
    /// </summary>
    Normal,
    /// <summary>
    /// Ball sound effect.
    /// </summary>
    BallEffect,
    /// <summary>
    /// Mechanism sound effect.
    /// </summary>
    ModulEffect,
    /// <summary>
    /// UI sound.
    /// </summary>
    UI,
    /// <summary>
    /// Background music.
    /// </summary>
    Background,
  }
}
