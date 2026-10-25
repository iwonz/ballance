using System;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* GameAssetsInfo.cs
* 
* Purpose:
* Static game resource reference structure.
*
* Author:
* mengyu
*/

namespace Ballance2.Res
{
    [Serializable]
    public class GameAssetsInfo
    {
        public UnityEngine.Object Object;
        public string Name;

        public override string ToString() { return Name; }
    }
}
