using UnityEditor;
using UnityEngine;

namespace BallancePhysics.Wapper
{
  
  [AddComponentMenu("BallancePhysics/PhysicsConstraint")]
  
  public class PhysicsConstraint : PhysicsConstraintBase
  {
    
    [Tooltip("The other object to connect; leave empty to connect to the world.")]
    /// <summary>
    /// The other connected object; null connects the constraint to the world.
    /// </summary>
    public PhysicsObject Other;
    
    
    
    public enum CoordinateIndex {
      
      X = 0,
      
      Y = 1,
      
      Z = 2 
    };

    [Tooltip("Fraction of the distance to the target applied by the constraint. At 0.5, it aims halfway between the current and target positions for smoother motion.")]
    
    /// <summary>
    /// The force factor controls progress toward the target: 0.5 moves halfway from the current position, producing smoother motion.
    /// </summary>
    public float force_factor = 1.0f;
    [Tooltip("Higher damping slows movement toward the target. A force-to-damping ratio below 1.0 produces smoother motion with less bouncing.")]
    
    /// <summary>
    /// A higher damping factor slows the controller. A force-to-damping ratio below 1.0 smooths motion and reduces bouncing.
    /// </summary>
    public float damp_factor = 1.0f;

    [EnumFlagProperty]
    [Tooltip("Translation axes constrained by this constraint.")]
    
    public CoordinateIndex translation_limit;
    
    [Tooltip("Minimum translation limit for each axis.")]
    
    public Vector3 translation_freedom_min;
    [Tooltip("Maximum translation limit for each axis.")]
    
    public Vector3 translation_freedom_max;
    
    [EnumFlagProperty]
    [Tooltip("Rotation axes constrained by this constraint.")]
    
    public CoordinateIndex rotation_limit;

    [Tooltip("Minimum rotation limit for each axis.")]
    
    public Vector3 rotation_freedom_min;
    [Tooltip("Maximum rotation limit for each axis.")]
    
    public Vector3 rotation_freedom_max;

    protected override void DoCreateConstraint()
    {
      var obj = GetComponent<PhysicsObject>();
      if(!obj.IsPhysicalized)
        return;
      if(Other != null && !Other.IsPhysicalized) {
        Other.AddPendCreateComponent(this);
        return;
      }

      Handle = PhysicsApi.API.set_physics_constraint(obj.Handle, Other != null ? Other.Handle : System.IntPtr.Zero, 
        force_factor, damp_factor, 
        (int)translation_limit, 
        translation_freedom_min, 
        translation_freedom_max,
        (int)rotation_limit, 
        rotation_freedom_min, 
        rotation_freedom_max);
    }
  }
}