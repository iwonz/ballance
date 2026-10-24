using System;
using System.IO;
using Ballance2.Editor.Modding;
using Ballance2.Editor.Modding.LevelMaker;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Ballance2.Editor
{
  // Used by both scripts/build-macos.sh and the release workflow.
  public static class MacOSBuild
  {
    private const BuildTarget Target = BuildTarget.StandaloneOSX;
    private const string PhysicsPlugin = "Assets/BallancePhysics/Plugins/macOS/libbphysics.dylib";

    public static void BuildAppleSilicon()
    {
      // Switching platforms inside -executeMethod can trigger a domain reload.
      if (EditorUserBuildSettings.activeBuildTarget != Target)
        throw new BuildFailedException("Start Unity with -buildTarget StandaloneOSX.");
      if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, Target))
        throw new BuildFailedException("Install the Unity Mac Build Support (Mono) module.");

      var output = Path.GetFullPath(GetArgument("-customBuildPath", "Builds/macOS/Ballance.app"));
      // GameCI passes a macOS build basename without the .app suffix.
      if (!Path.HasExtension(output))
        output += ".app";
      if (!output.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
        throw new BuildFailedException("The build output must end in .app.");
      if (Directory.Exists(output) || File.Exists(output))
        throw new BuildFailedException("Use an empty output path to avoid stale release files: " + output);

      var plugin = AssetImporter.GetAtPath(PhysicsPlugin) as PluginImporter;
      if (plugin == null || !plugin.GetCompatibleWithPlatform(Target) ||
          plugin.GetPlatformData(Target, "CPU") != "ARM64")
        throw new BuildFailedException("The macOS physics plugin must be enabled for ARM64.");

      var oldArchitecture = EditorUserBuildSettings.GetPlatformSettings("OSXUniversal", "Architecture");
      var oldBackend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Standalone);
      var staging = Path.GetFullPath("Library/BallanceBuild/" + Guid.NewGuid().ToString("N"));
      try
      {
        EditorUserBuildSettings.SetPlatformSettings("OSXUniversal", "Architecture", "ARM64");
        // Mods load C# assemblies dynamically, which requires Mono rather than IL2CPP.
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
        Directory.CreateDirectory(staging);
        PackModule("Assets/Game/PackageDef.xml", "core", Path.Combine(staging, "Core"));

        var packages = Directory.GetDirectories("Assets/Packages");
        Array.Sort(packages, StringComparer.Ordinal);
        foreach (var package in packages)
        {
          var definition = package.Replace('\\', '/') + "/PackageDef.xml";
          if (File.Exists(definition))
            PackModule(definition, Path.GetFileName(package), Path.Combine(staging, "Packages"));
        }

        var levels = Path.Combine(staging, "Levels");
        Directory.CreateDirectory(levels);
        for (int i = 1; i <= 13; i++)
        {
          var name = "Level" + i.ToString("00");
          var definition = LoadDefinition("Assets/Game/Levels/" + name + "/Level.json");
          CheckPackResult(LevelPacker.DoPackPackage(Target, definition, name.ToLowerInvariant(), levels));
          RequireFile(Path.Combine(levels, name.ToLowerInvariant() + ".ballance"));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
          scenes = new[] { "Assets/Scenes/Main.unity" },
          locationPathName = output,
          target = Target,
          subtarget = (int)StandaloneBuildSubtarget.Player,
          options = BuildOptions.StrictMode
        });
        if (report.summary.result != BuildResult.Succeeded)
          throw new BuildFailedException("macOS build failed: " + report.summary.result);

        // On macOS Application.dataPath is <app>/Contents, not Contents/Resources/Data.
        foreach (var folder in new[] { "Core", "Packages", "Levels" })
        {
          var destination = Path.Combine(output, "Contents", folder);
          Directory.CreateDirectory(destination);
          foreach (var file in Directory.GetFiles(Path.Combine(staging, folder), "*.ballance"))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        }
        Debug.Log("Apple Silicon player and all built-in packages created: " + output);
      }
      finally
      {
        EditorUtility.ClearProgressBar();
        EditorUserBuildSettings.SetPlatformSettings("OSXUniversal", "Architecture", oldArchitecture);
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, oldBackend);
        if (Directory.Exists(staging))
          Directory.Delete(staging, true);
      }
    }

    private static void PackModule(string definition, string name, string destination)
    {
      Directory.CreateDirectory(destination);
      CheckPackResult(PackagePacker.DoPackPackage(Target, LoadDefinition(definition), name, destination));
      RequireFile(Path.Combine(destination, name + ".ballance"));
    }

    private static TextAsset LoadDefinition(string path)
    {
      var definition = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
      if (definition == null)
        throw new BuildFailedException("Missing package definition: " + path);
      return definition;
    }

    private static void CheckPackResult(string error)
    {
      if (!string.IsNullOrEmpty(error))
        throw new BuildFailedException(error);
    }

    private static void RequireFile(string path)
    {
      if (!File.Exists(path) || new FileInfo(path).Length == 0)
        throw new BuildFailedException("Missing or empty build output: " + path);
    }

    private static string GetArgument(string name, string fallback)
    {
      var args = Environment.GetCommandLineArgs();
      for (int i = 0; i < args.Length - 1; i++)
        if (args[i] == name)
          return args[i + 1];
      return fallback;
    }
  }
}
