using Ballance2.Res;
using Ballance2.Utils;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ballance2.Editor.Modding.LevelMaker
{
    public class WindowLevelMaker : EditorWindow
    {
        public WindowLevelMaker()
        {
            titleContent = new GUIContent("Create Ballance Level Template");
        }

        private string levelName = "";

        private SerializedObject serializedObject;

        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
        }

        private void OnDisable() {
            serializedObject = null;
        }

        private void OnGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space(50);

            levelName = EditorGUILayout.TextField("Folder name", levelName);

            var empty = StringUtils.isNullOrEmpty(levelName);
               

            EditorGUI.BeginDisabledGroup(empty);

            if(GUILayout.Button("Generate"))
                Make();

            EditorGUI.EndDisabledGroup();

            if(empty)
                EditorGUILayout.HelpBox("Folder name is empty.", MessageType.Error);

            EditorGUILayout.Space(50);
            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck() && serializedObject != null)
                serializedObject.ApplyModifiedProperties();
        }

        private void Make()
        {
            string folderPath = GamePathManager.DEBUG_LEVEL_FOLDER + "/" + levelName;
            if (Directory.Exists(folderPath))
            {
                if (!EditorUtility.DisplayDialog("Information", "Level folder " + levelName + " already exists at: \n" +
                    folderPath + "\nReplace it?", "Replace", "Cancel"))
                    return;
            }

            DirectoryInfo directoryInfo = Directory.CreateDirectory(folderPath);
            if(directoryInfo == null)
            {
                EditorUtility.DisplayDialog("Error", "Failed to create folder:\n" + folderPath, "OK");
                return;
            }

            File.Copy(GamePathManager.DEBUG_LEVEL_FOLDER + "/template_Level.json", folderPath + "/Level.json");
            File.Copy(GamePathManager.DEBUG_LEVEL_FOLDER + "/template_Level.prefab", folderPath + "/Level.prefab");
            File.Copy(GamePathManager.DEBUG_LEVEL_FOLDER + "/template_LevelLogo.png", folderPath + "/LevelLogo.png");
            File.Copy(GamePathManager.DEBUG_LEVEL_FOLDER + "/template_LevelPreview.png", folderPath + "/LevelPreview.png");

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Information", "Template generated successfully!", "OK");

            Close();
        }
    }
}
