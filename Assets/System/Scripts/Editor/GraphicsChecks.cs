using System;
using System.IO;
using System.Linq;
using Ballance2.Menu;
using Ballance2.Services.I18N;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Ballance2.Editor
{
  public static class GraphicsChecks
  {
    private static void Require(bool condition, string message)
    {
      if (!condition) throw new InvalidOperationException("Graphics check failed: " + message);
    }

    public static void Run()
    {
      foreach (var target in new[] { BuildTarget.StandaloneWindows64, BuildTarget.StandaloneLinux64,
        BuildTarget.StandaloneOSX, BuildTarget.Android, BuildTarget.WebGL })
      {
        bool automatic = PlayerSettings.GetUseDefaultGraphicsAPIs(target);
        var preference = PlayerSettings.WebGL.powerPreference;
        try
        {
          PlayerSettings.SetUseDefaultGraphicsAPIs(target, false);
          PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.LowPower;
          PlayerBuild.ConfigureGraphics(target);
          Require(PlayerSettings.GetUseDefaultGraphicsAPIs(target), "automatic API selection: " + target);
          var apis = PlayerSettings.GetGraphicsAPIs(target);
          Require(apis.Length > 0 && !apis.Contains(GraphicsDeviceType.Null), "graphics backend available: " + target);
          Require(PlayerSettings.WebGL.powerPreference == (target == BuildTarget.WebGL
            ? WebGLPowerPreference.HighPerformance : WebGLPowerPreference.LowPower), "Web preference scope");
          Debug.Log("GRAPHICS_POLICY " + target + ": " + string.Join(", ", apis));
        }
        finally
        {
          PlayerSettings.SetUseDefaultGraphicsAPIs(target, automatic);
          PlayerSettings.WebGL.powerPreference = preference;
        }
      }

      foreach (string name in new[] { "ANGLE SwiftShader", "LLVMpipe", "softpipe", "Microsoft Basic Render Driver", "Software Rasterizer", "GDI Generic" })
        Require(GraphicsDeviceInfo.IsSoftwareRenderer(name), "software renderer: " + name);
      foreach (string name in new[] { null, "", "Apple M3", "NVIDIA GeForce RTX 4070", "AMD Radeon RX 7800 XT", "Intel Iris Xe" })
        Require(!GraphicsDeviceInfo.IsSoftwareRenderer(name), "hardware or unavailable name: " + name);

      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Menu/Prefabs/Pages/PageSettingsGraphics.prefab");
      var previousLanguage = I18NProvider.GetCurrentLanguage();
      GameObject page = null;
      try
      {
        page = Object.Instantiate(prefab);
        var info = page.GetComponentInChildren<GraphicsDeviceInfo>(true);
        Require(info != null, "device information component bound in settings");
        var label = info.GetComponent<Text>();
        Require(!label.supportRichText && !label.raycastTarget, "device text is literal and does not intercept controls");
        foreach (var language in new[] { SystemLanguage.English, SystemLanguage.Russian,
          SystemLanguage.ChineseSimplified, SystemLanguage.ChineseTraditional })
        {
          I18NProvider.ClearAllLanguageResources();
          I18NProvider.SetCurrentLanguage(language);
          Require(I18NProvider.LoadLanguageResources(File.ReadAllText("Assets/Game/PackageLanguageRes.xml")), "language resource");
          foreach (string device in new[] { null, "Apple M3", "ANGLE (Apple, ANGLE Metal Renderer: Apple M3, Unspecified Version)",
            "ANGLE (Google, Vulkan 1.3.0 (SwiftShader Device (Subzero) (0x0000C0DE)), SwiftShader driver)" })
          {
            foreach (bool web in new[] { true, false })
            {
              label.text = GraphicsDeviceInfo.Describe(device, web ? "WebGL 2" : "Vulkan", web);
              Require(!label.text.Contains("[Key ") && !label.text.Contains("Failed to format"), "localized description");
              if (device == null) Require(label.text.Contains(I18N.Tr("core.ui.GraphicsDeviceUnknown")), "unknown device");
              else Require(label.text.Contains(device), "preserve reported device name");
              Require(label.text.Contains(I18N.Tr(web ? "core.ui.GraphicsSoftwareWeb" : "core.ui.GraphicsSoftwareSystem"))
                == GraphicsDeviceInfo.IsSoftwareRenderer(device), "software-only guidance");
              Require(label.preferredHeight <= label.rectTransform.rect.height + 1, "device text fits: " + language);
            }
          }
        }
      }
      finally
      {
        if (page != null) Object.DestroyImmediate(page);
        I18NProvider.ClearAllLanguageResources();
        I18NProvider.SetCurrentLanguage(previousLanguage);
      }
      Debug.Log("GRAPHICS_CHECKS_PASSED: API policy, software detection, four-language device information and layout.");
    }
  }
}
