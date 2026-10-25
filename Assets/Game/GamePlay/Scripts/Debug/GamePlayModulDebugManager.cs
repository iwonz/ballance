using System.Collections.Generic;
using Ballance2.Game.GamePlay.Balls;
using Ballance2.Package;
using Ballance2.Services;
using Ballance2.Services.Debug;
using Ballance2.Utils;
using BallancePhysics.Wapper;
using UnityEngine;
using UnityEngine.UI;

namespace Ballance2.Game.GamePlay.Debug
{
  /// <summary>
  /// Minimal mechanism debugging environment.
  /// </summary>
  public static class GamePlayModulDebugManager
  {
    private static Text stateText;
    private static GameObject Modul_Instace;

    public static void Init() {
      var GameDebugEntry = Ballance2.Entry.GameDebugEntry.Instance;
      // Validate parameters.
      if (GameDebugEntry.ModulTestFloor == null) {
        GameErrorChecker.ThrowGameError(GameError.ParamNotProvide, "ModulTestFloor is not set!");
        return;
      }
      if (GameDebugEntry.ModulTestUI == null) {
        GameErrorChecker.ThrowGameError(GameError.ParamNotProvide, "ModulTestUI is not set!");
        return;
      }
      if (GameDebugEntry.ModulInstance) {
        GameErrorChecker.ThrowGameError(GameError.ParamNotProvide, "ModulInstance is not set!");
        return;
      }
      if (GameDebugEntry.ModulName == "") {
        GameErrorChecker.ThrowGameError(GameError.ParamNotProvide, "ModulName is not set!");
        return;
      }

      GamePlayInitManager.GamePlayInit(false, () => {
        GameObject PR_Resetpoint = null;
        GameObject Modul_Placeholder = null;
        GamePlayManager GamePlayManager = GamePlay.GamePlayManager.Instance;
        
        // Load the physics environment.
        GamePlayManager.GamePhysicsWorld.Create();

        // Clone floors.
        var physicsData = GamePhysFloor.Data["Phys_Floors"];
        var physicsDataStopper = GamePhysFloor.Data["Phys_FloorStopper"];
        var group = GameManager.Instance.InstancePrefab(GameDebugEntry.ModulTestFloor, "ModulTestFloor");
        var childCount = group.transform.childCount;
        for(int i = 0; i < childCount; i++) {
          var go = group.transform.GetChild(i).gameObject;
          if (go.activeSelf) {
            if (go.name.StartsWith("Floor")) {
              var meshFilter = go.GetComponent<MeshFilter>();
              if (meshFilter != null && meshFilter.mesh != null) {
                var body = go.AddComponent<PhysicsObject>();
                body.DoNotAutoCreateAtAwake = true;
                body.Fixed = true;
                body.BuildRootConvexHull = false;
                body.Concave.Add(meshFilter.mesh);
                body.Friction = physicsData.Friction;
                body.Elasticity = physicsData.Elasticity;
                body.Layer = physicsData.Layer;
                body.UseExistsSurface = true;
                body.CollisionID = GamePlayManager.BallSoundManager.GetSoundCollIDByName(physicsData.CollisionLayerName);
                body.Physicalize();
              }
            } 
            else if (go.name.StartsWith("FloorStopper")) {
              var meshFilter = go.GetComponent<MeshFilter>();
              if (meshFilter != null && meshFilter.mesh != null) {
                var body = go.AddComponent<PhysicsObject>();
                body.DoNotAutoCreateAtAwake = true;
                body.Fixed = true;
                body.BuildRootConvexHull = false;
                body.Concave.Add(meshFilter.mesh);
                body.Friction = physicsDataStopper.Friction;
                body.Elasticity = physicsDataStopper.Elasticity;
                body.Layer = physicsDataStopper.Layer;
                body.UseExistsSurface = true;
                body.CollisionID = GamePlayManager.BallSoundManager.GetSoundCollIDByName(physicsDataStopper.CollisionLayerName);
                body.Physicalize();
              }
            }
            else if (go.name == "PR_Resetpoint") {
              PR_Resetpoint = go;
            } 
            else if (go.name == GameDebugEntry.ModulName) {
              Modul_Placeholder = go;
            }
          }
        }

        if (PR_Resetpoint == null) {
          GameErrorChecker.ThrowGameError(GameError.ParamNotProvide, "PR_Resetpoint was not found. Add one to the test floor prefab.");
          return;
        }
        if (Modul_Placeholder == null) {
          GameErrorChecker.ThrowGameError(GameError.ParamNotProvide, $"{GameDebugEntry.ModulName} was not found. Add it to the test floor prefab.");
          return;
        }

        // Clone mechanisms.
        Modul_Instace = GameManager.Instance.InstancePrefab(GameDebugEntry.ModulInstance, "ModulInstance");
        Modul_Instace.transform.position = Modul_Placeholder.transform.position;
        Modul_Instace.transform.rotation = Modul_Placeholder.transform.rotation;
        Modul_Placeholder.SetActive(false);

        var Modul_Classes = Modul_Instace.GetComponents<ModulBase>();
        if (Modul_Classes.Length == 0) {
          GameErrorChecker.ThrowGameError(GameError.ParamNotProvide, $"ModulBase was not found on {Modul_Instace.name}. Check the attached scripts.");
          return;
        }
        var Modul_Class = Modul_Classes[0];
        
        // Clone UI.
        var ui = GameManager.Instance.InstancePrefab(GameDebugEntry.ModulTestUI, GameManager.Instance.GameCanvas, "ModulTestUI");
        stateText = ui.transform.Find("Text").GetComponent<Text>();
        var button = ui.transform.Find("Text").GetComponent<Button>();

        var GameUIManager = Ballance2.Services.GameUIManager.Instance;

        button = ui.transform.Find("ButtonActive").GetComponent<Button>();
        button.onClick.AddListener(() => {
          Modul_Class.Active();
          UpdateText("Active");
        });
        button = ui.transform.Find("ButtonDeactive").GetComponent<Button>();
        button.onClick.AddListener(() => {
          Modul_Class.Deactive();
          UpdateText("Deactive");
        });
        button = ui.transform.Find("ButtonResetLevel").GetComponent<Button>();
        button.onClick.AddListener(() => {
          Modul_Class.Reset(ModulBaseResetType.LevelRestart);
          UpdateText("Reset levelRestart");
        });    
        button = ui.transform.Find("ButtonResetSector").GetComponent<Button>();
        button.onClick.AddListener(() => {
          Modul_Class.Reset(ModulBaseResetType.SectorRestart);
          UpdateText("Reset sectorRestart");
        });
        button = ui.transform.Find("ButtonQuit").GetComponent<Button>();
        button.onClick.AddListener(() => {
          ui.gameObject.SetActive(false);
          GameManager.Instance.QuitGame();
        });
        button = ui.transform.Find("ButtonBackup").GetComponent<Button>();
        button.onClick.AddListener(() => {
          Modul_Class.Backup();
          GameUIManager.GlobalToast("Modul_Class.Backup() !");
        });
        button = ui.transform.Find("ButtonCustom1").GetComponent<Button>();
        button.onClick.AddListener(() => {
          Modul_Class.DebugTestCommand(1);
          GameUIManager.GlobalToast("Modul_Class.DebugTestCommand(1) !");
        });
        button = ui.transform.Find("ButtonCustom2").GetComponent<Button>();
        button.onClick.AddListener(() => {
          Modul_Class.DebugTestCommand(2);
          GameUIManager.GlobalToast("Modul_Class.DebugTestCommand(2) !");
        });

        PR_Resetpoint.SetActive(false);

        Modul_Class.gameObject.SetActive(true);
        GameTimer.Delay(0.3f, () => {
          // Back up initial state.
          Modul_Class.Backup();
          Modul_Class.Deactive();

          // Build data.
          GamePlayManager.SectorManager.CurrentLevelSectorCount = 1;
          GamePlayManager.SectorManager.CurrentLevelSectors[1] = new SectorManager.SectorDataStorage{ moduls = { Modul_Class } };
          GamePlayManager.SectorManager.CurrentLevelRestPoints[1] = new SectorManager.RestPointsDataStorage {
            point = PR_Resetpoint,
            flame = null
          };
          
          GamePlayManager.CreateSkyAndLight("L", null, StringUtils.StringToColor("#B09D89"));

          UpdateText("Active");
          // Start the level.
          GameMediator.Instance.NotifySingleEvent("CoreGamePlayManagerInitAndStart", null);
        });
      });
    } 

    private static void UpdateText(string state) {
      stateText.text = $"Modul state tools.\n{Modul_Instace.name} state. {state}";
    }
  }
}