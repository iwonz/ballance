using System.Collections.Generic;
using UnityEngine;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* GameStaticResourcesPool.cs
* 
* Purpose:
* Static game resource pool.
* Configure static references in GameEntry to access packaged resources directly without an additional load.
* Too many static references can slow startup.
*
* Author:
* mengyu
*/

namespace Ballance2.Res
{
  /// <summary>
  /// Static game resource pool.
  /// </summary>
  
  
  
  public class GameStaticResourcesPool
  {
    public static GameObject PrefabUIEmpty { get; private set; }
    public static GameObject PrefabEmpty { get; private set; }

    /// <summary>
    /// Find a named prefab among static references.
    /// </summary>
    /// <param name="name">Resource name.</param>
    /// <returns>The prefab, or null if not found.</returns>
    
    
    public static GameObject FindStaticPrefabs(string name)
    {
      if (GamePrefab == null)
        return null;
      foreach (GameObjectInfo gameObjectInfo in GamePrefab)
      {
        if (gameObjectInfo.Name == name)
          return gameObjectInfo.Object;
      }
      return null;
    }
    /// <summary>
    /// Find a named asset among static references.
    /// </summary>
    /// <param name="name">Resource name.</param>
    /// <returns>The asset, or null if not found.</returns>
    public static T FindStaticAssets<T>(string name) where T : UnityEngine.Object
    {
      if (GameAssets == null)
        return null;
      foreach (GameAssetsInfo gameAssetsInfo in GameAssets)
      {
        if (gameAssetsInfo.Name == name)
          return (T)gameAssetsInfo.Object;
      }
      return null;
    }
    /// <summary>
    /// Find a named asset among static references.
    /// </summary>
    /// <param name="name">Resource name.</param>
    /// <returns>The asset, or null if not found.</returns>
    
    
    public static Object FindStaticAssets(string name)
    {
      if (GameAssets == null)
        return null;
      foreach (GameAssetsInfo gameAssetsInfo in GameAssets)
      {
        if (gameAssetsInfo.Name == name)
          return gameAssetsInfo.Object;
      }
      return null;
    }

    internal static void InitStaticPrefab(List<GameObjectInfo> gamePrefab, List<GameAssetsInfo> gameAssets)
    {
      GamePrefab = gamePrefab;
      GameAssets = gameAssets;

      PrefabEmpty = FindStaticPrefabs("PrefabEmpty");
      PrefabUIEmpty = FindStaticPrefabs("PrefabUIEmpty");
    }
    internal static void ReleaseAll()
    {
      PrefabUIEmpty = null;
      PrefabEmpty = null;
      GamePrefab = null;
      GameAssets = null;
    }

    /// <summary>
    /// Static prefab reference.
    /// </summary>
    
    public static List<GameObjectInfo> GamePrefab { get; private set; }
    /// <summary>
    /// Static resource reference.
    /// </summary>
    
    public static List<GameAssetsInfo> GameAssets { get; private set; }
  }
}
