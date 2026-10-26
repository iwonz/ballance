using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Ballance2.Services;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using static BallancePhysics.PhysicsApi;

namespace BallancePhysics.Wapper
{
  [AddComponentMenu("BallancePhysics/PhysicsEnvironment")]
  [DefaultExecutionOrder(10)]
  [DisallowMultipleComponent]
  /// <summary>
  /// Component hosting the physics world.
  /// </summary>
  public class PhysicsEnvironment : MonoBehaviour
  {
    [Tooltip("World gravity; default (0, -9.8, 0). Changes after simulation starts have no effect.")]
    /// <summary>
    /// World gravity, default (0, -9.8, 0). Changes after simulation starts have no effect.
    /// </summary>
    /// <returns></returns>
    public Vector3 Gravity = new Vector3(0, -9.81f, 0);
    [Tooltip("Deactivate objects below this Y coordinate. Positive values disable this threshold.")]
    /// <summary>
    /// Automatically deactivate objects below this Y coordinate. Positive values disable the cutoff.
    /// </summary>
    /// <returns></returns>
    public float DePhysicsFall = -5000;
    [Tooltip("Simulation steps per second (10-100). Changes after simulation starts have no effect.")]
    /// <summary>
    /// Simulation frequency, 10-100 steps per second. Changes after simulation starts have no effect.
    /// </summary>
    public int SimulationRate = 66;
    [Tooltip("Simulation time multiplier. A value of 2.0 makes physics run twice as fast as the normal value of 1.0.")]
    /// <summary>
    /// Physics time multiplier: 2.0 makes objects fall twice as fast as the normal value of 1.0.
    /// </summary>
    public float TimeFactor = 1;
    [Tooltip("Delete all collision surfaces when the environment is destroyed.")]
    /// <summary>
    /// Delete all collision surfaces when the environment is destroyed.
    /// </summary>
    public bool DeleteAllSurfacesWhenDestroy = true;
    [Tooltip("Enable simulation.")]
    /// <summary>
    /// Enable simulation.
    /// </summary>
    public bool Simulate = true;
    [Tooltip("Create automatically.")]
    /// <summary>
    /// Create automatically.
    /// </summary>
    public bool AutoCreate = true;

    /// <summary>
    /// All physics environments.
    /// </summary>
    /// <typeparam name="int">Unity scene buildIndex.</typeparam>
    /// <typeparam name="PhysicsWorld"></typeparam>
    /// <returns></returns>
    public static Dictionary<int, PhysicsEnvironment> PhysicsWorlds { get; } = new Dictionary<int, PhysicsEnvironment>();
    /// <summary>
    /// Get the physics environment for the current scene.
    /// </summary>
    /// <returns>Null if the current scene has no physics environment.</returns>
    public static PhysicsEnvironment GetCurrentScensePhysicsWorld()
    {
      int currentScenseIndex = SceneManager.GetActiveScene().buildIndex;
      if (PhysicsWorlds.TryGetValue(currentScenseIndex, out var a))
        return a;
      return null;
    }

    /// <summary>
    /// Get the native pointer for this physics environment.
    /// </summary>
    /// <value></value>
    public IntPtr Handle { get; private set; } = IntPtr.Zero;

    private static readonly ErrorReportCallback callback = ReportPhysicsMessage;

    [AOT.MonoPInvokeCallback(typeof(ErrorReportCallback))]
    private static int ReportPhysicsMessage(int level, int len, IntPtr _msg)
    {
      string msg = Marshal.PtrToStringAnsi(_msg, len);
      if (level == sInfo)
        Debug.Log(msg);
      else if (level == sWarning)
        Debug.LogWarning(msg);
      else if (level == sError)
        Debug.LogError(msg);
      else 
        Debug.Log(msg);
      return 1;
    }

    private GameTimeMachine.GameTimeMachineTimeTicket fixedUpdateTicket = null;

