using UnityEngine;

/*
* Copyright(c) 2022 mengyu
*
* Module:
* GameDebugModulEntry.cs
* 
* Purpose:
* Custom mechanism/level debug scene configuration.
*
* Author:
* mengyu
*/

namespace Ballance2.Entry
{
  public class GameDebugEntry : MonoBehaviour
  {
    public static GameDebugEntry Instance { get; private set; }

    public GameDebugEntry() {
      Instance = this;
    }
    
    [Tooltip("Mechanism name. The matching placeholder object will be replaced by the mechanism.")]
    public string ModulName = "";
    [Tooltip("Mechanism instance.")]
    public GameObject ModulInstance = null;
    [Tooltip("Test floor instance for the mechanism.")]
    public GameObject ModulTestFloor = null; 
    [Tooltip("Test UI instance for the mechanism.")]
    public GameObject ModulTestUI = null; 
    
    [Tooltip("Level name used by LevelCustomDebug.")]
    public string LevelName = "level01";
  }
}
