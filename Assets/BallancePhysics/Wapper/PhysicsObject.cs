using System;
using System.Collections.Generic;
using UnityEngine;
using BallancePhysics.Api;
using System.Runtime.InteropServices;
using System.Collections;
using System.Text;

namespace BallancePhysics.Wapper
{
  
  [AddComponentMenu("BallancePhysics/PhysicsObject")]
  [DefaultExecutionOrder(20)]
  [DisallowMultipleComponent]
  
  /// <summary>
  /// Ballance physics object component.
  /// </summary>
  public class PhysicsObject : MonoBehaviour
  {
    #region Properties

    [Tooltip("Object mass in kilograms.")]
    [SerializeField]
    private float m_Mass = 1.0f;
    [Tooltip("Friction coefficient: 0 means no friction, 0.1 resembles ice, and 3 is very rough. Contact friction is the product of the two objects' coefficients.")]
    [SerializeField]
    [Range(0, 3f)]
    private float m_Friction = 0.7f;
    [Tooltip("Elasticity. Values above 1.0 cause perpetual bouncing; values below 0.2 require extra CPU time. Unrealistically high values, such as 10, can destabilize the simulation.")]
    [SerializeField]
    [Range(0, 2f)]
    private float m_Elasticity = 0.4f;
    [Tooltip("Linear velocity damping, usable as air resistance. Zero leaves velocity unchanged. Too little damping can allow unrealistic acceleration; see native IVP damping behavior.")]
    [SerializeField]
    private float m_LinearSpeedDamping = 0.1f;
    [Tooltip("Angular velocity damping, usable as rotational air resistance. Zero leaves velocity unchanged. Too little damping can allow unrealistic acceleration; see native IVP damping behavior.")]
    [SerializeField]
    private float m_RotSpeedDamping = 0.1f;
    [Tooltip("Sphere collision radius.")]
    [SerializeField]
    private float m_BallRadius = 1;
    [Tooltip("Use a sphere collision shape.")]
    [SerializeField]
    private bool m_UseBall = false;
    [Tooltip("Generate an additional convex hull around the object. This can improve movable concave objects' performance but rarely helps large static objects that others constantly penetrate.")]
    [SerializeField]
    private bool m_BuildRootConvexHull = true;
    [Tooltip("Enable collision for this object.")]
    [SerializeField]
    private bool m_EnableCollision = true;
    [Tooltip("Start asleep until a collision, impulse, or attached spring wakes the object. Useful for objects already resting on a floor, avoiding unnecessary initial stabilization work.")]
    [SerializeField]
    private bool m_StartFrozen = false;
    [Tooltip("Treat the object as immovable; gravity and forces have no effect.")]
    [SerializeField]
    private bool m_Fixed = false;
    [Tooltip("Object center of mass.")]
    [SerializeField]
    private Vector3 m_ShiftMassCenter = Vector3.zero;
    [Tooltip("Handle falling through OnFallCallback instead of automatically removing the physics object.")]
    [SerializeField]
    public bool m_CustomDePhysicsFall = false;

    [Tooltip("Do not automatically create the physics body in Awake.")]
    [SerializeField]
    private bool m_DoNotAutoCreateAtAwake = true;
    [Tooltip("Calculate CenterOfMass automatically.")]
    [SerializeField]
    private bool m_AutoMassCenter = true;
    [Tooltip("Synchronize physics body activation with the GameObject.")]
    [SerializeField]
    private bool m_AutoControlActive = true;
    [Tooltip("Enable the constant force applied to this object.")]
    [SerializeField]
    private bool m_EnableConstantForce = true;
    [Tooltip("Object collision surface.")]
    [SerializeField]
    private int m_Layer = -1;
    [Tooltip("Collision group name; empty means unset. Call ForceUpdateCollisionFilterInfo after changing it on an existing object.")]
    [SerializeField]
    private string m_SystemGroupName = "";
    [Tooltip("Collision subgroup ID, unique within its group; default 0. Call ForceUpdateCollisionFilterInfo after changing it on an existing object.")]
    [SerializeField]
    private int m_SubSystemId = 0;
    [Tooltip("Subgroup excluded from collision; default 0. Call ForceUpdateCollisionFilterInfo after changing it on an existing object.")]
    [SerializeField]
    private int m_SubSystemDontCollideWith = 0;
    [Tooltip("Collision group ID used to filter collision events.")]
    [SerializeField]
    private int m_CollisionID = 0;

    [Tooltip("Convex collision mesh.")]
    [SerializeField]
    private List<Mesh> m_Convex = new List<Mesh>();
    [Tooltip("Concave collision mesh. It costs more simulation time; prefer several convex parts when practical.")]
    [SerializeField]
    private List<Mesh> m_Concave = new List<Mesh>();
    [Tooltip("Name of the collision surface generated from this mesh. Keep names unique unless intentionally reusing a surface.")]
    [SerializeField]
    private string m_SurfaceName = "";
    [Tooltip("Reuse an existing collision surface instead of creating a new one.")]
    [SerializeField]
    private bool m_UseExistsSurface = false;
    [SerializeField]
    private float m_ExtraRadius = 0.0f;
    [Tooltip("Enable gravity for this object.")]
    [SerializeField]
    private bool m_EnableGravity = true;
    [Tooltip("Enable collision events for this object.")]
    [SerializeField]
    private bool m_EnableCollisionEvent = false;
    [Tooltip("Minimum interval between collision callbacks, in seconds.")]
    [SerializeField]
    private float m_CollisionEventCallSleep = 0.5f;

    #endregion

