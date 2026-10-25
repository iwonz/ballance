using Ballance2.Entry;
using Ballance2.Res;
using Ballance2.Services;
using Ballance2.Services.Debug;
using UnityEngine;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* CloneUtils.cs
* 
* Purpose:
* GameObject cloning helpers.
* Create empty objects or instantiate existing prefabs.
*
* Author:
* mengyu
*/

namespace Ballance2.Utils
{
  /// <summary>
  /// Cloning helpers.
  /// </summary>
  public static class CloneUtils
  {
    /// <summary>
    /// Instantiate a prefab.
    /// </summary>
    /// <param name="prefab">Prefab</param>
    /// <param name="name">New object name.</param>
    /// <returns>Created object.</returns>
    public static GameObject CloneNewObject(GameObject prefab, string name)
    {
      if (prefab == null)
      {
        GameErrorChecker.SetLastErrorAndLog(GameError.ParamNotProvide, "CloneUtils", "Can not clone {0}, The prefab is null", name);
        return null;
      }
      GameObject go = Object.Instantiate(prefab, GameManager.Instance != null ? GameManager.Instance.transform : GameEntry.Instance.transform);
      go.name = name;
      return go;
    }
    /// <summary>
    /// Instantiate a prefab under a parent transform.
    /// </summary>
    /// <param name="prefab">Prefab</param>
    /// <param name="parent">Parent object.</param>
    /// <param name="name">New object name.</param>
    /// <returns>Created object.</returns>
    public static GameObject CloneNewObjectWithParent(GameObject prefab, Transform parent, string name)
    {
      if (prefab == null)
      {
        GameErrorChecker.SetLastErrorAndLog(GameError.ParamNotProvide, "CloneUtils", "Can not clone {0}, The prefab is null", name);
        return null;
      }
      GameObject go = Object.Instantiate(prefab, parent);
      go.name = name;
      return go;
    }
    /// <summary>
    /// Instantiate a prefab under a parent transform.
    /// </summary>
    /// <param name="prefab">Prefab</param>
    /// <param name="parent">Parent object.</param>
    /// <returns>Created object.</returns>
    public static GameObject CloneNewObjectWithParent(GameObject prefab, Transform parent)
    {
      if (prefab == null)
      {
        GameErrorChecker.SetLastErrorAndLog(GameError.ParamNotProvide, "CloneUtils", "Can not clone object, The prefab is null");
        return null;
      }
      return Object.Instantiate(prefab, parent);
    }
    /// <summary>
    /// Instantiate a prefab under a parent transform.
    /// </summary>
    /// <param name="prefab">Prefab</param>
    /// <param name="parent">Parent object.</param>
    /// <param name="name">New object name.</param>
    /// <param name="active">Activate the object.</param>
    /// <returns>Created object.</returns>
    public static GameObject CloneNewObjectWithParent(GameObject prefab, Transform parent, string name, bool active)
    {
      if (prefab == null)
      {
        GameErrorChecker.SetLastErrorAndLog(GameError.ParamNotProvide, "CloneUtils", "Can not clone {0}, The prefab is null", name);
        return null;
      }
      GameObject go = Object.Instantiate(prefab, parent);
      go.name = name;
      go.SetActive(active);
      return go;
    }

    /// <summary>
    /// Create an empty object.
    /// </summary>
    /// <param name="name">New object name.</param>
    /// <returns>Created object.</returns>
    public static GameObject CreateEmptyObject(string name)
    {
      GameObject go = Object.Instantiate(GameStaticResourcesPool.PrefabEmpty, GameManager.Instance != null ? GameManager.Instance.transform : GameEntry.Instance.transform);
      go.name = name;
      return go;
    }
    /// <summary>
    /// Create an empty object under a parent transform.
    /// </summary>
    /// <param name="parent">Parent object.</param>
    /// <param name="name">New object name.</param>
    /// <returns>Created object.</returns>
    public static GameObject CreateEmptyObjectWithParent(Transform parent, string name)
    {
      GameObject go = Object.Instantiate(GameStaticResourcesPool.PrefabEmpty, parent);
      go.name = name;
      return go;
    }
    /// <summary>
    /// Create an empty object under a parent transform.
    /// </summary>
    /// <param name="parent">Parent object.</param>
    /// <returns>Created object.</returns>
    public static GameObject CreateEmptyObjectWithParent(Transform parent)
    {
      return Object.Instantiate(GameStaticResourcesPool.PrefabEmpty, parent);
    }

    /// <summary>
    /// Create an empty UI object.
    /// </summary>
    /// <param name="name">New object name.</param>
    /// <returns>Created object.</returns>
    public static GameObject CreateEmptyUIObject(string name)
    {
      GameObject go = Object.Instantiate(GameStaticResourcesPool.PrefabUIEmpty, GameManager.Instance.GameCanvas.transform);
      go.name = name;
      return go;
    }
    /// <summary>
    /// Create an empty UI object under a parent transform.
    /// </summary>
    /// <param name="parent">Parent object.</param>
    /// <param name="name">New object name.</param>
    /// <returns>Created object.</returns>
    public static GameObject CreateEmptyUIObjectWithParent(Transform parent, string name)
    {
      GameObject go = Object.Instantiate(GameStaticResourcesPool.PrefabUIEmpty, parent);
      go.name = name;
      return go;
    }
    /// <summary>
    /// Create an empty UI object under a parent transform.
    /// </summary>
    /// <param name="parent">Parent object.</param>
    /// <returns>Created object.</returns>
    public static GameObject CreateEmptyUIObjectWithParent(Transform parent)
    {
      return Object.Instantiate(GameStaticResourcesPool.PrefabUIEmpty, parent);
    }
  }
}
