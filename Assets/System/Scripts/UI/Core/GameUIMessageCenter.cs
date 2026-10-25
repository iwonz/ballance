using System.Collections.Generic;
using UnityEngine;
using static Ballance2.Services.GameManager;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* GameUIMessageUtils.cs
* 
* Purpose:
* UI message center for event handling.
*
* Provides simple event binding and synchronized value binding.
*
* Author:
* mengyu
*/

namespace Ballance2.UI.Core
{
  /// <summary>
  /// UI message center for event handling.
  /// </summary>
  [RequireComponent(typeof(RectTransform))]
  [AddComponentMenu("Ballance/UI/MessageCenter")]
  public class GameUIMessageCenter : MonoBehaviour
  {

    private static Dictionary<string, GameUIMessageCenter> messageCenters = new Dictionary<string, GameUIMessageCenter>();

    /// <summary>
    /// Find a UI message center.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <returns>UI message center instance, or null if absent.</returns>
    public static GameUIMessageCenter FindGameUIMessageCenter(string name)
    {
      if (messageCenters.ContainsKey(name))
        return messageCenters[name];
      return null;
    }

    private Dictionary<string, GameUIControlValueBinder> valueBinders = new Dictionary<string, GameUIControlValueBinder>();
    private Dictionary<string, List<VoidDelegate>> events = new Dictionary<string, List<VoidDelegate>>();

    /// <summary>
    /// Register a data binder; called automatically.
    /// </summary>
    /// <param name="binder"></param>
    /// <returns></returns>
    public bool RegisterValueBinder(GameUIControlValueBinder binder)
    {
      if (!valueBinders.ContainsKey(binder.Name))
      {
        valueBinders[binder.Name] = binder;
        return true;
      }
      return false;
    }

    /// <summary>
    /// Unregister a data binder; called automatically.
    /// </summary>
    /// <param name="binder"></param>
    /// <returns></returns>
    public bool UnRegisterValueBinder(GameUIControlValueBinder binder)
    {
      if (valueBinders.ContainsKey(binder.Name))
      {
        valueBinders.Remove(binder.Name);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Subscribe to a data binder.
    /// </summary>
    /// <param name="binderName">Data binder name.</param>
    /// <param name="callbackFun">Value update callback.</param>
    /// <returns>Callback that updates the control's displayed value.</returns>
    public GameUIControlValueBinderSupplierCallback SubscribeValueBinder(string binderName, GameUIControlValueBinderUserUpdateCallback callbackFun)
    {
      if (valueBinders.TryGetValue(binderName, out GameUIControlValueBinder binder))
      {
        binder.UserUpdateCallbacks.Add(callbackFun);
        return binder.BinderSupplierCallback;
      }
      return null;
    }
    public GameUIControlValueBinderSupplierCallback SubscribeValueBinder(GameUIControlValueBinder binder, GameUIControlValueBinderUserUpdateCallback callbackFun)
    {
      if (!valueBinders.ContainsKey(binder.Name))
        valueBinders[binder.Name] = binder;
      binder.UserUpdateCallbacks.Add(callbackFun);
      return binder.BinderSupplierCallback;
    }

    /// <summary>
    /// Get the control instance through its data binder.
    /// </summary>
    /// <param name="binderName">Data binder name.</param>
    /// <returns></returns>
    public GameObject GetComponentInstance(string binderName)
    {
      if (valueBinders.TryGetValue(binderName, out GameUIControlValueBinder binder))
        return binder.gameObject;
      return null;
    }

    /// <summary>
    /// Unsubscribe from a data binder.
    /// </summary>
    /// <param name="binderName">Data binder name.</param>
    /// <param name="callbackFun">Value update callback.</param>
    /// <returns>Whether the operation succeeded.</returns>
    public bool UnSubscribeValueBinder(string binderName, GameUIControlValueBinderUserUpdateCallback callbackFun)
    {
      if (valueBinders.TryGetValue(binderName, out GameUIControlValueBinder binder))
      {
        binder.UserUpdateCallbacks.Remove(callbackFun);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Subscribe to a single message.
    /// </summary>
    /// <param name="evtName">Message name.</param>
    /// <param name="callBack">Message callback.</param>
    public void SubscribeEvent(string evtName, VoidDelegate callBack)
    {
      if (!events.TryGetValue(evtName, out var handlers))
      {
        handlers = new List<VoidDelegate>();
        events.Add(evtName, handlers);
      }
      handlers.Add(callBack);
    }

    /// <summary>
    /// Unsubscribe from a single message.
    /// </summary>
    /// <param name="evtName">Message name.</param>
    /// <param name="callBack">Message callback.</param>
    public bool UnSubscribeEvent(string evtName, VoidDelegate callBack)
    {
      if (events.TryGetValue(evtName, out var handlers))
      {
        handlers.Remove(callBack);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Send a single message.
    /// </summary>
    /// <param name="evtName">Message name.</param>
    public void NotifyEvent(string evtName)
    {
      if (events.TryGetValue(evtName, out var handlers))
        handlers.ForEach((h) => h.Invoke());
    }

    private void Start()
    {
      if (!messageCenters.ContainsKey(Name))
        messageCenters.Add(Name, this);
    }
    private void OnDestroy()
    {
      messageCenters.Remove(Name);
    }

    [Tooltip("Message center name.")]
    public string Name = "";
  }
}
