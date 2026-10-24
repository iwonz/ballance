using System;
using System.IO;
using System.Linq;
using Ballance2.Editor.Modding;
using Ballance2.Editor.Modding.LevelMaker;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
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

        // Compile the player first so modules reference this build's runtime API,
        // not editor assemblies or DLLs checked in by a previous developer.
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

        // Unity 2021 copies this ARM64 dylib into a CPU subfolder, but the
        // macOS Mono player resolves DllImport("bphysics") from PlugIns itself.
        var plugins = Path.Combine(output, "Contents", "PlugIns");
        var nestedPhysics = Path.Combine(plugins, "ARM64", "libbphysics.dylib");
        var playerPhysics = Path.Combine(plugins, "libbphysics.dylib");
        if (File.Exists(nestedPhysics))
          File.Move(nestedPhysics, playerPhysics);
        RequireFile(playerPhysics);

        PackModule("Assets/Game/PackageDef.xml", "core", Path.Combine(staging, "Core"));

        var packages = Directory.GetDirectories("Assets/Packages");
        Array.Sort(packages, StringComparer.Ordinal);
        foreach (var package in packages)
        {
          var definition = package.Replace('\\', '/') + "/PackageDef.xml";
          if (File.Exists(definition))
          {
            var name = Path.GetFileName(package);
            var code = CompileModule(package, name, staging, output);
            PackModule(definition, name, Path.Combine(staging, "Packages"), code);
          }
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

    private static string CompileModule(string source, string name, string staging, string player)
    {
      var scripts = Directory.GetFiles(source, "*.cs", SearchOption.AllDirectories);
      if (scripts.Length == 0)
        return null;
      Array.Sort(scripts, StringComparer.Ordinal);

      // AssemblyBuilder compiles external source files without importing new assets.
      var codeDirectory = Path.Combine(staging, "Code", name);
      Directory.CreateDirectory(codeDirectory);
      var externalScripts = new string[scripts.Length];
      for (int i = 0; i < scripts.Length; i++)
      {
        externalScripts[i] = Path.Combine(codeDirectory, i + ".cs");
        File.Copy(scripts[i], externalScripts[i]);
      }

      var assembly = Path.Combine(codeDirectory, name + ".dll");
      var managed = Path.Combine(player, "Contents", "Resources", "Data", "Managed");
      RequireFile(Path.Combine(managed, "Assembly-CSharp.dll"));
      var builder = new AssemblyBuilder(assembly, externalScripts)
      {
        buildTarget = Target,
        buildTargetGroup = BuildTargetGroup.Standalone,
        flags = AssemblyBuilderFlags.None,
        additionalReferences = Directory.GetFiles(managed, "*.dll")
          .Where(path => !Path.GetFileName(path).StartsWith("mod.", StringComparison.Ordinal))
          .ToArray()
      };
      // Use only the newly built player references, including its runtime framework.
      builder.excludeReferences = builder.defaultReferences;
      CompilerMessage[] messages = null;
      builder.buildFinished += (path, result) => messages = result;
      if (!builder.Build())
        throw new BuildFailedException("Could not start compilation of " + name);
      while (builder.status != AssemblyBuilderStatus.Finished)
        System.Threading.Thread.Sleep(10);
      if (messages == null || messages.Any(message => message.type == CompilerMessageType.Error))
        throw new BuildFailedException("Module compilation failed: " + name + "\n" +
          (messages == null ? "No compiler result" : string.Join("\n", messages.Select(message => message.message))));
      RequireFile(assembly);
      // Unity may also copy the checked-in plugin to Managed. Replace that copy
      // too, so assembly resolution cannot pick an older DLL by the same name.
      File.Copy(assembly, Path.Combine(managed, name + ".dll"), true);
      Debug.Log("Recompiled module from source: " + name);
      return assembly;
    }

    private static void PackModule(string definition, string name, string destination, string compiledCode = null)
    {
      Directory.CreateDirectory(destination);
      CheckPackResult(PackagePacker.DoPackPackage(Target, LoadDefinition(definition), name, destination, compiledCode));
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
