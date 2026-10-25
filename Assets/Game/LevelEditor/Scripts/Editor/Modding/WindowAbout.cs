using UnityEditor;
using UnityEngine;

namespace Ballance2.Editor.Modding
{
    public class WindowAbout : EditorWindow
    {
        public WindowAbout()
        {
            titleContent = new GUIContent("About Reballance");
        }

        [@MenuItem("Ballance/Help", false, 104)]
        static void ShowHelp()
        {
            EditorUtility.OpenWithDefaultApp("https://github.com/iwonz/ballance/blob/main/docs/architecture.md");
        }
        [@MenuItem("Ballance/About", false, 109)]
        static void ShowAbout()
        {
            EditorWindow.GetWindowWithRect(typeof(WindowAbout), new Rect(200, 150, 390, 500));
        }

        private Texture2D logo;
        private GUIStyle DefaultCenteredLargeText;
        private GUIStyle DefaultCenteredText;
        private GUIStyle LinkLabel;
        private GUIStyle ButtonMid;
        private GUIStyle OLbox;

        private void OnEnable()
        {
            logo = Resources.Load<Texture2D>("Remaster/TitleArtwork");
        }

        private void OnGUI()
        {
            if (OLbox == null) OLbox = GUI.skin.GetStyle("OL box");
            if (DefaultCenteredLargeText == null) DefaultCenteredLargeText = GUI.skin.GetStyle("DefaultCenteredLargeText");
            if (DefaultCenteredText == null) DefaultCenteredText = GUI.skin.GetStyle("DefaultCenteredText");
            if (LinkLabel == null) LinkLabel = GUI.skin.GetStyle("LinkLabel");
            if (ButtonMid == null) ButtonMid = GUI.skin.GetStyle("ButtonMid");

            EditorGUILayout.BeginVertical(OLbox);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(logo, GUILayout.Width(360), GUILayout.Height(203));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(16);
            GUILayout.Label("Reballance", DefaultCenteredLargeText);
            GUILayout.Label("Independent Unity reconstruction", DefaultCenteredText);

            EditorGUILayout.Space(3);
            GUILayout.Label("Modern materials and seven environments.\nBased on the classic game by Cyparade.\nSee the repository for development and license details.", DefaultCenteredText);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("github.com/iwonz/ballance", LinkLabel))
                EditorUtility.OpenWithDefaultApp("https://github.com/iwonz/ballance");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Copyright © 2020 mengyu 版权所有，MIT 开源协议", DefaultCenteredText);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical(GUILayout.Width(270));
            if (GUILayout.Button("Development Help", ButtonMid))
                ShowHelp();
            if (GUILayout.Button("Close", ButtonMid))
                Close();
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
    }
}
