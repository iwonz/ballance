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
  public static class PlayerBuild
  {
    public const string UnityVersion = "6000.6.7f1";

    [MenuItem("Ballance/Build/Build current platform with game content")]
    public static void Build()
    {
      if (Application.unityVersion != UnityVersion)
        throw new BuildFailedException("Use Unity " + UnityVersion);
      var target = EditorUserBuildSettings.activeBuildTarget;
      var group = BuildPipeline.GetBuildTargetGroup(target);
      if (!BuildPipeline.IsBuildTargetSupported(group, target))
        throw new BuildFailedException("Install the Unity Hub build module for " + target);
      var output = Path.GetFullPath(Argument("-customBuildPath", DefaultOutput(target)));
      if (target == BuildTarget.StandaloneOSX && !output.EndsWith(".app")) output += ".app";
      if (File.Exists(output) || Directory.Exists(output))
        throw new BuildFailedException("Move the previous build aside first: " + output);

      var desktop = group == BuildTargetGroup.Standalone;
      var streaming = "Assets/StreamingAssets/BuiltInPackages";
      if (!desktop && Directory.Exists(streaming) && Directory.GetFileSystemEntries(streaming).Length != 0)
        throw new BuildFailedException("Move existing staged content aside first: " + streaming);
      Configure(target, output);
      var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
      var inputHandling = settings.FindProperty("activeInputHandler");
      int previousInputHandling = inputHandling.intValue;
      var staging = Path.GetFullPath("Library/BallanceBuild/" + Guid.NewGuid().ToString("N"));
      try
      {
        PackContent(target, staging);
        if (!desktop) CopyContent(staging, streaming);
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        if (target == BuildTarget.Android)
        {
          inputHandling.intValue = 1; // Input System only; AndroidInput retains legacy controls.
          settings.ApplyModifiedPropertiesWithoutUndo();
        }
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
          scenes = new[] { "Assets/Scenes/Main.unity" },
          locationPathName = output,
          target = target,
          subtarget = desktop ? (int)StandaloneBuildSubtarget.Player : 0,
          options = BuildOptions.StrictMode
        });
        if (report.summary.result != BuildResult.Succeeded)
          throw new BuildFailedException(target + " build failed: " + report.summary.result);

        if (desktop)
        {
          var data = target == BuildTarget.StandaloneOSX ? Path.Combine(output, "Contents") :
            Path.Combine(Path.GetDirectoryName(output), Path.GetFileNameWithoutExtension(output) + "_Data");
          CopyContent(staging, data);
          if (target == BuildTarget.StandaloneOSX)
          {
            var plugins = Path.Combine(output, "Contents", "PlugIns");
            var nested = Path.Combine(plugins, "ARM64", "libbphysics.dylib");
            var physics = Path.Combine(plugins, "libbphysics.dylib");
            if (File.Exists(nested)) File.Move(nested, physics);
            RequireFile(physics);
          }
        }
        if (target == BuildTarget.WebGL)
        {
          RequireFile(Path.Combine(output, "index.html"));
          File.WriteAllText(Path.Combine(output, ".nojekyll"), "");
        }
        Debug.Log("Built " + target + " with all 13 levels: " + output);
      }
      finally
      {
        EditorUtility.ClearProgressBar();
        if (target == BuildTarget.Android)
        {
          settings.Update();
          settings.FindProperty("activeInputHandler").intValue = previousInputHandling;
          settings.ApplyModifiedPropertiesWithoutUndo();
          AssetDatabase.SaveAssets();
        }
        if (Directory.Exists(staging)) Directory.Delete(staging, true);
        if (!desktop && Directory.Exists(streaming)) Directory.Delete(streaming, true);
      }
    }

    public static string DefaultOutput(BuildTarget target)
    {
      switch (target)
      {
        case BuildTarget.StandaloneOSX: return "Builds/macOS/Ballance.app";
        case BuildTarget.StandaloneWindows64: return "Builds/Windows/Ballance.exe";
        case BuildTarget.StandaloneLinux64: return "Builds/Linux/Ballance.x86_64";
        case BuildTarget.Android: return "Builds/Android/Ballance.apk";
        case BuildTarget.iOS: return "Builds/iOS";
        case BuildTarget.WebGL: return "Builds/WebGL";
        default: throw new BuildFailedException("Unsupported build target: " + target);
      }
    }

    internal static void ConfigureGraphics(BuildTarget target)
    {
      PlayerSettings.SetUseDefaultGraphicsAPIs(target, true);
      if (target == BuildTarget.WebGL)
        PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.HighPerformance;
    }

    private static void Configure(BuildTarget target, string output)
    {
      var named = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(target));
      var desktop = BuildPipeline.GetBuildTargetGroup(target) == BuildTargetGroup.Standalone;
      PlayerSettings.SetScriptingBackend(named, desktop ? ScriptingImplementation.Mono2x : ScriptingImplementation.IL2CPP);
      // The game's behaviours and UI types also live in separately loaded asset bundles.
      PlayerSettings.SetManagedStrippingLevel(named, desktop ? ManagedStrippingLevel.Disabled : ManagedStrippingLevel.Minimal);
      PlayerSettings.stripEngineCode = false;
      ConfigureGraphics(target);
      if (target == BuildTarget.StandaloneOSX)
        EditorUserBuildSettings.SetPlatformSettings("OSXUniversal", "Architecture", "ARM64");
      if (target == BuildTarget.Android)
      {
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        EditorUserBuildSettings.buildAppBundle = output.EndsWith(".aab", StringComparison.OrdinalIgnoreCase);
      }
      if (target == BuildTarget.WebGL)
      {
        RequireFile("Assets/BallancePhysics/Plugins/WebGL/libbphysics.a");
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.threadsSupport = false;
        PlayerSettings.WebGL.wasm2023 = true;
#if UNITY_WEBGL
        UnityEditor.WebGL.UserBuildSettings.codeOptimization = UnityEditor.WebGL.WasmCodeOptimization.RuntimeSpeedLTO;
#endif
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithStacktrace;
        PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.External;
        PlayerSettings.WebGL.initialMemorySize = 256;
        PlayerSettings.WebGL.maximumMemorySize = 2048;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
      }
      AssetDatabase.SaveAssets();
    }

    private static void PackContent(BuildTarget target, string staging)
    {
      PackModule(target, "Assets/Game/PackageDef.xml", "core", Path.Combine(staging, "Core"));
      var packages = Directory.GetDirectories("Assets/Packages");
      Array.Sort(packages, StringComparer.Ordinal);
      foreach (var package in packages)
      {
        var definition = package.Replace('\\', '/') + "/PackageDef.xml";
        if (File.Exists(definition))
          PackModule(target, definition, Path.GetFileName(package), Path.Combine(staging, "Packages"));
      }
      var levels = Path.Combine(staging, "Levels");
      for (int i = 1; i <= 13; i++)
      {
        var name = "Level" + i.ToString("00");
        Check(LevelPacker.DoPackPackage(target, Definition("Assets/Game/Levels/" + name + "/Level.json"), name.ToLowerInvariant(), levels));
        RequireFile(Path.Combine(levels, name.ToLowerInvariant() + ".ballance"));
      }
    }

    private static void PackModule(BuildTarget target, string definition, string name, string output)
    {
      Directory.CreateDirectory(output);
      Check(PackagePacker.DoPackPackage(target, Definition(definition), name, output, includeCode: false));
      RequireFile(Path.Combine(output, name + ".ballance"));
    }

    private static void CopyContent(string source, string destination)
    {
      foreach (var folder in new[] { "Core", "Packages", "Levels" })
      {
        var output = Path.Combine(destination, folder);
        Directory.CreateDirectory(output);
        foreach (var file in Directory.GetFiles(Path.Combine(source, folder), "*.ballance"))
          File.Copy(file, Path.Combine(output, Path.GetFileName(file)));
      }
    }

    private static TextAsset Definition(string path)
    {
      var result = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
      if (result == null) throw new BuildFailedException("Missing package definition: " + path);
      return result;
    }

    private static void Check(string error)
    {
      if (!string.IsNullOrEmpty(error)) throw new BuildFailedException(error);
    }

    private static void RequireFile(string path)
    {
      if (!File.Exists(path) || new FileInfo(path).Length == 0)
        throw new BuildFailedException("Missing or empty build output: " + path);
    }

    private static string Argument(string name, string fallback)
    {
      var args = Environment.GetCommandLineArgs();
      for (int i = 0; i < args.Length - 1; i++)
        if (args[i] == name) return args[i + 1];
      return fallback;
    }
  }
}
