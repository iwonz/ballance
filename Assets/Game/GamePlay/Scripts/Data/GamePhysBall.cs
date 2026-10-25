using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Ballance2.Game.GamePlay
{
  /// <summary>
  /// Ball physics parameters.
  /// </summary>
  /// <remarks>
  /// Add custom physics parameters to Data before registering the ball, using the same ball name as the registration key.
  /// </remarks>
  public static class GamePhysBall 
  {
    /// <summary>
    /// Get data.
    /// </summary>
    public static Dictionary<string, GamePhysBallData> Data;

    static GamePhysBall() {
      var dataAsset = Resources.Load<TextAsset>("GamePhysBallData");
      if (dataAsset != null)
        Data = JsonConvert.DeserializeObject<Dictionary<string, GamePhysBallData>>(dataAsset.text);
      else
        Data = new Dictionary<string, GamePhysBallData>();

      UnityEngine.Debug.Log("GamePhysBallData: " + Data.Count);
    }
  }

  [JsonObject]
  public struct GamePhysBallPiecesPhysicsData 
  {
    /// <summary>
    /// Friction coefficient.
    /// </summary>
    [JsonProperty]
    public float Friction;
    /// <summary>
    /// Elasticity coefficient.
    /// </summary>
    [JsonProperty]
    public float Elasticity;
    /// <summary>
    /// Mass.
    /// </summary>
    [JsonProperty]
    public float Mass;
    /// <summary>
    /// Linear damping.
    /// </summary>
    [JsonProperty]
    public float LinearDamp;
    /// <summary>
    /// Angular damping.
    /// </summary>
    [JsonProperty]
    public float RotDamp;
  }
  [JsonObject]
  public struct GamePhysBallData 
  {
    /// <summary>
    /// Ball movement force.
    /// </summary>
    [JsonProperty]
    public float Force; 
    /// <summary>
    /// Ball friction coefficient.
    /// </summary>
    [JsonProperty]
    public float Friction; 
    /// <summary>
    /// Ball elasticity coefficient.
    /// </summary>
    [JsonProperty]
    public float Elasticity; 
    /// <summary>
    /// Ball mass.
    /// </summary>
    [JsonProperty]
    public float Mass; 
    /// <summary>
    /// Linear damping.
    /// </summary>
    [JsonProperty]
    public float LinearDamp; 
    /// <summary>
    /// Collision layer.
    /// </summary>
    [JsonProperty]
    public int Layer; 
    /// <summary>
    /// Angular damping.
    /// </summary>
    [JsonProperty]
    public float RotDamp; 
    /// <summary>
    /// Minimum fragment explosion force.
    /// </summary>
    [JsonProperty]
    public float PiecesMinForce; 
    /// <summary>
    /// Maximum fragment explosion force.
    /// </summary>
    [JsonProperty]
    public float PiecesMaxForce; 
    /// <summary>
    /// Fragment physics parameters.
    /// </summary>
    [JsonProperty]
    public GamePhysBallPiecesPhysicsData PiecesPhysicsData; 
    /// <summary>
    /// Upward ball force, available only in debug mode.
    /// </summary>
    [JsonProperty]
    public float UpForce; 
    /// <summary>
    /// Ball radius; zero uses a convex mesh.
    /// </summary>
    [JsonProperty]
    public float BallRadius; 
    /// <summary>
    /// Ball radius used for trigger detection; default 2.
    /// </summary>
    [JsonProperty]
    public float TiggerBallRadius; 
    /// <summary>
    /// Downward ball force, available only in debug mode.
    /// </summary>
    [JsonProperty]
    public float DownForce; 
  }
}