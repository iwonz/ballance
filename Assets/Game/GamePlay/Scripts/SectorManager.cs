using System.Collections;
using System.Collections.Generic;
using Ballance2.Base;
using Ballance2.Game.GamePlay.Moduls;
using Ballance2.Menu;
using Ballance2.Services;
using Ballance2.Utils;
using UnityEngine;

namespace Ballance2.Game.GamePlay
{
  /// <summary>
  /// Manage mechanism state for each level sector.
  /// </summary>
  public class SectorManager : MonoBehaviour 
  {
    private const string TAG = "SectorManager";

    /// <summary>
    /// Spawn point data.
    /// </summary>
    public class RestPointsDataStorage {
      /// <summary>
      /// Spawn point placeholder object.
      /// </summary>
      public GameObject point;
      /// <summary>
      /// Flame mechanism: PS_FourFlames or PC_TwoFlames.
      /// </summary>
      public ModulBase flame;
    }
    /// <summary>
    /// Sector data.
    /// </summary>
    public class SectorDataStorage {
      /// <summary>
      /// All mechanism instances in the current sector.
      /// </summary>
      public List<ModulBase> moduls = new List<ModulBase>();
    }
    /// <summary>
    /// Mechanism data.
    /// </summary>
    public class ModulDataStorage {
      /// <summary>
      /// Mechanism instance.
      /// </summary>
      public ModulBase modul;
      /// <summary>
      /// Original placeholder.
      /// </summary>
      public GameObject go;
    }

    public const int MAX_SECTOR_COUNT = 32;

    public int CurrentLevelSectorCount = 0;
    public int CurrentLevelModulCount = 0;
    public SectorDataStorage[] CurrentLevelSectors = new SectorDataStorage[MAX_SECTOR_COUNT + 1];
    public Dictionary<string, LevelBuilder.LevelBuilder.LevelBuilderModulStorage>.ValueCollection CurrentLevelModuls  
      => LevelBuilder.LevelBuilder.Instance.CurrentLevelModuls.Values;
    public RestPointsDataStorage[] CurrentLevelRestPoints = new RestPointsDataStorage[MAX_SECTOR_COUNT + 1];
    public PE_Balloon CurrentLevelEndBalloon = null;

    private void Start() {
      InitCommand();
      InitEvents();
    }
    private void OnDestroy() {
      DestroyCommand();
      DestroyEvent();
    }
      
    #region Events

    /// <summary>
    /// Sector ended.
    /// </summary>
    [HideInInspector]
    public GameEventEmitterStorage EventSectorDeactive;
    /// <summary>
    /// Sector changed.
    /// </summary>
    [HideInInspector]
    public GameEventEmitterStorage EventSectorChanged;
    /// <summary>
    /// Sector activated.
    /// </summary>
    [HideInInspector]
    public GameEventEmitterStorage EventSectorActive;
    /// <summary>
    /// All sectors reset.
    /// </summary>
    [HideInInspector]
    public GameEventEmitterStorage EventResetAllSector;

    private void InitEvents() {
      var events = GameMediator.Instance.RegisterEventEmitter("SectorManager");
      this.EventSectorDeactive = events.RegisterEvent("SectorDeactive");
      this.EventSectorChanged = events.RegisterEvent("SectorChanged");
      this.EventSectorActive = events.RegisterEvent("SectorActive");
      this.EventResetAllSector = events.RegisterEvent("ResetAllSector");
    }
    private void DestroyEvent() {
      GameMediator.Instance.UnRegisterEventEmitter("SectorManager");
    }

    #endregion

    #region Commands

