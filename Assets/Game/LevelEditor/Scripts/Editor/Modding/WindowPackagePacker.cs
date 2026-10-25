using Ballance2.Config.Settings;
using Ballance2.Res;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* PackagePackerWindow.cs
* 
* Purpose:
* Package module code.
*
* Author:
* mengyu
*
* 
* 
*
*/

namespace Ballance2.Editor.Modding
{
    public class WindowPackagePacker : EditorWindow
    {
        public WindowPackagePacker()
        {
            titleContent = new GUIContent("Pack Ballance Package");
        }

        private SerializedObject serializedObject;
        private SerializedProperty pPackDefFile = null;
        private SerializedProperty pPackTarget = null;

        private bool isError = false;
        private string errStr = "";
        private bool isResult = false;
        private bool isClosed = false;

        [SerializeField]
        private TextAsset packDefFile = null;
        [SerializeField]
        private BuildTarget packTarget = BuildTarget.NoTarget;

        private GUIStyle groupBox = null;
        private int tab = 0;
        private string[] tabText = new string[] {  "Select a package under Packages", "Select PackageDef.xml" };
        private int selectedMod = 0;
        private Vector2 scrollRect = new Vector2();

        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
            pPackDefFile = serializedObject.FindProperty("packDefFile");
            pPackTarget = serializedObject.FindProperty("packTarget");

            LoadDefConfig();
            LoadModsPath();
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
            EditorGUILayout.HelpBox(new GUIContent(@"Pack your package with this tool.
First generate a package template under Assets/Packages."), true);
            EditorGUILayout.Space(15);

            tab = GUILayout.Toolbar(tab, tabText);
            EditorGUILayout.Space(10);

            if (tab == 1)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(pPackDefFile, new GUIContent("Select PackageDef.xml."));
                if (GUILayout.Button("Editor selection", GUILayout.Width(130)))
                    SelectInEditor();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                selectedMod = EditorGUILayout.Popup(new GUIContent("Select a level folder "), selectedMod, packsPathArr);
                if (GUILayout.Button("Refresh", GUILayout.Width(80)))
                    LoadModsPath();
                EditorGUILayout.EndHorizontal();
            }


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
                GUILayout.Label(packPackageName + " package resources:");
                foreach(string s in allAssetsPath)
                    GUILayout.Label(s);
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

        private List<string> allAssetsPath = new List<string>();

        private List<string> packsPath = new List<string>();
        private string[] packsPathArr = null;
        private string packPackageName = "";

        private void LoadDefConfig()
        {
            tab = EditorPrefs.GetInt("ModMakerDefTab", 0);
            selectedMod = EditorPrefs.GetInt("ModMakerDefMod", 0);
            packTarget = (BuildTarget)EditorPrefs.GetInt("ModMakerDefTarget", -2);
        }
        private void LoadModsPath()
        {
            packsPath.Clear();
            packsPath.Add("Select...");
            packsPath.Add("core");

            DirectoryInfo direction = new DirectoryInfo(GamePathManager.DEBUG_PACKAGE_FOLDER);
            DirectoryInfo[] dirs = direction.GetDirectories("*", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < dirs.Length; i++)
                packsPath.Add(dirs[i].Name);

            packsPathArr = packsPath.ToArray();
        }
        private void SaveDefConfig()
        {
            EditorPrefs.SetInt("ModMakerDefTab", tab);
            EditorPrefs.SetInt("ModMakerDefMod", selectedMod);
            EditorPrefs.SetInt("ModMakerDefTarget", (int)packTarget);
        }
        private void SelectInEditor()
        {
            if(Selection.activeObject == null)
            {
                EditorUtility.DisplayDialog("Information", "Select your PackageDef.xml in the editor first.", "OK");
                return;
            }
            if(Selection.activeObject.GetType() != typeof(TextAsset))
            {
                EditorUtility.DisplayDialog("Information", "The selected PackageDef.xml must be a TextAsset.", "OK");
                return;
            }
            packDefFile = Selection.activeObject as TextAsset;
        }

        private void DoPack(bool chooseFolder)
        {
            isError = false;

            //selectedMod to packDefFile
            if (tab == 0)
            {
                if (selectedMod == 0)
                {
                    isError = true;
                    errStr = "Select your package.";
                    return;
                } 
                else if (selectedMod == 1)
                {
                    packDefFile = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Game/PackageDef.xml");
                }
                else if (selectedMod > 1 && selectedMod < packsPathArr.Length)
                {
                    string p = GamePathManager.DEBUG_PACKAGE_FOLDER + "/" + 
                        packsPathArr[selectedMod] + "/PackageDef.xml";
                    packDefFile = AssetDatabase.LoadAssetAtPath<TextAsset>(p);
                    if(packDefFile  == null)
                    {
                        isError = true;
                        errStr = "PackageDef.xml was not found in this folder. Generate a template first.";
                        return;
                    }
                }
            }
            //check
            if (packDefFile == null)
            {
                isError = true;
                errStr = "Select your PackageDef.xml.";
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
            if (selectedMod != 1 && chooseFolder)
                dir = EditorUtility.OpenFolderPanel("Save Package", EditorPrefs.GetString("ModMakerDefSaveDir", GamePathManager.DEBUG_PATH), "");
            else
            {
                dir = selectedMod != 1 ? (DebugSettings.Instance.DebugFolder + "/" + packTarget + "/Packages/") : (DebugSettings.Instance.DebugFolder + "/" + packTarget + "/Core/");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
            if (!string.IsNullOrEmpty(dir))
            {
                if (dir != GamePathManager.DEBUG_PATH)
                    EditorPrefs.SetString("ModMakerDefSaveDir", GamePathManager.DEBUG_PATH);

                string err = PackagePacker.DoPackPackage(packTarget, packDefFile, packsPathArr[selectedMod], dir);
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
