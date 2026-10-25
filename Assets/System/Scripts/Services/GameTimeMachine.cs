/*
* Copyright(c) 2021  mengyu
*
* Module:
* GameTimeMachine.cs
* 
* Purpose:
* Update scheduling manager.
* TimeMachine provides callbacks similar to MonoBehaviour.Update for convenient time-driven code.
*
* Author:
* mengyu
*/

using System;
using System.Collections.Generic;

namespace Ballance2.Services
{
  /// <summary>
  /// Update scheduling manager.
  /// </summary>
  public class GameTimeMachine : GameService<GameTimeMachine>
  {
    private const string TAG = "GameTimeMachine";

    public GameTimeMachine() : base(TAG) {}

    public override bool Initialize()
    {
      base.Initialize();
      return true;
    }
    
    public override void Destroy()
    { 
      updates.Clear();
      lateUpdates.Clear();
      fixUpdates.Clear();
      base.Destroy();
    }

    /// <summary>
    /// Registration handle used to unregister an update callback.
    /// </summary>
    public class GameTimeMachineTimeTicket {

      private GameTimeMachine service = null;
      private int type;
      internal int sleepTick = 0;
      internal int errTickCount = 0;

      public GameTimeMachineTimeTicket(GameTimeMachine service, Action updateAction, int order, int interval, int type) {
        this.service = service;
        this.type = type;
        this.updateAction = updateAction;
        this.order = order;
        this.interval = interval;
      }
      
      /// <summary>
      /// Get the registered update callback.
      /// </summary>
      public Action updateAction { get; }
      /// <summary>
      /// Update order; lower values run first.
      /// </summary>
      public int order { get; }
      /// <summary>
      /// Frame interval, default 1; 2 runs every other frame. Can be changed at runtime.
      /// </summary>     
      public int interval;
      /// <summary>
      /// Whether this update registration is enabled; can be changed at runtime.
      /// </summary>     
      public bool enable { get; private set; } = true;

      /// <summary>
      /// Disable this update callback.
      /// </summary>      
      public void Disable() {
        enable = false;
      }
      /// <summary>
      /// Re-enable this update callback.
      /// </summary>      
      public void Enable() {
        errTickCount = 0;
        enable = true;
      }   
      /// <summary>
      /// Unregister this update callback.
      /// </summary>      
      public void Unregister() {
        switch(type) {
          case 1: 
            service.updates.Remove(this);
            break;
          case 2: 
            service.lateUpdates.Remove(this);
            break;
          case 3: 
            service.fixUpdates.Remove(this);
            break;
        }
      }
    }

    private LinkedList<GameTimeMachineTimeTicket> updates = new LinkedList<GameTimeMachineTimeTicket>();
    private LinkedList<GameTimeMachineTimeTicket> lateUpdates = new LinkedList<GameTimeMachineTimeTicket>();
    private LinkedList<GameTimeMachineTimeTicket> fixUpdates = new LinkedList<GameTimeMachineTimeTicket>();

    private void InsertTimeMachineTimeTicketToList(GameTimeMachineTimeTicket ticket, LinkedList<GameTimeMachineTimeTicket> list) {
      // Insert directly according to order.
      LinkedListNode<GameTimeMachineTimeTicket> item = list.First;
      while(item != null) {
        if(item.Value.order >= ticket.order) {
          list.AddBefore(item, ticket);
          return;
        }
        item = item.Next;
      }
      // End of the list.
      list.AddFirst(ticket);
    } 
    private void RemoveActionInList(Action updateAction, LinkedList<GameTimeMachineTimeTicket> list) {
      LinkedListNode<GameTimeMachineTimeTicket> item = list.First;
      while(item != null) {
        if(item.Value.updateAction == updateAction)
          list.Remove(item);
        item = item.Next;
      }
    }
    private void DoFlushUpdateList(string name, LinkedList<GameTimeMachineTimeTicket> list) {
      GameTimeMachineTimeTicket current = null;
      LinkedListNode<GameTimeMachineTimeTicket> item = list.First;
      while(item != null) {
        current = item.Value;
        if(current.enable) {
          // Sleep.
          if(current.interval > 0) {
            if(current.sleepTick > 0) {
              current.sleepTick--;
              continue;
            }
            current.sleepTick = current.interval;
          }

          // Execute.
          try {
            current.updateAction.Invoke();
            if(current.errTickCount > 0)
              current.errTickCount --;
          } catch(Exception e) {
            current.errTickCount++;
            if(current.errTickCount < 5) 
              Log.W(TAG, name + " TimeMachine encountered an exception more than {0} times in {1} order: {2} interval: {3}. exception: {4}", current.errTickCount, current.updateAction.ToString(), current.order, current.interval, e.ToString());
            else {
              current.Disable();
              Log.E(TAG, name + " TimeMachine encountered an exception more than 5 times in {0} order: {1} interval: {2}, and it was disabled. exception: {3}", current.updateAction.ToString(), current.order, current.interval, e.ToString());
            }
          }
        }
        item = item.Next;
      }
    }