    /// <summary>
    /// Get this object's unique ID.
    /// </summary>
    /// <value></value>
    public int Id { get; private set; } = 0;
    /// <summary>
    /// Get this object's native engine pointer.
    /// </summary>
    /// <value></value>
    public IntPtr Handle { get; private set; } = IntPtr.Zero;
    /// <summary>
    /// Friction coefficient: 0 is frictionless, 0.1 resembles ice, and 3 is very rough. Contact friction is the product of both objects' coefficients.
    /// </summary>
    /// <value></value>
    public float Friction { get => m_Friction; set => m_Friction = value; }
    /// <summary>
    /// Elasticity: values above 1.0 cause perpetual bouncing; values below 0.2 need extra CPU work. Unrealistic values such as 10 eventually destabilize simulation.
    /// </summary>
    /// <value></value>
    public float Elasticity { get => m_Elasticity; set => m_Elasticity = value; }
    /// <summary>
    /// Linear damping: 0.0 leaves translation unchanged; 1.0 reduces speed by 30% per second and 0.1 by 90%. This models air resistance; very low values may cause unrealistic acceleration.
    /// </summary>
    /// <value></value>
    public float LinearSpeedDamping { get => m_LinearSpeedDamping; set => m_LinearSpeedDamping = value; }
    /// <summary>
    /// Angular damping: 0.0 leaves rotation unchanged; 1.0 reduces speed by 30% per second and 0.1 by 90%. This models air resistance; very low values may cause unrealistic acceleration.
    /// </summary>
    /// <value></value>
    public float RotSpeedDamping { get => m_RotSpeedDamping; set => m_RotSpeedDamping = value; }
    /// <summary>
    /// Handle falling manually in OnFallCallback instead of automatically removing the object from physics.
    /// </summary>
    /// <value></value>
    public bool CustomDePhysicsFall { get => m_CustomDePhysicsFall; set => m_CustomDePhysicsFall = value; }
    /// <summary>
    /// Sphere collider radius.
    /// </summary>
    /// <value></value>
    public float BallRadius { get => m_BallRadius; set => m_BallRadius = value; }
    /// <summary>
    /// Use a sphere collider.
    /// </summary>
    /// <value></value>
    public bool UseBall { get => m_UseBall; set => m_UseBall = value; }
    /// <summary>
    /// Generate an extra convex hull around the object to speed up movable concave objects. This does not help large landscapes whose hulls are constantly penetrated.
    /// </summary>
    /// <value></value>
    public bool BuildRootConvexHull { get => m_BuildRootConvexHull; set => m_BuildRootConvexHull = value; }
    /// <summary>
    /// Enable collision for this object.
    /// </summary>
    /// <value></value>
    public bool EnableCollision { get => m_EnableCollision; set => m_EnableCollision = value; }
    /// <summary>
    /// Keep the object asleep until a collision, impulse, or newly attached spring wakes it. Useful when initializing many objects already resting on a floor.
    /// </summary>
    /// <value></value>
    public bool StartFrozen { get => m_StartFrozen; set => m_StartFrozen = value; }
    /// <summary>
    /// Center of mass.
    /// </summary>
    /// <value></value>
    public Vector3 ShiftMassCenter { get => m_ShiftMassCenter; set => m_ShiftMassCenter = value; }
    /// <summary>
    /// Skip automatic rigid-body creation in Awake.
    /// </summary>
    /// <value></value>
    public bool DoNotAutoCreateAtAwake { get => m_DoNotAutoCreateAtAwake; set => m_DoNotAutoCreateAtAwake = value; }
    /// <summary>
    /// Calculate CenterOfMass automatically.
    /// </summary>
    /// <value></value>
    public bool AutoMassCenter { get => m_AutoMassCenter; set => m_AutoMassCenter = value; }
    /// <summary>
    /// Synchronize rigid-body activation with GameObject activation.
    /// </summary>
    /// <value></value>
    public bool AutoControlActive { get => m_AutoControlActive; set => m_AutoControlActive = value; }
    /// <summary>
    /// Object collision layer.
    /// </summary>
    /// <value></value>
    public int Layer { get => m_Layer; set => m_Layer = value; }
    /// <summary>
    /// Collision group name; empty leaves it unset. After creation, call ForceUpdateCollisionFilterInfo to apply changes.
    /// </summary>
    /// <value></value>
    public string SystemGroupName { get => m_SystemGroupName; set => m_SystemGroupName = value; }
    /// <summary>
    /// Collision subgroup ID, unique within its group; default 0. After creation, call ForceUpdateCollisionFilterInfo to apply changes.
    /// </summary>
    /// <value></value> 
    public int SubSystemId { get => m_SubSystemId; set => m_SubSystemId = value; }
    /// <summary>
    /// Subgroup excluded from collisions; default 0. After creation, call ForceUpdateCollisionFilterInfo to apply changes.
    /// </summary>
    /// <value></value> 
    public int SubSystemDontCollideWith{ get => m_SubSystemDontCollideWith; set => m_SubSystemDontCollideWith = value; }
    /// <summary>
    /// Convex meshes for this object.
    /// </summary> 
    public List<Mesh> Convex => m_Convex;
    /// <summary>
    /// Concave meshes cost more simulation time; prefer several convex pieces where possible.
    /// </summary> 
    public List<Mesh> Concave => m_Concave;
    /// <summary>
    /// Collision surface name generated from this object's mesh. It must be unique unless intentionally reusing an existing surface.
    /// </summary>
    /// <value></value>   
    public string SurfaceName { get => m_SurfaceName; set => m_SurfaceName = value; }
    /// <summary>
    /// Reuse an existing collision surface instead of creating one.
    /// </summary>
    /// <value></value>
    public bool UseExistsSurface { get => m_UseExistsSurface; set => m_UseExistsSurface = value; }
    /// <summary>
    /// Extra phantom radius delaying the leave event until the object is outside the phantom plus this margin.
    /// </summary>
    /// <value></value>
    public float ExtraRadius { get => m_ExtraRadius; set => m_ExtraRadius = value; }
    /// <summary>
    /// Minimum interval between collision callbacks, in seconds.
    /// </summary>
    /// <value></value>
    public float CollisionEventCallSleep { get => m_CollisionEventCallSleep; set => m_CollisionEventCallSleep = value; }
    /// <summary>
    /// Custom collision group ID used by sound collision events.
    /// </summary>
    /// <value></value>
    public int CollisionID { 
      get => m_CollisionID; 
      set {
        m_CollisionID = value;
        if(IsPhysicalized)
          PhysicsApi.API.physics_set_col_id(Handle, m_CollisionID);
      }
    }

