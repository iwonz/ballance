using System.Collections.Generic;
using Ballance2.Base;
using Ballance2.Package;
using Ballance2.Services;
using BallancePhysics.Wapper;
using Newtonsoft.Json;
using UnityEngine;

namespace Ballance2.Game.GamePlay.Balls
{
  /// <summary>
  /// Manage ball sounds; also supports collision sounds for other objects.
  /// </summary>
  public class BallSoundManager : MonoBehaviour 
  {
    private const string TAG = "BallSoundManager";

    public AnimationCurve BallRollSpeedVolumeCruve = null;

    [JsonObject]
    public class BallSoundCollData {
      /// <summary>
      /// Minimum collision detection speed.
      /// </summary>
      [JsonProperty]
      public float MinSpeed; 
      /// <summary>
      /// Maximum collision detection speed.
      /// </summary>
      [JsonProperty]
      public float MaxSpeed; 
      /// <summary>
      /// Collision detection delay.
      /// </summary>
      [JsonProperty]
      public float SleepAfterwards; 
      /// <summary>
      /// Minimum speed threshold for collision detection.
      /// </summary>
      [JsonProperty]
      public float SpeedThreadhold; 
      /// <summary>
      /// Delay before rolling detection starts.
      /// </summary>
      [JsonProperty]
      public float TimeDelayStart;
      /// <summary>
      /// Delay before rolling detection ends.
      /// </summary>
      [JsonProperty]
      public float TimeDelayEnd;
      /// <summary>
      /// Whether rolling sound is available.
      /// </summary>
      [JsonProperty]
      public bool HasRollSound; 
      /// <summary>
      /// Rolling sound names from the ball's RollSound data.
      /// </summary>
      [JsonProperty]
      public string RollSoundName; 
      /// <summary>
      /// Collision sound names from the ball's HitSound data.
      /// </summary>
      [JsonProperty]
      public string HitSoundName; 
    }

    private List<GameEventHandler> _MyEventHandlers = new List<GameEventHandler>();
    private Dictionary<int, BallSoundCollData> _SoundCollData = new Dictionary<int, BallSoundCollData>();
    private Dictionary<string, int> _CollIDNames = new Dictionary<string, int>();
    private int _CurrentCollId = 1;

    // Load and unload.
    private void Start() {
      var sysPackage = GamePackage.GetSystemPackage();
      this._MyEventHandlers.Add(GameMediator.Instance.RegisterEventHandler(sysPackage, GameEventNames.EVENT_LEVEL_BUILDER_BEFORE_START, TAG, (evtName, param) => {
        this.RemoveAllSoundCollData();
        this._AddInternalSoundCollData();
        return false;
      }));
    }
    private void OnDestroy() {
      if (GameMediator.Instance != null && _MyEventHandlers.Count > 0)
        GameMediator.Instance.UnRegisterEventHandler(GameEventNames.EVENT_LEVEL_BUILDER_BEFORE_START, _MyEventHandlers[0]);
    }

    #region Collision data

    // Internal methods.
    private void _AddInternalSoundCollData() 
    {
      _CollIDNames.Add("Nome", 0);
      AddSoundCollData(0, new BallSoundCollData() {
        MinSpeed = 0,
        MaxSpeed = 0,
        SleepAfterwards = 0,
        SpeedThreadhold = 0,
        HasRollSound = false,
        RollSoundName = null,
        HitSoundName = null
      });
      AddSoundCollData(GetSoundCollIDByName("Stone"), new BallSoundCollData() {
        MinSpeed = 5,
        MaxSpeed = 30,
        SleepAfterwards = 0.6f,
        SpeedThreadhold = 20,
        HasRollSound = true,
        RollSoundName = "Stone",
        HitSoundName = "Stone"
      });
      AddSoundCollData(GetSoundCollIDByName("Wood"), new BallSoundCollData() {
        MinSpeed = 5,
        MaxSpeed = 30,
        SleepAfterwards = 0.6f,
        SpeedThreadhold = 20f,
        HasRollSound = true,
        RollSoundName = "Wood",
        HitSoundName = "Wood"
      });
      AddSoundCollData(GetSoundCollIDByName("Metal"), new BallSoundCollData() {
        MinSpeed = 5,
        MaxSpeed = 30,
        SleepAfterwards = 0.6f,
        SpeedThreadhold = 20,
        HasRollSound = true,
        RollSoundName = "Metal",
        HitSoundName = "Metal"
      });
    }
    private void RemoveAllSoundCollData() 
    {
      _CurrentCollId = 1;
      _CollIDNames.Clear();
      _SoundCollData.Clear();
    }

