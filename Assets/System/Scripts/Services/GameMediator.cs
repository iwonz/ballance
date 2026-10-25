using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Ballance2.Utils;
using Ballance2.Base;
using Ballance2.Package;
using Ballance2.Services.Debug;
using Ballance2.Utils.ServiceUtils;
using UnityEngine.Profiling;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* GameMediator.cs
* 
* Purpose:
* Central mediator for game event routing and handling.
* Event interaction types:
*
*   Global events: broadcast to multiple subscribers.
*   Single events: send a notification to one subscriber.
*   Event emitters: a class exposes events to multiple subscribers.
*
* Events do not require an explicit RegisterGlobalEvent/RegisterSingleEvent call before supported operations such as DispatchGlobalEvent;
* those operations register missing events automatically.
*
* Author:
* mengyu
*/

namespace Ballance2.Services
{
  /// <summary>
  /// Game mediator.
  /// </summary>
  [Serializable]
  public class GameMediator : GameService<GameMediator>
  {
    private readonly string TAG = "GameMediator";
    private GameObject go = null;
    private GameMediatorDelayCaller DelayCaller = null;

    public GameMediator() : base("GameMediator")
    {

    }

    public override void Destroy()
    {
      if (go != null)
        UnityEngine.Object.Destroy(go);
      UnLoadAllEvents();
    }
    public override bool Initialize()
    {
      InitAllEvents();
      
      DelayCaller = gameObject.AddComponent<GameMediatorDelayCaller>();
      DelayCaller.GameMediator = this;

      RegisterEventHandler(GamePackage.GetSystemPackage(),
          GameEventNames.EVENT_BASE_INIT_FINISHED, TAG, (evtName, param) =>
          {
            InitCommands();
            return false;
          });
      return true;
    }

    #region Global event controller

    [SerializeField, SetProperty("Events")]
    private Dictionary<string, GameEvent> events = new Dictionary<string, GameEvent>();
    private Dictionary<string, GameEventEmitter> eventEmitts = new Dictionary<string, GameEventEmitter>();
    private Dictionary<string, GameEventHandler> singleEvents = new Dictionary<string, GameEventHandler>();

    public Dictionary<string, GameEvent> Events { get { return events; } }

    #region Event emitters

    /// <summary>
    /// Register an event emitter.
    /// </summary>
    /// <param name="name">Event emitter name.</param>
    /// <returns></returns>
    public GameEventEmitter RegisterEventEmitter(string name) {
      GameEventEmitter result = null;
      if(eventEmitts.TryGetValue(name, out result))
        return result;
      result = new GameEventEmitter(name);
      return result; 
    }
    /// <summary>
    /// Unregister an event emitter.
    /// </summary>
    /// <param name="name">Event emitter name.</param>
    public void UnRegisterEventEmitter(string name) {
      eventEmitts.Remove(name);
    }

    #endregion

    #region Single-handler events

    /// <summary>
    /// Register a single-handler event.
    /// </summary>
    /// <param name="evtName">Event name.</param>
    public bool RegisterSingleEvent(string evtName)
    {
      if (string.IsNullOrEmpty(evtName))
      {
        Log.W(TAG, "RegisterSingleEvent: evtName was not provided.");
        GameErrorChecker.LastError = GameError.ParamNotProvide;
        return false;
      }
      if (!singleEvents.ContainsKey(evtName))
        singleEvents.Add(evtName, null);
      return true;
    }
    /// <summary>
    /// Unregister a single-handler event.
    /// </summary>
    /// <param name="evtName">Event name.</param>
    
    
    public bool UnRegisterSingleEvent(string evtName)
    {
      if (string.IsNullOrEmpty(evtName))
      {
        Log.W(TAG, "UnRegisterSingleEvent: evtName was not provided.");
        GameErrorChecker.LastError = GameError.ParamNotProvide;
        return false;
      }
      if (singleEvents.ContainsKey(evtName))
      {
        singleEvents.Remove(evtName);
        return true;
      }
      else
      {
        GameErrorChecker.LastError = GameError.NotRegister;
        return false;
      }
    }
    /// <summary>
    /// Whether a single-handler event is registered.
    /// </summary>
    /// <param name="evtName">Event name.</param>
    /// <returns>Whether it is registered.</returns>
    
    
    public bool IsSingleEventRegistered(string evtName)
    {
      return singleEvents.ContainsKey(evtName);
    }
    /// <summary>
    /// Whether a single-handler event has a subscriber.
    /// </summary>
    /// <param name="evtName">Event name.</param>
    /// <returns>Whether a subscriber is attached.</returns>
    
    
    public bool CheckSingleEventAttatched(string evtName)
    {
      if (string.IsNullOrEmpty(evtName))
      {
        Log.W(TAG, "NotifySingleEvent: evtName was not provided.");
        GameErrorChecker.LastError = GameError.ParamNotProvide;
        return false;
      }
      if (singleEvents.TryGetValue(evtName, out GameEventHandler handler))
      {
        return (handler != null);
      }
      else
      {
        Log.W(TAG, "Event {0} is not registered.", evtName);
        GameErrorChecker.LastError = GameError.NotRegister;
        return false;
      }
    }