    private PhysicsEnvironment currentEnvironment = null;
    
    /// <summary>
    /// Create the object's native physics body.
    /// </summary>
    public void Physicalize()
    {
      if (Handle == IntPtr.Zero)
      {
        currentEnvironment = PhysicsEnvironment.GetCurrentScensePhysicsWorld();
        if (currentEnvironment == null)
        {
          Debug.LogWarning("[Physicalize:" + name + "] Not found PhysicsEnvironment, please add it");
          return;
        }

        string surfaceName = m_SurfaceName;
        bool doNotCreateSurface = false;

        if (m_UseBall)
          doNotCreateSurface = true; // Spheres do not need mesh generation.
        else if (surfaceName == "") {
          // Generate a name from the object's mesh.
          string scaleName = gameObject.transform.lossyScale != Vector3.one ? ("_" + gameObject.transform.lossyScale.ToString()) : "";
          if(m_Convex.Count == 1)
            surfaceName = m_Convex[0].name + scaleName;
          else if(m_Concave.Count == 1) 
            surfaceName = m_Concave[0].name + scaleName;
          else {
            if(m_Concave.Count == 0 && m_Convex.Count == 0) {
              var meshFilter = GetComponent<MeshFilter>();
              if(meshFilter != null && meshFilter.sharedMesh != null) {
                var mesh = meshFilter.sharedMesh;
                surfaceName = mesh.name + scaleName;
              }
            } else
              surfaceName = gameObject.name + scaleName;
          }
        } 
        else if(PhysicsApi.API.surface_exist_by_name(currentEnvironment.Handle, surfaceName) || m_UseExistsSurface) {
          // Require SurfaceName before generating a surface.
          Debug.LogWarning("Surface " + surfaceName + " already exist");
          doNotCreateSurface = true;
        }

        List<IntPtr> pConvexs = new List<IntPtr>();
        List<IntPtr> pConcavies = new List<IntPtr>();
        if (!doNotCreateSurface)
        {
          foreach (var mesh in m_Convex)
            pConvexs.Add(PhysicsApi.API.create_points_buffer(mesh.vertices, transform.lossyScale));
          foreach (var mesh in m_Concave)
            pConcavies.Add(PhysicsApi.API.create_polyhedron(mesh.vertices, mesh.triangles, transform.lossyScale));

          // If no sphere or explicit convex/concave mesh is configured, try to use the object's mesh.
          if(pConvexs.Count == 0 && pConcavies.Count == 0) {
            var meshFilter = GetComponent<MeshFilter>();
            if(meshFilter == null || meshFilter.sharedMesh == null) {
              Debug.LogWarning("Object " + name + " create failed, No mesh was provided");
              return;
            }
            var mesh = meshFilter.sharedMesh;
            //if(m_Fixed) 
            //  pConcavies.Add(PhysicsApi.API.create_polyhedron(mesh.vertices, mesh.triangles, transform.lossyScale));
            //else 
              pConvexs.Add(PhysicsApi.API.create_points_buffer(mesh.vertices, transform.lossyScale));
          }
        }

        Handle = PhysicsApi.API.physicalize(currentEnvironment.Handle, name, m_Layer, currentEnvironment.GetSystemGroup(m_SystemGroupName),
        m_SubSystemId, m_SubSystemDontCollideWith, m_Mass, m_Friction, m_Elasticity, m_LinearSpeedDamping,
        m_RotSpeedDamping, m_BallRadius, m_UseBall, m_BuildRootConvexHull, m_AutoMassCenter, m_EnableCollision, m_StartFrozen,
        m_Fixed, transform.position, m_ShiftMassCenter, transform.rotation, m_UseExistsSurface, surfaceName,
        pConvexs.Count, pConvexs, pConcavies.Count, pConcavies, m_ExtraRadius, m_CollisionID);
        
        foreach (var ptr in pConvexs)
          PhysicsApi.API.delete_points_buffer(ptr);
        foreach (var ptr in pConcavies)
          PhysicsApi.API.delete_points_buffer(ptr);

        if(Handle == IntPtr.Zero)
          return;

        Id = PhysicsApi.API.physics_get_id(Handle);
        callbackObjects.Add(Handle, this);

        currentEnvironment.AddPhysicsObject(Id, this);

        CreateComponents();

        EnableGravity = m_EnableGravity;
        EnableCollisionEvent = m_EnableCollisionEvent;
      }
    }
    /// <summary>
    /// Remove the object's native physics body.
    /// </summary>
    /// <param name="silently">Remove silently; otherwise nearby objects are activated.</param>
    public void UnPhysicalize(bool silently = true)
    {
      if (Handle != IntPtr.Zero)
      {
        DestroyComponents();
        Debug.Assert(currentEnvironment != null);
        PhysicsApi.API.unphysicalize(currentEnvironment.Handle, Handle, silently);
        callbackObjects.Remove(Handle);
        Handle = IntPtr.Zero;
        currentEnvironment.RemovePhysicsObject(this);
      }
    }
    /// <summary>
    /// Whether the object has a native physics body.
    /// </summary>
    /// <value></value>
    public bool IsPhysicalized { get => Handle != IntPtr.Zero; }

