
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

  /// <summary>
  /// GameObject pool.
  /// </summary>
  
  
  public class GameObjectPool
  {
    private string TAG {
      get {
        return "GameObjectPool:" + poolName;
      }
    } 

    private int maxSize;
    private int poolSize;
    private string poolName;
    private Transform poolRoot;
    private GameObject poolObjectPrefab;
    private Stack<GameObject> availableObjStack = new Stack<GameObject>();

    /// <summary>
    /// Release resources.
    /// </summary>
    
    public void Destroy() {
      availableObjStack.Clear();
      poolObjectPrefab = null;
    }

    /// <summary>
    /// Create a GameObject pool.
    /// </summary>
    /// <param name="poolName">Pool name.</param>
    /// <param name="poolObjectPrefab">Pooled prefab.</param>
    /// <param name="initCount">Initial size.</param>
    /// <param name="maxSize">Maximum size.</param>
    /// <param name="pool">Root transform for pooled objects.</param>
    
    public GameObjectPool(string poolName, GameObject poolObjectPrefab, int initCount, int maxSize, Transform pool)
    {
      this.poolName = poolName;
      this.poolSize = initCount;
      this.maxSize = maxSize;
      this.poolRoot = pool;
      this.poolObjectPrefab = poolObjectPrefab;

      //populate the pool
      for (int index = 0; index < initCount; index++)
      {
        AddObjectToPool(NewObjectInstance());
      }
    }

    //o(1)
    private void AddObjectToPool(GameObject go)
    {
      //add to pool
      go.SetActive(false);
      availableObjStack.Push(go);
      go.transform.SetParent(poolRoot, false);
    }
    private GameObject NewObjectInstance()
    {
      return GameObject.Instantiate(poolObjectPrefab) as GameObject;
    }
    
    /// <summary>
    /// Get an available object.
    /// </summary>
    /// <returns>GameObject instance, or null when none is available.</returns>
    
    public GameObject NextAvailableObject()
    {
      GameObject go = null;
      if (availableObjStack.Count > 0)
      {
        go = availableObjStack.Pop();
      }
      else
      {
        Log.W(TAG, "No object available & cannot grow pool: " + poolName);
      }
      if(go != null)
        go.SetActive(true);
      return go;
    }

    /// <summary>
    /// Return a named child of the pool root to the pool.
    /// </summary>
    /// <param name="objName">Object name.</param>
    
    
    public void ReturnObjectFromParent(string objName)
    {
      var obj = this.poolRoot.Find(objName);
      if(obj != null)
        AddObjectToPool(obj.gameObject);
      else
        Log.E(TAG, string.Format("Not found the pool child object {0}.", objName));
    }
    /// <summary>
    /// Whether the pool root has a child with the given name.
    /// </summary>
    /// <param name="objName">Object name.</param>
    
    
    public bool ContainsObjectInParent(string objName)
    {
      return this.poolRoot.Find(objName) != null;
    }
    /// <summary>
    /// Clear children of the pool root.
    /// </summary>
    
    public void Clear()
    {
      for(int i = this.poolRoot.childCount - 1; i >= 0; i--)
        ReturnObjectToPool(this.poolRoot.GetChild(i).gameObject);
    }

    /// <summary>
    /// Return an object to the pool; only children of this pool's root are accepted.
    /// </summary>
    /// <param name="po">Object.</param>
    
    
    public void ReturnObjectToPool(GameObject po)
    {
      if(po.transform.parent == this.poolRoot)
        AddObjectToPool(po);
      else
        Log.E(TAG, string.Format("Trying to add object to incorrect pool {0}, it parent {1} not this pool root.", poolName, po.transform.parent.name));
    }
    
    //o(1)
    /// <summary>
    /// Return an object to a pool.
    /// </summary>
    /// <param name="pool">Pool name.</param>
    /// <param name="po">Object.</param>
    
    
    
    public void ReturnObjectToPool(string pool, GameObject po)
    {
      if (poolName.Equals(pool))
      {
        AddObjectToPool(po);
      }
      else
      {
        Log.E(TAG, string.Format("Trying to add object to incorrect pool {0} ", poolName));
      }
    }
  }
}