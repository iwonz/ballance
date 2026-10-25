using System.Collections.Generic;
using UnityEngine.Events;

namespace Ballance2.Services.Pool
{
  /// <summary>
  /// Generic object pool.
  /// </summary>
  /// <typeparam name="T">Element type.</typeparam>
  public class ObjectPool<T> where T : class
  {
    private readonly Stack<T> m_Stack = new Stack<T>();
    private readonly UnityAction<T> m_ActionOnGet;
    private readonly UnityAction<T> m_ActionOnRelease;

    public int countAll { get; private set; }
    public int countActive { get { return countAll - countInactive; } }
    public int countInactive { get { return m_Stack.Count; } }

    /// <summary>
    /// Create a pool with callbacks.
    /// </summary>
    /// <param name="actionOnGet">Acquisition callback.</param>
    /// <param name="actionOnRelease">Callback when releasing an object.</param>
    public ObjectPool(UnityAction<T> actionOnGet, UnityAction<T> actionOnRelease)
    {
      m_ActionOnGet = actionOnGet;
      m_ActionOnRelease = actionOnRelease;
    }

    /// <summary>
    /// Get an available object.
    /// </summary>
    /// <returns>The object, or null when none is available.</returns>
    public T Get()
    {
      T element = m_Stack.Pop();
      if (m_ActionOnGet != null)
        m_ActionOnGet(element);
      return element;
    }

    /// <summary>
    /// Release and return an object.
    /// </summary>
    /// <param name="element">Object.</param>
    public void Release(T element)
    {
      if (m_Stack.Count > 0 && ReferenceEquals(m_Stack.Peek(), element))
        UnityEngine.Debug.LogError("Internal error. Trying to destroy object that is already released to pool.");
      if (m_ActionOnRelease != null)
        m_ActionOnRelease(element);
      m_Stack.Push(element);
    }
  }
}