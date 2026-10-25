using Ballance2.Config.Settings;
using Ballance2.Res;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ballance2.Editor.Modding.LevelMaker
{
    public class WindowLevelPacker : EditorWindow
    {
        public WindowLevelPacker()
        {
            titleContent = new GUIContent("Pack Ballance Level");
        }

        private SerializedObject serializedObject;
        private SerializedProperty pPackDefFile = null;
        private SerializedProperty pPackTarget = null;

        private bool isError = false;
        private string errStr = "";
        private bool isResult = false;
        private bool isClosed = false;

        [SerializeField]
        private TextAsset levelJsonFile = null;
        [SerializeField]
        private BuildTarget packTarget = BuildTarget.NoTarget;

        private GUIStyle groupBox = null;
        private int selectedLevel = 0;
        private Vector2 scrollRect = new Vector2();

        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
            pPackDefFile = serializedObject.FindProperty("levelJsonFile");
            pPackTarget = serializedObject.FindProperty("packTarget");

            LoadDefConfig();
            LoadLevelsPath();
        }
        private void OnDisable()
        {
            EditorUtility.ClearProgressBar();
            SaveDefConfig();
        }
        private void OnGUI()
        {
            serializedObject.Update();

            if (groupBox == null)
                groupBox = GUI.skin.FindStyle("GroupBox");

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginVertical(groupBox);

            EditorGUILayout.Space(20);
            EditorGUILayout.HelpBox(new GUIContent(@"Use this tool to pack your level."), true);
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            selectedLevel = EditorGUILayout.Popup(new GUIContent("Select a level folder "), selectedLevel, packsPathArr);
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
                LoadLevelsPath();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(pPackTarget, new GUIContent("Target platform "));

            GUILayout.Space(50);

            if (GUILayout.Button("Pack to DebugFolder"))
                DoPack(false);
            if (GUILayout.Button("Pack to Custom Folder"))
                DoPack(true);

            GUILayout.Space(5);

            if(isError)
                EditorGUILayout.HelpBox(errStr, MessageType.Error);
            if(isResult)
            {
                EditorGUILayout.BeginScrollView(scrollRect, GUI.skin.GetStyle("window"));
                GUILayout.Space(3);
                GUILayout.Label("Packing complete!");
                EditorGUILayout.EndScrollView();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Clear Results"))  isResult = false;
                if (GUILayout.Button("Close Window")) { isClosed = true; Close(); }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck() && !isClosed)
                serializedObject.ApplyModifiedProperties();
        }

        private List<string> packsPath = new List<string>();
        private string[] packsPathArr = null;

        private void LoadDefConfig()
        {
            selectedLevel = EditorPrefs.GetInt("LevelMakerDefLevel", 0);
            packTarget = (BuildTarget)EditorPrefs.GetInt("LevelMakerDefTarget", -2);
        }
        private void LoadLevelsPath()
        {
            packsPath.Clear();
            packsPath.Add("Select...");

            DirectoryInfo direction = new DirectoryInfo(GamePathManager.DEBUG_LEVEL_FOLDER);
            DirectoryInfo[] dirs = direction.GetDirectories("*", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < dirs.Length; i++)
              if(dirs[i].Name != "MakerAssets")
                packsPath.Add(dirs[i].Name);

            packsPathArr = packsPath.ToArray();
        }
        private void SaveDefConfig()
        {
            EditorPrefs.SetInt("LevelMakerDefLevel", selectedLevel);
            EditorPrefs.SetInt("LevelMakerDefTarget", (int)packTarget);
        }

        private void DoPack(bool chooseFolder)
        {
            isError = false;

            if (selectedLevel == 0)
            {
                isError = true;
                errStr = "Select your package.";
                return;
            } 
            else if (selectedLevel < packsPathArr.Length)
            {
                string p = GamePathManager.DEBUG_LEVEL_FOLDER + "/" + packsPathArr[selectedLevel] + "/Level.json";
                levelJsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>(p);
                if(levelJsonFile  == null)
                {
                    isError = true;
                    errStr = "Level.json was not found in this folder. Generate a template first.";
                    return;
                }
            }
            
            //check
            if (levelJsonFile == null)
            {
                isError = true;
                errStr = "Select your Level.json.";
                return;
            }
            if (packTarget == BuildTarget.NoTarget)
            {
                isError = true;
                errStr = "Select the target platform.";
                return;
            }
            if(!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(packTarget), packTarget))
            {
                isError = true;
                errStr = "This Unity installation cannot build for target "  + packTarget + ". The corresponding module may be missing.";
                return;
            }
            string dir = "";
            if (selectedLevel != 1 && chooseFolder)
                dir = EditorUtility.OpenFolderPanel("Save Level", EditorPrefs.GetString("LevelMakerDefSaveDir", GamePathManager.DEBUG_PATH), "");
            else
            {
                dir = DebugSettings.Instance.DebugFolder + "/" + packTarget + "/Levels/";
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
            if (!string.IsNullOrEmpty(dir))
            {
                if (dir != GamePathManager.DEBUG_PATH)
                    EditorPrefs.SetString("LevelMakerDefSaveDir", GamePathManager.DEBUG_PATH);

                string err = LevelPacker.DoPackPackage(packTarget, levelJsonFile, packsPathArr[selectedLevel], dir);
                if(!string.IsNullOrEmpty(err))
                {
                    isError = true;
                    errStr = err;
                    return;
                } else {
                    isError = false;
                    EditorUtility.DisplayDialog("Information", "Package built successfully!", "OK");
                }
            }
            else
            {
                isError = true;
                errStr = "Save cancelled.";
            }
        }
    }
}