    /// <summary>
    /// Create the physics environment manually.
    /// </summary>
    public void Create()
    {
      int currentScenseIndex = SceneManager.GetActiveScene().buildIndex;
      if (PhysicsWorlds.ContainsKey(currentScenseIndex))
        Debug.LogError("There can only one PhysicsWorld instance in a scense.");
      else if(Handle == IntPtr.Zero)
      {
        var layerNames = Resources.Load<PhysicsLayerNames>("BallancePhysicsLayerNames");
        if(layerNames == null)
          throw new Exception("BallancePhysicsLayerNames not found. Click menu in Assets to create it.");

        PhysicsWorlds.Add(currentScenseIndex, this);
        Handle = PhysicsApi.API.create_environment(Gravity, 1.0f / SimulationRate, -2147483647, layerNames.GetGroupFilterMasks(), Marshal.GetFunctionPointerForDelegate(callback));
      
        if (fixedUpdateTicket != null)
        {
          fixedUpdateTicket.Unregister();
          fixedUpdateTicket = null;
        }
        fixedUpdateTicket = GameManager.GameTimeMachine.RegisterFixedUpdate(_FixedUpdate, 15);
      }
    }

    private bool destroyLock = false;

    /// <summary>
    /// Whether the physics environment has been created.
    /// </summary>
    public bool IsCreated()
    {
      return Handle != IntPtr.Zero;
    }
    /// <summary>
    /// Destroy the physics environment manually.
    /// </summary>
    public void Destroy()
    {
      if (Handle != IntPtr.Zero)
      {
        if(fixedUpdateTicket != null)
        {
          fixedUpdateTicket.Unregister();
          fixedUpdateTicket = null;
        }

        Simulate = false;
        destroyLock = true;

        LinkedListNode<PhysicsObject> obj = objects.First;
        while (obj != null) {
          var bodyCurrent = obj.Value;
          if(bodyCurrent.IsPhysicalized) {
            obj.Value.UnPhysicalize(true);
            obj = obj.Next;
            continue;
          }
        }
        Debug.Log("Destroy: " + objects.Count + " PhysicsObject destroyed. ");
        objects.Clear();

        if(DeleteAllSurfacesWhenDestroy)
          PhysicsApi.API.delete_all_surfaces(Handle);
        PhysicsApi.API.destroy_environment(Handle);
        Handle = IntPtr.Zero;

        int currentScenseIndex = SceneManager.GetActiveScene().buildIndex;
        PhysicsWorlds.Remove(currentScenseIndex);

        destroyLock = false;
      }
    }

    private void Awake() { 
      StartCoroutine(LateCreate()); 
    }
    private void OnDestroy()
    {
      Destroy();
    }
    private IEnumerator LateCreate() {
      yield return new WaitForSeconds(0.02f);
      if(AutoCreate)
        Create();
    }

    /// <summary>
    /// Get the previous frame's simulation time in seconds.
    /// </summary>
    /// <value></value>
    public float PhysicsTime { get; private set; }
    /// <summary>
    /// Get the number of active physics objects.
    /// </summary>
    /// <value></value>
    public int PhysicsActiveBodies { get { return _PhysicsActiveBodies; } }
    /// <summary>
    /// Get the total number of physics objects.
    /// </summary>
    /// <value></value>
    public int PhysicsBodies { get; private set; }
    /// <summary>
    /// Get the current simulation time.
    /// </summary>
    /// <value></value>
    public double PhysicsSimuateTime { get { return _PhysicsSimuateTime; } }
    /// <summary>
    /// Get the number of objects receiving constant forces.
    /// </summary>
    /// <value></value>
    public int PhysicsConstantPushBodies { get { return _PhysicsConstantPushBodies; } }
    /// <summary>
    /// Get the number of objects collected after falling.
    /// </summary>
    /// <value></value>
    public int PhysicsFallCollectBodies { get { return _PhysicsFallCollectBodies; } }
    /// <summary>
    /// Get the number of static physics objects.
    /// </summary>
    /// <value></value>
    public int PhysicsFixedBodies { get { return _PhysicsFixedBodies; } }
    /// <summary>
    /// Get the number of updated physics objects.
    /// </summary>
    /// <value></value>
    public int PhysicsUpdateBodies { get { return _PhysicsUpdateBodies; } }
    /// <summary>
    /// Get the physics force scale.
    /// </summary>
    public float PhysicsFactorFinalValue = 1;

