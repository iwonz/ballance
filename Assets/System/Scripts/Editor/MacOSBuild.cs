using UnityEditor;
using UnityEditor.Build;

namespace Ballance2.Editor
{
  public static class MacOSBuild
  {
    public static void BuildAppleSilicon()
    {
      if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneOSX)
        throw new BuildFailedException("Start Unity with -buildTarget StandaloneOSX.");
      PlayerBuild.Build();
    }
  }
}