    /// <summary>
    /// Notify a single-handler event after a delay.
    /// </summary>
    /// <param name="evtName">Event name.</param>
    /// <param name="delayeSecond">Delay in seconds.</param>
    /// <param name="pararms">Event arguments.</param>
    /// <returns>Whether the operation succeeded.</returns>
    public bool DelayedNotifySingleEvent(string evtName, float delayeSecond, params object[] pararms)
    {
      if (string.IsNullOrEmpty(evtName))
      {
        Log.W(TAG, "NotifySingleEvent: evtName was not provided.");
        GameErrorChecker.LastError = GameError.ParamNotProvide;
        return false;
      }
      DelayCaller.AddDelayCallSingle(evtName, delayeSecond, pararms);
      return true;
    }
    /// <summary>
    /// Notify a single-handler event.
    /// </summary>
    /// <param name="evtName">Event name.</param>
    /// <param name="pararms">Event arguments.</param>
    /// <returns>Whether the operation succeeded.</returns>
    public bool NotifySingleEvent(string evtName, params object[] pararms)
    {
      if (string.IsNullOrEmpty(evtName))
      {
        Log.W(TAG, "NotifySingleEvent: evtName was not provided.");
        GameErrorChecker.LastError = GameError.ParamNotProvide;
        return false;
      }
      if (singleEvents.TryGetValue(evtName, out GameEventHandler handler))
      {
        Profiler.BeginSample("NotifySingleEvent" + evtName);
        
        if (handler != null)
          handler.CallEventHandler(evtName, pararms);

        Profiler.EndSample();
        return true;
      }
      else
      {
        Log.W(TAG, "Event {0} is not registered.", evtName);
        GameErrorChecker.LastError = GameError.NotRegister;
        return false;
      }
    }

    /// <summary>
    /// Subscribe to a global single-handler event.
    /// </summary>
    /// <param name="package">Owning package.</param>
    /// <param name="evtName">Event name.</param>
    /// <param name="name">Handler name.</param>
    /// <param name="gameHandlerDelegate">Callback.</param>
    /// <returns>Handler instance, or null on failure; inspect LastError for details.</returns>
    public GameEventHandler SubscribeSingleEvent(GamePackage package, string evtName, string name, GameEventHandlerDelegate gameHandlerDelegate)
    {
      if (string.IsNullOrEmpty(evtName) || string.IsNullOrEmpty(name) || gameHandlerDelegate == null)
      {
        Log.W(TAG, "Missing argument.", evtName);
        GameErrorChecker.LastError = GameError.ParamNotProvide;
        return null;
      }
      if (!singleEvents.ContainsKey(evtName))
        RegisterSingleEvent(evtName);
      var oldHandler = singleEvents[evtName];
      if (oldHandler != null)
      {
        Log.W(TAG, "Single event {0} is already subscribed by {1}.", evtName, oldHandler.Name);
        GameErrorChecker.LastError = GameError.NotRegister;
        return null;
      }

      GameEventHandler gameHandler = new GameEventHandler(package, (handler) => {
        UnsubscribeSingleEvent(package, evtName, handler);
      }, name, gameHandlerDelegate);
      singleEvents[evtName] = gameHandler;
      return gameHandler;
    }
    /// <summary>
    /// Unsubscribe from a global single-handler event.
    /// </summary>
    /// <param name="package">Owning package.</param>
    /// <param name="evtName">Event name.</param>
    /// <param name="name">Handler name.</param>
    /// <param name="gameHandler">Registered handler instance.</param>
    /// <returns>Whether the operation succeeded.</returns>
    public bool UnsubscribeSingleEvent(GamePackage package, string evtName, GameEventHandler gameHandler)
    {
      if (string.IsNullOrEmpty(evtName) || gameHandler == null)
      {
        Log.W(TAG, "Missing argument.", evtName);
        GameErrorChecker.LastError = GameError.ParamNotProvide;
        return false;
      }

      if (!singleEvents.ContainsKey(evtName))
      {
        GameErrorChecker.LastError = GameError.NotRegister;
        return false;
      }
      var oldHandler = singleEvents[evtName];
      if (oldHandler != gameHandler)
      {
        Log.W(TAG, "Single event {0} is subscribed by {1}; only that subscriber may unsubscribe it.", evtName, oldHandler.Name);
        GameErrorChecker.LastError = GameError.AccessDenined;
        return false;
      }

      singleEvents[evtName] = null;
      return true;
    }


