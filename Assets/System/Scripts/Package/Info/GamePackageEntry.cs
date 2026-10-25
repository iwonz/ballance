/*
* Copyright(c) 2021  mengyu
*
* Module:
* GamePackage.cs
* 
* Purpose:
* Game package entry definition.
*
* Author:
* mengyu
*/

namespace Ballance2.Package
{
  /// <summary>
  /// Package entry callbacks and metadata.
  /// All package code lifecycle callbacks are exposed here.
  /// </summary>
  public class GamePackageEntry {

    // Event callbacks.

    /// <summary>
    /// Package load callback.
    /// </summary>
    public GamePackageEntryDelogate OnLoad;
    /// <summary>
    /// Package unload callback.
    /// </summary>
    public GamePackageEntryDelogate OnBeforeUnLoad;
    /// <summary>
    /// Called after UI initialization; register custom UI here.
    /// </summary>
    public GamePackageEntryDelogate OnLoadUI;
    /// <summary>
    /// Package version.
    /// </summary>
    public int Version = 0;
  }
  
  /// <summary>
  /// Package entry callback.
  /// </summary>
  /// <returns></returns>
  
  public delegate bool GamePackageEntryDelogate(GamePackage package);
}
