using UnityEngine;

namespace BallancePhysics.Wapper
{
  
  [AddComponentMenu("BallancePhysics/PhysicsFixedConstraint")]
  
  public class PhysicsFixedConstraint : PhysicsConstraintBase
  {
    [Tooltip("The other object to connect; leave empty to connect to the world.")]
    
    /// <summary>
    /// The other connected object; null connects the constraint to the world.
    /// </summary>
    public PhysicsObject Other = null;

    protected override void DoCreateConstraint()
    {
      var obj = GetComponent<PhysicsObject>();
      if(!obj.IsPhysicalized)
        return;
      if(Other == null)
        return;
      if(!Other.IsPhysicalized) {
        Other.AddPendCreateComponent(this);
        return;
      }
      Handle = PhysicsApi.API.set_physics_fixed_constraint(obj.Handle, Other.Handle);
    }
  }
}