    /// <summary>
    /// Wake the object.
    /// </summary>
    public void WakeUp() {
      checkPhysicalized();
      PhysicsApi.API.physics_wakeup(Handle);
    }
    /// <summary>
    /// Freeze the object immediately, ignoring its velocity.
    /// </summary>
    /// <returns>True on success.</returns>
    public bool Freeze() {
      checkPhysicalized();
      return PhysicsApi.API.physics_freeze(Handle);
    } 

    /// <summary>
    /// Get the object's speed.
    /// </summary>
    /// <value></value>
    public float Speed
    {
      get
      {
        checkPhysicalized();
        return PhysicsApi.API.physics_get_speed(Handle);
      }
    }
    /// <summary>
    /// Get the object's velocity vector.
    /// </summary>
    /// <value></value>
    public Vector3 SpeedVector
    {
      get
      {
        checkPhysicalized();
        PhysicsApi.API.physics_get_speed_vec(Handle, out var v);
        return v;
      }
    }
    /// <summary>
    /// Get angular velocity in object coordinates.
    /// </summary>
    /// <value></value>
    public Vector3 RotSpeed
    {
      get
      {
        checkPhysicalized();
        PhysicsApi.API.physics_get_rot_speed(Handle, out var v);
        return v;
      }
    }
    /// <summary>
    /// Get or set object mass in kilograms.
    /// </summary>
    /// <value></value>
    public float Mass {
      get { return m_Mass; }
      set {
        m_Mass = value;
        if(IsPhysicalized)
          PhysicsApi.API.physics_change_mass(Handle, value);
      }
    } 
    /// <summary>
    /// Whether this is a static object.
    /// </summary>
    public bool Fixed {
      get {
        //if(IsPhysicalized)
        //  return PhysicsApi.API.physics_is_fixed(Handle);
        return m_Fixed;
      }
      set {
        m_Fixed = value;
        if(IsPhysicalized) 
          PhysicsApi.API.physics_change_unmovable_flag(Handle, value);
      }
    }
    /// <summary>
    /// Whether gravity is enabled for this object.
    /// </summary>
    public bool EnableGravity {
      get {
        if(IsPhysicalized) 
          return PhysicsApi.API.physics_is_gravity_enabled(Handle);
        return m_EnableGravity;
      }
      set {
        m_EnableGravity = value;
        if(IsPhysicalized) 
          PhysicsApi.API.physics_enable_gravity(Handle, m_EnableGravity);
      }
    }  

    #region Collisions

    /// <summary>
    /// Check whether this object collides with another object.
    /// </summary>
    /// <param name="other">Other object.</param>
    /// <returns>True on collision; otherwise false.</returns>
    public bool IsContact(PhysicsObject other) {
      if(other == null)
        throw new Exception("other is null!");
      checkPhysicalized();
      other.checkPhysicalized();
      return PhysicsApi.API.physics_is_contact(Handle, other.Handle);
    }
    /// <summary>
    /// Enable collision detection for this object.
    /// </summary>
    public void EnableCollisionDetection() {
      checkPhysicalized();
      PhysicsApi.API.physics_enable_collision_detection(Handle);
    }
    /// <summary>
    /// Disable collision detection for this object.
    /// </summary>
    public void DisableCollisionDetection() {
      checkPhysicalized();
      PhysicsApi.API.physics_enable_collision_detection(Handle);
    }
    /// <summary>
    /// Force an update of the object's collision filter information.
    /// </summary>
    public void ForceUpdateCollisionFilterInfo()
    {
      checkPhysicalized();
      PhysicsApi.API.physics_set_layer(
        currentEnvironment.Handle,
        Handle,
        m_Layer,
        currentEnvironment.GetSystemGroup(m_SystemGroupName),
        m_SubSystemId,
        m_SubSystemDontCollideWith);
    }  
    /// <summary>
    /// Get or set whether collision callbacks are enabled.
    /// </summary>
    public bool EnableCollisionEvent {
      get => m_EnableCollisionEvent;
      set {
        m_EnableCollisionEvent = value;
        if(IsPhysicalized) {
          if(m_EnableCollisionEvent) {
            collisionEventCallback = DispatchCollisionEvent;
            frictionEventCallback = DispatchFrictionEvent;
            PhysicsApi.API.physics_set_collision_listener(Handle, collisionEventCallback, m_CollisionEventCallSleep, frictionEventCallback);
          }
          else
            PhysicsApi.API.physics_remove_collision_listener(Handle);
        }
      }
    }  

    #endregion

    #region Transform updates

    /// <summary>
    /// Copy the Unity transform to the physics engine.
    /// </summary>
    /// <param name="optimize_for_repeated_calls">Set true for repeated calls, such as per-frame transform updates, to allow engine optimization.</param>
    public void BeamObjectToNewPosition(bool optimize_for_repeated_calls) {
      BeamObjectToNewPosition(transform.position, transform.rotation, optimize_for_repeated_calls);
    }
    /// <summary>
    /// Set the object's transform in the physics engine.
    /// </summary>
    /// <param name="pos">New world position.</param>
    /// <param name="rot">New rotation.</param>
    /// <param name="optimize_for_repeated_calls">Set true for repeated calls, such as per-frame transform updates, to allow engine optimization.</param>
    public void BeamObjectToNewPosition(Vector3 pos, Quaternion rot, bool optimize_for_repeated_calls) {
      checkPhysicalized();
      PhysicsApi.API.physics_beam_object_to_new_position(Handle, rot, pos, optimize_for_repeated_calls);
    }

