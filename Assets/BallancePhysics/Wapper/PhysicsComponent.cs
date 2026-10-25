using System;
using UnityEngine;

namespace BallancePhysics.Wapper
{
  
  [RequireComponent(typeof(PhysicsObject))]
  
  public class PhysicsComponent : MonoBehaviour
  {
    /// <summary>
    /// Get the component handle, or IntPtr.Zero if the constraint has not been created.
    /// </summary>
    /// <value></value>
    public IntPtr Handle { get; protected set; } = IntPtr.Zero;

    protected virtual void DoCreate() {
    }
    protected virtual void DoDestroy(IntPtr ptr) {
      Handle = IntPtr.Zero;
    }
    protected virtual void Awake() {
      var obj = GetComponent<PhysicsObject>();
      if(obj.ComponentsCreated) 
        Create();
    }

    /// <summary>
    /// Create this component manually.
    /// </summary>
    
    public void Create() {
      if(Handle == IntPtr.Zero) 
        DoCreate();
    }
    /// <summary>
    /// Destroy this component manually.
    /// </summary>
    
    public void Destroy() {
      if(Handle != IntPtr.Zero) {
        DoDestroy(Handle);
        Handle = IntPtr.Zero;
      }
    }
  }
}