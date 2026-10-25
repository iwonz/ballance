/*
 * Copyright (c) 2020  mengyu
 * 
 * Module:
 * GameSingletonBehavior.cs
 *
 * Purpose:
 * Base class for singleton objects.
 * 
 * Author:
 * mengyu
 */
using UnityEngine;

namespace Ballance2.Base
{
  /// <summary>
  /// Singleton object base class.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class GameSingletonBehavior<T> : MonoBehaviour where T : class
  {
    public static T Instance;

    public GameSingletonBehavior() {
      Instance = this as T;
    }

    protected virtual void OnDestroy() {
      Instance = null;
    }
  }
}