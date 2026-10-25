using Ballance2.Res;
using SubjectNerd.Utilities;
using System.Collections.Generic;
using UnityEngine;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* GameStaticResEntry.cs
* 
* Purpose:
* Static prefab resource references attached to GameEntry.
*
* Author:
* mengyu
*/

namespace Ballance2.Entry
{
  public class GameStaticResEntry : MonoBehaviour
  {
    /// <summary>
    /// Static prefab references.
    /// </summary>
    [Reorderable("GamePrefab", true, "Name")]
    public List<GameObjectInfo> GamePrefab = null;
    /// <summary>
    /// Static resource references.
    /// </summary>
    [Reorderable("GameAssets", true, "Name")]
    public List<GameAssetsInfo> GameAssets = null;

    private void Start()
    {
      GameSystem.FillResEntry(this);
    }
  }
}
