using System.IO;
using UnityEditor;
using Ballance2.Config.Settings;
using Ballance2.Res;
using UnityEngine;

namespace Ballance2.Editor.Modding
{
  class MenuMakerTools
  {
    [@MenuItem("Ballance/Tools/Copy Debug Folder to Default Output", false, 103)]
    static void CopyDebugFolderToOutput()
    {
      WindowChoosePlatform choosePlatformWindow = EditorWindow.GetWindowWithRect<WindowChoosePlatform>(new Rect(200, 150, 450, 250));
      choosePlatformWindow.OnChoose = (target) =>
      {
        Debug.Log("OnChoose: " + target);

        string debugFolder = DebugSettings.Instance.DebugFolder + "/" + target;

        if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64)
        {
          // Copy directly to the Windows output directory.
          string folder = DebugSettings.Instance.OutputFolder;
          if (Directory.Exists(debugFolder) && Directory.Exists(folder))
          {
            CopyDebugFolder("Core", debugFolder, folder);
            CopyDebugFolder("Packages", debugFolder, folder);
            CopyDebugFolder("Levels", debugFolder, folder);

            EditorUtility.DisplayDialog("Information", "Copy complete.", "OK");
          }
          else EditorUtility.DisplayDialog("Error", "Output folder does not exist:\n" + debugFolder + "\n▼\n" + folder, "OK");
        }
        else if (target == BuildTarget.StandaloneLinux64)
        {
          // Copy directly to the Windows output directory.
          string folder = DebugSettings.Instance.OutputAppLinux + "/Ballance_Data/";
          if (Directory.Exists(debugFolder) && Directory.Exists(folder))
          {
            CopyDebugFolder("Core", debugFolder, folder);
            CopyDebugFolder("Packages", debugFolder, folder);
            CopyDebugFolder("Levels", debugFolder, folder);

            EditorUtility.DisplayDialog("Information", "Copy complete.", "OK");
          }
          else EditorUtility.DisplayDialog("Error", "Output folder does not exist:\n" + debugFolder + "\n▼\n" + folder, "OK");
        }
        else if (target == BuildTarget.StandaloneOSX)
        {
          // Copy directly to the macOS output directory.
          string folder = DebugSettings.Instance.OutputAppMac + "/Contents/";
          if (Directory.Exists(debugFolder) && Directory.Exists(folder))
          {
            CopyDebugFolder("Core", debugFolder, folder);
            CopyDebugFolder("Packages", debugFolder, folder);
            CopyDebugFolder("Levels", debugFolder, folder);

            EditorUtility.DisplayDialog("Information", "Copy complete.", "OK");
          }
          else EditorUtility.DisplayDialog("Error", "Output folder does not exist:\n" + debugFolder + "\n▼\n" + folder, "OK");
        }
        else if (target == BuildTarget.Android || target == BuildTarget.iOS || target == BuildTarget.WSAPlayer || target == BuildTarget.Switch)
        {
          // Copy to StreamingAssets for iOS/Android.
          string folder = "Assets/StreamingAssets/BuiltInPackages";
          if (Directory.Exists(debugFolder))
          {
            if(!Directory.Exists(folder))
              Directory.CreateDirectory(folder);

            CopyDebugFolder("Core", debugFolder, folder);
            CopyDebugFolder("Packages", debugFolder, folder);
            CopyDebugFolder("Levels", debugFolder, folder);

            EditorUtility.DisplayDialog("Information", "Copy complete.", "OK");
          }
          else EditorUtility.DisplayDialog("Error", "Debug folder does not exist:\n" + debugFolder + "\n▼\n" + folder, "OK");
        }
        else
        {
          EditorUtility.DisplayDialog("Error", "This platform is not supported.", "OK");
        }
      };
      choosePlatformWindow.Show();
    }
    [@MenuItem("Ballance/Tools/Copy Debug Folder to Custom Output", false, 103)]
    static void CopyDebugFolder()
    {
      WindowChoosePlatform choosePlatformWindow = EditorWindow.GetWindowWithRect<WindowChoosePlatform>(new Rect(200, 150, 450, 250));
      choosePlatformWindow.OnChoose = (target) =>
      {
        string debugFolder = DebugSettings.Instance.DebugFolder + "/";
        string folder = EditorUtility.OpenFolderPanel("Select Output Folder", EditorPrefs.GetString("CopyDebugFolderDefSaveDir", GamePathManager.DEBUG_PATH), "");
        if (string.IsNullOrEmpty(folder))
          return;
        if (Directory.Exists(folder) && Directory.Exists(folder))
        {
          if (folder != GamePathManager.DEBUG_PATH)
            EditorPrefs.SetString("CopyDebugFolderDefSaveDir", GamePathManager.DEBUG_PATH);

          CopyDebugFolder("Core", debugFolder, folder);
          CopyDebugFolder("Packages", debugFolder, folder);
          CopyDebugFolder("Levels", debugFolder, folder);

          EditorUtility.DisplayDialog("Information", "Copy complete.", "OK");
        }
        else EditorUtility.DisplayDialog("Error", "Folder does not exist:\n" + debugFolder + "\n▼\n" + folder, "OK");
      };
      choosePlatformWindow.Show();
    }
    [@MenuItem("Ballance/Tools/Clear BuiltInPackages Folder", false, 103)]
    static void ClearBuiltInPackages() {
      Directory.Delete("Assets/StreamingAssets/BuiltInPackages", true);
    }

    private static void CopyDebugFolder(string name, string debugFolder, string folder)
    {
      string folderCoreSrc = debugFolder + "/" + name;
      string folderCoreTarget = folder + "/" + name;
      if (Directory.Exists(folderCoreSrc))
      {
        if (!Directory.Exists(folderCoreTarget))
          Directory.CreateDirectory(folderCoreTarget);

        DirectoryInfo direction = new DirectoryInfo(folderCoreSrc);
        FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
          if (files[i].Name.EndsWith(".ballance")
              || files[i].Name.EndsWith(".txt")
              || files[i].Name.EndsWith(".xml"))
          {
            File.Copy(
                folderCoreSrc + "/" + files[i].Name,
                folderCoreTarget + "/" + files[i].Name,
                true);
          }
        }
      }
    }
  }
}
