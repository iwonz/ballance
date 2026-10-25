using System;
using UnityEngine;

namespace BallancePhysics.Wapper
{
  
  [AddComponentMenu("BallancePhysics/PhysicsSpring")]
  
  public class PhysicsSpring : PhysicsComponent
  {
    [Tooltip("Reference point on object 1.")]
    
    /// <summary>
    /// Reference point on object 1.
    /// </summary>
    public Transform Position1Ref;
    
    [Tooltip("Reference point on object 2.")]
    /// <summary>
    /// Reference point on object 2.
    /// </summary>
    public Transform Position2Ref;
    
    [Tooltip("The other object to connect.")]
    /// <summary>
    /// The other connected object.
    /// </summary>
    public PhysicsObject Other = null;
     
    [Tooltip("Spring rest length.")]
    
    /// <summary>
    /// Spring length at rest.
    /// </summary>
    public float length = 1;
    
    [Tooltip("Spring stiffness. Stiff springs: 0.001 (very soft) to 1 (very hard). Normal springs: nonnegative newtons per meter; 0 is very soft, 10 is hard.")]
    /// <summary>
    /// Spring stiffness: stiff springs use [0.001, 1], from very soft to very hard. Ordinary springs use [0, infinity) N/m; 0 is very soft and 10 is hard.
    /// </summary>
    public float constant = 0.002f;
    
    [Tooltip("Spring damping. Stiff springs: 0-1, preferably close to stiffness (e.g. stiffness 0.1, damping 0.05-0.2). Normal springs: any nonnegative value.")]
    /// <summary>
    /// Constant spring damping: stiff springs use [0, 1], preferably near the stiffness value (for stiffness 0.1, damping 0.05-0.2). Ordinary springs use [0, infinity).
    /// </summary>
    public float spring_damping = 0.1f;
    
    [Tooltip("Damping including spring rotation. Not implemented for stiff springs. For normal springs, this can reduce the number of springs needed.")]
    /// <summary>
    /// Damping including spring rotation. Not implemented for stiff springs; for ordinary springs it can reduce the number of springs needed.
    /// </summary>
    public float global_damping = 0.1f;
    
    [Tooltip("Create a stiff spring. Use stiffness 0.001-1 and damping 0-1. It avoids normal spring integration instability and can be made harder than a normal spring.")]
    /// <summary>
    /// Create a stiff spring: stiffness [0.001, 1], damping [0, 1]. This avoids integrator instability and allows greater stiffness than an ordinary spring.
    /// </summary>
    public bool UseStiffSpring = false;
    
    [Tooltip("Set this to TRUE, if spring values should be multiplied with the average virtual mass of both objects")]
    /// <summary>
    /// Set this to TRUE, if spring values should be multiplied with the average virtual mass of both objects
    /// </summary>
    public bool ValuesAreRelative = false;
    
    [Tooltip("Set this to TRUE, if spring values should only be applied when the length exceeds spring_len")]
    /// <summary>
    /// Set this to TRUE, if spring values should only be applied when the length exceeds spring_len
    /// </summary>
    public bool ForceOnlyOnStretch = false;

    protected override void DoCreate()
    {
      var obj = GetComponent<PhysicsObject>();
      if(!obj.IsPhysicalized)
        return;
      if(Other != null && !Other.IsPhysicalized) {
        Other.AddPendCreateComponent(this);
        return;
      }
      Handle = PhysicsApi.API.create_physics_spring(
        obj.Handle, Other != null ? Other.Handle : IntPtr.Zero, Position1Ref.transform.position, Position2Ref.transform.position, length, constant, spring_damping, 
        global_damping, UseStiffSpring, ValuesAreRelative, ForceOnlyOnStretch
      );
    }
    protected override void DoDestroy(IntPtr ptr)
    {
      PhysicsApi.API.destroy_physics_force(ptr);
      base.DoDestroy(ptr);
    }
  }
}