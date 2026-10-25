using System;
using System.IO;
using Ballance2.Utils;
using UnityEditor;
using UnityEngine;

namespace Ballance2.Editor
{
  public static class BuildChecks
  {
    public static void Run()
    {
      Require(Application.unityVersion == PlayerBuild.UnityVersion, "Unity version mismatch");
      foreach (var target in new[] { BuildTarget.StandaloneOSX, BuildTarget.StandaloneWindows64,
        BuildTarget.StandaloneLinux64, BuildTarget.Android, BuildTarget.iOS, BuildTarget.WebGL })
      {
        Require(PlayerBuild.DefaultOutput(target).StartsWith("Builds/"), "Missing output for " + target);
        Require(BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target), "Missing module: " + target);
      }
      var local = Path.GetFullPath("Builds/path with spaces/level.ballance");
      Require(PathUtils.FixFilePathScheme(PathUtils.ToRequestUrl(local)) == local, "File URL round trip");
      foreach (var url in new[] { "https://iwonz.github.io/ballance/StreamingAssets/core.ballance",
        "http://localhost:8000/ballance/core.ballance", "jar:file:///app.apk!/assets/core.ballance" })
      {
        Require(PathUtils.IsRemotePath(url), "Remote resource: " + url);
        Require(PathUtils.IsAbsolutePath(url), "Absolute resource: " + url);
        Require(PathUtils.ToRequestUrl(url) == url, "URL was changed: " + url);
        Require(PathUtils.FixFilePathScheme(url) == url, "Remote URL was treated as a file");
      }
      Require(!PathUtils.IsAbsolutePath("level01"), "Level name must be relative");
      Require(!PathUtils.IsRemotePath(local), "Local path treated as remote");
      InputChecks.Run();
      LocalizationChecks.Run();
      ModernizationChecks.Run();
      GraphicsChecks.Run();
      Debug.Log("Unity 6 build and resource-path checks passed for all six platforms.");
    }

    private static void Require(bool valid, string message)
    {
      if (!valid) throw new InvalidOperationException(message);
    }
  }
}
