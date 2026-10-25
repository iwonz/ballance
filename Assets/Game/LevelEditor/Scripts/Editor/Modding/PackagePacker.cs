using System.IO;
using UnityEditor;
using UnityEngine;
using ICSharpCode.SharpZipLib.Checksum;
using Ballance2.Utils;
using ICSharpCode.SharpZipLib.Zip;
using System.Xml;
using System.Collections.Generic;

namespace Ballance2.Editor.Modding
{
  class PackagePacker
  {
    private static string projPath = "";
    private static string projModDefFile = "";
    private static string projModDirPath = "";
    private static string projLogoFile = "";
    private static string packPackageName = "";

    private static List<string> allAssetsPath = new List<string>();

    public static string DoPackPackage(BuildTarget packTarget, TextAsset packDefFile, string sourceName, string targetDir, string compiledCode = null, bool includeCode = true)
    {
      string targetPath = targetDir + "/" + sourceName + ".ballance";
      if (!string.IsNullOrEmpty(targetPath))
      {

        DoSolvePackageDef(packDefFile);

        if (string.IsNullOrEmpty(packPackageName))
          return "PackageDef.xml must specify the package name (packageName).";

        allAssetsPath.Clear();

        bool isCore = packPackageName == "core";
        string dirTargetPath = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(projModDirPath))
        {
          // Enumerate directory contents.
          if (Directory.Exists(projModDirPath))
          {
            DirectoryInfo direction = new DirectoryInfo(projModDirPath);
            FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
              string filesPath = files[i].FullName.Replace("\\", "/");

              if (files[i].Name.StartsWith(".")) continue;
              if (filesPath.EndsWith(".meta")) continue;
              if (filesPath.EndsWith(".shader")) continue;
              if (filesPath.EndsWith(".shadergraph")) continue;
              if (filesPath.EndsWith(".cginc")) continue;
              if (filesPath.EndsWith(".md")) continue;
              if (filesPath.EndsWith(".dll")) continue;
              if (filesPath.Contains("NoPackage")) continue;
              if (filesPath.EndsWith(".cs")) continue;
              if (filesPath.EndsWith(".csproj")) continue;
              if (isCore && filesPath.Contains("Levels")) continue;
              if (isCore && filesPath.Contains("Scripts/Native")) continue;

              allAssetsPath.Add(filesPath.Replace(projPath, ""));
            }
          }

          string name = Path.GetFileNameWithoutExtension(targetPath);

          // Package content.
          AssetBundleBuild assetBundleBuild = new AssetBundleBuild();
          assetBundleBuild.assetBundleName = packPackageName;
          assetBundleBuild.assetBundleVariant = "assetbundle";
          assetBundleBuild.assetNames = allAssetsPath.ToArray();

          // Package content.
          var manifest = BuildPipeline.BuildAssetBundles(dirTargetPath, new AssetBundleBuild[]{
                        assetBundleBuild
                    }, BuildAssetBundleOptions.ChunkBasedCompression, packTarget);
          if (manifest == null)
            return "BuildAssetBundles failed for " + packPackageName;

          EditorUtility.DisplayProgressBar("Packing", "Packing, please wait...", 0.6f);

          // Process .ballance packages.
          DoSolveBallancePack(packTarget, dirTargetPath, dirTargetPath + "/" + name, targetPath, compiledCode, includeCode);

          EditorUtility.ClearProgressBar();
        }
        else return "PackageDef.xml is outside this project.";
      }
      return "";
    }
    
    private static void DoSolvePackageDef(TextAsset packDefFile)
    {
      packPackageName = "";
      projPath = Directory.GetCurrentDirectory().Replace("\\", "/") + "/";
      projModDefFile = projPath + AssetDatabase.GetAssetPath(packDefFile);
      projModDirPath = projPath + Path.GetDirectoryName(AssetDatabase.GetAssetPath(packDefFile));
      projLogoFile = projModDirPath + Path.DirectorySeparatorChar + "PackageLogo.png";

      // Process package metadata.
      XmlDocument packDefXmlDoc = new XmlDocument();
      packDefXmlDoc.LoadXml(packDefFile.text);
      XmlNode nodePackage = packDefXmlDoc.SelectSingleNode("Package");

      if (nodePackage.Attributes["name"] != null)
        packPackageName = nodePackage.Attributes["name"].Value;

      foreach (XmlNode node in nodePackage.ChildNodes)
      {
        if (node.Name == "BaseInfo")
        {
          foreach (XmlAttribute attribute in node.Attributes)
          {
            if (attribute.Name == "packageName")
              packPackageName = attribute.Value;
          }
          break;
        }
      }
    }
    private static void DoSolveBallancePack(BuildTarget target, string dirTargetPath, string bundlePath, string targetPath, string compiledCode, bool includeCode)
    {
      Crc32 crc = new Crc32();
      ZipOutputStream zipStream = ZipUtils.CreateZipFile(targetPath);
      string basePath = projModDirPath.Replace("\\", "/").Replace(projPath, "");

      // Add to the package.
      ZipUtils.AddFileToZip(zipStream, bundlePath + ".assetbundle", "assets/" + Path.GetFileName(bundlePath) + ".assetbundle", ref crc);
      ZipUtils.AddFileToZip(zipStream, bundlePath + ".assetbundle.manifest", "assets/" + Path.GetFileName(bundlePath) + ".assetbundle.manifest", ref crc);
      ZipUtils.AddFileToZip(zipStream, projModDefFile, projModDirPath.Length, ref crc);

      // Add language resources.
      var i18nRes = projModDirPath + Path.DirectorySeparatorChar + "PackageLanguageResPre.xml"; 
      if (File.Exists(i18nRes)) ZipUtils.AddFileToZip(zipStream, i18nRes, projModDirPath.Length, ref crc);

      // Add the logo image.
      if (File.Exists(projLogoFile)) ZipUtils.AddFileToZip(zipStream, projLogoFile, projModDirPath.Length, ref crc);

      if (includeCode)
      {
        // Add the DLL.
        string projDllFileSrc = projModDirPath + Path.DirectorySeparatorChar + ".dll" + Path.DirectorySeparatorChar + packPackageName + ".dll";
        string projDllFile = projModDirPath + Path.DirectorySeparatorChar + "dll" + Path.DirectorySeparatorChar + packPackageName + ".dll";
        if (compiledCode != null)
        {
          if (!File.Exists(compiledCode))
            throw new FileNotFoundException("Missing freshly compiled package code", compiledCode);
          projDllFile = compiledCode;
        }
        else if (File.Exists(projDllFileSrc))
          File.Copy(projDllFileSrc, projDllFile, true);

        // Add the DLL.
        if (File.Exists(projDllFile))
          ZipUtils.AddFileToZip(zipStream, projDllFile, "csharp/" + packPackageName + ".dll", ref crc);

      }

      zipStream.Finish();
      zipStream.Close();
    }
  }
}
