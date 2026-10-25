using System;
using System.Collections.Generic;
using Ballance2.Services;
using UnityEngine;

/*
 * Copyright (c) 2020  mengyu
 * 
 * Module:
 * GameEvent.cs
 *
 * Purpose:
 * Global game event definitions and storage.
 * 
 * Author:
 * mengyu
 */

namespace Ballance2.Base
{
  /// <summary>
  /// Global event storage.
  /// </summary>
  [Serializable]
  public class GameEvent
  {
    /// <summary>
    /// Construct global event storage.
    /// </summary>
    /// <param name="evtName">Event name.</param>
    public GameEvent(string evtName)
    {
      _EventName = evtName;
      _EventHandlers = new List<GameEventHandler>();
    }

    /// <summary>
    /// Release resources.
    /// </summary>
    public void Dispose()
    {
      _EventHandlers.Clear();
      _EventHandlers = null;
      _Unregistered = true;
    }
    
    /// <summary>
    /// Unregister this event; equivalent to GameMediator.UnRegisterGlobalEvent.
    /// </summary>
    public void Delete() {
      if(!_Unregistered) {
        GameManager.GameMediator.UnRegisterGlobalEvent(_EventName);
      }
    }

    /// <summary>
    /// Dispatch this event; equivalent to GameMediator.DispatchGlobalEvent.
    /// </summary>
    public void Dispatch(params object [] param) {
      if(!_Unregistered) {
        GameManager.GameMediator.DispatchGlobalEvent(this, param);
      }
    }
    
    private bool _Unregistered = false;
    [SerializeField, SetProperty("EventName")]
    private string _EventName;
    [SerializeField, SetProperty("EventHandlers")]
    private List<GameEventHandler> _EventHandlers;

    /// <summary>
    /// Get the event name.
    /// </summary>
    public string EventName { get { return _EventName; } }
    /// <summary>
    /// Get the event handler list.
    /// </summary>
    public List<GameEventHandler> EventHandlers { get { return _EventHandlers; } }
  }

  /// <summary>
  /// Internal game event definitions.
  /// </summary>
  public static class GameEventNames
  {
    /// <summary>
    /// Raised after all base managers finish initialization.
    /// </summary>
    /// <remarks>
    /// Event arguments: none.
    /// </remarks>
    public const string EVENT_BASE_INIT_FINISHED = "EVENT_BASE_INIT_FINISHED";

    /// <summary>
    /// Raised after GameManager initialization, when packages take over the main game flow.
    /// </summary>
    /// <remarks>
    /// Event arguments: none.
    /// </remarks>
    public const string EVENT_GAME_MANAGER_INIT_FINISHED = "EVENT_GAME_MANAGER_INIT_FINISHED";

    /// <summary>
    /// Raised after UI manager initialization.
    /// </summary>
    /// <remarks>
    /// Event arguments: none.
    /// </remarks>
    public const string EVENT_UI_MANAGER_INIT_FINISHED = "EVENT_UI_MANAGER_INIT_FINISHED";

    /// <summary>
    /// Raised when a global Alert or Confirm dialog closes.
    /// </summary>
    /// <remarks>
    /// Event arguments:
    /// [0] Dialog ID.
    /// [1] Whether the user chose Confirm; always false for Alert.
    /// </remarks>
    public const string EVENT_GLOBAL_ALERT_CLOSE = "EVENT_GLOBAL_ALERT_CLOSE";

    /// <summary>
    /// Raised before the game quits.
    /// </summary>
    /// <remarks>
    /// Event arguments: none.
    /// </remarks>
    public const string EVENT_BEFORE_GAME_QUIT = "EVENT_BEFORE_GAME_QUIT";

    /// <summary>
    /// Package loaded successfully.
    /// </summary>
    /// <remarks>
    /// Event arguments:
    /// [0] Package name.
    /// [1] Package object.
    /// </remarks>
    public const string EVENT_PACKAGE_LOAD_SUCCESS = "EVENT_PACKAGE_LOAD_SUCCESS";

    /// <summary>
    /// Package load failed.
    /// </summary>
    /// <remarks>
    /// Event arguments:
    /// [0] Package name.
    /// [1] Package object.
    /// [2] Error message.
    /// </remarks>
    public const string EVENT_PACKAGE_LOAD_FAILED = "EVENT_PACKAGE_LOAD_FAILED";

    /// <summary>
    /// Package registered.
    /// </summary>
    /// <remarks>
    /// Event arguments:
    /// [0] Package name.
    /// </remarks>
    public const string EVENT_PACKAGE_REGISTERED = "EVENT_PACKAGE_REGISTERED";
    /// <summary>
    /// Package unregistered.
    /// </summary>
    /// <remarks>
    /// Event arguments:
    /// [0] Package name.
    /// </remarks>
    public const string EVENT_PACKAGE_UNREGISTERED = "EVENT_PACKAGE_UNREGISTERED";

    /// <summary>
    /// Package unloaded.
    /// </summary>
    /// <remarks>
    /// Event arguments:
    /// [0] Package name.
    /// [1] Package object.
    /// </remarks>
    public const string EVENT_PACKAGE_UNLOAD = "EVENT_PACKAGE_UNLOAD";

    /// <summary>
    /// Screen resolution changed.
    /// </summary>
    /// <remarks>
    /// Event arguments:
    /// [0] New screen width.
    /// [1] New screen height.
    /// [2] New refresh rate.
    /// </remarks>
    public const string EVENT_SCREEN_SIZE_CHANGED = "EVENT_SCREEN_SIZE_CHANGED";

    /// <summary>
    /// Entered a logical scene.
    /// </summary>
    /// <remarks>
    /// Event arguments:
    /// [0] Scene name.
    /// </remarks>
    public const string EVENT_LOGIC_SECNSE_ENTER = "EVENT_LOGIC_SECNSE_ENTER";

    /// <summary>
    /// Left a logical scene.
    /// </summary>
    /// <remarks>
    /// Event arguments:
    /// [0] Scene name.
    /// </remarks>
    public const string EVENT_LOGIC_SECNSE_QUIT = "EVENT_LOGIC_SECNSE_QUIT";
  
  
  
    public const string EVENT_LEVEL_BUILDER_JSON_LOADED = "EVENT_LEVEL_BUILDER_JSON_LOADED";
  
    public const string EVENT_LEVEL_BUILDER_MAIN_PREFAB_STANDBY = "EVENT_LEVEL_BUILDER_MAIN_PREFAB_STANDBY";
  
    public const string EVENT_LEVEL_BUILDER_BEFORE_START = "EVENT_LEVEL_BUILDER_BEFORE_START";
    
    public const string EVENT_LEVEL_BUILDER_START = "EVENT_LEVEL_BUILDER_START";
  
    public const string EVENT_LEVEL_BUILDER_UNLOAD_START = "EVENT_LEVEL_BUILDER_UNLOAD_START";
  
  }
  
  /// <summary>
  /// Event handler callback.
  /// </summary>
  /// <param name="evtName">Event name.</param>
  /// <param name="pararms">Arguments.</param>
  /// <returns>Whether to stop dispatching to the remaining handlers.</returns>
  public delegate bool GameEventHandlerDelegate(string evtName, params object[] pararms);
}
