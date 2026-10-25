/*
* Copyright(c) 2021  mengyu
*
* Module:
* GamePoolManager.cs
* 
* Purpose:
* Object pool manager.
*
* Author:
* mengyu
*/

using System;
using System.Collections.Generic;
using Ballance2.Services.Debug;
using Ballance2.Services.Pool;
using Ballance2.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Ballance2.Services
{
  /// <summary>
  /// Manage both ordinary object pools and GameObject resource pools.
  /// </summary>
  public class GamePoolManager : GameService<GamePoolManager>
  {
    private const string TAG = "GamePoolManager";

    public GamePoolManager() : base(TAG) { }

    public override bool Initialize()
    {
      base.Initialize();
      return true;
    }
    
    public override void Destroy()
    { 
      foreach (var pool in m_ObjectPools.Values) pool.Dispose();
      m_ObjectPools.Clear();
      foreach(var o in m_GameObjectPools)
        o.Value.Destroy();
      m_GameObjectPools.Clear();
      base.Destroy();
    }

    private Transform m_PoolRootObject = null;
    private Dictionary<Type, IDisposable> m_ObjectPools = new Dictionary<Type, IDisposable>();
    private Dictionary<string, GameObjectPool> m_GameObjectPools = new Dictionary<string, GameObjectPool>();

    private Transform PoolRootObject
    {
      get
      {
        if (m_PoolRootObject == null)
        {
          var objectPool = CloneUtils.CreateEmptyObject("GameObjectPool");
          objectPool.transform.SetParent(transform);
          objectPool.transform.localScale = Vector3.one;
          objectPool.transform.localPosition = Vector3.zero;
          m_PoolRootObject = objectPool.transform;
        }
        return m_PoolRootObject;
      }
    }

    /// <summary>
    /// Create a GameObject pool.
    /// </summary>
    /// <param name="poolName">Pool name.</param>
    /// <param name="poolObjectPrefab">Pooled object prefab.</param>
    /// <param name="initCount">Initial size.</param>
    /// <param name="maxSize">Maximum size.</param>
    /// <param name="pool">Root transform for pooled objects.</param>
    /// <returns>GameObject pool.</returns>
    public GameObjectPool CreatePool(string poolName, int initSize, int maxSize, GameObject prefab)
    {
      var pool = new GameObjectPool(poolName, prefab, initSize, maxSize, PoolRootObject);
      if (m_GameObjectPools.TryGetValue(poolName, out var previous)) previous.Destroy();
      m_GameObjectPools[poolName] = pool;
      return pool;
    }

    /// <summary>
    /// Get a named GameObject pool.
    /// </summary>
    /// <param name="poolName">Pool name.</param>
    /// <returns>The GameObject pool, or null if not found.</returns>
    public GameObjectPool GetPool(string poolName)
    {
      if (m_GameObjectPools.ContainsKey(poolName))
      {
        return m_GameObjectPools[poolName];
      }
      GameErrorChecker.LastError = GameError.NotFound;
      return null;
    }

    /// <summary>
    /// Get an available object from a named pool.
    /// </summary>
    /// <param name="poolName">Pool name.</param>
    /// <returns>The object, or null if the pool is missing or has no available instance.</returns>
    public GameObject Get(string poolName)
    {
      GameObject result = null;
      if (m_GameObjectPools.ContainsKey(poolName))
      {
        GameObjectPool pool = m_GameObjectPools[poolName];
        result = pool.NextAvailableObject();
        if (result == null)
        {
          Log.W(TAG, "Pool capacity exhausted: " + poolName);
          GameErrorChecker.LastError = GameError.NotAvailable;
        }
      }
      else
      {
        Log.E(TAG, "Invalid pool name specified: " + poolName);
        GameErrorChecker.LastError = GameError.NotFound;
      }
      return result;
    }

    /// <summary>
    /// Return an object to a named pool.
    /// </summary>
    /// <param name="poolName">Pool name.</param>
    /// <param name="go">Object.</param>
    public void Release(string poolName, GameObject go)
    {
      if (m_GameObjectPools.ContainsKey(poolName))
      {
        GameObjectPool pool = m_GameObjectPools[poolName];
        pool.ReturnObjectToPool(poolName, go);
      }
      else
      {
        Log.W(TAG, "No pool available with name: " + poolName);
        GameErrorChecker.LastError = GameError.NotFound;
      }
    }

    // Custom pool helpers.
    ///-----------------------------------------------------------------------------------------------

    /// <summary>
    /// Create a pool for a type.
    /// </summary>
    /// <typeparam name="T">Element type.</typeparam>
    /// <param name="actionOnGet">Callback when acquiring an object.</param>
    /// <param name="actionOnRelease">Callback when releasing an object.</param>
    public ObjectPool<T> CreatePool<T>(UnityAction<T> actionOnGet, UnityAction<T> actionOnRelease) where T : class
    {
      var type = typeof(T);
      var pool = new ObjectPool<T>(actionOnGet, actionOnRelease);
      if (m_ObjectPools.TryGetValue(type, out var previous)) previous.Dispose();
      m_ObjectPools[type] = pool;
      return pool;
    }

    /// <summary>
    /// Get the pool for a type.
    /// </summary>
    /// <typeparam name="T">Element type.</typeparam>
    public ObjectPool<T> GetPool<T>() where T : class
    {
      var type = typeof(T);
      ObjectPool<T> pool = null;
      if (m_ObjectPools.ContainsKey(type))
      {
        pool = m_ObjectPools[type] as ObjectPool<T>;
      }
      return pool;
    }

    /// <summary>
    /// Acquire an available object from the pool for a type.
    /// </summary>
    /// <typeparam name="T">Element type.</typeparam>
    public T Get<T>() where T : class
    {
      var pool = GetPool<T>();
      if (pool != null)
      {
        return pool.Get();
      }
      return default(T);
    }

    /// <summary>
    /// Release an object to the pool for a type.
    /// </summary>
    /// <param name="obj">Object to release.</param>
    /// <typeparam name="T">Element type.</typeparam>
    public void Release<T>(T obj) where T : class
    {
      var pool = GetPool<T>();
      if (pool != null)
      {
        pool.Release(obj);
      }
    }

  }
}
