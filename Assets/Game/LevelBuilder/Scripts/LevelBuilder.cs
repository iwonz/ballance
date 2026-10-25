using Ballance2.Services;
using UnityEngine;
using Ballance2.Package;
using Ballance2.Base;
using Ballance2.Game.GamePlay;
using System.Collections.Generic;
using Ballance2.UI.Core.Controls;
using UnityEngine.UI;
using Ballance2.Services.I18N;
using Ballance2.Services.Debug;
using System.Collections;
using Newtonsoft.Json;
using Ballance2.Game.GamePlay.Moduls;
using BallancePhysics.Wapper;
using Ballance2.Game.Utils;
using Ballance2.Utils;
using System.Collections.ObjectModel;
using Ballance2.Game.Remaster;

namespace Ballance2.Game.LevelBuilder {

  /// <summary>
  /// Level loader.
  /// </summary>
  public class LevelBuilder : GameSingletonBehavior<LevelBuilder> {

    private const string TAG = "LevelBuilder";

    /// <summary>
    /// Registered mechanism information.
    /// </summary>
    public Dictionary<string, LevelBuilderModulRegStorage> RegisteredModuls = new Dictionary<string, LevelBuilderModulRegStorage>();
    /// <summary>
    /// Mechanisms in the current level.
    /// </summary>
    public Dictionary<string, LevelBuilderModulStorage> CurrentLevelModuls = new  Dictionary<string, LevelBuilderModulStorage>();
    /// <summary>
    /// Floors in the current level.
    /// </summary>
    public List<GameObject> CurrentLevelFloors = new List<GameObject>();
    /// <summary>
    /// Current level resource file.
    /// </summary>
    public LevelAssets CurrentLevelAsset = null;
    /// <summary>
    /// Current level JSON definition.
    /// </summary>
    public LevelJson CurrentLevelJson = null;
    /// <summary>
    /// Current level sky layer.
    /// </summary>
    public GameObject CurrentLevelSkyLayer = null;
    /// <summary>
    /// Current level root object.
    /// </summary>
    public GameObject CurrentLevelObject = null;

    public Material _HalfTransparentMaterial;

    private RectTransform LevelBuilderUI;
    private Progress LevelBuilderUIProgress;
    private Text LevelBuilderUITextErrorContent;
    private GameObject LevelBuilderUIPanelFailed;
    private Button LevelBuilderUIButtonBack;
    private Button LevelBuilderUIButtonSubmitBug;
    private Button LevelBuilderUIButtonCopyErrInfo;
    private string LevelBuilderCurrentError = "";
    private LevelLoaderNative LevelLoader;

    private void Start() {

      LevelLoader = GetComponent<LevelLoaderNative>();
      GameMediator.Instance.RegisterEventHandler(
        GamePackage.GetSystemPackage(),
        GameEventNames.EVENT_GAME_MANAGER_INIT_FINISHED, 
        "LevelBuilder", 
        (evtName, pararms) => {
          GameMediator.Instance.RegisterGlobalEvent(GameEventNames.EVENT_LEVEL_BUILDER_JSON_LOADED);
          GameMediator.Instance.RegisterGlobalEvent(GameEventNames.EVENT_LEVEL_BUILDER_MAIN_PREFAB_STANDBY);
          GameMediator.Instance.RegisterGlobalEvent(GameEventNames.EVENT_LEVEL_BUILDER_START);
          GameMediator.Instance.RegisterGlobalEvent(GameEventNames.EVENT_LEVEL_BUILDER_UNLOAD_START);

          LevelBuilderUI = GameUIManager.Instance.InitViewToCanvas(GamePackage.GetSystemPackage().GetPrefabAsset("LevelBuilderUI.prefab"), "GameLevelBuilderUI", true);
          LevelBuilderUIProgress = LevelBuilderUI.Find("Progress").GetComponent<Progress>();
          LevelBuilderUITextErrorContent = LevelBuilderUI.Find("PanelFailed/ErrorArea/ScrollView/Viewport/TextErrorContent").GetComponent<Text>();
          LevelBuilderUIPanelFailed = LevelBuilderUI.Find("PanelFailed").gameObject;
          LevelBuilderUIButtonBack = LevelBuilderUI.Find("PanelFailed/ButtonBack").GetComponent<Button>();
          LevelBuilderUIButtonSubmitBug = LevelBuilderUI.Find("PanelFailed/ButtonSubmitBug").GetComponent<Button>();
          LevelBuilderUIButtonCopyErrInfo = LevelBuilderUI.Find("PanelFailed/ButtonCopyErrInfo").GetComponent<Button>();
          LevelBuilderUIPanelFailed.gameObject.SetActive(false);
          LevelBuilderUI.gameObject.SetActive(false);

          LevelBuilderUIButtonBack.onClick.AddListener(() => {
            GameUIManager.Instance.MaskBlackSet(true);
            GameManager.Instance.SetGameBaseCameraVisible(true);
            LevelBuilderUI.gameObject.SetActive(false);
            UnLoadLevel(null);
          });
          LevelBuilderUIButtonSubmitBug.onClick.AddListener(() => {
            Application.OpenURL(ConstLinks.BugReportURL) ;
          });
          LevelBuilderUIButtonCopyErrInfo.onClick.AddListener(() => { 
            GUIUtility.systemCopyBuffer = LevelBuilderCurrentError;
            GameUIManager.Instance.GlobalToast(I18N.Tr("core.ui.ErrorUIErrInfoCopied"));
          });

          UpdateErrStatus(false, null, null);
          return false;
        }
      );
      InitCommands();
    }
    protected override void OnDestroy() {
      if (LevelBuilderUI != null) {
        UnityEngine.Object.Destroy(LevelBuilderUI.gameObject);
        LevelBuilderUI = null;
      }
      LevelInternal.UnInitBulitInModuls();
      DestroyCommand();
    }

    #region Commands

