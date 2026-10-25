using UnityEngine;

/*
 * Copyright (c) 2021  mengyu
 * 
 * Module:
 * TiggerTester.cs
 * 
 * Purpose:
 * Trigger detection helper.
 * 
 * Author:
 * mengyu
 */

namespace Ballance2.Game
{
  /// <summary>
  /// Wrap OnTriggerEnter and OnTriggerExit for game trigger detection.
  /// </summary>
  public class TiggerTester : MonoBehaviour
  {
    public delegate void OnTiggerTesterEventCallback(GameObject self, GameObject other);

    /// <summary>
    /// Raised when a collider enters this trigger (OnTriggerEnter).
    /// </summary>
    public OnTiggerTesterEventCallback onTriggerEnter;
    /// <summary>
    /// Raised when a collider leaves this trigger (OnTriggerExit).
    /// </summary>
    public OnTiggerTesterEventCallback onTriggerExit;

    private void OnTriggerEnter(Collider other) {
      if(onTriggerEnter != null) 
        onTriggerEnter.Invoke(gameObject, other.gameObject);
    }    
    private void OnTriggerExit(Collider other) {
      if(onTriggerExit != null) 
        onTriggerExit.Invoke(gameObject, other.gameObject);
    }
  }
}