    #endregion

    #region Impulses

    /// <summary>
    /// Enable constant forces acting on this object.
    /// </summary>
    /// <value></value>
    public bool EnableConstantForce { get => m_EnableConstantForce; set => m_EnableConstantForce = value; }

    private int m_ConstantForce_ID = 0;
    private Dictionary<int, PhysicsConstantForceData> m_ConstantForces = new Dictionary<int, PhysicsConstantForceData>();

    /// <summary>
    /// Add a constant force to this object.
    /// </summary>
    /// <param name="value">Force magnitude.</param>
    /// <param name="dircetion">Force direction.</param>
    /// <returns>Constant force ID; pass it to DeleteConstantForce to remove the force.</returns>
    public PhysicsConstantForceData AddConstantForce(float value, Vector3 dircetion) {
      return AddConstantForceWithPositionAndRef(value, dircetion, Vector3.zero, null, null);
    }
    /// <summary>
    /// Add a constant force using this object as the position reference.
    /// </summary>
    /// <param name="value">Force magnitude in newtons.</param>
    /// <param name="dircetion">Force direction.</param>
    /// <returns>Constant force ID; pass it to DeleteConstantForce to remove the force.</returns>
    public PhysicsConstantForceData AddConstantForceLocalCenter(float value, Vector3 dircetion) {
      return AddConstantForceWithPositionAndRef(value, dircetion, Vector3.zero, null, transform);
    }
    /// <summary>
    /// Add a constant force to this object.
    /// </summary>
    /// <param name="value">Force magnitude in newtons.</param>
    /// <param name="dircetion">Force direction.</param>
    /// <param name="postion">Force application position.</param>
    /// <returns>Constant force ID; pass it to DeleteConstantForce to remove the force.</returns>
    public PhysicsConstantForceData AddConstantForceWithPosition(float value, Vector3 dircetion, Vector3 postion) {
      return AddConstantForceWithPositionAndRef(value, dircetion, postion, null, null);
    }
    /// <summary>
    /// Add a constant force to this object.
    /// </summary>
    /// <param name="value">Force magnitude in newtons.</param>
    /// <param name="dircetion">Force direction.</param>
    /// <param name="postion">Force application position.</param>
    /// <param name="directionRef">Object providing the direction reference.</param>
    /// <param name="positionRef">Object providing the position reference.</param>
    /// <returns>Constant force ID; pass it to DeleteConstantForce to remove the force.</returns>
    public PhysicsConstantForceData AddConstantForceWithPositionAndRef(float value, Vector3 dircetion, Vector3 postion, Transform directionRef, Transform positionRef) {
      if(m_ConstantForce_ID < int.MaxValue - 1) m_ConstantForce_ID++;
      else m_ConstantForce_ID = 0;

      PhysicsConstantForceData data = new PhysicsConstantForceData(this, m_ConstantForce_ID);
      data.Force = value;
      data.Direction = dircetion;
      data.Pos = postion;
      data.PositionRef = positionRef;
      data.DirectionRef = directionRef;

      m_ConstantForces.Add(m_ConstantForce_ID, data);
      return data;
    }
    /// <summary>
    /// Get a constant force by ID.
    /// </summary>
    /// <param name="forceId">Constant force ID.</param>
    /// <returns>The constant force, or null if not found.</returns>
    public PhysicsConstantForceData GetConstantForceByID(int forceId) {
      if(m_ConstantForces.TryGetValue(forceId, out var c)) 
        return c;
      return null;
    }
    /// <summary>
    /// Remove a constant force from this object.
    /// </summary>
    /// <param name="forceId">ID returned by AddConstantForce.</param>
    public void DeleteConstantForce(int forceId) {
      m_ConstantForces.Remove(forceId);
    }
    /// <summary>
    /// Remove all constant forces from this object.
    /// </summary>
    public void ClearConstantForce() {
      m_ConstantForces.Clear();
      m_ConstantForce_ID = 0;
    }

    /// <summary>
    /// Apply constant forces.
    /// </summary>
    internal void DoApplyConstantForce() {
      Vector3 finalPos, finalForce;
      foreach(var f in m_ConstantForces) 
      {
        PhysicsConstantForceData data = f.Value;
        if(data.Force == 0)
          continue;

        // Set the reference object.
        if(data.DirectionRef != null)
          finalForce = data.DirectionRef.TransformDirection(data.Direction) * data.Force;
        else
          finalForce = data.Force * data.Direction;

        if(data.PositionRef != null)
          finalPos = data.PositionRef.TransformPoint(data.Pos);
        else
          finalPos = data.Pos;

        Impluse(finalPos, finalForce);
      }
    }

    /// <summary>
    /// Apply an impulse at the object's center.
    /// </summary>
    /// <param name="impluse">Impulse direction and magnitude in world coordinates.</param>
    public void Impluse(Vector3 impluse) {
      Impluse(transform.position, impluse);
    }

