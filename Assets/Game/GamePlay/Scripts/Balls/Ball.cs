using System.Collections.Generic;
using System.Text;
using Ballance2.Services;
using Ballance2.UI.Utils;
using BallancePhysics.Wapper;
using Newtonsoft.Json;
using UnityEngine;

namespace Ballance2.Game.GamePlay.Balls
{
  /// <summary>
  /// Base Ballance ball definition.
  /// Subclass this type to implement a custom ball.
  /// </summary>
  public class Ball : MonoBehaviour
  {
    /// <summary>
    /// Ball rigid body.
    /// </summary>
    public PhysicsObject _Rigidbody = null;
    /// <summary>
    /// Ball fragments.
    /// </summary>
    public GameObject _Pieces = null;
    /// <summary>
    /// Fragment configuration.
    /// </summary>
    public BallPiecesData _PiecesData;
    
    public float _UpForce = 0; 
    public float _DownForce = 0; 
    public float _Force = 0; 

    #region Ball sound configuration

    [JsonObject]
    public class BallSoundConfig {
      /// <summary>
      /// Ball collision sound configuration.
      /// </summary>
      [JsonProperty]
      public BallHitSoundConfigHit HitSound = new BallHitSoundConfigHit(); 
      /// <summary>
      /// Ball rolling sound configuration.
      /// </summary>
      [JsonProperty]
      public BallHitSoundConfigRoll RollSound = new BallHitSoundConfigRoll();
      /// <summary>
      /// Whether the ball has rolling sounds.
      /// </summary>
      [JsonProperty]
      public bool HasRollSound = true;
      /// <summary>
      /// Reference speed for the rolling sound curve, normally the ball's maximum speed.
      /// </summary>
      [JsonProperty]
      public float RollSoundSpeedReference = 0;

      [JsonIgnore]
      public StringBuilder _SoundManagerDebugString = new StringBuilder();
    }

    /// <summary>
    /// Ball collision sound configuration.
    /// </summary>
    public BallSoundConfig _SoundConfig = new BallSoundConfig();

    #endregion

    #region Fragment configuration

    /// <summary>
    /// Fragment sounds.
    /// </summary>
    public string _PiecesSoundName = "";
    /// <summary>
    /// Minimum fragment scattering force.
    /// </summary>
    public float _PiecesMinForce = 0;
    /// <summary>
    /// Maximum fragment scattering force.
    /// </summary>
    public float _PiecesMaxForce = 5; 
    /// <summary>
    /// Fragment lifetime in seconds.
    /// </summary>
    public float _PiecesTimeLive = 30; 
    /// <summary>
    /// Maximum collision speed used to calculate fragment sound volume.
    /// </summary>
    public float _PiecesColSoundMaxSpeed = 10; 
    /// <summary>
    /// Minimum collision speed used to calculate fragment sound volume.
    /// </summary>
    public float _PiecesColSoundMinSpeed = 1; 
    /// <summary>
    /// Fragment physics data.
    /// </summary>
    public GamePhysBallPiecesPhysicsData _PiecesPhysicsData = default(GamePhysBallPiecesPhysicsData); 
    /// <summary>
    /// Custom fragment physics callback; null uses the default parameters.
    /// </summary>
    protected PiecesPhysCallback _PiecesPhysCallback = null;  
    /// <summary>
    /// Fragments with sound enabled.
    /// </summary>
    public List<string> _PiecesHaveColSound = new List<string>();

    protected delegate PhysicsObject PiecesPhysCallback(GameObject gameObject, GamePhysBallPiecesPhysicsData physicsData);

    #endregion

    #region Ball initialization

    // Initialize fragments.
    private void _InitPeices() {
      if (_Pieces != null) {
        AudioClip piecesSound = null;
        if (!string.IsNullOrEmpty(_PiecesSoundName)) {
          piecesSound = GameSoundManager.Instance.LoadAudioResource(_PiecesSoundName);
        }
      
        var parent = _Pieces;
        var data = new BallPiecesData();
        data.parent = parent;
        data.bodys = new List<PhysicsObject>();
        data.fadeObjects = new List<FadeObject>();

        for (int i = 0; i < parent.transform.childCount; i++) {
          var child = parent.transform.GetChild(i);
          PhysicsObject body = null;
          if (_PiecesPhysCallback != null)
            body = _PiecesPhysCallback(child.gameObject, _PiecesPhysicsData);
          else {
            body = child.gameObject.AddComponent<PhysicsObject>();
            if(_PiecesPhysicsData.Mass > 0) {
              //Mesh
              var meshFilter = child.GetComponent<MeshFilter>();
              if (meshFilter != null && meshFilter.mesh != null) {
                body.Mass = _PiecesPhysicsData.Mass;
                body.Elasticity = _PiecesPhysicsData.Elasticity;
                body.Friction = _PiecesPhysicsData.Friction;
                body.LinearSpeedDamping = _PiecesPhysicsData.LinearDamp;
                body.RotSpeedDamping = _PiecesPhysicsData.RotDamp;
                body.AutoControlActive = false;
                body.DoNotAutoCreateAtAwake = true;
                body.EnableCollision = true;
                body.AutoMassCenter = false;
                body.UseExistsSurface = true;
                body.Layer = GameLayers.LAYER_PHY_BALL_PEICES;
                body.Convex.Add(meshFilter.mesh);
              }
              else {
                Log.W($"Ball {gameObject.name}", $"Not found MeshFilter or mesh in peices '{child.name}'");
              }
            }
            else {
              Log.W($"Ball {gameObject.name}", "No _PiecesPhysCallback or valid _PiecesPhysicsData found for this ball");
            }
            body.DoNotAutoCreateAtAwake = true;

            // Fragment sounds.
            if (piecesSound != null && _PiecesHaveColSound.IndexOf(body.name) != -1) {
              var sound = GameSoundManager.Instance.RegisterSoundPlayer(GameSoundType.BallEffect, piecesSound, false, true, "PiecesSound" + child.name);
              sound.spatialBlend = 1;
              sound.maxDistance = 30;
              sound.minDistance = 10;
              sound.dopplerLevel = 0;
              sound.volume = 1;
              body.CollisionEventCallSleep = 0.5f;
              body.EnableCollisionEvent = true;
              body.OnPhysicsCollision = (self, other, contact_point_ws, speed, surf_normal) => {
                if (data.throwed) {
                  sound.volume = (speed.sqrMagnitude - _PiecesColSoundMinSpeed) / (_PiecesColSoundMaxSpeed - _PiecesColSoundMinSpeed);
                  sound.Play();
                }
              };
            }
          }
          data.bodys.Add(body);
        }
        _PiecesData = data;
      }
    }
  