    private int _CommandId = 0;
    private void InitCommands() {
      _CommandId = GameManager.Instance.GameDebugCommandServer.RegisterCommand("lb", (keyword, fullCmd, argsCount, args) => {
        var k2 = args[1];
        if (k2 == "quit") {
          Log.V(TAG, "Unload");
          UnLoadLevel(null);
        } else if (k2 == "load") {
          if (argsCount < 2)
            Log.E(TAG, "Missing arg 2");
          else {
            Log.V(TAG, "Load");
            GameMediator.Instance.NotifySingleEvent("CoreStartLoadLevel", args[2]);
          }
        } else {
          Log.W(TAG, "Unknow option "  + k2);
          return false;
        }
        return true;
      }, 0, "lb <quit/load>\n" +
        "  <quit>                    ▶ Cancel the current level load\n" +
        "  <load> <levelname:string> ▶ Load the specified level\n");
    }
    private void DestroyCommand() {
      if (GameManager.Instance != null)
        GameManager.Instance.GameDebugCommandServer.UnRegisterCommand(this._CommandId);
    }

    #endregion

    #region Main methods

    /// <summary>
    /// Whether preview mode is active.
    /// </summary>
    /// <value></value>
    public bool IsPreviewMode { get; private set; }

    /// <summary>
    /// Start unloading the level.
    /// </summary>
    /// <param name="endCallback">Completion callback.</param>
    public void UnLoadLevel(GameManager.VoidDelegate endCallback) {
      if (IsLoading) {
        Log.E(TAG, "Level is loading! ");
        return;
      }

      IsLoading = true;
      StartCoroutine(UnLoadLevelInternal(endCallback));
    }

    private IEnumerator UnLoadLevelInternal(GameManager.VoidDelegate endCallback) {
      if (EnvironmentController.Active != null) EnvironmentController.Active.Release();
      // Emit the start event.
      GameMediator.Instance.DispatchGlobalEvent(GameEventNames.EVENT_LEVEL_BUILDER_UNLOAD_START);

      Log.D(TAG, "UnLoad moduls");

      // Notify all mechanisms of unloading.
      GamePlayManager.Instance.SectorManager.DoUnInitAllModuls();
      
      // Invoke the level's custom unload callback.
      if (CurrentLevelJson != null && CurrentLevelJson.level != null) {
        var level = CurrentLevelJson.level;
        yield return StartCoroutine(CallLoadStep("unload"));
        if (!string.IsNullOrEmpty(level.customModEventName)) {
          GameMediator.Instance.DispatchGlobalEvent(level.customModEventName, "unload");
          GameMediator.Instance.UnRegisterGlobalEvent(level.customModEventName);
        }
      }

      yield return new WaitForSeconds(0.1f);

      Log.D(TAG, "Clear all");

      // Clear data.
      GamePlayManager.Instance.SectorManager.ClearAll();

      // Remove music data.
      if (CurrentLevelJson != null) {
        var customMusicTheme = CurrentLevelJson.level.customMusicTheme;
        if (customMusicTheme != null) {
          GamePlayManager.Instance.MusicManager.DeleteMusicTheme(customMusicTheme.id);
        }
      }
      GamePlayManager.Instance.MusicManager.SetCurrentTheme(1);
      // Stop all background sounds.
      GamePlayManager.Instance.BallSoundManager.StopAllSound();

      // Delete all mechanisms.
      var tickCount = 0;
      foreach (var item in CurrentLevelModuls)
      {
        if (tickCount > 16) {
          yield return new WaitForSeconds(0.01f);
          tickCount = 0;
        }
        UnityEngine.Object.Destroy(item.Value.go);
        tickCount = tickCount + 1;
      }
      // Delete floors.
      foreach (var floor in CurrentLevelFloors) {
        UnityEngine.Object.Destroy(floor);
      }
      CurrentLevelFloors.Clear();

      // Clear the sky and clouds.
      if (IsPreviewMode)
        GamePreviewManager.Instance.HideSkyAndLight();
      else
        GamePlayManager.Instance.HideSkyAndLight();
      
      if (CurrentLevelSkyLayer != null) {
        UnityEngine.Object.Destroy(CurrentLevelSkyLayer);
        CurrentLevelSkyLayer = null;
      }

      // Delete resource linkers.
      GameLevelResourcesLinker.allRes.Clear();

      // Delete level objects.
      UnityEngine.Object.Destroy(CurrentLevelObject);

      // Clear fields.
      CurrentLevelObject = null;
      CurrentLevelJson = null;
      CurrentLevelModuls.Clear();
      CurrentLevelFloors.Clear();

      Log.D(TAG, "Unload level asset");

      // Unload the AssetBundle.
      LevelLoader.UnLoadLevel(CurrentLevelAsset);
      CurrentLevelAsset = null;

      // Delete the level's physics collision information.
      if (!IsPreviewMode && GamePlayManager.Instance != null && GamePlayManager.Instance.GamePhysicsWorld != null) {
        GamePlayManager.Instance.GamePhysicsWorld.Destroy();
      }

      Log.D(TAG, "Unload level finish");

      IsLoading = false;

      if (endCallback != null)
        endCallback();
      else
        // Request a return to MenuLevel.
        GameManager.Instance.RequestEnterLogicScense("MenuLevel");
      yield break;
    }