    /// <summary>
    /// Apply an impulse to the object.
    /// </summary>
    /// <param name="pos">Impulse position in world coordinates.</param>
    /// <param name="impluse">Impulse direction and magnitude in world coordinates.</param>
    public void Impluse(Vector3 pos, Vector3 impluse) {
      checkPhysicalized();
      if(impluse.sqrMagnitude > 0)
        PhysicsApi.API.physics_impluse(Handle, pos, impluse * currentEnvironment.PhysicsFactorFinalValue);
    }
    /// <summary>
    /// Apply an angular impulse to the object.
    /// </summary>
    /// <param name="rotVec">Each vector component is the impulse around the corresponding object-core axis.</param>
    public void Torque(Vector3 rotVec) {
      checkPhysicalized();
      if(rotVec.sqrMagnitude > 0) {
        PhysicsApi.API.physics_torque(Handle, rotVec * currentEnvironment.PhysicsFactorFinalValue);
      }
    }
    /// <summary>
    /// Add velocity to the object.
    /// </summary>
    /// <param name="speed">Velocity in world coordinates.</param>
    public void AddSpeed(Vector3 speed) {
      checkPhysicalized(); 
      PhysicsApi.API.physics_add_speed(Handle, speed);
    }
  
    #endregion

    #region Phantoms

    /// <summary>
    /// Convert the physics object to a phantom volume.
    /// </summary>
    public void ConvertToPhantom() {
      if(IsPhantom()) 
        Debug.LogWarning("Object " + name + " is already phantom");
      else {
        phantomEventCallback = DispatchPhantomEvent;
        PhysicsApi.API.physics_convert_to_phantom(Handle, m_ExtraRadius, phantomEventCallback);
      }
    }
    /// <summary>
    /// Whether this object is a phantom.
    /// </summary>
    public bool IsPhantom() {
      checkPhysicalized();
      return PhysicsApi.API.physics_is_phantom(Handle);
    }
    /// <summary>
    /// Check whether another object is inside this phantom.
    /// </summary>
    /// <param name="other">Other object.</param>
    /// <returns></returns>
    public bool IsInsidePhantom(PhysicsObject other) {
      if(other == null)
        throw new Exception("other is null!");
      checkPhysicalized();
      other.checkPhysicalized();
      return PhysicsApi.API.physics_is_inside_phantom(Handle, other.Handle);
    }
    
    #endregion

    #region Motion controller

    /// <summary>
    /// Whether the motion controller is enabled.
    /// </summary>
    /// <returns></returns>
    public bool IsMotionEnabled() {
      checkPhysicalized();
      return PhysicsApi.API.physics_is_motion_enabled(Handle);
    }
    /// <summary>
    /// Set the motion controller's enabled state.
    /// </summary>
    /// <param name="eanble">Whether to enable it.</param>
    public void SetMotionEnabled(bool eanble) {
      checkPhysicalized();
      PhysicsApi.API.physics_enable_motion(Handle, eanble);
    }

    #endregion

    #region Other helpers

    /// <summary>
    /// Get an object ID from its native engine pointer.
    /// </summary>
    /// <param name="handle">Native engine pointer.</param>
    /// <returns>Zero if lookup fails.</returns>
    public static int GetIdByHandle(IntPtr handle) {
      return PhysicsApi.API.physics_get_id(handle);
    }

    private void checkPhysicalized()
    {
      if (!IsPhysicalized)
        throw new Exception("Object" + name + " not physicalized");
    }

    #endregion

    #region Events

    public delegate void OnPhysicsPhantomEventCallback(PhysicsObject self, PhysicsObject other);
    public delegate void OnPhysicsCollisionEventCallback(PhysicsObject self, PhysicsObject other, Vector3 contact_point_ws, Vector3 speed, Vector3 surf_normal);
    public delegate void OnPhysicsFrictionEventCallback(PhysicsObject self, PhysicsObject other, IntPtr friction_handle, Vector3 contact_point_ws, Vector3 speed, Vector3 surf_normal);
    public delegate void OnPhysicsCallback(PhysicsObject self);
    
    public delegate void OnPhysicsCollDetectionEventCallback(PhysicsObject self, int col_id, float speed_precent);
    public delegate void OnPhysicsContactEventCallback(PhysicsObject self, int col_id);

    /// <summary>
    /// Object entered the phantom.
    /// </summary>
    [HideInInspector]
    public OnPhysicsPhantomEventCallback OnPhantomEnter;
    /// <summary>
    /// Object left the phantom.
    /// </summary>
    public OnPhysicsPhantomEventCallback OnPhantomLeave;
    /// <summary>
    /// Physics collision event.
    /// </summary>
    [HideInInspector]
    public OnPhysicsCollisionEventCallback OnPhysicsCollision;
    /// <summary>
    /// Friction contact created.
    /// </summary>
    [HideInInspector]
    public OnPhysicsFrictionEventCallback OnPhysicsFrictionCreated;
    /// <summary>
    /// Friction contact deleted.
    /// </summary>
    [HideInInspector]
    public OnPhysicsFrictionEventCallback OnPhysicsFrictionDeleted;
    /// <summary>
    /// Physics tool collision event.
    /// </summary>
    [HideInInspector]
    public OnPhysicsCollDetectionEventCallback OnPhysicsCollDetection;
    /// <summary>
    /// Object began touching another object.
    /// </summary>
    [HideInInspector]
    public OnPhysicsContactEventCallback OnPhysicsContactOn;
    /// <summary>
    /// Object stopped touching another object.
    /// </summary>
    [HideInInspector]
    public OnPhysicsContactEventCallback OnPhysicsContactOff;
    /// <summary>
    /// Object fell below the collection threshold.
    /// </summary>
    [HideInInspector]
    public OnPhysicsCallback OnFallCallback;



    #endregion

    #region Collision handling

