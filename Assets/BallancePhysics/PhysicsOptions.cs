
using UnityEditor;
using UnityEngine;

namespace BallancePhysics
{
  /// <summary>
  /// Physics engine configuration.
  /// </summary>
  public class PhysicsOptions : ScriptableObject
  {
    [Tooltip("Show the console window (Windows only).")]
    /// <summary>
    /// Show the console window (Windows only).
    /// </summary>
    public bool ShowConsole = false;
    [Tooltip("")]
    [Range(8, 64)]
    /// <summary>
    /// Size of the small memory pool.
    /// </summary>
    public int SmallPoolSize = 16;

    #region Base

    private static PhysicsOptions _instance = null;

    public static PhysicsOptions Instance
    {
      get
      {
        if (_instance == null)
        {
          _instance = Resources.Load<PhysicsOptions>("BallancePhysicsOptions");
          if (_instance == null)
          {
#if UNITY_EDITOR
            _instance = PhysicsOptions.CreateInstance<PhysicsOptions>();
            try
            {
              AssetDatabase.CreateAsset(_instance, "Assets/Resources/BallancePhysicsOptions.asset");
            }
            catch
            {
            }
#else
            Debug.LogError("Load BallancePhysicsOptions.asset failed !");
#endif
          }
        }
        return _instance;
      }
    }

#if UNITY_EDITOR
    [MenuItem("Ballance/Physics/Settings")]
    public static void Open()
    {
      Selection.activeObject = Instance;
    }
#endif

    #endregion
  }
}
