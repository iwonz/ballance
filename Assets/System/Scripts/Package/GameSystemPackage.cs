/*
* Copyright(c) 2021  mengyu
*
* Module:
* GameSystemPackage.cs
* 
* Purpose:
* Specialized core code package definition.
*
* Author:
* mengyu
*/

using System.IO;
using System.Collections.Generic;
using System.Xml;
using Ballance2.Config;
using Ballance2.Services;
using UnityEngine;
using Ballance2.Res;
using System.Text;
using Ballance2.Game;

namespace Ballance2.Package
{
  class GameSystemPackage : GameZipPackage
  {
    public GameSystemPackage() {
      PackageName = GamePackageManager.SYSTEM_PACKAGE_NAME;
      PackageEntry = GameCorePackageEntry.Main();
      Type = GamePackageType.Module;
      _Status = GamePackageStatus.LoadSuccess;
      SetFlag(GetFlag() | (GamePackage.FLAG_PACK_NOT_UNLOADABLE | GamePackage.FLAG_PACK_SYSTEM_PACKAGE));
    }

    public override string ListResource() {
      StringBuilder sb = new StringBuilder();
      sb.Append("[System internal package]");
      return base.ListResource();
    }
  }
}
