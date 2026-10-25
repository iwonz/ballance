using System;
using UnityEngine;

namespace BallancePhysics.Wapper
{
  
  [RequireComponent(typeof(PhysicsObject))]
  
  public class PhysicsConstraintBase : PhysicsComponent
  {
    protected virtual void DoCreateConstraint() {
    }

    /// <summary>
    /// Create the constraint.
    /// </summary>
    protected override void DoCreate() {
      DoCreateConstraint();
    }
    /// <summary>
    /// Destroy the constraint.
    /// </summary>
    /// <param name="handle"></param>
    protected override void DoDestroy(IntPtr handle) {
      PhysicsApi.API.destroy_physics_constraint(handle);
    }
  }
}