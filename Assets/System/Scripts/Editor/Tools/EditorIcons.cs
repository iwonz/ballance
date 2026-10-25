using System;
using UnityEditor;
using UnityEngine;

namespace Ballance2.Editor
{
    class EditorIcons : EditorWindow
    {
        public EditorIcons()
        {
            titleContent = new GUIContent("Unity Built-in Icon Viewer");
        }

        [MenuItem("Tools/GUI/Editor Icon")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(EditorIcons));
        }

        public Vector2 scrollPosition;

        private GUIStyle window = null;

        private void OnEnable()
        {
            
        }
        private void OnGUI()
        {
            if(window == null)
                window = GUI.skin.FindStyle("window");

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(window, GUILayout.Width(120));// Button hover style.
            GUILayout.Label("Style when hovering over the button.");

            foreach (MouseCursor item in Enum.GetValues(typeof(MouseCursor)))
            {
                GUILayout.Button(Enum.GetName(typeof(MouseCursor), item));
                EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), item);
                GUILayout.Space(10);
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();
        }
    }
}
