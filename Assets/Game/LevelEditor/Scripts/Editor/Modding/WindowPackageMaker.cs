using Ballance2.Config;
using Ballance2.Package;
using Ballance2.Res;
using Ballance2.Utils;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace Ballance2.Editor.Modding
{
  public class WindowPackageMaker : EditorWindow
  {
    public WindowPackageMaker()
    {
      titleContent = new GUIContent("Create Ballance Package");
    }

    private string modPackageName = "com.yourname.packagename";
    private string modName = "";
    private string modAuthor = "";
    private string modIntroduction = "";
    private string modUserVersion = "1.0";
    private int modVersion = 1;
    private GamePackageType PackageType = GamePackageType.Module;

    private SerializedObject serializedObject;

    private GUIStyle groupBox = null;
    private bool error = false;

    private TextAsset template_PackageDef;

    private void OnEnable()
    {
      serializedObject = new SerializedObject(this);

      template_PackageDef = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Packages/template_PackageDef.xml");
    }

    private void OnDisable()
    {
      serializedObject = null;
    }

    private void OnGUI()
    {
      serializedObject.Update();

      if (groupBox == null)
        groupBox = GUI.skin.FindStyle("GroupBox");

      error = false;

      EditorGUI.BeginChangeCheck();

      EditorGUILayout.BeginVertical(groupBox);

      EditorGUILayout.Space(20);
      EditorGUILayout.HelpBox(new GUIContent("Generate a package template under Assets/Packages."), true);
      EditorGUILayout.Space(15);

      modPackageName = EditorGUILayout.TextField("Package ID", modPackageName);
      if (StringUtils.isNullOrEmpty(modPackageName))
      {
        EditorGUILayout.HelpBox("Package ID is required.", MessageType.Error);
        error = true;
      }
      if (!StringUtils.IsPackageName(modPackageName))
      {
        EditorGUILayout.HelpBox("Package ID must use the xxxx.xxx.xxx format.", MessageType.Error);
        error = true;
      }

      modName = EditorGUILayout.TextField("Package display name", modName);
      modAuthor = EditorGUILayout.TextField("Package author", modAuthor);
      modIntroduction = EditorGUILayout.TextField("Package description", modIntroduction, GUILayout.Height(60));
      modVersion = EditorGUILayout.IntField("Package version (default 1)", modVersion);
      modUserVersion = EditorGUILayout.TextField("Package version display name", modUserVersion);
      PackageType = (GamePackageType)EditorGUILayout.EnumPopup("Package type", PackageType);

      EditorGUILayout.Space(20);

      if (GUILayout.Button("Generate") && !error)
      {
        Make();
      }

      EditorGUILayout.EndVertical();

      if (EditorGUI.EndChangeCheck() && serializedObject != null)
        serializedObject.ApplyModifiedProperties();
    }

    private void Make()
    {
      string folderPath = GamePathManager.DEBUG_PACKAGE_FOLDER + "/" + modPackageName;
      if (Directory.Exists(folderPath))
      {
        if (!EditorUtility.DisplayDialog("Information", "Package " + modPackageName + " already exists at: \n" +
            folderPath + "\nReplace it?", "Replace", "Cancel"))
          return;
      }

      DirectoryInfo directoryInfo = Directory.CreateDirectory(folderPath);
      if (directoryInfo == null)
      {
        EditorUtility.DisplayDialog("Error", "Failed to create folder:\n" + folderPath, "OK");
        return;
      }

      XmlDocument xml = new XmlDocument();
      xml.LoadXml(template_PackageDef.text);

      XmlNode Package = xml.SelectSingleNode("Package");
      XmlNode BaseInfo = Package.SelectSingleNode("BaseInfo");
      XmlNode Name = BaseInfo.SelectSingleNode("Name");
      XmlNode Author = BaseInfo.SelectSingleNode("Author");
      XmlNode Introduction = BaseInfo.SelectSingleNode("Introduction");
      XmlNode VersionName = BaseInfo.SelectSingleNode("VersionName");

      Package.Attributes["name"].InnerText = modPackageName;
      Package.Attributes["version"].InnerText = modVersion.ToString();

      Name.InnerText = modName;
      Author.InnerText = modAuthor;
      Introduction.InnerText = modIntroduction;
      VersionName.InnerText = modUserVersion;

      XmlNode MinVersion = xml.SelectSingleNode("Package/Compatibility/MinVersion");
      XmlNode TargetVersion = xml.SelectSingleNode("Package/Compatibility/TargetVersion");

      MinVersion.InnerText = GameConst.GameBulidVersion.ToString();
      TargetVersion.InnerText = GameConst.GameBulidVersion.ToString();

      XmlNode PackageType = xml.SelectSingleNode("Package/Type");

      PackageType.InnerText = this.PackageType.ToString();

      xml.Save(folderPath + "/PackageDef.xml");

      if (this.PackageType == GamePackageType.Module)
      {
        string str = File.ReadAllText(GamePathManager.DEBUG_PACKAGE_FOLDER + "/template_PackageEntry.cs");
        str = str.Replace("ModNamespace", modPackageName);
        File.WriteAllText(folderPath + "/PackageEntry.cs", str);
        File.WriteAllText(folderPath + "/.gitignore", "bin/\nobj/\n.vs/\n.dll/\n!*.csproj");
        MakeCSproj(folderPath);
      }
      File.Copy(GamePathManager.DEBUG_PACKAGE_FOLDER + "/template_PackageLogo.png", folderPath + "/PackageLogo.png");

      AssetDatabase.Refresh();
      EditorUtility.DisplayDialog("Information", "Template generated successfully!", "OK");

      Close();
    }

    private void MakeCSproj(string folderPath) {
      
      XmlDocument xml = new XmlDocument();
      xml.LoadXml(File.ReadAllText("./Assembly-CSharp.csproj"));

      var addedRes = false;

      foreach(XmlNode node in xml.DocumentElement.ChildNodes) {
        if (node.Name == "PropertyGroup") {
          bool isRoot = false;
          foreach(XmlNode node1 in node.ChildNodes) {
            if (node1.Name == "ProjectGuid") {
              node1.InnerText = GUID.Generate().ToString();
            } else if (node1.Name == "AssemblyName") {
              node1.InnerText = modPackageName;
            } else if (node1.Name == "OutputPath") {
              node1.InnerText = ".dll";
            } else if (node1.Name == "RootNamespace") {
              isRoot = true;
            }
          }

          if (isRoot) {
            var biNode = xml.CreateElement("BaseIntermediateOutputPath");
            biNode.InnerText = "..\\..\\..\\obj\\";
            node.AppendChild(biNode);
          }
        } else if (node.Name == "ItemGroup") {
          for (var i = node.ChildNodes.Count - 1; i >= 0; i--)
          {
            var node1 = node.ChildNodes[i];
            if (node1.Name == "Compile" || node1.Name == "None" || node1.Name == "ProjectReference") {
              node.RemoveChild(node1);
            }
          }

          if (!addedRes) {
            addedRes = true;
            
            var csNode = xml.CreateElement("Compile");
            var csInclude = xml.CreateAttribute("Include");
            csInclude.Value = "PackageEntry.cs";
            csNode.Attributes.Append(csInclude);

            var xmlNode = xml.CreateElement("None");
            var xmlNodeInclude = xml.CreateAttribute("Include");
            xmlNodeInclude.Value = "PackageDef.xml";
            xmlNode.Attributes.Append(xmlNodeInclude);

            var refACNode = xml.CreateElement("Reference");
            var refACNodeInclude = xml.CreateAttribute("Include");
            var refACNodeHintPath = xml.CreateElement("HintPath");
            refACNodeHintPath.InnerText = "..\\..\\..\\Library\\ScriptAssemblies\\Assembly-CSharp.dll";
            refACNodeInclude.Value = "Assembly-CSharp";
            refACNode.AppendChild(refACNodeHintPath);
            refACNode.Attributes.Append(refACNodeInclude);

            node.AppendChild(csNode);
            node.AppendChild(xmlNode);
            node.AppendChild(refACNode);
          }
        } 

      }

      xml.Save(folderPath + "/Package.csproj");
    }
  }
}
