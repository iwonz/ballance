using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Ballance2.VirtoolsLoader;
using Ballance2.Package;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ballance2.Res;

/*
 * Copyright (c) 2022  mengyu
 * 
 * Module:
 * GameLevelLoaderNMO.cs
 * 
 * Purpose:
 * Load NMO level files.
 * 
 * Author:
 * mengyu
 */

namespace Ballance2.Game.LevelBuilder
{
  // NMO resources.
  public class LevelNMOAssets : LevelAssets 
  {
    public LevelNMOAssets(string path) : base(path, false) {}

    internal VirtoolsLoaderLoadNMOResult result;

    public override Texture GetTextureAsset(string name)
    {
      if (result.textureList.TryGetValue(name, out var v))
        return v;
      return null;
    }
    public override Texture2D GetTexture2DAsset(string name)
    {
      return GetTextureAsset(name) as Texture2D;
    }
    public override Mesh GetMeshAsset(string name)
    {
      if (result.meshList.TryGetValue(name, out var v))
        return v;
      return null;
    }
    public override AudioClip GetAudioClipAsset(string name)
    {
      return null;
    }
    public override GameObject GetPrefabAsset(string name)
    {
      if (result.objectNameList.TryGetValue(name, out var v))
        return v;
      return null;
    }
    public override Material GetMaterialAsset(string name)
    {
      if (result.materialList.TryGetValue(name, out var v))
        return v;
      return null;
    }
  }

  // NMO level loader.
  public class LevelLoaderNMO 
  {
    private static Dictionary<string, float> modelRotationCorrecting = new Dictionary<string, float>();