    private int _PhysicsActiveBodies = 0;
    private int _PhysicsConstantPushBodies = 0;
    private int _PhysicsFallCollectBodies = 0;
    private int _PhysicsFixedBodies = 0;
    private int _PhysicsUpdateBodies = 0;
    private double _PhysicsSimuateTime = 0;
    private bool lastPauseIsSimuate = false;
    private List<PhysicsObject> nextNeedUnFallCollectObjects = new List<PhysicsObject>();

    private void _FixedUpdate() {
      if(Simulate && Handle != IntPtr.Zero) {

        if(destroyLock) {
          Debug.Log("Bad state, no Simulate at destroy!");
          return;
        }

        Profiler.BeginSample("PhysicsEnvironmentUpdate");

        PhysicsFactorFinalValue = TimeFactor;//(Time.fixedDeltaTime / (1.0f / SimulationRate));
        
        // Set counters.

	      float startTime = Time.realtimeSinceStartup;
        PhysicsBodies = objects.Count;
        _PhysicsUpdateBodies = 0;
        _PhysicsConstantPushBodies = 0;
        _PhysicsFallCollectBodies = 0;
        _PhysicsFixedBodies = 0;

        // Update forces first.
        float[] dat = new float[4];
        LinkedListNode<PhysicsObject> obj = objects.First;
  
        // Simulate.
        Profiler.BeginSample("PhysicsEnvironmentSimulate");
        PhysicsApi.API.environment_simulate_dtime(Handle, /* Time.fixedDeltaTime */(1.0f / SimulationRate) * TimeFactor);
        Profiler.EndSample();

        PhysicsApi.API.do_update_all(Handle);
        // Read simulation statistics.
        PhysicsApi.API.get_stats(Handle, ref _PhysicsActiveBodies, ref _PhysicsSimuateTime);

        // Copy transforms back to C#.
        obj = objects.First;
        while (obj != null) {
          var bodyCurrent = obj.Value;
          if(bodyCurrent.Fixed) {
            obj = obj.Next;
            _PhysicsFixedBodies++;
            continue;
          }

          var t = bodyCurrent.gameObject.transform;
          IntPtr ptr = bodyCurrent.Handle; //pos 0
          ptr = IntPtr.Add(ptr, Marshal.SizeOf<int>()); //pos 1
          Marshal.Copy(ptr, dat, 0, 3);      //float[3]

          var p = new Vector3(dat[0], dat[1], dat[2]);
          t.position = p;

          ptr = IntPtr.Add(ptr, Marshal.SizeOf<float>() * 3); //pos 4
          Marshal.Copy(ptr, dat, 0, 4);      //float[4]

          t.rotation = new Quaternion(dat[0], dat[1], dat[2], dat[3]);
          _PhysicsUpdateBodies++;

          // Collect fallen objects.
          if(DePhysicsFall < 0 && p.y < DePhysicsFall) {
            //DePhysics and DeActive
            nextNeedUnFallCollectObjects.Add(bodyCurrent);
            _PhysicsFallCollectBodies++;
          }
          else if(bodyCurrent.EnableConstantForce) {
            bodyCurrent.DoApplyConstantForce();
            _PhysicsConstantPushBodies++;
          }

          if(GameTimeMachine.FixedUpdateTick % 10 == 0) 
            bodyCurrent.DoSendContractCacheEvent();

          obj = obj.Next;
        }

        Profiler.EndSample();

        // Update collision handlers.
        if(GameTimeMachine.FixedUpdateTick % 12 == 0) {
          Profiler.BeginSample("PhysicsEnvironmentContactEvent");
          PhysicsApi.API.do_update_all_physics_contact_detection(Handle);
          Profiler.EndSample();
        }
        // Objects to remove from physics after falling.
        if(nextNeedUnFallCollectObjects.Count > 0) {
          for (var i = nextNeedUnFallCollectObjects.Count - 1; i >= 0; i--)
          {
            var bodyCurrent = nextNeedUnFallCollectObjects[i];
            if (!bodyCurrent.CustomDePhysicsFall) {
              bodyCurrent.UnPhysicalize(true);
              bodyCurrent.gameObject.SetActive(false);
            }
            if (bodyCurrent.OnFallCallback != null)
              bodyCurrent.OnFallCallback.Invoke(bodyCurrent);
          }
          nextNeedUnFallCollectObjects.Clear();
        }

        // End time.
        PhysicsTime = Time.realtimeSinceStartup - startTime;
      }
    }