    private int _CommandId = 0;
    private void InitCommand() {
      this._CommandId = GameManager.Instance.GameDebugCommandServer.RegisterCommand("sector", (eyword, fullCmd, argsCount, args) => {
        var type = args[0];
        if (type == "next")
          this.NextSector();
        else if (type == "set") {
          var o = DebugUtils.CheckIntDebugParam(1, args, out var n, true, 1);
          if (!o) return false;
          this.SetCurrentSector(n);
        }
        else if (type == "reset")
          this.ResetCurrentSector(true);
        else if (type == "reset-all")
          this.ResetAllSector(true);
        return true;
      }, 1, "sector <next/set/reset/reset-all> Sector manager commands" +
          "  next                  ▶ Enter the next sector" +
          "  set <sector:number>   ▶ Set the active sector" +
          "  reset                 ▶ Reset mechanisms in the current sector" +
          "  reset-all             ▶ Reset all sectors"
      );
    }
    private void DestroyCommand() {
      GameManager.Instance.GameDebugCommandServer.UnRegisterCommand(this._CommandId);
    }
  
    #endregion

    internal void DoInitAllModuls() 
    {
      this.CurrentLevelModulCount = 0;
      // Ask each mechanism to back up its state after initial loading.
      foreach(var value in CurrentLevelModuls) {
        if (value != null) {
          value.modul.Backup();
          value.modul.Deactive();
          CurrentLevelModulCount++;
        }
      }
    }
    internal void DoUnInitAllModuls() 
    {
      DeactiveAllModuls(true, false, false);
    }
    internal void ClearAll() 
    {
      this.CurrentLevelSectorCount = 0;

      for (int i = 0; i < CurrentLevelRestPoints.Length; i++)
        CurrentLevelRestPoints[i] = null;
      for (int i = 0; i < CurrentLevelSectors.Length; i++)
        CurrentLevelSectors[i] = null;
      this.CurrentLevelEndBalloon = null;
    }
    internal void ActiveAllModulsForPreview() 
    {
      // Notify each mechanism of unloading.
      foreach (var item in CurrentLevelModuls)
        if (item != null)
          item.modul.ActiveForPreview();
    }
    
    private void DeactiveAllModuls(bool afterUnload, bool afterActive, bool reset) {
      var preview = LevelBuilder.LevelBuilder.Instance.IsPreviewMode;
      // Notify each mechanism of unloading.
      foreach (var item in CurrentLevelModuls)
      {
        if (item == null)
          continue;
        if (preview)
          item.modul.DeactiveForPreview();
        else
          item.modul.Deactive();

        if (afterUnload)
          item.modul.UnLoad();
        if (afterActive)
          item.modul.Active();
        if (reset)
          item.modul.Reset(ModulBaseResetType.LevelRestart);
      }
    }

    /// <summary>
    /// Enter the next sector.
    /// </summary>
    public void NextSector() 
    {
      if (GamePlayManager.Instance.CurrentSector < this.CurrentLevelSectorCount)
        this.SetCurrentSector(GamePlayManager.Instance.CurrentSector + 1);
    }
    /// <summary>
    /// Set the active sector.
    /// </summary>
    /// <param name="sector"></param>
    public void SetCurrentSector(int sector) 
    {
      var oldSector = GamePlayManager.Instance.CurrentSector;
      if (oldSector != sector) {
        // Deactivate all mechanisms in the previous sector.
        if (oldSector > 0) {
          var s = this.CurrentLevelSectors[oldSector];
          foreach (var value in s.moduls)
            value.Deactive();

          // Set flame state.
          var flame = this.CurrentLevelRestPoints[oldSector].flame;
          if (flame != null) {
            if (flame is PC_TwoFlames)
              ((PC_TwoFlames)flame).CheckPointActived = true;
            flame.Deactive();
          }
          else {
            Log.D(TAG, "No flame found for sector " + oldSector);
          }

          this.EventSectorDeactive.Emit(oldSector);
        }

        if (sector > 0) { 
          GamePlayManager.Instance.CurrentSector = sector;
          this.ActiveCurrentSector(true);
        }

        this.EventSectorChanged.Emit(sector, oldSector);
      }
    }
    /// <summary>
    /// Activate mechanisms in the current sector.
    /// </summary>
    /// <param name="playCheckPointSound">Play the checkpoint sound.</param>
    public void ActiveCurrentSector(bool playCheckPointSound) 
    {
      var sector = GamePlayManager.Instance.CurrentSector;
      var nowSector = this.CurrentLevelRestPoints[sector];

      // Set flame state.

      if (nowSector.flame != null)
        nowSector.flame.Active();
      else
        Log.D(TAG, "No flame found for sector " + sector);
      if (sector < this.CurrentLevelSectorCount) {
        nowSector = this.CurrentLevelRestPoints[sector + 1];
        // Flames for the next sector.
        var flameNext = nowSector.flame;
        if (flameNext != null && flameNext is PC_TwoFlames)
          ((PC_TwoFlames)flameNext).InternalActive();
      }

      // Play music.
      if (playCheckPointSound && sector > 1)
        GameSoundManager.Instance.PlayFastVoice("core.sounds:Misc_Checkpoint.wav", GameSoundType.Normal);

      // Activate the balloon in the final sector.
      if (this.CurrentLevelEndBalloon != null) {
        if (sector == this.CurrentLevelSectorCount)
          this.CurrentLevelEndBalloon.Active();
        else
          this.CurrentLevelEndBalloon.Deactive();
      }
      else
        Log.W(TAG, "No found CurrentLevelEndBalloon !");

      Log.D(TAG, "Active Sector " + sector);

      // Activate mechanisms in the current sector.
      StartCoroutine(ActiveSectorModuls(sector));

      this.EventSectorActive.Emit(sector, playCheckPointSound);
    }