    /// <summary>
    /// Start the level loading sequence.
    /// </summary>
    /// <param name="name">Level filename.</param>
    /// <param name="preview">Whether to use preview mode.</param>
    public void LoadLevel(string name, bool preview = false) {
      if (IsLoading) {
        Log.E(TAG, "Level is loading! ");
        return;
      }

      IsLoading = true;
      IsPreviewMode = preview;

      Log.D(TAG, "Load level start ");
      if (preview)
        Log.I(TAG, "Load level in preview mode");
      
      // Reset the loading UI.
      if (!GameManager.Instance.GameSettings.GetBool("debugDisableLoadUI", false)) {
        LevelBuilderUI.gameObject.SetActive(true);
        GameUIManager.Instance.MaskBlackSet(true);
      } else {
        GameUIManager.Instance.MaskBlackSet(false);
      }
      UpdateLoadProgress(0);
      UpdateErrStatus(false, null, null);

      // Load built-in modules.
      LevelInternal.InitBulitInModuls();

      if (!preview) {
        // Load the physics environment.
        GamePlayManager.Instance.GamePhysicsWorld.Create();
      }

      // Emit the start event.
      GameMediator.Instance.DispatchGlobalEvent(GameEventNames.EVENT_LEVEL_BUILDER_BEFORE_START);

      // Load built-in sounds.
      LevelInternal.InitBulitInModulCustomSounds();

      LevelLoader.LoadLevel(name, (mainObj, jsonString, level) => {
        CurrentLevelAsset = level;

        try {
          CurrentLevelJson = JsonConvert.DeserializeObject<LevelJson>(jsonString);
        }catch (System.Exception e) {
          UpdateErrStatus(true, "BAD_LEVEL_JSON", "Failed to decode json, error: " + e.ToString());
          return;
        }
        
        Log.D(TAG, "Level asset loaded");

        GameMediator.Instance.DispatchGlobalEvent(GameEventNames.EVENT_LEVEL_BUILDER_JSON_LOADED, CurrentLevelJson);

        // Check base compatibility.
        var missedPackages = "";
        var requiredPackages = CurrentLevelJson.requiredPackages;
        if (requiredPackages != null && requiredPackages.Count > 0) {
          foreach (var v in requiredPackages) {
            if (!string.IsNullOrEmpty(v.name) && !GamePackageManager.Instance.CheckRequiredPackage(v.name, v.minVersion)) 
              missedPackages += $"\nName：{v.name} Version：{v.minVersion}";
          }
        }
        // Check mod compatibility.
        if (missedPackages != "") {
          UpdateErrStatus(true, "DEPENDS_CHECK_FAILED", "The level depends on the following module. The module may not be enabled or failed to load: " + missedPackages);
          return;
        }

        Log.D(TAG, "Load level prefab");

        // Load the prefab.
        CurrentLevelObject = mainObj; 
        mainObj.transform.SetParent(gameObject.transform);
        mainObj.name = "GameLevelMain";
        GameMediator.Instance.DispatchGlobalEvent(GameEventNames.EVENT_LEVEL_BUILDER_MAIN_PREFAB_STANDBY, CurrentLevelObject);

        // Load.
        StartCoroutine(LoadLevelInternal());
      }, 
      (code, err) => {
        UpdateErrStatus(true, code, err);
      });
    }

