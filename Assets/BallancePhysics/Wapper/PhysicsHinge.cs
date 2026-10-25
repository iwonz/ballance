using UnityEngine;

namespace BallancePhysics.Wapper
{
  
  [AddComponentMenu("BallancePhysics/PhysicsHinge")]
  
  public class PhysicsHinge : PhysicsConstraintBase
  {
    [Tooltip("Reference frame for position and axis.")]
    
    /// <summary>
    /// Position and axis reference.
    /// </summary>
    public Transform HingeRef = null;
    /// <summary>
    /// The other connected object; null connects the constraint to the world.
    /// </summary>
    
    [Tooltip("The other object to connect; leave empty to connect to the world.")]
    public PhysicsObject Other = null;

    protected override void DoCreateConstraint()
    {
      var obj = GetComponent<PhysicsObject>();
      if(!obj.IsPhysicalized)
        return;
      if(Other != null && !Other.IsPhysicalized) {
        Other.AddPendCreateComponent(this);
        return;
      }
      Handle = PhysicsApi.API.set_physics_hinge(obj.Handle, Other != null ? Other.Handle : System.IntPtr.Zero, 
        HingeRef.transform.position, HingeRef.transform.forward.normalized);
    }
  }
}