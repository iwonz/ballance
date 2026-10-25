using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.UI;

/*
* Copyright(c) 2023 mengyu
*
* Module:
* KeyChooseMutexGroup.cs
* 
* Purpose:
* Enforce mutual exclusion between key selection controls.
*
* Author:
* mengyu
*/

namespace Ballance2.UI.Core.Controls
{
  /// <summary>
  /// Enforce mutual exclusion between key selection controls.
  /// </summary>
  public class KeyChooseMutexGroup : MonoBehaviour
  {
    public KeyChoose[] KeyChooses = new KeyChoose[0];
    private Dictionary<KeyChoose, UnityAction<KeyCode>> KeyChooseListeners = new Dictionary<KeyChoose, UnityAction<KeyCode>>();

    private void Start() {
      foreach (var item in KeyChooses)
        AddKeyChooseMutexItem(item);
    }

    private void CheckMutexAndReset(KeyCode code, KeyChoose self) { 
      if (code == KeyCode.None)
        return;
      foreach (var item in KeyChooses)
      {
        if (item.value == code && item != self)
          item.value = KeyCode.None;
      }
    }

    public void AddKeyChooseMutexItem(KeyChoose choose) {
      UnityAction<KeyCode> listener = (code) => CheckMutexAndReset(code, choose);
      choose.onValueChanged.AddListener(listener);
      KeyChooseListeners.Add(choose, listener);
    }
    public void RemoveKeyChooseMutexItem(KeyChoose choose) {
      if (KeyChooseListeners.TryGetValue(choose, out var listener)) {
        choose.onValueChanged.RemoveListener(listener);
        KeyChooseListeners.Remove(choose);
      }
    }
  }
}