    #endregion

    #region Global events

    /// <summary>
    /// Register an event.
    /// </summary>
    /// <param name="evtName">Event name.</param>
    public GameEvent RegisterGlobalEvent(string evtName)
    {
      GameEvent gameEvent;

      if (string.IsNullOrEmpty(evtName))
      {
        Log.W(TAG, "RegisterGlobalEvent: evtName was not provided.");
        GameErrorChecker.LastError = GameError.ParamNotProvide;
        return null;
      }
      if (IsGlobalEventRegistered(evtName, out gameEvent))
        return gameEvent;

      gameEvent = new GameEvent(evtName);
      events.Add(evtName, gameEvent);
      return gameEvent;
    }
    /// <summary>
    /// Unregister an event.
    /// </summary>
    /// <param name="evtName">Event name.</param>
    public bool UnRegisterGlobalEvent(string evtName)
    {
      if (string.IsNullOrEmpty(evtName))
      {
        Log.W(TAG, "UnRegisterGlobalEvent: evtName was not provided.");
        GameErrorChecker.LastError = GameError.ParamNotProvide;
        return false;
      }
      if (IsGlobalEventRegistered(evtName, out GameEvent gameEvent))
      {
        gameEvent.Dispose();
        events.Remove(evtName);
        return true;
      }
      else
      {
        GameErrorChecker.LastError = GameError.NotRegister;
        return false;
      }
    }
    /// <summary>
    /// Whether an event is registered.
    /// </summary>
    /// <param name="evtName">Event name.</param>
    /// <returns>Whether it is registered.</returns>
    public bool IsGlobalEventRegistered(string evtName)
    {
      return events.ContainsKey(evtName);
    }
    /// <summary>
    /// Check registration and return the event instance if present.
    /// </summary>
    /// <param name="evtName">Event name.</param>
    /// <param name="e">Returned event instance.</param>
    /// <returns>Whether it is registered.</returns>
    public bool IsGlobalEventRegistered(string evtName, out GameEvent e)
    {
      if (events.TryGetValue(evtName, out e))
        return true;
      e = null;
      return false;
    }
    /// <summary>
    /// Get an event instance.
    /// </summary>
    /// <param name="evtName">Event name.</param>
    /// <returns>Event instance.</returns>
    public GameEvent GetRegisteredGlobalEvent(string evtName)
    {
      GameEvent gameEvent = null;

      if (string.IsNullOrEmpty(evtName))
      {
        Log.W(TAG, "GetRegisteredGlobalEvent: evtName was not provided.");
        GameErrorChecker.LastError = GameError.ParamNotProvide;
        return gameEvent;
      }

      events.TryGetValue(evtName, out gameEvent);
      return gameEvent;
    }

