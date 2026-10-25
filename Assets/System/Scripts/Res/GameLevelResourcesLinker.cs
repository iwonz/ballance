using UnityEngine;
using System.Collections.Generic;
using SubjectNerd.Utilities;

/*
* Copyright(c) 2022 mengyu
*
* Module:
* GameLevelResourcesLinker.cs
* 
* Purpose:
* Allow custom levels to supply custom resources.
*
* Author:
* mengyu
*
*/

namespace Ballance2 {
  /// <summary>
  /// Level resource linker.
  /// </summary>
  public class GameLevelResourcesLinker : MonoBehaviour {

    internal static Dictionary<string, GameLevelResourcesLinker> allRes = new Dictionary<string, GameLevelResourcesLinker>();

    private void Start() {
      if(allRes.ContainsKey(Name))
        Log.W("GameLevelResourcesLinker", "The name " + Name + " already exists in allRes!");
      else
        allRes.Add(Name, this);
    }

    /// <summary>
    /// Find a named level resource linker.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <returns></returns>
    public GameLevelResourcesLinker GetLevelResourcesLinker(string name) {
      if(allRes.TryGetValue(name, out var res))
        return res;
      return null;
    }

    /// <summary>
    /// Level resource linker name.
    /// </summary>
    public string Name = "";
    /// <summary>
    /// Audio resources.
    /// </summary>
    [Reorderable("AudioAssets", false)]
    public List<AudioClip> AudioAssets = null;
    /// <summary>
    /// Texture resources.
    /// </summary>
    [Reorderable("TextureAssets", false)]
    public List<Texture> TextureAssets = null;
  }
}