using UnityEngine;

namespace Ballance2.Game.GamePlay
{
  /// <summary>
  /// Mechanism definition.
  /// Subclass this type to define a custom mechanism.
  /// </summary>
  public class ModulBase : MonoBehaviour 
  {
    /// <summary>
    /// Whether the base class automatically activates and deactivates the mechanism.
    /// </summary>
    protected bool AutoActiveBaseGameObject = false;
    /// <summary>
    /// Whether the player's ball is inside this mechanism's detection range.
    /// </summary>
    protected bool BallInRange = false;
    /// <summary>
    /// Enable periodic ball range checks; entering the range emits BallEnterRange.
    /// </summary>
    protected bool EnableBallRangeChecker = false;
    /// <summary>
    /// Ball detection range; it can also be changed through modul.BallRangeChecker.Diatance after creation.
    /// </summary>
    protected float BallCheckeRange = 60;
    /// <summary>
    /// Whether the mechanism was loaded in preview mode.
    /// </summary>
    [HideInInspector]
    public bool IsPreviewMode = false;
    /// <summary>
    /// Whether the mechanism was loaded in test mode.
    /// </summary>
    [HideInInspector]
    public bool IsDebugMode = false;
    /// <summary>
    /// Whether the mechanism is active.
    /// </summary>
    [HideInInspector]
    protected bool IsActive = false;

    private TiggerTester BallRangeChecker = null;

    protected virtual void Start() {
      // Initialize built-in ball range detection.
      if (this.EnableBallRangeChecker) {
        var collide = this.gameObject.AddComponent<SphereCollider>();
        collide.radius = this.BallCheckeRange;
        collide.isTrigger = true;
        this.BallRangeChecker = this.gameObject.AddComponent<TiggerTester>();
        this.BallRangeChecker.onTriggerEnter = (obj, other) => {
          if (!this.BallInRange && other.tag == "Ball") 
          {
            this.BallInRange = true;
            this.BallEnterRange();
          }
        };
        this.BallRangeChecker.onTriggerExit = (obj, other) => {
          if (this.BallInRange && other.tag == "Ball") {
            this.BallInRange = false;
            this.BallLeaveRange();
          }
        };
      }
    }

    /// <summary>
    /// Raised when the mechanism activates on sector entry.
    /// Raised after Backup.
    /// </summary>
    public virtual void Active() {
      if (AutoActiveBaseGameObject)
        gameObject.SetActive(true);
      IsActive = true;
    }
    /// <summary>
    /// Raised when the mechanism is hidden at sector end.
    /// </summary>
    public virtual void Deactive() {
      IsActive = false;
      if (AutoActiveBaseGameObject)
        gameObject.SetActive(false);
    }
    /// <summary>
    /// Raised when the level unloads.
    /// </summary>
    public virtual void UnLoad() {}
    /// <summary>
    /// Raised after Deactive when resetting to the initial state after a failed sector attempt.
    /// </summary>
    public virtual void Reset(ModulBaseResetType type) {}
    /// <summary>
    /// Raised after initial loading when the mechanism is ready to back up its state.
    /// </summary>
    public virtual void Backup() {}
    /// <summary>
    /// Raised when the ball enters this mechanism's range.
    /// </summary>
    protected virtual void BallEnterRange() {}
    /// <summary>
    /// Raised when the ball leaves this mechanism's range.
    /// </summary>
    protected virtual void BallLeaveRange() {}
    
    /// <summary>
    /// Raised on activation in preview mode.
    /// </summary>
    public virtual void ActiveForPreview() {}
    /// <summary>
    /// Raised when hidden in preview mode.
    /// </summary>
    public virtual void DeactiveForPreview() {}
    /// <summary>
    /// Raised when a test command runs in test mode.
    /// </summary>
    public virtual void DebugTestCommand(int command) {}
  }

  /// <summary>
  /// Mechanism reset type.
  /// </summary>
  public enum ModulBaseResetType {
    /// <summary>
    /// Restart the sector after player failure.
    /// </summary>
    SectorRestart,
    /// <summary>
    /// Restart the level.
    /// </summary>
    LevelRestart,
  }
}