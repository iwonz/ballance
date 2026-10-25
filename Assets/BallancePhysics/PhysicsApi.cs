using System;
using System.Runtime.InteropServices;
using BallancePhysics.Api;
using BallancePhysics.Wapper;
using System.Collections.Generic;
using static BallancePhysics.Api.ApiStruct;
using UnityEngine;

namespace BallancePhysics
{
  public static class PhysicsApi
  {
    #region Basic definitions

#if UNITY_EDITOR && UNITY_EDITOR_WIN
    private const string DLL_NNAME = "bphysics_unity";
#elif UNITY_EDITOR
    private const string DLL_NNAME = "bphysics";
#elif UNITY_IOS || UNITY_WEBGL
    private const string DLL_NNAME = "__Internal";
#else
    private const string DLL_NNAME = "bphysics";
#endif

    public const int sError = 0;
    public const int sWarning = 1;
    public const int sInfo = 2;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int ErrorReportCallback(int level, int len, IntPtr msg);

    public delegate void InitFinishCallback();

    [DllImport(DLL_NNAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr ballance_physics_entry(int init, IntPtr options);

    public static InitFinishCallback InitFinish;

    public const int sTrue = 1;
    public const int sFalse = 0;

    public static int boolToSbool(bool b) {
      return b ? sTrue : sFalse;
    }

    #endregion

    /// <summary>
    /// Native API entry points.
    /// </summary>
    /// <returns></returns>
    public static ApiStruct API { get; } = new ApiStruct();
    /// <summary>
    /// Initialization key.
    /// </summary>
    /// <value></value>
    public static string SecretKey { get; set; }

    public static void PhysicsApiInit()
    {
      Debug.Log("PhysicsApiInit");

      IntPtr apiStructPtr = IntPtr.Zero;

      // Check whether initialization has already completed.
      if(ballance_physics_entry(4, IntPtr.Zero).ToInt64() == 1) {
        
        // Recreate function pointers after initialization because Unity Editor releases them between runs.
        apiStructPtr = ballance_physics_entry(5, IntPtr.Zero);

        // Resolve all function pointers.
        API.initAll(apiStructPtr, 256);

      } else {

        // Copy the initial configuration structure.
        sInitStruct initStruct = new sInitStruct();
        initStruct.smallPoolSize = PhysicsOptions.Instance.SmallPoolSize;
#if UNITY_EDITOR
        initStruct.showConsole = boolToSbool(PhysicsOptions.Instance.ShowConsole);
#else
        initStruct.showConsole = boolToSbool(false);
#endif
        initStruct.key = SecretKey;

        IntPtr initStructPtr = Marshal.AllocHGlobal(Marshal.SizeOf<sInitStruct>());
        Marshal.StructureToPtr(initStruct, initStructPtr, false);

        // Initialize the native engine.
        apiStructPtr = ballance_physics_entry(sTrue, initStructPtr);

        // Resolve all function pointers.
        API.initAll(apiStructPtr, 256);
        
        // Release resources.
        Marshal.FreeHGlobal(initStructPtr);
      }
    }
    public static void PhysicsApiDestroy()
    {
      Debug.Log("PhysicsApiDestroy");
      // Contact deletion callbacks still use the native vector pool during world cleanup.
      foreach (var world in new List<PhysicsEnvironment>(PhysicsEnvironment.PhysicsWorlds.Values))
        if (world != null) world.Destroy();
      PhysicsEnvironment.PhysicsWorlds.Clear();
      ballance_physics_entry(sFalse, IntPtr.Zero);
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Ballance/Physics/Open Console")]
    public static void OpenConsole() {
      ballance_physics_entry(3, IntPtr.Zero);
    }
    [UnityEditor.MenuItem("Ballance/Physics/Close Console")]
    public static void CloseConsole() {
      ballance_physics_entry(2, IntPtr.Zero);
    }
#endif
  }
}
