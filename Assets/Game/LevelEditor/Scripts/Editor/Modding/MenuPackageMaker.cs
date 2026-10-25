using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ballance2.Editor.Modding
{
  class MenuPackageMaker
  {
    [@MenuItem("Ballance/Package Tools/Create Package Template", false, 100)]
    static void MakeModFile()
    {
      EditorWindow.GetWindowWithRect(typeof(WindowPackageMaker), new Rect(200, 150, 450, 400));
    }
    [@MenuItem("Ballance/Package Tools/Pack Package", false, 100)]
    static void PackModFile()
    {
      EditorWindow.GetWindowWithRect(typeof(WindowPackagePacker), new Rect(200, 150, 450, 550));
    }
    [@MenuItem("Ballance/Package Tools/Pack All Packages to Debug Folder", false, 100)]
    static void PackModFileAll()
    {
      EditorWindow.GetWindowWithRect(typeof(WindowPackageMakerAll), new Rect(200, 150, 350, 150));
    }


  }
}
