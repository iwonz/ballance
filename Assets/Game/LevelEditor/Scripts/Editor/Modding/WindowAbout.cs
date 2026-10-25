using UnityEditor;
using UnityEngine;

namespace Ballance2.Editor.Modding
{
    public class WindowAbout : EditorWindow
    {
        public WindowAbout()
        {
            titleContent = new GUIContent("About Ballance");
        }

        [@MenuItem("Ballance/Help", false, 104)]
        static void ShowHelp()
        {

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
            logo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/System/Textures/splash_app.bmp");
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
            GUILayout.Label(logo, GUILayout.Width(360));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(16);
            GUILayout.Label("Ballance Rebuild Project", DefaultCenteredLargeText);
            GUILayout.Label("Ballance Remake (Unity)", DefaultCenteredText);

            EditorGUILayout.Space(3);
            GUILayout.Label("A remake of Ballance in Unity.\nThis project began as the original author's long-standing personal goal.\n" +
                "The original author was a student learning game development.\nSee the project on GitHub for more information.", DefaultCenteredText);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Original project: github.com/imengyu/Ballance2", LinkLabel))
                EditorUtility.OpenWithDefaultApp("https://github.com/imengyu/Ballance2");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Copyright © 2020 mengyu 版权所有，MIT 开源协议", LinkLabel))
                EditorUtility.OpenWithDefaultApp("https://imyzc.com/");
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