    #endregion

    #region Public overridable methods

    /// <summary>
    /// Start event.
    /// </summary>
    protected virtual void Start() {
      _Rigidbody = gameObject.GetComponent<PhysicsObject>();
      _InitPeices();
    }
    /// <summary>
    /// Ball activated.
    /// </summary>
    public virtual void Active() {}
    /// <summary>
    /// Ball deactivated.
    /// </summary>
    public virtual void Deactive() {}
    /// <summary>
    /// Get this ball's fragments.
    /// </summary>
    public virtual GameObject GetPieces() { return _Pieces; }
    /// <summary>
    /// Provide a custom fragment controller.
    /// </summary>
    public virtual IBallPiecesControllManager GetBallPiecesControllManager() { return GamePlayManager.Instance.BallManager.BallPiecesControllManager; }
    /// <summary>
    /// Scatter fragments of this ball type.
    /// </summary>
    public virtual void ThrowPieces(Vector3 pos) {
      if (_PiecesData != null)
        GetBallPiecesControllManager().ThrowPieces(_PiecesData, pos, _PiecesMinForce, _PiecesMaxForce, _PiecesTimeLive);
    }
    /// <summary>
    /// Collect fragments of this ball type.
    /// </summary>
    public virtual void ResetPieces() {
      if (_PiecesData != null)
         GetBallPiecesControllManager().ResetPieces(_PiecesData);
    }
  
    #endregion
  }

  [JsonObject]
  public class BallHitSoundConfigBase {
    /// <summary>
    /// Sound configuration.
    /// The key is the collision layer name.
    /// The value is the sound resource name.
    /// </summary>
    [JsonProperty]
    public Dictionary<string, string> Names = new Dictionary<string, string>();
  }
  [JsonObject]
  public class BallHitSoundConfigRoll : BallHitSoundConfigBase {
    /// <summary>
    /// Delay before rolling sound starts.
    /// </summary>
    [JsonProperty]
    public float TimeDelayStart = 0.5f;
    /// <summary>
    /// Delay before rolling sound stops.
    /// </summary>
    [JsonProperty]
    public float TimeDelayEnd = 0.5f;
    /// <summary>
    /// Base rolling sound pitch.
    /// </summary>
    [JsonProperty]
    public float PitchBase = 0.6f;
    /// <summary>
    /// Rolling sound pitch multiplier.
    /// </summary>
    [JsonProperty]
    public float PitchFactor = 0.03f;
    /// <summary>
    /// Base rolling sound volume.
    /// </summary>
    [JsonProperty]
    public float VolumeBase = 0f;
  }  
  [JsonObject]
  public class BallHitSoundConfigHit : BallHitSoundConfigBase {
    /// <summary>
    /// Maximum collision speed used to calculate sound volume.
    /// </summary>
    [JsonProperty]
    public float MaxSpeed = 20;
    /// <summary>
    /// Minimum collision speed used to calculate sound volume.
    /// </summary>
    [JsonProperty]
    public float MinSpeed = 2;
  }
  /// <summary>
  /// Ball fragment data structure.
  /// </summary>
  public class BallPiecesData {
    /// <summary>
    /// All fragment physics bodies.
    /// </summary>
    public List<PhysicsObject> bodys; 
    /// <summary>
    /// Parent GameObject.
    /// </summary>
    public GameObject parent;
    /// <summary>
    /// Fade-out delay timer.
    /// </summary>
    public int fadeOutTimerID = 0;
    /// <summary>
    /// Hide delay timer.
    /// </summary>
    public int delayHideTimerID = 0;
    /// <summary>
    /// Whether the fragments have been scattered.
    /// </summary>
    public bool throwed = false; 
    /// <summary>
    /// Fade controller.
    /// </summary>
    public List<FadeObject> fadeObjects;
  }
}