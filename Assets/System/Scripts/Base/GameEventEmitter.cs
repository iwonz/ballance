using System;
using System.Collections.Generic;

/*
 * Copyright (c) 2022  mengyu
 * 
 * Module:
 * GameEventEmitter.cs
 *
 * Purpose:
 * Game event emitter.
 * 
 * Author:
 * imengyu
 */

namespace Ballance2.Base
{
  /// <summary>
  /// Game event emitter.
  /// Allow a class to publish a set of events to multiple subscribers.
  /// Inspired by Node.js EventEmitter.
  /// </summary>
  [Serializable]
  public class GameEventEmitter
  {
    public GameEventEmitter(string name) { Name = name; }

    /// <summary>
    /// Emitter name.
    /// </summary>
    /// <value></value>
    public string Name { get; }

    public Dictionary<string, GameEventEmitterStorage> _Events = new Dictionary<string, GameEventEmitterStorage>();
    
    /// <summary>
    /// Get an event.
    /// </summary>
    /// <param name="name">Event name.</param>
    /// <returns>Event storage instance.</returns>
    public GameEventEmitterStorage GetEvent(string name) {
      GameEventEmitterStorage result = null;
      _Events.TryGetValue(name, out result);
      return result;
    }
    /// <summary>
    /// Register event storage, returning the existing instance if already registered.
    /// </summary>
    /// <param name="name">Event name.</param>
    /// <returns>Event storage instance.</returns>
    public GameEventEmitterStorage RegisterEvent(string name) {
      GameEventEmitterStorage result = null;
      if(_Events.TryGetValue(name, out result))
        return result;
      result = new GameEventEmitterStorage(name);
      return result; 
    }
    /// <summary>
    /// Emit the named event.
    /// </summary>
    /// <param name="name">Event name.</param>
    /// <param name="param">Event arguments.</param>
    public void EmitEvent(string name, object param) {
      if(_Events.TryGetValue(name, out var result))
        result.Emit(param);
    }
    /// <summary>
    /// Delete an event and all its subscribed callbacks.
    /// </summary>
    /// <param name="name">Event name.</param>
    public void DeleteEvent(string name) {
      if(_Events.ContainsKey(name))
        _Events.Remove(name);
    }
  }

  /// <summary>
  /// Event storage for an emitter.
  /// </summary>
  [Serializable]
  public class GameEventEmitterStorage
  {
    public GameEventEmitterStorage(string name) { Name = name; }

    /// <summary>
    /// Event name.
    /// </summary>
    /// <value></value>
    public string Name { get; }

    /// <summary>
    /// Emit this event.
    /// </summary>
    /// <param name="obj">Event arguments.</param>
    public void Emit(params object[] obj) {
      GameEventEmitterHandler n = _Listeners;
      GameEventEmitterHandler next = null;
      while(n != null) {
        next = n.Next;
        n.Delegate.Invoke(obj);
        // Remove after one invocation.
        if(n.Once) 
          n.Off();
        // Next.
        n = next;
      }
    }

    internal GameEventEmitterHandler _Listeners;

    /// <summary>
    /// Add an event listener.
    /// </summary>
    /// <param name="fn">Event listener callback.</param>
    public GameEventEmitterHandler On(GameEventEmitterDelegate fn) { return AddListener(fn, null, false); }
    /// <summary>
    /// Add a tagged listener that can be removed by tag.
    /// </summary>
    /// <param name="fn">Event listener callback.</param>
    /// <param name="tag">Listener tag.</param>
    public GameEventEmitterHandler OnWithTag(GameEventEmitterDelegate fn, string tag) { return AddListener(fn, tag, false); }
    /// <summary>
    /// Add a one-shot listener.
    /// </summary>
    /// <param name="fn">Event listener callback.</param>
    public GameEventEmitterHandler Once(GameEventEmitterDelegate fn) { return AddListener(fn, null, true); }

    private GameEventEmitterHandler AddListener(GameEventEmitterDelegate fn, string tag, bool once) {
      GameEventEmitterHandler result = new GameEventEmitterHandler(this);
      if(_Listeners != null) {
        result.Next = _Listeners;
        _Listeners.Prev = result;
      }

      _Listeners = result;
      result.Delegate = fn;
      result.Tag = tag;
      result.Once = once;
      return result;
    }

    /// <summary>
    /// Remove an event listener.
    /// </summary>
    /// <param name="fn">Event listener callback.</param>
    public void Off(GameEventEmitterDelegate fn) {
      GameEventEmitterHandler n = _Listeners;
      GameEventEmitterHandler next = null;
      while(n != null) {
        next = n.Next;
        if(n.Delegate == fn) 
          n.Off();
        n = next;
      }
    }
    /// <summary>
    /// Remove listeners with the specified tag.
    /// </summary>
    /// <param name="tag">Listener tag.</param>
    public void OffAllTag(string tag) {
      GameEventEmitterHandler n = _Listeners;
      GameEventEmitterHandler next = null;
      while(n != null) {
        next = n.Next;
        if(n.Tag == tag) 
          n.Off();
        n = next;
      }
    }
    /// <summary>
    /// Remove all listeners for this event.
    /// </summary>
    public void Clear() {
      GameEventEmitterHandler n = _Listeners;
      GameEventEmitterHandler next = null;
      while(n != null) {
        next = n.Next;
        n.Off();
        n = next;
      }
      _Listeners = null;
    }
  }

  [Serializable]
  public class GameEventEmitterHandler
  {
    /// <summary>
    /// Get the event owning this callback.
    /// </summary>
    public GameEventEmitterStorage Storage { get; }

    public GameEventEmitterHandler(GameEventEmitterStorage storage) {
      Storage = storage;
    }

    public GameEventEmitterHandler Next = null;
    public GameEventEmitterHandler Prev = null;
    public GameEventEmitterDelegate Delegate;
    public bool Once = false;
    public string Tag = null;

    /// <summary>
    /// Whether this listener has been removed.
    /// </summary>
    public bool Deleted { get; private set; } = false;

    /// <summary>
    /// Remove this listener; equivalent to GameEventEmitterStorage.Off.
    /// </summary>
    public void Off() {
      if(Deleted)
        return;
      if(this == Storage._Listeners)// First.
        Storage._Listeners = Next;
      if(Prev != null)
        Prev.Next = Next;
      if(Next != null)
        Next.Prev = Prev;
      Deleted = true;
    }
  }

  /// <summary>
  /// Internal event handler callback.
  /// </summary>
  /// <param name="evtName">Event name.</param>
  /// <param name="pararms">Arguments.</param>
  /// <returns>Whether to stop dispatching to the remaining handlers.</returns>
  public delegate void GameEventEmitterDelegate(params object[] obj);
}
