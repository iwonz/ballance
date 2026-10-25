namespace Ballance2.Game.GamePlay
{
  /// <summary>
  /// Game physics collision layer definitions.
  /// </summary>
  /// <remarks>
  /// There are at most 32 collision layers. Layers after 14 are available, but prefer existing layers. Inspect their interactions in Ballance Physics Settings.
  /// </remarks>
  public static class GameLayers 
  {
    /// <summary>
    /// Default collision layer, colliding with all objects.
    /// </summary>
    public const int LAYER_PHY_GLOBAL = 0;
    /// <summary>
    /// Ball collision layer.
    /// </summary>
    public const int LAYER_PHY_BALL = 1;
    /// <summary>
    /// Ball fragment collision layer.
    /// </summary>
    public const int LAYER_PHY_BALL_PEICES = 2;
    /// <summary>
    /// Mechanism layer colliding with balls and floors.
    /// </summary>
    public const int LAYER_PHY_MODUL_COL_BALL = 3;
    /// <summary>
    /// Mechanism layer excluding balls.
    /// </summary>
    public const int LAYER_PHY_MODUL_NOCOL_BALL = 4;
    /// <summary>
    /// Mechanism layer excluding floors.
    /// </summary>
    public const int LAYER_PHY_MODUL_NOCOL_FLOOR = 5;
    /// <summary>
    /// Mechanism layer colliding only with other mechanism layers, excluding floors and balls.
    /// </summary>
    public const int LAYER_PHY_MODUL_NOCOL = 6;
    /// <summary>
    /// Mechanism layer colliding only with balls, excluding other mechanisms and floors.
    /// </summary>
    public const int LAYER_PHY_MODUL_ONLY_COL_BALL = 7;
    /// <summary>
    /// Trigger colliding only with balls.
    /// </summary>
    public const int LAYER_PHY_TRANFO_TIGGER = 8;
    /// <summary>
    /// Unused reserved layer.
    /// </summary>
    public const int LAYER_UNUSED_9 = 9;
    /// <summary>
    /// Unused reserved layer.
    /// </summary>
    public const int LAYER_UNUSED_10 = 10;
    /// <summary>
    /// Floor layer.
    /// </summary>
    public const int LAYER_PHY_FLOOR = 11;
    /// <summary>
    /// Rail layer.
    /// </summary>
    public const int LAYER_PHY_FLOOR_RAIL = 12;
    /// <summary>
    /// Wooden floor layer.
    /// </summary>
    public const int LAYER_PHY_FLOOR_WOODS = 13;
    /// <summary>
    /// Barrier layer.
    /// </summary>
    public const int LAYER_PHY_FLOOR_STOPPER = 14;
  }
}