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
  public class WindowPackageMakerAll : EditorWindow
    {
        public WindowPackageMakerAll()
        {
            titleContent = new GUIContent("Pack All Packages to Debug Folder");
        }

        private SerializedObject serializedObject;
        private SerializedProperty pPackTarget = null;

        private bool isError = false;
        private string errStr = "";
        private bool isClosed = false;

        [SerializeField]
        private BuildTarget packTarget = BuildTarget.NoTarget;

        private GUIStyle groupBox = null;

        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
            packTarget = (BuildTarget)EditorPrefs.GetInt("WindowPackageMakerAllDialogPackTarget", (int)BuildTarget.NoTarget);
            pPackTarget = serializedObject.FindProperty("packTarget");

            LoadDefConfig();
            LoadModsPath();
        }
        private void OnDisable()
        {
            EditorPrefs.SetInt("WindowPackageMakerAllDialogPackTarget", (int)packTarget);
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
            EditorGUILayout.PropertyField(pPackTarget, new GUIContent("Target platform "));

            GUILayout.Space(50);

            if (GUILayout.Button("Start Packing"))
                DoPack();

            GUILayout.Space(5);
            
            if(isError)
                EditorGUILayout.HelpBox(errStr, MessageType.Error);

            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck() && !isClosed)
                serializedObject.ApplyModifiedProperties();
        }

        private List<string> packsPath = new List<string>();
        private string[] packsPathArr = null;

        private void LoadDefConfig()
        {
            packTarget = (BuildTarget)EditorPrefs.GetInt("ModMakerDefTarget", -2);
        }
        private void LoadModsPath()
        {
            packsPath.Clear();
            packsPath.Add("core");

            DirectoryInfo direction = new DirectoryInfo(GamePathManager.DEBUG_PACKAGE_FOLDER);
            DirectoryInfo[] dirs = direction.GetDirectories("*", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < dirs.Length; i++)
                packsPath.Add(dirs[i].Name);

            packsPathArr = packsPath.ToArray();
        }
        private void SaveDefConfig()
        {
            EditorPrefs.SetInt("ModMakerDefTarget", (int)packTarget);
        }
        private void DoPack()
        {
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

            EditorUtility.DisplayProgressBar("Packing", "Packing, please wait...", 0);

            foreach(string path in packsPath) {
                TextAsset packageDef = null;
                if(path == "core") {
                    packageDef = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Game/PackageDef.xml");
                    dir = DebugSettings.Instance.DebugFolder + "/" + packTarget + "/Core/";
                }
                else {
                    packageDef = AssetDatabase.LoadAssetAtPath<TextAsset>(GamePathManager.DEBUG_PACKAGE_FOLDER + "/" + path + "/PackageDef.xml");
                    dir = DebugSettings.Instance.DebugFolder + "/" + packTarget + "/Packages/";
                }
                if(packageDef  == null)
                {
                    isError = true;
                    Debug.LogWarning("Could not find PackageDef.xml in " + path + "; skipping this package.");
                    continue;
                } 
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string err = PackagePacker.DoPackPackage(packTarget, packageDef, path, dir);
                if(!string.IsNullOrEmpty(err))
                    Debug.LogError("Packing package " + path + " failed: " + err);
            }

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Information", "Package built successfully!", "OK");

            isError = false;
        }
    }
}