    private PhantomEventCallback phantomEventCallback = null;
    private CollisionEventCallback collisionEventCallback = null;
    private FrictionEventCallback frictionEventCallback = null;
    private ContractEventCallback contractEventCallback = null;

    // IL2CPP can pass only static, explicitly annotated callbacks to native code.
    private static readonly Dictionary<IntPtr, PhysicsObject> callbackObjects = new Dictionary<IntPtr, PhysicsObject>();

    [AOT.MonoPInvokeCallback(typeof(CollisionEventCallback))]
    private static void DispatchCollisionEvent(IntPtr self, IntPtr other, IntPtr point, IntPtr speed, IntPtr normal) {
      if (callbackObjects.TryGetValue(self, out var body))
        body.OnCollisionEvent(self, other, point, speed, normal);
    }

    [AOT.MonoPInvokeCallback(typeof(FrictionEventCallback))]
    private static void DispatchFrictionEvent(int create, IntPtr self, IntPtr other, IntPtr friction, IntPtr point, IntPtr speed, IntPtr normal) {
      if (callbackObjects.TryGetValue(self, out var body))
        body.OnFrictionEvent(create, self, other, friction, point, speed, normal);
    }

    [AOT.MonoPInvokeCallback(typeof(PhantomEventCallback))]
    private static void DispatchPhantomEvent(int enter, IntPtr self, IntPtr other) {
      if (callbackObjects.TryGetValue(self, out var body))
        body.OnPhantomEvent(enter, self, other);
    }

    [AOT.MonoPInvokeCallback(typeof(ContractEventCallback))]
    private static void DispatchContractEvent(IntPtr self, int id, short type, float speed, short on) {
      if (callbackObjects.TryGetValue(self, out var body))
        body.OnContractEvent(self, id, type, speed, on);
    }
    
    private Dictionary<int, bool> contractStateCache = new Dictionary<int, bool>();
    private Dictionary<int, bool> contractStateCache2 = new Dictionary<int, bool>();

    private void OnCollisionEvent(IntPtr self, IntPtr other, IntPtr contact_point_ws, IntPtr speed, IntPtr surf_normal) {
      if(self == Handle && OnPhysicsCollision != null) {
        var id = GetIdByHandle(other);
        var obj = currentEnvironment.GetObjectById(GetIdByHandle(other));
        OnPhysicsCollision.Invoke(
          this, 
          obj,
          PhysicsApi.API.ptr_to_vec3(contact_point_ws),
          PhysicsApi.API.ptr_to_vec3(speed),
          PhysicsApi.API.ptr_to_vec3(surf_normal)
        );
      }
    }
    private void OnFrictionEvent(int create, IntPtr self, IntPtr other, IntPtr friction_handle, IntPtr contact_point_ws, IntPtr speed, IntPtr surf_normal) {
      if(self == Handle) {
        var obj = currentEnvironment.GetObjectById(GetIdByHandle(other));
        var vspeed = PhysicsApi.API.ptr_to_vec3(speed);
        if(create > 0) {
          // Friction events.
          if(OnPhysicsFrictionCreated != null) {
            OnPhysicsFrictionCreated.Invoke(
              this, 
              obj,
              friction_handle,
              PhysicsApi.API.ptr_to_vec3(contact_point_ws),
              vspeed,
              PhysicsApi.API.ptr_to_vec3(surf_normal)
            );
          } 

        } else {
          // Friction events.
          if(OnPhysicsFrictionDeleted != null) {
            OnPhysicsFrictionDeleted.Invoke(
              this, 
              obj,
              friction_handle,
              PhysicsApi.API.ptr_to_vec3(contact_point_ws),
              vspeed,
              PhysicsApi.API.ptr_to_vec3(surf_normal)
            );
          }
        }
      }
    }
    private void OnPhantomEvent(int enter, IntPtr self, IntPtr other) {
      if(self == Handle) {
        if(enter > 0)
          OnPhantomEnter?.Invoke(this, currentEnvironment.GetObjectById(GetIdByHandle(other)));
        else 
          OnPhantomLeave?.Invoke(this, currentEnvironment.GetObjectById(GetIdByHandle(other)));
      }
    }
    private void OnContractEvent(IntPtr self, int col_id, short type, float speed_precent, short isOn) {
      if(type == 1)
        OnPhysicsCollDetection?.Invoke(this, col_id, speed_precent);
      else if(type == 2) {
        if(isOn == 1) {
          contractStateCache[col_id] = true;
          contractStateCache2[col_id] = true;
        }
        else {
          contractStateCache[col_id] = false;
          contractStateCache2[col_id] = false;
        }
      }
    }

    internal void DoSendContractCacheEvent() {
      foreach(var item in contractStateCache)
        (item.Value ? OnPhysicsContactOn : OnPhysicsContactOff).Invoke(this, item.Key);
      contractStateCache.Clear();
    }

    public string ContractCacheString {
      get {
        StringBuilder sb = new StringBuilder();
        foreach(var item in contractStateCache2) {
          if(item.Value) {
            sb.Append("[");
            sb.Append(item.Key);
            sb.Append("]");
          }
        }
        return sb.ToString();
      }
    }

    /// <summary>
    /// Enable touch-tool events for this object.
    /// </summary>
    public void EnableContractEventCallback() {
      contractEventCallback = DispatchContractEvent;
      PhysicsApi.API.physics_set_contract_listener(Handle, Marshal.GetFunctionPointerForDelegate(contractEventCallback));
    }
    /// <summary>
    /// Disable touch-tool events for this object.
    /// </summary>
    public void DisableContractEventCallback() {
      PhysicsApi.API.physics_disable_collision_detection(Handle);
      contractEventCallback = null;
    }

