using System;
using UnityEngine;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* GameAssetsInfo.cs
* 
* Purpose:
* Static prefab reference structure.
*
* Author:
* mengyu
*/

namespace Ballance2.Res
{
    [Serializable]
    public class GameObjectInfo
    {
        public GameObject Object;
        public string Name;

        public override string ToString() { return Name; }
    }
}