    /// <summary>
    /// Register an Update callback.
    /// </summary>
    /// <param name="updateAction">Update callback.</param>
    /// <param name="order">Update order; lower values run first.</param>
    /// <param name="interval">Frame interval, default 1; 2 runs every other frame. Can be changed at runtime.</param>
    /// <returns>Registration handle used to unregister the callback.</returns>
    public GameTimeMachineTimeTicket RegisterUpdate(Action updateAction, int order = 0, int interval = 0) {
      var ticket = new GameTimeMachineTimeTicket(this, updateAction, order, interval, 1);
      InsertTimeMachineTimeTicketToList(ticket, updates);
      return ticket;
    }
    /// <summary>
    /// Unregister an Update callback.
    /// </summary>
    /// <param name="updateAction">Update callback.</param>
    public void UnRegisterUpdate(Action updateAction) {
      RemoveActionInList(updateAction, updates);
    }

    /// <summary>
    /// Register a LateUpdate callback.
    /// </summary>
    /// <param name="updateAction">Update callback.</param>
    /// <param name="order">Update order; lower values run first.</param>
    /// <param name="interval">Frame interval, default 1; 2 runs every other frame. Can be changed at runtime.</param>
    /// <returns>Registration handle used to unregister the callback.</returns>
    public GameTimeMachineTimeTicket RegisterLateUpdate(Action updateAction, int order = 0, int interval = 0) {
      var ticket = new GameTimeMachineTimeTicket(this, updateAction, order, interval, 2);
      InsertTimeMachineTimeTicketToList(ticket, lateUpdates);
      return ticket;
    }
    /// <summary>
    /// Unregister a LateUpdate callback.
    /// </summary>
    /// <param name="updateAction">Update callback.</param>
    public void UnRegisterLateUpdate(Action updateAction) {
      RemoveActionInList(updateAction, lateUpdates);
    }

    /// <summary>
    /// Register a FixedUpdate callback.
    /// </summary>
    /// <param name="updateAction">Update callback.</param>
    /// <param name="order">Update order; lower values run first.</param>
    /// <param name="interval">Frame interval, default 1; 2 runs every other frame. Can be changed at runtime.</param>
    /// <returns>Registration handle used to unregister the callback.</returns>
    public GameTimeMachineTimeTicket RegisterFixedUpdate(Action updateAction, int order = 0, int interval = 0) {
      var ticket = new GameTimeMachineTimeTicket(this, updateAction, order, interval, 3);
      InsertTimeMachineTimeTicketToList(ticket, fixUpdates);
      return ticket;
    }
    /// <summary>
    /// Unregister a FixedUpdate callback.
    /// </summary>
    /// <param name="updateAction">Update callback.</param>
    public void UnRegisterFixedUpdate(Action updateAction) {
      RemoveActionInList(updateAction, fixUpdates);
    }

    public static int FixedUpdateTick = 0;

    protected override void Update() {
      DoFlushUpdateList("Update", updates);
    }
    private void LateUpdate() {
      DoFlushUpdateList("LateUpdate", lateUpdates);
    }
    protected override void FixedUpdate() {
      if(FixedUpdateTick < 1024) FixedUpdateTick++;
      else FixedUpdateTick = 0;

      DoFlushUpdateList("FixedUpdate", fixUpdates);
    }

  }
}