/*
 * Copyright (c) 2020  mengyu
 * 
 * Module:
 * GameEventHandler.cs
 *
 * Purpose:
 * Event handler instance used to unregister a listener.
 * 
 * Author:
 * mengyu
 */
using Ballance2.Package;
using static Ballance2.Services.GameManager;

namespace Ballance2.Base
{
  /// <summary>
  /// Event handler instance.
  /// </summary>
  public class GameEventHandler {

    /// <summary>
    /// Owning package.
    /// </summary>
    public GamePackage BelongPackage { get; private set; }
    /// <summary>
    /// Handler name.
    /// </summary>
    public string Name { get; private set; }
    /// <summary>
    /// Whether it has been released.
    /// </summary>
    public bool Destroyed { get; private set; } = false;

    /// <summary>
    /// Unregister this event handler.
    /// </summary>
    public void UnRegister() {
      if (unregisterCallback != null) {
        unregisterCallback(this);
        Destroyed = true;
        unregisterCallback = null;
      }
    }

    private UnregisterCallbackDelegate unregisterCallback = null;
    public delegate void UnregisterCallbackDelegate(GameEventHandler evt);
    internal GameEventHandler(GamePackage package, UnregisterCallbackDelegate unregisterCallback, string name, GameEventHandlerDelegate gameHandlerDelegate) {
      this.BelongPackage = package;
      this.Name = name;
      this.unregisterCallback = unregisterCallback;
      this.eventHandlerDelegate = gameHandlerDelegate;
    }

    /// <summary>
    /// Invoke the callback manually.
    /// </summary>
    /// <param name="evtName">Event name.</param>
    /// <param name="pararms">Arguments.</param>
    /// <returns></returns>
    public bool CallEventHandler(string evtName, params object[] pararms)
    {
      if (Destroyed)
        return false;
      return eventHandlerDelegate.Invoke(evtName, pararms);
    }

    private GameEventHandlerDelegate eventHandlerDelegate = null;
  }
}