    /// <summary>
    /// Add a ball collision sound group.
    /// </summary>
    /// <param name="colId">Custom collision layer ID; use GetSoundCollIDByName to avoid duplicate IDs.</param>
    /// <param name="data">Collision data.</param>
    public void AddSoundCollData(int colId, BallSoundCollData data) 
    {
      if (_SoundCollData.ContainsKey(colId)) 
      {
        Log.E(TAG, $"AddSoundCollData failed because SoundCollData id: {colId} already added");
        return;
      }
      _SoundCollData[colId] = data;
    }
    /// <summary>
    /// Remove a ball collision sound group.
    /// </summary>
    /// <param name="colId">Changes apply on the next ball activation, not to the active ball; avoid calling this during gameplay.</param>
    public void RemoveSoundCollData(int colId) 
    {
      _SoundCollData.Remove(colId);
    }
    /// <summary>
    /// Allocate a sound group ID by name, returning the existing ID when already registered.
    /// </summary>
    /// <param name="name">Custom sound group name.</param>
    /// <returns></returns>
    public int GetSoundCollIDByName(string name) 
    {
      if (_CollIDNames.TryGetValue(name, out var id))
        return id;
      else
      {
        id = _CurrentCollId;
        _CurrentCollId++;
        _CollIDNames[name] = id;
        return id;
      }
    }
    /// <summary>
    /// Force-stop all sounds.
    /// </summary>
    public void StopAllSound() {
      Log.D(TAG, "StopAllSound");
      foreach(var value in soundableBallWorkDatas) {
        foreach(var sound in value.Value._CurrentPlayingRollSounds) {
          sound.volume = 0;
          sound.pitch = 1;
        }
      }
    }

    #endregion

    #region Sound data

    private class BallHitSoundItem {
      public BallHitSoundItem(AudioSource sound1, AudioSource sound2) {
        this.sound1 = sound1;
        this.sound2 = sound2;
      }
      public AudioSource sound2;
      public AudioSource sound1;
      public bool isSound1 = true;
    }

    private class SoundableBallWorkData {
      public Dictionary<string, BallHitSoundItem> _HitSoundSounds = new Dictionary<string, BallHitSoundItem>();
      public Dictionary<string, AudioSource> _RollSoundSounds = new Dictionary<string, AudioSource>();
      public LinkedList<AudioSource> _CurrentPlayingRollSounds = new LinkedList<AudioSource>();
      public HashSet<AudioSource> _CurrentPlayingRollSoundsHashSet = new HashSet<AudioSource>();
      public SpeedMeter _SpeedMeter = null;
    }

    private Dictionary<EntityId, SoundableBallWorkData> soundableBallWorkDatas = new Dictionary<EntityId, SoundableBallWorkData>();

    // Load ball collision and rolling sounds.
    private SoundableBallWorkData InitSoundableBallWorkData(EntityId instanceId, Ball.BallSoundConfig _SoundConfig) {
      SoundableBallWorkData data = null;
      if (soundableBallWorkDatas.TryGetValue(instanceId, out data))
        return data;
      data = new SoundableBallWorkData();
      // Load ball collision and rolling sounds.
      var SoundManager = GameSoundManager.Instance;
      foreach (var item in _SoundConfig.HitSound.Names) {
        if (!string.IsNullOrEmpty(item.Value)) {
          var key = item.Key;
          var sound2 = SoundManager.RegisterSoundPlayer(GameSoundType.BallEffect, item.Value, false, true,  $"{gameObject.name}SoundHit{key}2");
          var sound1 = SoundManager.RegisterSoundPlayer(GameSoundType.BallEffect, item.Value, false, true, $"{gameObject.name}SoundHit{key}1");
          data._HitSoundSounds[key] = new BallHitSoundItem(sound1, sound2);
        }
      }
      foreach (var item in _SoundConfig.RollSound.Names) {
        if (!string.IsNullOrEmpty(item.Value)) {
          var sound = SoundManager.RegisterSoundPlayer(GameSoundType.BallEffect, item.Value, true, true, $"{gameObject.name}SoundRoll{item.Key}");
          sound.loop = true;
          sound.playOnAwake = true;
          sound.volume = 0;
          sound.Play();
          data._RollSoundSounds[item.Key] = sound;
        }
      }
      soundableBallWorkDatas.Add(instanceId, data);
      return data;
    }