    private void OnEnable() {
      if(fixedUpdateTicket != null)
        fixedUpdateTicket.Enable();
    }
    private void OnDisable() {
      if(fixedUpdateTicket != null)
        fixedUpdateTicket.Disable();
    }

    public static void HandleEditorPause() {
      foreach(var world in PhysicsWorlds.Values) {
        world.lastPauseIsSimuate = world.Simulate;
        world.Simulate = false;
      }
    }
    public static void HandleEditorPlay() {
      foreach(var world in PhysicsWorlds.Values) {
        if(world.lastPauseIsSimuate) {
          world.Simulate = true;
          world.lastPauseIsSimuate = false;
        }
      }
    }

    private Dictionary<string, int> dictSystemGroup = new Dictionary<string, int>();
    private LinkedList<PhysicsObject> objects = new LinkedList<PhysicsObject>();

    /// <summary>
    /// Called automatically by PhysicsObject; do not call manually.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="body"></param>
    internal void AddPhysicsObject(int id, PhysicsObject body)
    {
      if(destroyLock) {
        Debug.Log("Bad state, not allow create PhysicsObject at destroy!");
        return;
      }

      objects.AddLast(body);
    }
    /// <summary>
    /// Called automatically by PhysicsObject; do not call manually.
    /// </summary>
    /// <param name="body"></param>
    internal void RemovePhysicsObject(PhysicsObject body)
    {
      if(!destroyLock) 
        objects.Remove(body);
    }

    /// <summary>
    /// Get a collision subgroup by name, creating it if necessary.
    /// </summary>
    /// <param name="name">Subgroup name.</param>
    /// <returns>Collision subgroup ID.</returns>
    public int GetSystemGroup(string name)
    {
      if (Handle == IntPtr.Zero || string.IsNullOrEmpty(name))
        return 0;
      if (dictSystemGroup.TryGetValue(name, out var i))
        return i;
      i = PhysicsApi.API.environment_new_system_group(Handle);
      dictSystemGroup[name] = i;
      return i;
    }
    /// <summary>
    /// Delete all collision surfaces in the physics system.
    /// </summary>
    public void DeleteAllSurfaces() { PhysicsApi.API.delete_all_surfaces(Handle); }
    /// <summary>
    /// Find a physics object in the world by ID.
    /// </summary>
    /// <param name="id">ID</param>
    /// <returns>Null if not found.</returns>
    public PhysicsObject GetObjectById(int id)
    {
      LinkedListNode<PhysicsObject> obj = objects.First;
      while (obj != null) {
        if(obj.Value.Id == id)
          return obj.Value;
        obj = obj.Next;
      }
      return null;
    }

