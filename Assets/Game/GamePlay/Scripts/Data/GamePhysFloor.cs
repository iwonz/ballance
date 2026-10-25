using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Ballance2.Game.GamePlay
{
  /// <summary>
  /// Floor physics parameters.
  /// </summary>
  /// <remarks>
  /// Add parameters to Data to register a custom physics floor.
  /// Change the relevant data before entering a level to override default floor parameters.
  /// </remarks>
  public static class GamePhysFloor 
  {
    /// <summary>
    /// Get data.
    /// </summary>
    public static Dictionary<string, GamePhysFloorData> Data;

    static GamePhysFloor() {
      var dataAsset = Resources.Load<TextAsset>("GamePhysFloorData");
      if (dataAsset != null)
        Data = JsonConvert.DeserializeObject<Dictionary<string, GamePhysFloorData>>(dataAsset.text);
      else
        Data = new Dictionary<string, GamePhysFloorData>();
    }
  }

  [JsonObject]
  public struct GamePhysFloorData 
  {
    /// <summary>
    /// Floor friction coefficient.
    /// </summary>
    [JsonProperty]
    public float Friction; 
    /// <summary>
    /// Floor elasticity coefficient.
    /// </summary>
    [JsonProperty]
    public float Elasticity; 
    /// <summary>
    /// Floor collision layer.
    /// </summary>
    [JsonProperty]
    public int Layer; 
    /// <summary>
    /// Floor collision sound layer name.
    /// </summary>
    [JsonProperty]
    public string HitSound; 
    /// <summary>
    /// Floor collision layer.
    /// </summary>
    [JsonProperty]
    public string CollisionLayerName;
  }
}