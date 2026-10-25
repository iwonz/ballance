using System.Collections.Generic;
using UnityEngine;

/*
* Copyright(c) 2021 imengyu
*
* Module:
* ObjectStateBackupUtils.cs
* 
* Purpose:
* Object transform state backup.
* Virtools initial conditions (IC) save an object's initial state for
* repeated restoration. Many Ballance mechanisms need this behavior,
* so this helper provides transform backups.
* It stores position and rotation for an object, optionally including its direct children,
* which covers the common reset cases.
* 
* Author:
* mengyu
*/

namespace Ballance2.Utils
{
  /// <summary>
  /// Object transform state backup.
  /// </summary>
  /// <example>
  /// // Save the initial object state with BackUpObject:
  /// BackUpObject(self.gameObject)
  /// // Restore it later with RestoreObject:
  /// RestoreObject(self.gameObject)
  /// </example>
  public static class ObjectStateBackupUtils
  {
    private struct ObjectStateBackup
    {
      public Vector3 Pos;
      public Quaternion Rot;
    }
    private static Dictionary<EntityId, ObjectStateBackup> objectBackup = new Dictionary<EntityId, ObjectStateBackup>();

    // Called by GameManager.
    
    public static void ClearAll()
    {
      objectBackup.Clear();
    }

    /// <summary>
    /// Clear an object's backup.
    /// </summary>
    /// <param name="gameObject">GameObject to operate on.</param>
    public static void ClearObjectBackUp(GameObject gameObject)
    {
      objectBackup.Remove(gameObject.GetEntityId());
    }

    /// <summary>
    /// Back up an object's transform.
    /// </summary>
    /// <param name="gameObject">GameObject to back up.</param>
    public static void BackUpObject(GameObject gameObject)
    {
      var key = gameObject.GetEntityId();
      if (!objectBackup.TryGetValue(key, out var st))
        st = new ObjectStateBackup();

      st.Pos = gameObject.transform.position;
      st.Rot = gameObject.transform.rotation;

      objectBackup[key] = st;
    }

    /// <summary>
    /// Back up an object and its direct children's transforms.
    /// </summary>
    /// <param name="gameObject">GameObject to back up.</param>
    public static void BackUpObjectAndChilds(GameObject gameObject)
    {
      BackUpObject(gameObject);
      for (int i = 0; i < gameObject.transform.childCount; i++)
        BackUpObject(gameObject.transform.GetChild(i).gameObject);
    }

    /// <summary>
    /// Restore an object's transform from backup.
    /// </summary>
    /// <param name="gameObject">GameObject to restore.</param>
    public static void RestoreObject(GameObject gameObject)
    {
      var key = gameObject.GetEntityId();
      if (objectBackup.TryGetValue(key, out var st))
      {
        gameObject.transform.position = st.Pos;
        gameObject.transform.rotation = st.Rot;
      }
    }

    /// <summary>
    /// Restore an object and its direct children's transforms.
    /// </summary>
    /// <param name="gameObject">GameObject to restore.</param>
    public static void RestoreObjectAndChilds(GameObject gameObject)
    {
      RestoreObject(gameObject);
      for (int i = 0; i < gameObject.transform.childCount; i++)
        RestoreObject(gameObject.transform.GetChild(i).gameObject);
    }

  }
}