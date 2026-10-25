using UnityEngine;
using UnityEngine.EventSystems;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* EventTriggerListener.cs
* 
* Purpose:
* Legacy UI event listener; prefer binding through UGUI events for new controls.
*
* Author:
* mengyu
*/

namespace Ballance2.Services.InputManager
{
  public delegate void GameObjectDelegate(GameObject go);
  
  /// <summary>
  /// UI event listener.
  /// </summary>
  public class EventTriggerListener : EventTrigger
  {
    /// <summary>
    /// Pointer click event.
    /// </summary>
    public GameObjectDelegate onClick;
    /// <summary>
    /// Pointer down event.
    /// </summary>
    public GameObjectDelegate onDown;
    /// <summary>
    /// Pointer enter event.
    /// </summary>
    public GameObjectDelegate onEnter;
    /// <summary>
    /// Pointer exit event.
    /// </summary>
    public GameObjectDelegate onExit;
    /// <summary>
    /// Pointer up event.
    /// </summary>
    
    public GameObjectDelegate onUp;
    public GameObjectDelegate onSelect;
    public GameObjectDelegate onUpdateSelect;

    /// <summary>
    /// Create an event listener on a GameObject.
    /// </summary>
    /// <param name="go">GameObject.</param>
    /// <returns>Event listener instance.</returns>
    static public EventTriggerListener Get(GameObject go)
    {
      EventTriggerListener listener = go.GetComponent<EventTriggerListener>();
      if (listener == null) listener = go.AddComponent<EventTriggerListener>();
      return listener;
    }

    
    public override void OnPointerClick(PointerEventData eventData)
    {
      if (onClick != null) onClick(gameObject);
      else base.OnPointerClick(eventData);
    }
    
    public override void OnPointerDown(PointerEventData eventData)
    {
      if (onDown != null) onDown(gameObject);
      else base.OnPointerDown(eventData);
    }
    
    public override void OnPointerEnter(PointerEventData eventData)
    {
      if (onEnter != null) onEnter(gameObject);
      else base.OnPointerEnter(eventData);
    }
    
    public override void OnPointerExit(PointerEventData eventData)
    {
      if (onExit != null) onExit(gameObject);
      else base.OnPointerExit(eventData);
    }
    
    public override void OnPointerUp(PointerEventData eventData)
    {
      if (onUp != null) onUp(gameObject);
      else base.OnPointerUp(eventData);
    }
    
    public override void OnSelect(BaseEventData eventData)
    {
      if (onSelect != null) onSelect(gameObject);
      else base.OnSelect(eventData);
    }
    
    public override void OnUpdateSelected(BaseEventData eventData)
    {
      if (onUpdateSelect != null) onUpdateSelect(gameObject);
      else base.OnUpdateSelected(eventData);
    }
  }
}