    private IEnumerator LoadLevelInternal() {
      // Emit the start event.
      GameMediator.Instance.DispatchGlobalEvent(GameEventNames.EVENT_LEVEL_BUILDER_START);
      // Load base settings.
      var level = CurrentLevelJson.level;
      var levelName = CurrentLevelJson.name;

      var SystemPackage = GamePackage.GetSystemPackage();
      var GamePlayManagerInstance = GamePlayManager.Instance;
      var SectorManager = GamePlayManagerInstance.SectorManager;
      var MusicManager = GamePlayManagerInstance.MusicManager;
      var BallSoundManager = GamePlayManagerInstance.BallSoundManager;

      CurrentLevelModuls.Clear();
      CurrentLevelSkyLayer = null;
      CurrentLevelFloors.Clear();

      Log.D(TAG, "Pre load");

      // Run custom pre loading steps.
      yield return StartCoroutine(CallLoadStep("pre"));
      CallLevelCustomModEvent("beforeLoad");
      if (!string.IsNullOrEmpty(level.customModEventName)) {
        GameMediator.Instance.RegisterGlobalEvent(level.customModEventName);
        GameMediator.Instance.DispatchGlobalEvent(level.customModEventName, "pre");
      }

      Log.D(TAG, "Check config");

      if (IsPreviewMode && !CurrentLevelJson.allowPreview) {
        UpdateErrStatus(true, "BAD_CONFIG", "Not support preview");
        yield break;
      }
      // Validate configuration first.
      if (level.sectorCount < 1) {
        UpdateErrStatus(true, "BAD_CONFIG", "There must be at least 1 sector in this level");
        yield break;
      }
      if (level.sectorCount > SectorManager.MAX_SECTOR_COUNT) {
        UpdateErrStatus(true, "BAD_CONFIG", $"There are too many sectors (more than {SectorManager.MAX_SECTOR_COUNT})");
        yield break;
      }  
      if (level.autoGroup == true) {
        Log.D(TAG, "Generate auto group");
        DoLevelAutoGroup(level, CurrentLevelObject.transform); // Generate grouping information when autoGroup is enabled.
      } 
      else 
      {
        if (level.internalObjects == null) {
          UpdateErrStatus(true, "BAD_CONFIG", "\"internalObjects\" is invalid");
          yield break;
        }
        if (string.IsNullOrEmpty(level.internalObjects.PS_LevelStart)) {
          UpdateErrStatus(true, "BAD_CONFIG", "\"internalObjects.PS_LevelStart\" is invalid");
          yield break;
        }
        if (string.IsNullOrEmpty(level.internalObjects.PE_LevelEnd)) {
          UpdateErrStatus(true, "BAD_CONFIG", "\"internalObjects.PE_LevelEnd\" is invalid");
          yield break;
        }  
        if (level.internalObjects.PC_CheckPoints == null) {
          UpdateErrStatus(true, "BAD_CONFIG", "\"internalObjects.PC_CheckPoints\" is invalid");
          yield break;
        }  
        if (level.internalObjects.PR_ResetPoints == null || level.internalObjects.PR_ResetPoints.Count == 0) {
          UpdateErrStatus(true, "BAD_CONFIG", "\"internalObjects.PR_ResetPoints\" is invalid");
          yield break;
        }
        if (level.sectors == null || level.sectors.Count == 0) {
          UpdateErrStatus(true, "BAD_CONFIG", "\"sectors\" is invalid");
          yield break;
        }
        if (level.floors == null) {
          UpdateErrStatus(true, "BAD_CONFIG", "\"floors\" is invalid");
          yield break;
        }  
        if (level.groups == null) {
          UpdateErrStatus(true, "BAD_CONFIG", "\"groups\" is invalid");
          yield break;
        }
      }

      Log.D(TAG, "Load level data");

      // Populate spawn, sector, and end-point information first.
      SectorManager.CurrentLevelSectorCount = level.sectorCount;
      for (int i = 0; i < SectorManager.CurrentLevelSectors.Length; i++)
        SectorManager.CurrentLevelSectors[i] = null;
      for (int i = 0; i < SectorManager.CurrentLevelRestPoints.Length; i++)
        SectorManager.CurrentLevelRestPoints[i] = null;
      if (!IsPreviewMode) {
        GamePlayManagerInstance.NextLevelName = level.nextLevel == null ? "" : level.nextLevel;
        GamePlayManagerInstance.StartBall = level.firstBall;
        GamePlayManagerInstance.CurrentLevelName = CurrentLevelJson.name;
        GamePlayManagerInstance.CurrentEndWithUFO = level.endWithUFO;
      
        Log.D(TAG, $"Level Name: {GamePlayManagerInstance.CurrentLevelName}\nSectors: {level.sectorCount}");

        if (level.defaultHighscoreData != null)
          HighscoreManager.Instance.TryAddDefaultLevelHighScore(levelName, level.defaultHighscoreData);
        else {
          HighscoreManager.Instance.TryAddDefaultLevelHighScore(levelName, null);
          Log.D(TAG, "Not found user config defaultHighscoreData for this level, using system defaultHighscoreData");
        }

        if (level.startLife > 0 || level.startLife == -1)
          GamePlayManagerInstance.StartLife = level.startLife;
        if (level.startPoint > 0)
          GamePlayManagerInstance.StartPoint = level.startPoint;
        if (level.levelScore > 0)
          GamePlayManagerInstance.LevelScore = level.levelScore;
      }

      UpdateLoadProgress(0.1f);

      // Load.

      // Load internal objects.
      //===================================

      Log.D(TAG, "Load level internal objects");

      // Load spawn points and flames.
      //---------------------------
      for (int i = 1; i <= level.sectorCount; i++) {
        ModulBase flame = null;

        if (i == 1) {
          flame = ReplacePrefab(level.internalObjects.PS_LevelStart, FindRegisterModul("PS_LevelStart"), level.internalObjects.PS_LevelStartRotationCorrecting);
          if (flame == null) {
            UpdateErrStatus(true, "OBJECT_MISSING", "Object \"PS_LevelStart\" is missing");
            yield break;
          }
        } else {
          if (!level.internalObjects.PC_CheckPoints.ContainsKey($"{i}")) {
            UpdateErrStatus(true, "OBJECT_MISSING", $"Object \"PC_CheckPoints.{i}\" is missing");
            yield break;
          }
          flame = ReplacePrefab(level.internalObjects.PC_CheckPoints[$"{i}"], FindRegisterModul("PC_CheckPoints"), level.internalObjects.PC_CheckPointsRotationCorrecting);
          if (flame == null) {
            UpdateErrStatus(true, "OBJECT_MISSING", $"Object \"PC_CheckPoints.{i}\" is null");
            yield break;
          }
        }

        if (!level.internalObjects.PR_ResetPoints.ContainsKey($"{i}")) {
          UpdateErrStatus(true, "FIELD_MISSING", $"Field \"level.internalObjects.PR_ResetPoints[{i}]\" is missing");
          yield break;
        }

        var objName = level.internalObjects.PR_ResetPoints[$"{i}"];
        var r = GameObject.Find(objName);
        if (r == null) {
          UpdateErrStatus(true, "OBJECT_MISSING", $"\"level.internalObjects.PR_ResetPoints[{i}]\" => \"{objName}\" not found");
          yield break;
        }

        r.SetActive(false);

        SectorManager.CurrentLevelRestPoints[i] = new SectorManager.RestPointsDataStorage {
          point = r,
          flame = flame,
        };
      }
      // Load the ending object.
      //---------------------------
      SectorManager.CurrentLevelEndBalloon = ReplacePrefab(
        level.internalObjects.PE_LevelEnd, 
        FindRegisterModul("PE_LevelEnd"), 
        level.internalObjects.PE_LevelEndRotationCorrecting
      ) as PE_Balloon;
      if (SectorManager.CurrentLevelEndBalloon == null) {
        UpdateErrStatus(true, "OBJECT_MISSING", $"\"level.internalObjects.PE_LevelEnd\" => \"{level.internalObjects.PE_LevelEnd}\" not found");
        yield break;
      }

      yield return new WaitForSeconds(0.02f);
      UpdateLoadProgress(0.2f);

      // Load physics objects.
      //===================================
      // These objects need no physics in preview mode.
      if (!IsPreviewMode) {
        Log.D(TAG, "Load level floors");

        // Load physics floors.
        //----------------------------
        foreach (var floor in level.floors) {
          
          var floorCount = 0;
          var sleepCount = 0;
          if (GamePhysFloor.Data.ContainsKey(floor.name)) {
            var physicsData = GamePhysFloor.Data[floor.name] ;

            //StaticCompound
            var floorStatic = GameManager.Instance.InstanceNewGameObject(gameObject.transform, floor.name);
            CurrentLevelFloors.Add(floorStatic);
            
            //Floor childs
            foreach (var name in floor.objects) {

              if (sleepCount > 128) {
                yield return new WaitForSeconds(0.01f);
                sleepCount = 0;
              }

              var go = GameObject.Find(name);
              if (go != null) {
                //Mesh
                var meshFilter = go.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.mesh != null) {
                  go.transform.SetParent(floorStatic.transform);
                  go.tag = floor.name;
                  if (go.GetComponent<MeshCollider>() == null)
                    go.AddComponent<MeshCollider>();
                  if (go.GetComponent<MeshCollider>() == null)
                    go.AddComponent<MeshCollider>();
                  var body = go.GetComponent<PhysicsObject>();
                  if (body == null)
                    body = go.AddComponent<PhysicsObject>();
                  body.DoNotAutoCreateAtAwake = true;
                  body.Fixed = true;
                  body.Concave.Add(meshFilter.mesh);
                  body.Friction = physicsData.Friction;
                  body.Elasticity = physicsData.Elasticity;
                  body.BuildRootConvexHull = false;
                  body.Layer = physicsData.Layer;
                  body.CollisionID = BallSoundManager.GetSoundCollIDByName(physicsData.CollisionLayerName);
                  body.Physicalize();

                  if (!string.IsNullOrEmpty(physicsData.HitSound)) {
                    var hitSound = GameSoundManager.Instance.RegisterSoundPlayer(GameSoundType.Normal, physicsData.HitSound, false, true, $"Floor_{name}_HitSound");
                    
                    body.EnableContractEventCallback();
                    body.EnableCollisionEvent = true;
                    body.AddCollDetection(BallSoundManager.GetSoundCollIDByName("WoodenFlap"), 0.02f, 10, 0.2f, 0.1f);
                    body.AddCollDetection(BallSoundManager.GetSoundCollIDByName("Wood"), 0.02f, 12, 0.2f, 0.1f);
                    body.AddCollDetection(BallSoundManager.GetSoundCollIDByName("WoodOnlyHit"), 0.02f, 10, 0.2f, 0.1f);
                    // Collision callback.
                    body.OnPhysicsCollDetection = (_, col_id, speed_precent) => {
                      hitSound.volume = speed_precent;
                      hitSound.Play();
                    };
                  }
                } else {
                  Log.W(TAG, $"Not found MeshFilter or mesh in floor  \"{name}\"");
                }
                floorCount++;
                sleepCount++;
              } else {
                Log.W(TAG, $"Not found floor  \"{name}\" in type \"{floor.name}\"");
              }
            }

            if (floorCount == 0)
              floorStatic.SetActive(false); // Hide the static parent when no floors exist.
            else
              Log.D(TAG, $"Loaded floor {floor.name} count: {floorCount}");
            
          } else {
            Log.E(TAG, $"Unknow floor type \"{floor.name}\"");
          }
        }

        UpdateLoadProgress(0.3f);

        var lowerY = -1000.0f; // Minimum Y coordinate.
        var depthTestCubesCount = 0;

        // Load fall detection zones.
        //-----------------------------
        foreach (var name in level.depthTestCubes) {
          var go = GameObject.Find(name);
          if (go != null) {

            // Calculate the minimum Y coordinate used to collect fallen objects.
            var y = go.transform.position.y;
            if (y < lowerY)
              lowerY = y;

            // Disable the Renderer to hide the object.
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) 
              renderer.enabled = false;
            
            // Add the fall detection zone mesh.
            var collider = go.AddComponent<BoxCollider>();
            var tigger = go.AddComponent<TiggerTester>();
            
            collider.isTrigger = true;
            tigger.onTriggerEnter = (_, other) => {
              // Trigger a ball fall.
              if (other.tag == "Ball")
                GamePlayManagerInstance.Fall();
            };

            depthTestCubesCount++;
          } else {
            Log.W(TAG, $"Not found object \"{name}\" in depthTestCubes");
          }
        }