    #endregion

    #region Ball collision and rolling sound controls

    /// <summary>
    /// Add sound handlers to a physics body.
    /// </summary>
    public void AddSoundableBall(PhysicsObject rigidbody, Ball.BallSoundConfig _SoundConfig) 
    {
      // Add or retrieve collision data.
      SoundableBallWorkData soundableBallWorkData = InitSoundableBallWorkData(rigidbody.gameObject.GetEntityId(), _SoundConfig);

      // Register sound-layer tool listeners.
      foreach(var value in _SoundCollData) {
        rigidbody.AddCollDetection(value.Key, value.Value.MinSpeed, value.Value.MaxSpeed, value.Value.SleepAfterwards, value.Value.SpeedThreadhold);
        if (value.Value.HasRollSound)
          rigidbody.AddContractDetection(value.Key, _SoundConfig.RollSound.TimeDelayStart, _SoundConfig.RollSound.TimeDelayEnd);
      }
      // Add callbacks.
      rigidbody.EnableCollisionEvent = true;
      rigidbody.EnableCollisionDetection();
      rigidbody.EnableContractEventCallback();
      // Collision callback.
      rigidbody.OnPhysicsCollDetection = (_, col_id, speed_precent) => {
        var currentSoundCollData = _SoundCollData[col_id];
        if (currentSoundCollData != null && !string.IsNullOrEmpty(currentSoundCollData.HitSoundName)) 
        {
          // Get the sound for this layer and the target layer.
          soundableBallWorkData._HitSoundSounds.TryGetValue(currentSoundCollData.HitSoundName, out var sound);
          // Fall back to the All sound when no specific collision sound exists.
          if (sound == null) 
            soundableBallWorkData._HitSoundSounds.TryGetValue("All", out sound); 

          if (sound != null) 
          {
            // Alternate between two sound players because another collision
            // may occur before the previous sound finishes.
            if (sound.isSound1) 
            {
              sound.isSound1 = false;
              sound.sound1.volume = speed_precent;
              if (!sound.sound1.isPlaying) 
                sound.sound1.Play();
            }
            else
            {
              sound.isSound1 = true;
              sound.sound2.volume = speed_precent;
              if (!sound.sound2.isPlaying)
                sound.sound2.Play();
            }
          }
        }
      };
      
      // Rolling sounds.
      if (_SoundConfig.HasRollSound)
      {
        // Touch-start callback.
        rigidbody.OnPhysicsContactOn = (_, col_id) => {
          var currentSoundCollData = _SoundCollData[col_id];
          if (currentSoundCollData != null && currentSoundCollData.HasRollSound)
          {
            // Get the sound for this layer and the target layer.
            soundableBallWorkData._RollSoundSounds.TryGetValue(currentSoundCollData.RollSoundName, out var sound);
            // Fall back to the All sound when no specific collision sound exists.
            if (sound == null) 
              soundableBallWorkData._RollSoundSounds.TryGetValue("All", out sound); 
            // Add to the playing sounds.
            if (sound != null && !soundableBallWorkData._CurrentPlayingRollSoundsHashSet.Contains(sound)) {
              soundableBallWorkData._CurrentPlayingRollSoundsHashSet.Add(sound);
              soundableBallWorkData._CurrentPlayingRollSounds.AddLast(sound);
            }
          }
        };
        // Touch-end callback.
        rigidbody.OnPhysicsContactOff = (_, col_id) => {
          var currentSoundCollData = _SoundCollData[col_id];
          if (currentSoundCollData != null && currentSoundCollData.HasRollSound)
          {
            // Get the sound for this layer and the target layer.
            soundableBallWorkData._RollSoundSounds.TryGetValue(currentSoundCollData.RollSoundName, out var sound);
            // Fall back to the All sound when no specific collision sound exists.
            if (sound == null) 
              soundableBallWorkData._RollSoundSounds.TryGetValue("All", out sound); 
            // Remove from the playing sounds.
            if (sound != null && soundableBallWorkData._CurrentPlayingRollSoundsHashSet.Contains(sound)) {
              soundableBallWorkData._CurrentPlayingRollSoundsHashSet.Remove(sound);
              soundableBallWorkData._CurrentPlayingRollSounds.Remove(sound);
              sound.volume = 0;
              sound.pitch = 1;
            }
          }
        };
      
        // Add a speed meter to drive rolling sound effects.
        soundableBallWorkData._SpeedMeter = rigidbody.gameObject.GetComponent<SpeedMeter>();
        if (soundableBallWorkData._SpeedMeter == null)
          soundableBallWorkData._SpeedMeter = rigidbody.gameObject.AddComponent<SpeedMeter>();
        soundableBallWorkData._SpeedMeter.Enabled = true;
        soundableBallWorkData._SpeedMeter.Callback = (meter) => {
          HandlerBallRollSpeedChange(meter.NowAbsoluteSpeed, _SoundConfig, soundableBallWorkData, rigidbody);
        };
      }
    }
    /// <summary>
    /// Remove sound handlers from a physics body.
    /// </summary>
    public void RemoveSoundableBall(PhysicsObject rigidbody, Ball.BallSoundConfig _SoundConfig) 
    {
      SoundableBallWorkData soundableBallWorkData = InitSoundableBallWorkData(rigidbody.gameObject.GetEntityId(), _SoundConfig);

      // Remove callbacks.
      rigidbody.OnPhysicsCollDetection = null;
      rigidbody.OnPhysicsContactOn = null;
      rigidbody.OnPhysicsContactOff = null;
      if (rigidbody.IsPhysicalized) {
        rigidbody.DeleteAllCollDetection();
        rigidbody.DeleteAllContractDetection();
        rigidbody.DisableCollisionDetection();
      }
      rigidbody.EnableCollisionEvent = false;
      // Delete the speed meter.
      if (soundableBallWorkData._SpeedMeter != null) {
        Object.Destroy(soundableBallWorkData._SpeedMeter);
        soundableBallWorkData._SpeedMeter = null;
      }
      // Remove sound-layer tool listeners.
      var node = soundableBallWorkData._CurrentPlayingRollSounds.First;
      while(node != null) {
        var value = node.Value;
        value.volume = 0;
        value.pitch = 0;

        node = node.Next;
      }
      soundableBallWorkData._CurrentPlayingRollSounds.Clear();
      soundableBallWorkDatas.Remove(rigidbody.gameObject.GetEntityId());
    }
    /// <summary>
    /// Force-stop all ball sounds.
    /// </summary>
    public void StopSoundableBallAllSound(PhysicsObject rigidbody, Ball.BallSoundConfig _SoundConfig)
    {
      SoundableBallWorkData soundableBallWorkData = InitSoundableBallWorkData(rigidbody.gameObject.GetEntityId(), _SoundConfig);
      var node = soundableBallWorkData._CurrentPlayingRollSounds.First;
      while(node != null) {
        var value = node.Value;
        value.volume = 0;
        value.pitch = 1;
        node = node.Next;
      }
    }