    private static void initAllModelRotationCorrecting() {
      // Correct the rotation of mechanisms imported from NMO.
      if (modelRotationCorrecting.Count == 0) {
        modelRotationCorrecting.Add("PC_CheckPoints", 90);
        modelRotationCorrecting.Add("PS_LevelStart", 90);
        modelRotationCorrecting.Add("P_Modul_03", 90);
        modelRotationCorrecting.Add("P_Modul_25", 90);
        modelRotationCorrecting.Add("P_Modul_26", 90);
        modelRotationCorrecting.Add("P_Modul_29", -180);
        modelRotationCorrecting.Add("P_Modul_30", -180);
        modelRotationCorrecting.Add("P_Modul_37", 90);
        modelRotationCorrecting.Add("P_Modul_34", 180);
      }
    }
    private static float getModelRotationCorrecting(string groupName) {
      if (modelRotationCorrecting.TryGetValue(groupName, out var v))
        return v;
      return 0;
    }

#if UNITY_STANDALONE_WIN
    public static IEnumerator LoaderNMO(LevelAssets level, GameLevelLoaderNativeCallback callback, GameLevelLoaderNativeErrCallback errCallback) 
    {
      initAllModelRotationCorrecting();
      VirtoolsLoader.Init(Application.dataPath + "\\VirtoolsLoader\\CK2.dll");
      var result = VirtoolsLoader.LoadNMOToScense(level.Path, 
        MaterialCallback, 
        TextureCallback, 
        (err) => { errCallback("FAILED_LOAD_NMO", err); },
        GameStaticResourcesPool.FindStaticAssets<Shader>("BlinnPhongSpeicalEmission"),
        GameStaticResourcesPool.FindStaticAssets<Material>("DefaultMaterial")
      );

      if (result != null) {
        ((LevelNMOAssets)level).result = result;
        callback(result.mainObj, CreateJsonFromNMO(((LevelNMOAssets)level)), level);
      }
      yield break;
    }
    private static string CreateJsonFromNMO(LevelNMOAssets assets) {
      // Generate JSON for the loader.

      // Basic metadata is unknown.
      JObject level = new JObject();
      level.Add(new JProperty("name", assets.Path));
      level.Add(new JProperty("author", "unknow"));
      level.Add(new JProperty("version", "unknow"));
      level.Add(new JProperty("introduction", "unknow"));
      level.Add(new JProperty("allowPreview", false));

      // NMO lacks this level configuration, so use fixed defaults.
      JObject levelInfo = new JObject();
      levelInfo.Add(new JProperty("firstBall", "BallWood"));
      levelInfo.Add(new JProperty("levelScore", 100));
      levelInfo.Add(new JProperty("startPoint", 1000));
      levelInfo.Add(new JProperty("startLife", 3));
      levelInfo.Add(new JProperty("skyBox", "A"));
      levelInfo.Add(new JProperty("musicTheme", 1));
      levelInfo.Add(new JProperty("lightColor", "#ffffff"));
      levelInfo.Add(new JProperty("endWithUFO", false));
      levelInfo.Add(new JProperty("autoGroup", false));

      //Sky layer
      foreach(var key in assets.result.objectNameList.Keys) {
        if (key.StartsWith("SkyLayer")) {
          levelInfo.Add(new JProperty("skyLayer", "SkyLayer"));
          break;
        } else {
          levelInfo.Add(new JProperty("skyLayer", "SkyVoterx"));
          break;
        }
      }

      // Generate level configuration from group data.
      var groupList = assets.result.groupList;

      JObject sectors = new JObject();// Sectors.
      JObject internalObjects = new JObject(); // Internal objects.
      JArray floors = new JArray(); // Floors.
      JArray groups = new JArray(); // Groups.

      // Find sector groups to populate grouping data.
      int sector = 0;
      foreach (var key in groupList.Keys) {
        if (key.StartsWith("Sector_")) {
          // Sector information.
          if (int.TryParse(key.Substring(7), out var index)) {
            if (sector < index) sector = index;
            // Group information.
            sectors.Add(new JProperty(index.ToString(), new JArray(groupList[key])));
          }
        } else if (key.StartsWith("P_")) {
          // Mechanism information.
          groups.Add(new JObject(
            new JProperty("name", key),
            new JProperty("rotationCorrecting", new JObject(
              new JProperty("x", 0),
              new JProperty("y", getModelRotationCorrecting(key)),
              new JProperty("z", 0)
            )),
            new JProperty("objects", new JArray(groupList[key]))
          ));

        } else if (key == "PR_Resetpoints") {
          var array = groupList[key];
          array.Sort();
          JObject objects = new JObject();
          for(int i = 0; i < array.Count; i++) 
            objects.Add((i + 1).ToString(), array[i]);
          internalObjects.Add("PR_ResetPoints", objects);
        } else if (key == "PC_Checkpoints") {
          var array = groupList[key];
          array.Sort();
          JObject objects = new JObject();
          for(int i = 0; i < array.Count; i++) 
            objects.Add((i + 2).ToString(), array[i]);
          internalObjects.Add("PC_CheckPointsRotationCorrecting", new JObject(
            new JProperty("x", 0),
            new JProperty("y", getModelRotationCorrecting("PC_CheckPoints")),
            new JProperty("z", 0)
          ));
          internalObjects.Add("PC_CheckPoints", objects);
        } else if (key == "PS_Levelstart") {
          var array = groupList[key];
          if (array.Count > 0)
            internalObjects.Add("PS_LevelStart", array[0]);
          internalObjects.Add("PS_LevelStartRotationCorrecting", new JObject(
            new JProperty("x", 0),
            new JProperty("y", getModelRotationCorrecting("PS_LevelStart")),
            new JProperty("z", 0)
          ));
        } else if (key == "PE_Levelende") {
          var array = groupList[key];
          if (array.Count > 0)
            internalObjects.Add("PE_LevelEnd", array[0]);
        }
      }
      // Number of sectors.
      levelInfo.Add(new JProperty("sectorCount", sector));

      // Death-zone information.
      if (groupList.TryGetValue("DepthTestCubes", out var depthTestCubes)) 
        levelInfo.Add(new JProperty("depthTestCubes", new JArray(depthTestCubes)));
      // Floors.
      if (groupList.TryGetValue("Phys_Floors", out var arrayFloors)) {

        if (groupList.TryGetValue("Sound_HitID_02", out var arrayWood)) {
          // Original wooden floors differ by sound, so identify them through Sound_HitID_02.
          floors.Add(new JObject(
            new JProperty("name", "Phys_FloorWoods"),
            new JProperty("objects", new JArray(arrayWood))
          ));
          // Remove wooden floors from the general floor group to avoid duplicates.
          arrayFloors.RemoveAll((v) => arrayWood.Contains(v));
        }

        floors.Add(new JObject(
          new JProperty("name", "Phys_Floors"),
          new JProperty("objects", new JArray(arrayFloors))
        ));
      }
      // Rails.
      if (groupList.TryGetValue("Phys_FloorRails", out var arrayRails)) {
        floors.Add(new JObject(
          new JProperty("name", "Phys_FloorRails"),
          new JProperty("objects", new JArray(arrayRails))
        ));
      }
      // Barriers.
      if (groupList.TryGetValue("Phys_FloorStopper", out var stopperRails)) {
        floors.Add(new JObject(
          new JProperty("name", "Phys_FloorStopper"),
          new JProperty("objects", new JArray(stopperRails))
        ));
      }

      levelInfo.Add(new JProperty("internalObjects", internalObjects));
      levelInfo.Add(new JProperty("floors", floors));
      levelInfo.Add(new JProperty("sectors", sectors));
      levelInfo.Add(new JProperty("groups", groups));

      level.Add(new JProperty("level", levelInfo));

      var str = level.ToString();
      Debug.Log("CreateJsonFromNMO: " + str);
      return str;
    }
    private static Texture TextureCallback(string texName)
    {
      return GamePackage.GetSystemPackage().GetTextureAsset(texName);
    }
    private static Material MaterialCallback (string matName) 
    {
      return GamePackage.GetSystemPackage().GetMaterialAsset(matName);
    }
#endif
  }
}