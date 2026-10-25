/*
 * Copyright (c) 2020  mengyu
 * 
 * Module:
 * GameSingletonManager.cs
 *
 * Purpose:
 * Base class for singleton objects.
 * 
 * Author:
 * mengyu
 */

namespace Ballance2.Base
{
  /// <summary>
  /// Singleton object base class.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class GameSingletonManager<T> where T : class
  {
    public static T Instance = null;

    public GameSingletonManager() {
      if (Instance != null)
        throw new System.Exception("The Singleton " + typeof(T).Name + " already exists!");
      Instance = this as T;
    }
  }
}