    /// <summary>
    /// Dispatch an event after a delay.
    /// </summary>
    /// <param name="gameEvent">Event instance.</param>
    /// <param name="delayeSecond">Delay in seconds.</param>
    /// <param name="pararms">Event arguments.</param>
    /// <returns>Number of handlers notified.</returns>
    public bool DelayedDispatchGlobalEvent(string evtName, float delayeSecond, params object[] pararms)
    {
      if (string.IsNullOrEmpty(evtName))
      {
        Log.W(TAG, "NotifySingleEvent: evtName was not provided.");
        GameErrorChecker.LastError = GameError.ParamNotProvide;
        return false;
      }
      if (!IsGlobalEventRegistered(evtName, out GameEvent gameEvent))
      {
        Log.W(TAG, "Event {0} is not registered.", evtName);
        GameErrorChecker.LastError = GameError.NotRegister;
      }
      DelayCaller.AddDelayCallNormal(evtName, delayeSecond, pararms);
      return true;
    }

    /// <summary>
    /// Dispatch an event.
    /// </summary>
    /// <param name="gameEvent">Event instance.</param>
    /// <param name="pararms">Event arguments.</param>
    /// <returns>Number of handlers notified.</returns>
    public int DispatchGlobalEvent(GameEvent gameEvent, params object[] pararms)
    {
      Profiler.BeginSample("DispatchGlobalEvent_" + gameEvent.EventName);

      int handledCount = 0;
      if (gameEvent == null)
      {
        Log.W(TAG, "DispatchGlobalEvent: gameEvent was not provided.");
        GameErrorChecker.LastError = GameError.ParamNotProvide;
        return handledCount;
      }

      // Dispatch the event.
      GameEventHandler gameHandler;
      for (int i = gameEvent.EventHandlers.Count - 1; i >= 0; i--)
      {
        gameHandler = gameEvent.EventHandlers[i];
        if (gameHandler.Destroyed)
          gameEvent.EventHandlers.RemoveAt(i);
        handledCount++;
        if (gameHandler.CallEventHandler(gameEvent.EventName, pararms))
        {
          Log.D(TAG, "Event {0} was interrupted by : {1}", gameEvent.EventName, gameHandler.Name);
          break;
        }
      }

      Profiler.EndSample();
      return handledCount;
    }
    /// <summary>
    /// Dispatch an event.
    /// </summary>
    /// <param name="evtName">Event name.</param>
    /// <param name="pararms">Event arguments.</param>
    /// <returns>Number of handlers notified.</returns>
    public int DispatchGlobalEvent(string evtName, params object[] pararms)
    {
      int handledCount = 0;

      if (string.IsNullOrEmpty(evtName))
      {
        Log.W(TAG, "DispatchGlobalEvent: evtName was not provided.");
        GameErrorChecker.LastError = GameError.ParamNotProvide;
        return 0;
      }
      if (IsGlobalEventRegistered(evtName, out GameEvent gameEvent))
        return DispatchGlobalEvent(gameEvent, pararms);
      else
        GameErrorChecker.LastError = GameError.NotRegister;
      return handledCount;
    }
    
    /// <summary>
    /// Register a global event handler delegate.
    /// </summary>
    /// <param name="evtName">Event name.</param>
    /// <param name="name">Handler name.</param>
    /// <param name="gameHandlerDelegate">Callback.</param>
    /// <returns>Registered handler, which can be used to unsubscribe.</returns>
    public GameEventHandler RegisterEventHandler(GamePackage package, string evtName, string name, GameEventHandlerDelegate gameHandlerDelegate)
    {
      if (string.IsNullOrEmpty(evtName)
         || string.IsNullOrEmpty(name)
         || gameHandlerDelegate == null)
      {
        Log.W(TAG, "Missing argument.", evtName);
        GameErrorChecker.LastError = GameError.ParamNotProvide;
        return null;
      }

      if (!IsGlobalEventRegistered(evtName, out GameEvent gameEvent))
        gameEvent = RegisterGlobalEvent(evtName);

      GameEventHandler gameHandler = new GameEventHandler(package, (handler) => {
        UnRegisterEventHandler(evtName, handler);
      }, name, gameHandlerDelegate);
      gameEvent.EventHandlers.Add(gameHandler);
      return gameHandler;
    }
    /// <summary>
    /// Unregister a global event handler.
    /// </summary>
    /// <param name="evtName">Event name.</param>
    /// <param name="handler">Handler instance.</param>
    public void UnRegisterEventHandler(string evtName, GameEventHandler handler)
    {
      if (string.IsNullOrEmpty(evtName) || handler == null)
      {
        Log.W(TAG, "Missing argument.", evtName);
        GameErrorChecker.LastError = GameError.ParamNotProvide;
        return;
      }

      if (IsGlobalEventRegistered(evtName, out GameEvent gameEvent))
        gameEvent.EventHandlers.Remove(handler);
      else
        GameErrorChecker.LastError = GameError.NotRegister;
    }

