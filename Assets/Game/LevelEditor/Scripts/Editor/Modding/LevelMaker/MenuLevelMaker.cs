using System.IO;
using System.Text.RegularExpressions;
using Ballance2.Config.Settings;
using Ballance2.Res;
using UnityEditor;
using UnityEngine;

namespace Ballance2.Editor.Modding.LevelMaker
{
  class MenuLevelMaker
  {
    [@MenuItem("Ballance/Level Tools/Create Level Template", false, 101)]
    static void MakeLevelFile()
    {
      EditorWindow.GetWindowWithRect(typeof(WindowLevelMaker), new Rect(200, 150, 350, 200));
    }

    [@MenuItem("Ballance/Level Tools/Pack Level", false, 101)]
    static void PackLevelFile()
    {
      EditorWindow.GetWindowWithRect(typeof(WindowLevelPacker), new Rect(200, 150, 350, 400));
    }

    [@MenuItem("Ballance/Level Tools/Pack All Built-in Levels to Output", false, 102)]
    static void PackBuiltInLevelFile()
    {
      WindowChoosePlatform choosePlatformWindow = EditorWindow.GetWindowWithRect<WindowChoosePlatform>(new Rect(200, 150, 450, 250));
      choosePlatformWindow.OnChoose = (target) =>
      {
        if(!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target))
        {
          EditorUtility.DisplayDialog("Information", "This Unity installation cannot build for target "  + target + ". The corresponding module may be missing.", "OK");
          return;
        }

        DirectoryInfo direction = new DirectoryInfo(GamePathManager.DEBUG_LEVEL_FOLDER);
        DirectoryInfo[] dirs = direction.GetDirectories("*", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < dirs.Length; i++){
          var name = dirs[i].Name;

          EditorUtility.DisplayProgressBar("Packing", "Packing " + name, i / (float)dirs.Length);
          
          if(name != "MakerAssets" && Regex.IsMatch(name.ToLower(), "^level([0]{1}[0-9]{1})|([1]{1}[0-3]{1})$", RegexOptions.IgnoreCase)) {
            string p = GamePathManager.DEBUG_LEVEL_FOLDER + "/" + name + "/Level.json";
            var levelJsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>(p);
            if(levelJsonFile != null)
            {
              string err = LevelPacker.DoPackPackage(
                target, 
                levelJsonFile, 
                name, 
                DebugSettings.Instance.DebugFolder + "/" + target + "/Levels/"
              );
              if(!string.IsNullOrEmpty(err))
              {
                Debug.LogError("Packing level " + name + " failed. Error: " + err);
              }
            }
          }
        }
        
        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("Information", "Package built successfully!", "OK");
      };
      choosePlatformWindow.Show();
    }
  }
}
