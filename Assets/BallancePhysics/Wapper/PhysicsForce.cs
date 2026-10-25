using System;
using UnityEngine;

namespace BallancePhysics.Wapper
{
  /// <summary>
  /// Ballance does not use this component. It can move both objects, unlike PhysicsConstraintForce.
  /// For a constant force, use [PhysicsConstraintForce](BallancePhysics.Wapper.PhysicsConstraintForce).
  /// </summary>
  [AddComponentMenu("BallancePhysics/PhysicsForce")]
  public class PhysicsForce : PhysicsComponent
  {
    /// <summary>
    /// Reference point on object 1.
    /// </summary>
    [Tooltip("Reference point on object 1.")]
    public Transform Position1Ref;
    [Tooltip("Reference point on object 2.")]
    /// <summary>
    /// Reference point on object 2.
    /// </summary>
    public Transform Position2Ref;
    [Tooltip("Force magnitude.")]
    [SerializeField]
    private float _Force = 1.0f;
    [Tooltip("The other object to connect.")]
    public PhysicsObject Other = null;

    /// <summary>
    /// Force magnitude.
    /// </summary>
    /// <value></value>
    public float Force { 
      get => _Force;
      set {
        _Force = value;
        if(Handle != IntPtr.Zero)
          PhysicsApi.API.set_physics_force_value(Handle, _Force);
      }
    }
    /// <summary>
    /// Apply force to object 2.
    /// </summary>
    [Tooltip("Apply force to object 2 as well.")]
    public bool PushObject2 = false;

    protected override void DoCreate()
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
      Handle = PhysicsApi.API.create_physics_force(
        obj.Handle, Other.Handle, Position1Ref.transform.position, 
        Position2Ref.transform.position, Force, PushObject2
      );
    }
    protected override void DoDestroy(IntPtr ptr)
    {
      PhysicsApi.API.destroy_physics_force(ptr);
      base.DoDestroy(ptr);
    }
  }
}