    #endregion

    // Unload all events.
    private void UnLoadAllEvents()
    {
      foreach (var gameEvent in events)
        gameEvent.Value.Dispose();
      events.Clear();
      eventEmitts.Clear();
      singleEvents.Clear();
    }
    private void InitAllEvents()
    {
      // Register built-in events.
      RegisterGlobalEvent(GameEventNames.EVENT_BASE_INIT_FINISHED);
      RegisterGlobalEvent(GameEventNames.EVENT_BEFORE_GAME_QUIT);
      RegisterGlobalEvent(GameEventNames.EVENT_LOGIC_SECNSE_ENTER);
      RegisterGlobalEvent(GameEventNames.EVENT_LOGIC_SECNSE_QUIT);
    }

    #endregion

    #region Debug commands

    private void InitCommands()
    {
      GameManager.Instance.GameDebugCommandServer.RegisterCommand("gm", (keyword, full, argsCount, args) =>
      {
        switch (args[0])
        {
          case "single_event":
            return HandleSingleEventCommand(args);
          case "event":
            return HandleEventCommand(args);
        }
        return false;
      }, 1, "gm <single_event/event/action/store>\n" +
              "  single_event\n" +
              "    all Show all single events\n" +
              "    notify <eventName:string> [params:any[]] Notify a single event\n" +
              "  event\n" +
              "    all Show all global events\n" +
              "    dispatch <eventName:string> <handleFilter:string> [params:any[]] Dispatch a global event; use '*' for all handlers or a regular expression to filter them.\n");
    }
    private bool HandleSingleEventCommand(string[] args)
    {
      string act = "";
      if (!DebugUtils.CheckDebugParam(1, args, out act)) return false;
      switch (act)
      {
        case "all":
          {
            StringBuilder stringBuilder = new StringBuilder("Single events: ");
            stringBuilder.AppendLine(events.Count.ToString());
            foreach (var i in singleEvents)
              stringBuilder.AppendLine(string.Format("{0} => {1}", i.Key, i.Value == null ? "(null)" : i.Value.Name));
            Log.V(TAG, stringBuilder.ToString());
            return true;
          }
        case "notify":
          {
            string name = "";
            if (!DebugUtils.CheckDebugParam(2, args, out name)) return false;
            if (NotifySingleEvent(name, StringUtils.TryConvertStringArrayToValueArray(args, 3)))
              Log.V(TAG, "NotifySingleEvent success");
            else
              Log.V(TAG, "NotifySingleEvent failed");
            return true;
          }
      }
      return false;
    }
    private bool HandleEventCommand(string[] args)
    {
      string act = "";
      if (!DebugUtils.CheckDebugParam(1, args, out act)) return false;
      switch (act)
      {
        case "all":
          {
            StringBuilder stringBuilder = new StringBuilder("Events: ");
            stringBuilder.AppendLine(events.Count.ToString());
            foreach (var i in events)
            {
              stringBuilder.AppendLine(string.Format("{0} => Handlers: {1}", i.Key, i.Value.EventHandlers.Count));
              foreach (var h in i.Value.EventHandlers)
              {
                stringBuilder.Append("  ");
                stringBuilder.AppendLine(h.Name);
              }
            }
            Log.V(TAG, stringBuilder.ToString());
            return true;
          }
        case "dispatch":
          {
            string name = "";
            string filter = "";
            if (!DebugUtils.CheckDebugParam(2, args, out name)) return false;
            if (!DebugUtils.CheckDebugParam(3, args, out filter)) return false;

            int handlers = DispatchGlobalEvent(name, filter, StringUtils.TryConvertStringArrayToValueArray(args, 4));
            Log.V(TAG, "DispatchGlobalEvent finish > {0}", handlers);
            return true;
          }
      }
      return false;
    }

    #endregion
  }
}
