using System.Collections.Generic;
using UnityEngine;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* GameUIControlValueBinder.cs
* 
* Purpose:
* UI control data binder.
*
* Author:
* mengyu
*/

namespace Ballance2.UI.Core
{
  /// <summary>
  /// UI control data binder.
  /// </summary>
  [AddComponentMenu("Ballance/UI/ValueBinder/Custom")]
  [RequireComponent(typeof(RectTransform))]
  public class GameUIControlValueBinder : MonoBehaviour
  {
    /// <summary>
    /// Target UI message center name.
    /// </summary>
    public string MessageCenterName = null;

    /// <summary>
    /// Binder name used for lookup in the UI message center.
    /// </summary>
    public string Name = "";

    public List<GameUIControlValueBinderUserUpdateCallback> UserUpdateCallbacks = new List<GameUIControlValueBinderUserUpdateCallback>();
    public GameUIControlValueBinderSupplierCallback BinderSupplierCallback => OnBinderSupplierHandle;
    public GameUIMessageCenter MessageCenter = null;

    protected virtual bool OnBinderSupplierHandle(object value) {
      return false;
    }

    private void Start()
    {
      if (!string.IsNullOrEmpty(MessageCenterName)) {
        MessageCenter = GameUIMessageCenter.FindGameUIMessageCenter(MessageCenterName);
        if (MessageCenter != null)
          MessageCenter.RegisterValueBinder(this);
        else
          Log.W("GameUIControlValueBinder:" + Name, "Failed to find GameUIMessageCenter {0}", MessageCenterName);
      }
      BinderBegin();
    }
    private void Awake()
    {
      if (!string.IsNullOrEmpty(MessageCenterName))
        MessageCenter = GameUIMessageCenter.FindGameUIMessageCenter(MessageCenterName);
    }
    private void OnDestroy()
    {
      if (MessageCenter != null)
        MessageCenter.UnRegisterValueBinder(this);
    }

    protected virtual void BinderBegin() { }

    /// <summary>
    /// Notify a UI value update.
    /// </summary>
    /// <param name="newval">New value.</param>
    public void NotifyUserUpdate(object newval)
    {
      UserUpdateCallbacks.ForEach((a) => a.Invoke(newval));
    }
  }

  public delegate bool GameUIControlValueBinderSupplierCallback(object value);
  public delegate void GameUIControlValueBinderUserUpdateCallback(object value);
}
