using System;
using Ballance2.Services.I18N;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Ballance2.Menu
{
    [RequireComponent(typeof(Text))]
    public class GraphicsDeviceInfo : MonoBehaviour
    {
        private void OnEnable()
        {
            var label = GetComponent<Text>();
            label.supportRichText = false;
            bool web = Application.platform == RuntimePlatform.WebGLPlayer;
            string api = web && SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3
                ? "WebGL 2" : SystemInfo.graphicsDeviceType.ToString();
            label.text = Describe(SystemInfo.graphicsDeviceName, api, web);
        }

        public static string Describe(string device, string api, bool web)
        {
            if (string.IsNullOrWhiteSpace(device)) device = I18N.Tr("core.ui.GraphicsDeviceUnknown");
            string description = I18N.TrF("core.ui.GraphicsDeviceInfo", "", device, api);
            if (IsSoftwareRenderer(device))
                description += "\n" + I18N.Tr(web ? "core.ui.GraphicsSoftwareWeb" : "core.ui.GraphicsSoftwareSystem");
            return description;
        }

        public static bool IsSoftwareRenderer(string device)
        {
            if (string.IsNullOrWhiteSpace(device)) return false;
            // Driver-reported names are heuristic evidence, not hardware certification.
            foreach (string marker in new[] { "swiftshader", "llvmpipe", "softpipe", "software", "microsoft basic", "gdi generic" })
                if (device.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
    }
}
