using Ballance2.Game.Remaster;
using System;
using System.Collections.Generic;
using System.IO;
using Ballance2.Base;
using Ballance2.Package;
using Ballance2.Services;
using Ballance2.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Ballance2.Game.GamePlay
{
  public class HighscoreManager : GameSingletonBehavior<HighscoreManager>
  {
    private const string TAG = "HighscoreManager";
    private int HighscoreManagerCommand = 0;

    private RemasterRecords records;
    private RemasterRecords.Scores Current => records.Current;
    private Dictionary<string, List<HighscoreDataItem>> Data { get => Current.Data; set => Current.Data = value; }
    private List<string> LevelNames { get => Current.Names; set => Current.Names = value; }
    private Dictionary<string, bool> LevelPassStateData => records.Unlocks;

    private void Start() => InitCommand();
    internal void Load() => records = new RemasterRecords(Ballance2.Res.GamePathManager.PersistentDataPath);
    internal void Save() => records.Save();

    private static GameObject HighscoreManagerGameObject = null;

    internal static void Init() { 
      HighscoreManagerGameObject = CloneUtils.CreateEmptyObject("GameHighscoreManager");
      HighscoreManagerGameObject.AddComponent<HighscoreManager>();
    }
    internal static void Destroy() {
      if (HighscoreManagerGameObject != null) 
        UnityEngine.Object.Destroy(HighscoreManagerGameObject);
      HighscoreManagerGameObject = null;
    }

    // Add commands.
    private void InitCommand() {
      if (HighscoreManagerCommand == 0) {
        HighscoreManagerCommand = GameManager.Instance.GameDebugCommandServer.RegisterCommand("highscore", (keyword, fullCmd, argsCount, args) => {
          var type = args[0];
          if (type == "clear") {
            Data = new Dictionary<string, List<HighscoreDataItem>>();
            LevelNames = new List<string>(DefaultHighscoreDataGenerator.DefaultHighscoreLevelNamesData);
            LevelPassStateData.Clear();
            Log.D(TAG, "Reset to default");
            return true;
          }
          else if (type == "open-all") {
            Log.D(TAG, "Open level01-level12");
            UnLockAllInternalLevel();
            return true;
          }
          else if (type == "load") { 
            Load();
            Log.D(TAG, " Load manually");
            return true;
          }
          else if (type == "save") { 
            Save();
            Log.D(TAG, "Save manually");
            return true;
          }
          else {
            Log.E(TAG, "Unknow type " + type);
            return false;
          }
        }, 1, "highscore <clear/open-all/load/save>\n"
           + "  clear    ▶ Permanently delete all locally saved high scores (cannot be undone!)\n"
           + "  open-all ▶ Unlock all built-in levels\n"
           + "  load     ▶ Load high scores manually\n"
           + "  save     ▶ Save high scores manually");
      }
    } 

    /// <summary>
    /// Unlock all built-in levels.
    /// </summary>
    public void UnLockAllInternalLevel() {
      foreach(var key in DefaultHighscoreDataGenerator.DefaultHighscoreLevelNamesData)
        LevelPassStateData[key] = true;
    }
    /// <summary>
    /// Get the score list for a level.
    /// </summary>
    /// <param name="levelName">Level name.</param>
    /// <returns>The level's score list, or null if none exists.</returns>
    public List<HighscoreDataItem> GetData(string levelName) {
      if (Data.TryGetValue(levelName, out var list))
        return list;
      return null;
    }
    /// <summary>
    /// Get names of all levels stored in the score manager.
    /// </summary>
    /// <param name="levelName">Level name.</param>
    /// <returns>Array of all level names.</returns>
    public List<string> GetLevelNames() {
      return LevelNames;
    }
    /// <summary>
    /// Add a player's score for a level.
    /// </summary>
    /// <param name="levelName">Level name.</param>
    /// <param name="userName">Player name.</param>
    /// <param name="score">Score.</param>
    public void AddItem(string levelName, string userName, int score) {
      AddLevelPassState(levelName);
      // Get or create data.
      var list = GetData(levelName);
      if (list == null) {
        list = new List<HighscoreDataItem>();
        Data[levelName] = list;
      }
      
      // Insert data.
      HighscoreDataItem item = new HighscoreDataItem(userName, score, DateTime.Now.ToString("yyyy/M/d"));

      for (var i = 0; i < list.Count; i++)
      {
        // Insert at the appropriate position.
        if (score > list[i].score) {
          list.Insert(i, item);
          records.Save();
          return;
        }
      }
      // Append at the end.
      list.Add(item);
      records.Save();
    }
    /// <summary>
    /// Check whether the score is a new high score for the level.
    /// </summary>
    /// <param name="levelName">Level name.</param>
    /// <param name="score">Score.</param>
    /// <returns>Whether this is a new high score.</returns>
    public bool CheckLevelHighScore(string levelName, int score) {
      var list = GetData(levelName);
      if (list == null)
        return true;
      foreach (var data in list) {
        if (data.score > score)
          return false;
      }
      return true;
    }
    /// <summary>
    /// Mark a level as completed.
    /// </summary>
    /// <param name="levelName">Level name.</param>
    public void AddLevelPassState(string levelName) {
      if (string.IsNullOrEmpty(levelName)) return;
      LevelPassStateData[levelName] = true;
      records.Save();
    }
    /// <summary>
    /// Check whether a level has been completed.
    /// </summary>
    /// <param name="levelName">Level name.</param>
    /// <returns></returns>
    public bool CheckLevelPassState(string levelName) {
      if (LevelPassStateData.TryGetValue(levelName, out var pass))
        return pass;
      return false;
    }
    /// <summary>
    /// Add default scores for a level.
    /// </summary>
    /// <param name="levelName">Level name.</param>
    /// <param name="data">Default data; null uses DefaultHighscoreDataGenerator.DefaultHightScoreLev01To11Data.</param>
    public void TryAddDefaultLevelHighScore(string levelName, List<HighscoreDataItem> data) {
      if (!Data.ContainsKey(levelName)) {
        Data[levelName] = data != null ? data : DefaultHighscoreDataGenerator.DefaultHightScoreLev01To11Data;
        if (!LevelNames.Contains(levelName)) LevelNames.Add(levelName);
      }
    }
  }
}