    /// <summary>
    /// Update rolling sound volume and pitch.
    /// </summary>
    private void HandlerBallRollSpeedChange(float speed, Ball.BallSoundConfig _SoundConfig, SoundableBallWorkData soundableBallWorkData, PhysicsObject body)
    {
      float vol = Mathf.Min(1, _SoundConfig.RollSound.VolumeBase + BallRollSpeedVolumeCruve.Evaluate(speed / _SoundConfig.RollSoundSpeedReference));
      float pit = Mathf.Min(1, _SoundConfig.RollSound.PitchBase + (speed * _SoundConfig.RollSound.PitchFactor));
      if (body.TryGetSurfaceContact(out var contact)) {
        var surface = contact.state;
        float soft = Mathf.Clamp01(surface.snow + surface.sand * .6f + surface.vegetation * .3f);
        vol *= Mathf.Lerp(1, .45f, soft) * Mathf.Lerp(1, .85f, surface.wetness);
        pit *= Mathf.Lerp(1, .78f, soft);
      }

      if (GameManager.DebugMode)
        _SoundConfig._SoundManagerDebugString.Clear();

      // Apply volume to playing sounds.
      var node = soundableBallWorkData._CurrentPlayingRollSounds.First;
      while(node != null) {
        var value = node.Value;

        if (GameManager.DebugMode)
          _SoundConfig._SoundManagerDebugString.AppendLine(string.Format("> [{0}] {1:F2} {2:F2}", value.name, vol, pit));

        value.volume = vol;
        value.pitch = pit;

        node = node.Next;
      }
    }
  
    #endregion
  }
}
