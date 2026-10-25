using UnityEngine;

namespace BallancePhysics.Wapper
{
  
  [AddComponentMenu("BallancePhysics/PhysicsBallJoint")]
  
  public class PhysicsBallJoint : PhysicsConstraintBase
  {
    [Tooltip("Reference frame for the connection point.")]
    
    public Transform JointPositionRef = null;
    [Tooltip("The other physics object to connect; leave empty to connect to the world.")]
    
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
      Handle = PhysicsApi.API.set_physics_ball_joint(obj.Handle, Other != null ? Other.Handle : System.IntPtr.Zero, JointPositionRef.transform.position);
    }
  }
}