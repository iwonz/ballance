using System;
using UnityEngine.Events;

namespace Ballance2.Services.Pool
{
  // Preserve the package-facing API while Unity owns storage and collection checks.
  public class ObjectPool<T> : IDisposable where T : class
  {
    private readonly UnityEngine.Pool.ObjectPool<T> pool;

    public ObjectPool(UnityAction<T> actionOnGet, UnityAction<T> actionOnRelease)
    {
      pool = new UnityEngine.Pool.ObjectPool<T>(Activator.CreateInstance<T>,
        actionOnGet == null ? null : new Action<T>(actionOnGet.Invoke),
        actionOnRelease == null ? null : new Action<T>(actionOnRelease.Invoke));
    }

    public int countAll => pool.CountAll;
    public int countActive => pool.CountActive;
    public int countInactive => pool.CountInactive;
    public T Get() => pool.Get();
    public void Release(T element) => pool.Release(element);
    public void Dispose() => pool.Dispose();
  }
}