    private Dictionary<int, IntPtr> onCollDetection = new Dictionary<int, IntPtr>();
    private Dictionary<int, IntPtr> onContractDetection = new Dictionary<int, IntPtr>();

    /// <summary>
    /// Add a collision-tool handler for a layer.
    /// </summary>
    /// <param name="col_id">Custom collision layer ID.</param>
    /// <param name="min_speed">Minimum speed in m/s.</param>
    /// <param name="max_speed">Maximum speed in m/s.</param>
    /// <param name="sleep_afterwards">Cooldown in seconds during which the collision cannot trigger again.</param>
    /// <param name="speed_threadhold">Speed change threshold in m/s.</param>
    public void AddCollDetection(int col_id, float min_speed, float max_speed, float sleep_afterwards, float speed_threadhold) {
      if(onCollDetection.ContainsKey(col_id))
        return;
      var ptr = PhysicsApi.API.physics_coll_detection(Handle, col_id, min_speed, max_speed, sleep_afterwards, speed_threadhold);
      onCollDetection.Add(col_id, ptr);
    }
    /// <summary>
    /// Remove a collision-tool handler for a layer.
    /// </summary>
    /// <param name="col_id">Custom collision layer ID.</param>
    public void DeleteCollDetection(int col_id) {
      if(onCollDetection.ContainsKey(col_id)) {
        PhysicsApi.API.destroy_physics_coll_detection(onCollDetection[col_id]);
        onCollDetection.Remove(col_id);
      }
    }
    /// <summary>
    /// Remove all collision-tool handlers.
    /// </summary>
    public void DeleteAllCollDetection() {
      foreach(var coll in onCollDetection)
        PhysicsApi.API.destroy_physics_coll_detection(coll.Value);
      onCollDetection.Clear();
    }

    /// <summary>
    /// Add a touch-tool handler for a layer.
    /// </summary>
    /// <param name="col_id">Layer receiving the touch-tool handler.</param>
    /// <param name="time_delay_start">Delay before the touch-start event, in seconds.</param>
    /// <param name="time_delay_end">Delay before the touch-end event, in seconds.</param>
    public void AddContractDetection(int col_id, float time_delay_start, float time_delay_end) {
      if(onContractDetection.ContainsKey(col_id))
        return;
      var ptr = PhysicsApi.API.physics_contract_detection(Handle, col_id, time_delay_start, time_delay_end);
      onContractDetection.Add(col_id, ptr);
    }
    /// <summary>
    /// Remove a touch-tool handler for a layer.
    /// </summary>
    /// <param name="col_id">Custom collision layer ID.</param>
    public void DeleteContractDetection(int col_id) {
      if(onContractDetection.ContainsKey(col_id)) {
        PhysicsApi.API.destroy_physics_contract_detection(onContractDetection[col_id]);
        onContractDetection.Remove(col_id);
      }
    }
    /// <summary>
    /// Remove all touch-tool handlers.
    /// </summary>
    public void DeleteAllContractDetection() {
      foreach(var coll in onContractDetection)
        PhysicsApi.API.destroy_physics_contract_detection(coll.Value);
      onContractDetection.Clear();
    }

    #endregion

    #region Constraints, forces, and springs

    public bool ComponentsCreated { get; private set; }

    private List<PhysicsComponent> thisObjectComponents = new List<PhysicsComponent>();
    private List<PhysicsComponent> pendingCreateComponents = new List<PhysicsComponent>();

    private void DestroyComponents() {
      ComponentsCreated = false;
      foreach(var c in thisObjectComponents)
        c.Destroy();
    }
    private void CreateComponents() {
      var c = new List<PhysicsComponent>(GetComponents<PhysicsComponent>());
      thisObjectComponents.Clear();
      c.ForEach((a) => thisObjectComponents.Add(a));
      pendingCreateComponents.ForEach((e) => c.Add(e));
      pendingCreateComponents.Clear();

      foreach(var e in c)
        e.Create();

      ComponentsCreated = true;
    }
    
    
    public void AddPendCreateComponent(PhysicsComponent c) { pendingCreateComponents.Add(c); }

    #endregion

    private void OnDestroy() {
      m_ConstantForces.Clear();
      if(IsPhysicalized)
        UnPhysicalize(true);
    }
    private void Awake() {
      if(!m_DoNotAutoCreateAtAwake) 
        StartCoroutine(DelayCreate());
    }
    private IEnumerator DelayCreate() {
      yield return new WaitForSeconds(0.1f);
      Physicalize();
    }
  }

  /// <summary>
  /// Constant force data.
  /// </summary>
  public class PhysicsConstantForceData {
    /// <summary>
    /// Constant force position.
    /// </summary>
    public Vector3 Pos;
    /// <summary>
    /// Constant force ID.
    /// </summary>
    public int ID;
    /// <summary>
    /// Constant force magnitude.
    /// </summary>
    public float Force;
    /// <summary>
    /// Constant force direction.
    /// </summary>
    public Vector3 Direction;
    /// <summary>
    /// Direction reference object for the constant force.
    /// </summary>
    public Transform DirectionRef;
    /// <summary>
    /// Position reference object for the constant force.
    /// </summary>
    public Transform PositionRef;

    private PhysicsObject obj;

    internal PhysicsConstantForceData(PhysicsObject obj, int id) {
      this.obj = obj;
      this.ID = id;
    }

    /// <summary>
    /// Remove this constant force.
    /// </summary>
    public void Delete() {
      obj.DeleteConstantForce(ID);
    }
  };
}
