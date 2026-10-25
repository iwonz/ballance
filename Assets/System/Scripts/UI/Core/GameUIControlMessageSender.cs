using UnityEngine;

/*
* Copyright(c) 2022 mengyu
*
* Module:
* GameUIControlMessageSender.cs
* 
* Purpose:
* Send UI events from a bound control.
*
* Attach this component to a control such as a button and set MessageCenterName;
* the center is resolved at startup. Alternatively assign MessageCenter directly.
* In the editor, bind onClick to this GameUIControlMessageSender's
* NotifyEvent method, passing an event name registered with GameUIMessageCenter.SubscribeEvent.
* Clicking the button then sends the event to the message center for code to handle.
*
* Author:
* mengyu
*/

namespace Ballance2.UI.Core
{
  /// <summary>
  /// Send UI events from a bound control.
  /// </summary>
  [AddComponentMenu("Ballance/UI/MessageSender")]
  [RequireComponent(typeof(RectTransform))]
  public class GameUIControlMessageSender : MonoBehaviour
  {
    /// <summary>
    /// Target UI message center name.
    /// </summary>
    public string MessageCenterName = null;

    public GameUIMessageCenter MessageCenter = null;

    private void Awake()
    {
      MessageCenter = GameUIMessageCenter.FindGameUIMessageCenter(MessageCenterName);
    }
    private void Start()
    {
      MessageCenter = GameUIMessageCenter.FindGameUIMessageCenter(MessageCenterName);
    }
    private void OnDestroy()
    {
    }

    /// <summary>
    /// Send an event to the message center.
    /// </summary>
    /// <param name="name">Event name.</param>
    public void NotifyEvent(string name)
    {
      if (MessageCenter != null)
        MessageCenter.NotifyEvent(name);
    }
  }
}
