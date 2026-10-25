/*
* Copyright(c) 2021  mengyu
*
* Module:
* GameService.cs
* 
* Purpose:
* System service base class.
*
* Author:
* mengyu
*/

using Ballance2.Base;
using UnityEngine;

namespace Ballance2.Services
{
  /// <summary>
  /// System service base class.
  /// </summary>
  public class GameService<T> : GameSingletonBehavior<T> where T : class   
  {
    
    public GameService(string name)
    {
      Name = name;
    }

    /// <summary>
    /// Service name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Called during initialization.
    /// </summary>
    /// <returns>Whether initialization succeeded.</returns>
    public virtual bool Initialize()
    {
      return false;
    }
    /// <summary>
    /// Called during release.
    /// </summary>
    public virtual void Destroy()
    {
      Object.Destroy(this);
    }

    /// <summary>
    /// Update method.
    /// </summary>
    protected virtual void Update() {

    }
    /// <summary>
    /// Update method.
    /// </summary>
    protected virtual void FixedUpdate() {

    }
  }
}
