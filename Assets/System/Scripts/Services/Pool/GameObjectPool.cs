using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ballance2.Services.Pool
{
  [Serializable]
  public class PoolInfo
  {
    public string poolName;
    public GameObject prefab;
    public int poolSize;
    public bool fixedSize;
  }

  public class GameObjectPool : IDisposable
  {
    private readonly string poolName;
    private readonly int maxSize;
    private readonly Transform poolRoot;
    private readonly UnityEngine.Pool.ObjectPool<GameObject> pool;
    private readonly HashSet<GameObject> rented = new HashSet<GameObject>();
    private bool disposed;

    public GameObjectPool(string poolName, GameObject prefab, int initCount, int maxSize, Transform root)
    {
      if (prefab == null) throw new ArgumentNullException(nameof(prefab));
      if (root == null) throw new ArgumentNullException(nameof(root));
      if (initCount < 0 || maxSize < 1 || initCount > maxSize)
        throw new ArgumentOutOfRangeException(nameof(initCount), "Pool capacity must satisfy 0 <= initial <= maximum.");
      this.poolName = poolName;
      this.maxSize = maxSize;
      poolRoot = root;
      pool = new UnityEngine.Pool.ObjectPool<GameObject>(
        () => UnityEngine.Object.Instantiate(prefab, poolRoot),
        go => { rented.Add(go); go.SetActive(true); },
        go => { go.SetActive(false); go.transform.SetParent(poolRoot, false); },
        DestroyObject, collectionCheck: true, defaultCapacity: initCount, maxSize: maxSize);
      var initial = new GameObject[initCount];
      for (int i = 0; i < initCount; i++) initial[i] = pool.Get();
      foreach (var go in initial) ReturnObjectToPool(go);
    }

    public GameObject NextAvailableObject()
    {
      if (disposed) throw new ObjectDisposedException(poolName);
      // Unity's maxSize bounds inactive retention; the UI also needs a total cap.
      if (rented.Count >= maxSize) return null;
      return pool.Get();
    }

    public void ReturnObjectFromParent(string objName)
    {
      foreach (var go in rented)
        if (go != null && go.name == objName)
        {
          ReturnObjectToPool(go);
          return;
        }
    }

    public bool ContainsObjectInParent(string objName)
    {
      foreach (var go in rented)
        if (go != null && go.name == objName) return true;
      return false;
    }

    public void Clear()
    {
      if (disposed) return;
      foreach (var go in new List<GameObject>(rented)) ReturnObjectToPool(go);
    }

    public void ReturnObjectToPool(GameObject go)
    {
      if (disposed) throw new ObjectDisposedException(poolName);
      if (go == null || !rented.Remove(go))
        throw new InvalidOperationException("Object is not rented from pool: " + poolName);
      pool.Release(go);
    }

    public void ReturnObjectToPool(string name, GameObject go)
    {
      if (name != poolName) throw new ArgumentException("Incorrect pool name: " + name, nameof(name));
      ReturnObjectToPool(go);
    }

    public void Destroy() => Dispose();

    public void Dispose()
    {
      if (disposed) return;
      disposed = true;
      foreach (var go in rented) DestroyObject(go);
      rented.Clear();
      pool.Dispose();
    }

    private static void DestroyObject(GameObject go)
    {
      if (go == null) return;
      if (Application.isPlaying) UnityEngine.Object.Destroy(go);
      else UnityEngine.Object.DestroyImmediate(go);
    }
  }
}