    /// <summary>
    /// Cast a ray from the given position and return the first hit object.
    /// </summary>
    /// <param name="startPoint">Ray origin.</param>
    /// <param name="dirction">Ray direction vector.</param>
    /// <param name="rayLength">Ray length.</param>
    /// <param name="distance">Distance to the first hit object.</param>
    /// <returns>The first hit object, or null if nothing was hit.</returns>
    public PhysicsObject RaycastingOne(Vector3 startPoint, Vector3 dirction, float rayLength, out float distance)
    {
      if (Handle == IntPtr.Zero)
        throw new Exception("World not create or destroyed");

      var _distance = 0.0f;
      var id = PhysicsApi.API.raycasting_one(Handle, startPoint, dirction, rayLength, ref _distance);
      distance = _distance;
      return GetObjectById(id);
    }
    /// <summary>
    /// Cast a ray from the given position and test whether it hits the specified object.
    /// </summary>
    /// <param name="obj">Object to test.</param>
    /// <param name="startPoint">Ray origin.</param>
    /// <param name="dirction">Ray direction vector.</param>
    /// <param name="rayLength">Ray length.</param>
    /// <param name="distance">Distance to the first hit object.</param>
    /// <returns>True if the ray hits the object; otherwise false.</returns>
    public bool RaycastingObject(PhysicsObject obj, Vector3 startPoint, Vector3 dirction, float rayLength, out float distance)
    {
      if (Handle == IntPtr.Zero)
        throw new Exception("World not create or destroyed");
      if(obj.Handle == IntPtr.Zero)
        throw new Exception("Object " + obj.name + " is not physicalized");

      var _distance = 0.0f;
      var b = PhysicsApi.API.raycasting_object(obj.Handle, startPoint, dirction, rayLength, ref _distance);
      distance = _distance;
      return b;
    }

    /// <summary>
    /// Raycast flags.
    /// </summary>
    public enum RaySolverFlag
    {
      /// <summary>
      /// Hit all objects.
      /// </summary>
      All = 0,
      /// <summary>
      /// Hit phantoms.
      /// </summary>
      Phantoms = 1,
      /// <summary>
      /// Hit movable objects.
      /// </summary>
      Movings = 2,
      /// <summary>
      /// Hit immovable objects.
      /// </summary>
      Statics = 4
    }
    /// <summary>
    /// Raycast results.
    /// </summary>
    public struct RayCastResult
    {
      /// <summary>
      /// Hit objects.
      /// </summary>
      public List<PhysicsObject> HitObjects;

      /// <summary>
      /// Get the number of hit objects.
      /// </summary>
      /// <returns></returns>
      public int GetHitObjectsCount() { return HitObjects.Count; }
      /// <summary>
      /// Get a hit object by index.
      /// </summary>
      /// <param name="index">Index.</param>
      /// <returns></returns>
      public PhysicsObject GetHitObjectsAt(int index) { return HitObjects[index]; }

      /// <summary>
      /// Distances from the ray origin to hit objects.
      /// </summary>
      public List<float> HitDistances;

      
      /// <summary>
      /// Get the number of hit-distance entries.
      /// </summary>
      /// <returns></returns>
      public int GetHitDistancesCount() { return HitDistances.Count; }
      /// <summary>
      /// Get a hit distance by index.
      /// </summary>
      /// <param name="index">Index.</param>
      /// <returns></returns>
      public float GetHitDistancesAt(int index) { return HitDistances[index]; }
    }

    /// <summary>
    /// Cast a ray from the given position and get all hit objects.
    /// </summary>
    /// <param name="flags">Raycast flags.</param>
    /// <param name="startPoint">Ray origin.</param>
    /// <param name="dirction">Ray direction vector.</param>
    /// <param name="rayLength">Ray length.</param>
    /// <returns>Collision information.</returns>
    public RayCastResult Raycasting(RaySolverFlag flags, Vector3 startPoint, Vector3 dirction, float rayLength)
    {
      if (Handle == IntPtr.Zero)
        throw new Exception("World not create or destroyed");

      var result = PhysicsApi.API.raycasting(Handle, (int)flags, startPoint, dirction, rayLength);
      var rs = new RayCastResult();

      foreach(var o in result.hit_objects)
        rs.HitObjects.Add(GetObjectById(PhysicsApi.API.physics_get_id(o)));
      foreach(var o in result.hit_distances)
        rs.HitDistances.Add(o);

      return rs;
    }
  }
}