    private IEnumerator ActiveSectorModuls(int sector) {
      var s = this.CurrentLevelSectors[sector];
      if (s == null && sector != 0) {
        Log.E(TAG, $"Sector {sector} not found");
        GamePlayManager.Instance.CurrentSector = 0;
        yield break;
      }
      var count = 0;
      for (int i = 0; i < s.moduls.Count; i++)
      {
        s.moduls[i].Active();

        // Delay slightly to avoid spawning too many mechanisms at once.
        count++;
        if (count >= 2) {
          count = 0;
          yield return new WaitForSeconds(0.05f);
        }
      }

      if (GameManager.DebugMode) { 
        GamePlayUIControl.Instance.DebugStatValues["Sector"].Value = sector + "/" + this.CurrentLevelSectorCount;
        GamePlayUIControl.Instance.DebugStatValues["Moduls"].Value = s.moduls.Count + "/" + this.CurrentLevelModulCount;
      }
    }

    /// <summary>
    /// Deactivate mechanisms in the current sector.
    /// </summary>
    public void DeactiveCurrentSector()  
    {
      var sector = GamePlayManager.Instance.CurrentSector;
      if (sector > 0) {
        var s = this.CurrentLevelSectors[sector];
        foreach (var value in s.moduls) {
          value.Deactive();
          value.Reset(ModulBaseResetType.SectorRestart);
        }
      }

      // Debug information.
      if (GameManager.DebugMode) { 
        GamePlayUIControl.Instance.DebugStatValues["Sector"].Value = sector + "(Deactive)/" + this.CurrentLevelSectorCount;
        GamePlayUIControl.Instance.DebugStatValues["Moduls"].Value = "0";
      }

      Log.D(TAG, $"Deactive current sector {sector}");

      this.EventSectorDeactive.Emit(sector);
    }
    /// <summary>
    /// Reset mechanisms in the current sector.
    /// </summary>
    /// <param name="active">Reactivate mechanisms after resetting.</param>
    public void ResetCurrentSector(bool active)  
    {
      this.DeactiveCurrentSector();
      if (active) {
        this.ActiveCurrentSector(false);
      }
    }
    /// <summary>
    /// Reset all mechanisms.
    /// </summary>
    /// <param name="active">Reactivate mechanisms after resetting.</param>
    public void ResetAllSector(bool active) 
    {
      // Notify each mechanism of unloading.
      DeactiveAllModuls(false, active, true);

      // Reset the balloon.
      this.CurrentLevelEndBalloon.Reset(ModulBaseResetType.LevelRestart);

      Log.D(TAG, "Reset all sector");

      this.EventSectorDeactive.Emit(active);
    }
  }
}