        Log.D(TAG, $"Loaded depthTestCubes count: {depthTestCubesCount} lowerY: {lowerY}");

        // Set the collection Y coordinate.
        GamePlayManagerInstance.GamePhysicsWorld.DePhysicsFall = lowerY;
          
      } 
      else 
      {
        // Load fall detection zone data.
        //---------------------------

        var depthTestCubes = new List<GameObject>();
        foreach (var name in level.depthTestCubes) {
          var go = GameObject.Find(name);
          if (go != null) {

            // Add the border material.
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null)
              renderer = go.AddComponent<MeshRenderer>();
            
            renderer.material = _HalfTransparentMaterial;

            depthTestCubes.Add(go);
          } else {
            Log.W(TAG, $"Not found object \"{name}\" in depthTestCubes");
          }
        }

        GamePreviewManager.Instance.GameDepthTestCubes = depthTestCubes.ToArray();
      }

      Log.D(TAG, "Load level moduls");

      // Run custom modul loading steps.
      //===================================
      yield return StartCoroutine(CallLoadStep("modul"));
      UpdateLoadProgress(0.5f);

      // Load mechanisms.
      //---------------------------
      var tickCount = 0;
      foreach(var group in level.groups) {
        var modul = FindRegisterModul(group.name);
        var rotationCorrecting = group.rotationCorrecting;
        if (modul != null) {
          
          var modulCount = 0;
          var modulFailedCount = 0;

          Log.D(TAG, $"Load modul {group.name}");

          foreach(var name in group.objects) {

            if (tickCount > 16) {
              yield return new WaitForSeconds(0.02f);
              tickCount = 0;
            }

            var m = ReplacePrefab(name, modul, rotationCorrecting);
            if (m != null) {
              tickCount++;
              modulCount++;
            } else
              modulFailedCount++;
          }

          Log.D(TAG, $"Loaded modul {group.name} count : {modulCount}" + (modulFailedCount > 0 ? ($" failed count: {modulFailedCount}") : ""));
          if (modulFailedCount > 0)
            Log.W(TAG, $"Load failed modul {group.name} count : {modulFailedCount}");
        } else {
          Log.W(TAG, $"Modul \"{group.name}\" not register");
        }

      }
      UpdateLoadProgress(0.6f);

      Log.D(TAG, "Load init moduls");

      // Perform initial mechanism loading.
      //-----------------------------
      yield return new WaitForSeconds(0.02f);

      // Preview mode does not need mechanism state backups.
      if (!IsPreviewMode)
        SectorManager.DoInitAllModuls();

      UpdateLoadProgress(0.7f);

      Log.D(TAG, "Load level sector data");

      // Populate sector data.
      //-----------------------------  
      for(int i = 1; i <= level.sectorCount; i++) {
        if (!level.sectors.ContainsKey($"{i}"))
          Log.W(TAG, $"Sector key \"{i}\" not found");
        else {
          var sector = level.sectors[$"{i}"];
          var moduls = new List<ModulBase>();
          foreach (var name in sector) {
            if (!CurrentLevelModuls.ContainsKey(name))
              Log.W(TAG, $"Not found modul \"{name}\" in sectors.{i}");
            else {
              var modul = CurrentLevelModuls[name];
              moduls.Add(modul.modul);
            }
          }
          SectorManager.CurrentLevelSectors[i] = new SectorManager.SectorDataStorage {
            moduls = moduls
          };
        }
      }

      UpdateLoadProgress(0.8f);

      yield return new WaitForSeconds(0.02f);
      Log.D(TAG, "Load sky and light");

      // Load the skybox and lights.
      //---------------------------
      RenderSettings.fog = false;
      if (level.skyBox == "custom") {
        // Load a custom skybox.
        Log.D(TAG, "Load custom SkyBox");

        var B = CurrentLevelAsset.GetTextureAsset(level.customSkyBox.B);
        var F = CurrentLevelAsset.GetTextureAsset(level.customSkyBox.F);
        var L = CurrentLevelAsset.GetTextureAsset(level.customSkyBox.L);
        var R = CurrentLevelAsset.GetTextureAsset(level.customSkyBox.R);
        var T = CurrentLevelAsset.GetTextureAsset(level.customSkyBox.T);
        var D = CurrentLevelAsset.GetTextureAsset(level.customSkyBox.D);
        if (B == null) Log.W(TAG, "Failed to load customSkyBox.B texture");
        if (F == null) Log.W(TAG, "Failed to load customSkyBox.F texture");
        if (L == null) Log.W(TAG, "Failed to load customSkyBox.L texture");
        if (R == null) Log.W(TAG, "Failed to load customSkyBox.R texture");
        if (D == null) Log.W(TAG, "Failed to load customSkyBox.D texture");

        var skyMat = SkyBoxUtils.MakeCustomSkyBox(L, R, F, B, D, T);
        if (IsPreviewMode)
          GamePreviewManager.Instance.CreateSkyAndLight("", skyMat, StringUtils.StringToColor(level.lightColor));
        else
          GamePlayManagerInstance.CreateSkyAndLight("", skyMat, StringUtils.StringToColor(level.lightColor));
      } 
      else if (!string.IsNullOrEmpty(level.skyBox)) {
        // Use a built-in skybox.
        if (IsPreviewMode)
          GamePreviewManager.Instance.CreateSkyAndLight(level.skyBox, null, StringUtils.StringToColor(level.lightColor));
        else
          GamePlayManagerInstance.CreateSkyAndLight(level.skyBox, null, StringUtils.StringToColor(level.lightColor));
      } 
      else
        Log.E(TAG, "Invalid field \"level.skyBox\"");

      // Load the sky layer.
      //---------------------------
      if (level.skyLayer == "SkyLayer") {
        var oldSkyLayer = GameObject.Find("SkyLayer");
        if (oldSkyLayer != null) {
          CurrentLevelSkyLayer = GameManager.Instance.InstancePrefab(SystemPackage.GetPrefabAsset("FinalSkyLayer.prefab"), "SkyLayer");
          CurrentLevelSkyLayer.transform.position = oldSkyLayer.transform.position;
          CurrentLevelSkyLayer.transform.rotation = oldSkyLayer.transform.rotation;
          CurrentLevelSkyLayer.transform.localScale = oldSkyLayer.transform.localScale;
          oldSkyLayer.SetActive(false);
          Log.D(TAG, "Load SkyLayer object");
        } else
          Log.W(TAG, "Not found \"level.skyLayer\": SkyLayer object.");
      }
      else if (level.skyLayer == "SkyVoterx") {
        var oldSkyLayer = GameObject.Find("SkyVoterx");
        if (oldSkyLayer != null) {
          CurrentLevelSkyLayer = GameManager.Instance.InstancePrefab(SystemPackage.GetPrefabAsset("FinalSkyVoterx.prefab"), "SkyVoterx");
          CurrentLevelSkyLayer.transform.position = oldSkyLayer.transform.position;
          CurrentLevelSkyLayer.transform.rotation = oldSkyLayer.transform.rotation;
          CurrentLevelSkyLayer.transform.localScale = oldSkyLayer.transform.localScale;
          oldSkyLayer.SetActive(false);
          Log.D(TAG, "Load SkyVoterx object");
        } else
          Log.W(TAG, "Not found \"level.skyLayer\": SkyVoterx object.");
      } 
      else
        Log.D(TAG, "No SkyLayer");

      // Hide clouds when disabled in settings.
      var GameSettings = GameSettingsManager.GetSettings(GamePackageManager.SYSTEM_PACKAGE_NAME);
      if (CurrentLevelSkyLayer != null && !GameSettings.GetBool(SettingConstants.SettingsVideoCloud))
        CurrentLevelSkyLayer.SetActive(false);

      Log.D(TAG, "Load music");

      // Load custom music.
      //---------------------------
      var customMusicTheme = level.customMusicTheme;
      if (customMusicTheme != null) {

        var id = customMusicTheme.id;
        Log.D(TAG, $"Load customMusicTheme {id}");

        MusicManager.AddMusicTheme(id, new MusicManager.MusicThemeDataStorage {
          atmos = LoadCustomAudio(id, customMusicTheme.atmos),
          musics = LoadCustomAudio(id, customMusicTheme.musics),
          baseInterval = customMusicTheme.baseInterval,
          maxInterval = customMusicTheme.baseInterval,
          atmoInterval = customMusicTheme.baseInterval,
          atmoMaxInterval = customMusicTheme.baseInterval,
        });
      }
      // Set music.
      if (level.musicTheme > 0) {
        Log.D(TAG, "Set MusicTheme " + level.musicTheme);
        MusicManager.SetCurrentTheme(level.musicTheme);
      } 
      else {
        Log.D(TAG, "No MusicTheme");
        MusicManager.SetCurrentTheme(0);
      }

      UpdateLoadProgress(0.9f);
      yield return new WaitForEndOfFrame();

      Log.D(TAG, "Load others");

      // Load the first-level tutorial.
      //-----------------------------
      if (levelName == "level01")
        GameManager.Instance.InstancePrefab(SystemPackage.GetPrefabAsset("TutorialCore.prefab"), "GameTutorialCore");

      // Run custom last loading steps.
      //-----------------------------
      yield return StartCoroutine(CallLoadStep("last"));
      CallLevelCustomModEvent("finishLoad");

      EnvironmentController.Attach(CurrentLevelObject, CurrentLevelAsset, levelName, IsPreviewMode);

      Log.D(TAG, "Load finish");

      UpdateLoadProgress(1);
      yield return new WaitForSeconds(0.5f);

      // Final loading stage.
      //-----------------------------
      if (IsPreviewMode)
        GameMediator.Instance.DelayedNotifySingleEvent("CoreGamePreviewManagerInitAndStart", 0.1f,
          CurrentLevelJson.name,
          CurrentLevelJson.author,
          CurrentLevelJson.version
        );
      else
        GameMediator.Instance.DelayedNotifySingleEvent("CoreGamePlayManagerInitAndStart", 0.1f);

      yield return new WaitForSeconds(0.2f);

      // Hide the loading UI.
      GameUIManager.Instance.MaskBlackSet(true);
      LevelBuilderUI.gameObject.SetActive(false);
      IsLoading = false;

      yield break;
    }

    /// <summary>
    /// Replace a placeholder with a mechanism instance.
    /// </summary>
    /// <param name="objName">Placeholder name.</param>
    /// <param name="modulPrefab">Mechanism prefab.</param>
    /// <param name="rotationCorrecting">Rotation correction for the mechanism prefab.</param>
    /// <returns>The mechanism component, or null on error.</returns>
    public ModulBase ReplacePrefab(string objName, LevelBuilderModulRegStorage modulPrefab, LevelBuilderModulRotationCorrecting rotationCorrecting) {
      // Check for an existing mechanism with the same name.
      if (CurrentLevelModuls.ContainsKey(objName)) {
        Log.E(TAG, $"Find modul with the same name \"{objName}\"");
        return null;
      }
      var obj = GameObject.Find(objName);
      if (obj == null) { 
        Log.E(TAG, $"Not find modul placeholder \"{objName}\"");
        return null; 
      }

      // Hide the placeholder.
      obj.SetActive(false);
      // Clone mechanisms.
      var modul = GameManager.Instance.InstancePrefab(modulPrefab.basePrefab, gameObject.transform, "ModulInstance_" + objName);
      // Synchronize mechanism position.
      modul.transform.position = obj.transform.position;
      modul.transform.rotation = obj.transform.rotation;

      if (rotationCorrecting != null) {
        // Correct mechanism rotation.
        var eulerAngles = modul.transform.eulerAngles;
        var pos = modul.transform.position;
        if (Mathf.Abs(rotationCorrecting.y) > 0)
          modul.transform.RotateAround(pos, Vector3.up, rotationCorrecting.y);
        if (Mathf.Abs(rotationCorrecting.x) > 0)
          modul.transform.RotateAround(pos,  Vector3.forward, rotationCorrecting.x);
        if (Mathf.Abs(rotationCorrecting.z) > 0)
          modul.transform.RotateAround(pos,  Vector3.right, rotationCorrecting.z);
      }

      // Get the component.
      var modulClasses = modul.GetComponents<ModulBase>();
      if (modulClasses.Length == 0) {
        Log.E(TAG, $"Not find ModulBase class on modul \"{objName}\"");
        return null;
      }
      if (modulClasses.Length > 1) {
        Log.E(TAG, $"There a multiple ModulBase class on this modul \"{objName}\"");
        return null;
      }

      var modulClass = modulClasses[0];

      modulClass.IsPreviewMode = IsPreviewMode;
      CurrentLevelModuls[objName] = new LevelBuilderModulStorage() {
        go = modul,
        modul = modulClass
      };
      return modulClass;
    }

    private List<AudioClip> LoadCustomAudio(int id, List<string> orgArr) {
      var arr = new List<AudioClip>();
      foreach (var value in orgArr)
      {
        var audio = CurrentLevelAsset.GetAudioClipAsset(value);
        if (audio != null)
          arr.Add(audio);
        else {
          audio = GamePackageManager.Instance.GetAudioClipAsset(value);
          if (audio != null)
            arr.Add(audio);
          else
            Log.W(TAG, $"Not found custom audio resource in customMusicTheme.{name}, name : {value} , ignore this sound");
        }
      }
      return arr;
    }

    #endregion

    #region Loading status controls

    /// <summary>
    /// Whether loading is in progress.
    /// </summary>
    /// <value></value>
    public bool IsLoading { get; private set; } = false;
    /// <summary>
    /// Set loading progress percentage.
    /// </summary>
    /// <param name="precent">0%-100%</param>
    public void UpdateLoadProgress(float precent) {
      LevelBuilderUIProgress.value = precent;
    }
    /// <summary>
    /// Set loading error state.
    /// </summary>
    /// <param name="err">Whether loading failed.</param>
    /// <param name="statuaCode">Status code.</param>
    /// <param name="errMessage">Error message.</param>
    public void UpdateErrStatus(bool err, string statuaCode, string errMessage) {
      if (err) {
        IsLoading = false;

        Log.D(TAG, "Load level error {0} err:  {1}", statuaCode, errMessage);

        GameTimer.Delay(1.0f, () => {
          LevelBuilderCurrentError = $"Code: {statuaCode} \n {errMessage}";
          LevelBuilderUITextErrorContent.text = LevelBuilderCurrentError;
          LevelBuilderUIPanelFailed.gameObject.SetActive(true);
          GameSoundManager.Instance.PlayFastVoice("core.sounds:Misc_StartLevel.wav", GameSoundType.Normal);
        });
      } else {
        LevelBuilderUIPanelFailed.gameObject.SetActive(false);
      }
    }

    #endregion
    
    #region Mechanism registration

    /// <summary>
    /// Register a mechanism.
    /// </summary>
    /// <param name="name">Mechanism name.</param>
    /// <param name="basePrefab">Base mechanism prefab.</param>
    public void RegisterModul(string name, GameObject basePrefab) {
      if (RegisteredModuls.ContainsKey(name)) {
        GameErrorChecker.SetLastErrorAndLog(GameError.AlreadyRegistered, TAG, "Modul {0} already registered! ", name);
        return;
      }
      if (basePrefab == null) {
        GameErrorChecker.SetLastErrorAndLog(GameError.ParamNotProvide, TAG, "Failed to rgister modul {0}, basePrefab is null", name);
        return;
      }

      RegisteredModuls[name] = new LevelBuilderModulRegStorage() {
        name = name,
        basePrefab = basePrefab,
      };
    }
    /// <summary>
    /// Unregister a mechanism.
    /// </summary>
    /// <param name="name">Mechanism name.</param>
    public bool UnRegisterModul(string name) {
      return RegisteredModuls.Remove(name);
    }
    /// <summary>
    /// Get a registered mechanism, or null if absent.
    /// </summary>
    /// <param name="name">Mechanism name.</param>
    /// <returns></returns>
    public LevelBuilderModulRegStorage FindRegisterModul(string name) {
      if (RegisteredModuls.TryGetValue(name, out var v))
        return v;
      return null;
    }

    #endregion

    #region Custom loading step registration

    private Dictionary<string, LevelBuilderCustomLoadStep> RegisteredLoadSteps = new Dictionary<string, LevelBuilderCustomLoadStep>();

    /// <summary>
    /// Register a custom loading step.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <param name="type">Callback stage: pre, modul, last, or unload.</param>
    /// <param name="callback">Callback receives the levelBuilder instance.</param>
    public void RegisterLoadStep(string name, string type, MonoBehaviour callbackBehaviour, LevelBuilderCustomLoadStepCallback callback) {
      if (RegisteredLoadSteps.ContainsKey(name)) {
        GameErrorChecker.SetLastErrorAndLog(GameError.AlreadyRegistered, TAG, "LoadStep {0} already registered! ", name);
        return;
      }
      RegisteredLoadSteps[name] = new LevelBuilderCustomLoadStep() {
        type = type,
        callbackBehaviour = callbackBehaviour,
        callback = callback
      };
    }
    /// <summary>
    /// Unregister a custom loading step.
    /// </summary>
    /// <param name="name">Name.</param>
    public bool UnRegisterLoadStep(string name) {
      return RegisteredLoadSteps.Remove(name);
    }
    /// <summary>
    /// Run custom loading steps.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private IEnumerator CallLoadStep(string type) {
      foreach(var step in RegisteredLoadSteps) {
        if (step.Value.type == type)
          yield return step.Value.callbackBehaviour.StartCoroutine(
            step.Value.callback(this)
          );
      }
    }

    #endregion

    #region Automatic grouping

    /// <summary>
    /// Generate grouping information from the level root.
    /// </summary>
    /// <remark>
    /// This overwrites grouping information in the existing level JSON.
    /// </remark>
    /// <param name="level">Level json</param>
    /// <param name="transform">Level root transform.</param>
    private void DoLevelAutoGroup(LevelData level, Transform transform) {
      var childCount = transform.childCount;

      level.internalObjects = new LevelInternalObjectsData {
        PS_LevelStart = "",
        PE_LevelEnd = "",
        PR_ResetPoints = new Dictionary<string, string>(),
        PC_CheckPoints = new Dictionary<string, string>(),
      };
      level.sectors = new Dictionary<string, List<string>>();
      level.floors = new List<LevelGroupData>();
      level.groups = new List<LevelGroupData>();
      level.depthTestCubes = new List<string>();

      var groupsTemp = new Dictionary<string, List<string>>();

      for(int i = 1; i <= level.sectorCount; i++)
        level.sectors[$"{i}"] = new List<string>();

      for(int i = 0; i < childCount; i++) {
        var go = transform.GetChild(i).gameObject;
        var name = go.name;
        if (name == "PS_LevelStart") {
          // Starting flames.
          level.internalObjects.PS_LevelStart = "PS_LevelStart";
        } else if (name =="PE_LevelEnd") {
          // Ending balloon.
          level.internalObjects.PE_LevelEnd = "PE_LevelEnd";
        } else if (name.StartsWith("PR_ResetPoint:")) {
          // Spawn point.
          var sector = name.Substring(14);
          if (!level.internalObjects.PR_ResetPoints.ContainsKey(sector))
            level.internalObjects.PR_ResetPoints.Add(sector, name);
          else
            level.internalObjects.PR_ResetPoints[sector] = name;
        } else if (name.StartsWith("PC_CheckPoint:")) {
          // Checkpoint.
          var sector = name.Substring(14);
          if (!level.internalObjects.PC_CheckPoints.ContainsKey(sector))
            level.internalObjects.PC_CheckPoints.Add(sector, name);
          else
            level.internalObjects.PC_CheckPoints[sector] = name;
        } else if (name.StartsWith("S_")) {
          if (go.activeSelf) {
            // Static floor group.
            var floor_type = name.Substring(2);
            var c_names = new List<string>();
            var c_transform =  transform.GetChild(i);
            var c_childCount = c_transform.childCount;
            for(int j = 0; j < c_childCount; j++) {
              var goFloor = c_transform.GetChild(j).gameObject;
              if (goFloor.activeSelf) {
                goFloor.name = goFloor.name + floor_type + j.ToString();
                c_names.Add(goFloor.name);
              }
            }
            level.floors.Add(new LevelGroupData {
              name = "Phys_" + floor_type,
              objects = c_names
            });
          }
        } else if (name == "DepthTestCubes") {
          // Fall detection zone.
          var c_transform =  transform.GetChild(i);
          for (int j = 0; j < c_transform.childCount; j++) {
            var goObject = c_transform.GetChild(j).gameObject;
            goObject.name = goObject.name + "DepthTestCubes" + j;
            level.depthTestCubes.Add(goObject.name);
          }
        } else if (name.Contains(":")) {
          if (go.activeSelf) {
            var arr = name.Split(':');
            if (arr.Length > 2) {
              var nname = arr[0];
              var sector = arr[arr.Length - 1];
              if (nname.StartsWith("P_")) {
                // Mechanism.
                if (!groupsTemp.ContainsKey(nname))
                  groupsTemp.Add(nname, new List<string>());
                if (!level.sectors.ContainsKey(sector))
                  level.sectors.Add(sector, new List<string>());
                var gdata = groupsTemp[nname];
                var sdata = level.sectors[sector];
                gdata.Add(name);
                sdata.Add(name);
              }
            }
          }
        }
      }

      foreach (var item in groupsTemp)
      {
        level.groups.Add(new LevelGroupData {
          name = item.Key,
          objects = item.Value
        });
      }
    }

    #endregion

    public void CallLevelCustomModEvent(string type) {
      var level = CurrentLevelJson.level;
      if (level != null && !string.IsNullOrEmpty(level.customModEventName))
        GameMediator.Instance.DispatchGlobalEvent(level.customModEventName, type);
    }

    public delegate IEnumerator LevelBuilderCustomLoadStepCallback(LevelBuilder levelBuilder);

    private class LevelBuilderCustomLoadStep {
      public string type;
      public MonoBehaviour callbackBehaviour;
      public LevelBuilderCustomLoadStepCallback callback;
    }
    /// <summary>
    /// Mechanism registration data.
    /// </summary>
    public class LevelBuilderModulRegStorage {
      /// <summary>
      /// Mechanism name.
      /// </summary>
      public string name;
      /// <summary>
      /// Prefab instance.
      /// </summary>
      public GameObject basePrefab;
    }
    /// <summary>
    /// Mechanism instance data.
    /// </summary>
    public class LevelBuilderModulStorage {
      /// <summary>
      /// Mechanism GameObject instance.
      /// </summary>
      public GameObject go;
      /// <summary>
      /// Mechanism component instance.
      /// </summary>
      public ModulBase modul;
    }
    /// <summary>
    /// Placeholder rotation correction in Euler angles.
    /// </summary>
    [JsonObject]
    public class LevelBuilderModulRotationCorrecting {
      [JsonProperty]
      public float x;
      [JsonProperty]
      public float y;
      [JsonProperty]
      public float z;
    }
  }
}
