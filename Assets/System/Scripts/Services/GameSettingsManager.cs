using System.Collections.Generic;
using System.Text;
using Ballance2.Game;
using Ballance2.Utils;
using UnityEngine;

/*
* Copyright (c) 2020  mengyu
* 
* Module:
* GameSettingsManager.cs
* 
* Purpose:
* Manage settings across the game.
* 
* Get an actuator through GameSettingsManager.GetSettings("com.your.packagename").
* Use RegisterSettingsUpdateCallback to subscribe to setting changes.
* UI notifies changes through NotifySettingsUpdate.
* 
* Author:
* mengyu
*/

namespace Ballance2.Services
{
  /// <summary>
  /// System settings operation callback.
  /// </summary>
  
  
  public delegate bool GameSettingsCallback(string groupName, int action);

  /// <summary>
  /// System settings manager.
  /// </summary>
  public static class GameSettingsManager
  {
    private static Dictionary<string, GameSettingsActuator> settingsActuators = null;

    /// <summary>
    /// Get a settings actuator.
    /// </summary>
    /// <param name="packageName">Package identifier used by the actuator.</param>
    /// <returns>Settings actuator instance.</returns>
    public static GameSettingsActuator GetSettings(string packageName, GameSettingsActuator customActuator = null)
    {
      GameSettingsActuator gameSettingsActuator = null;
      if (!settingsActuators.TryGetValue(packageName, out gameSettingsActuator))
      {
         if (customActuator != null) {
          settingsActuators.Add(packageName, gameSettingsActuator);
          return customActuator;
        }
        if (customActuator == null && packageName == GamePackageManager.SYSTEM_PACKAGE_NAME)
          throw new System.Exception("Settings accessed before initialization.");
        gameSettingsActuator = new GameSettingsActuator(packageName);
        settingsActuators.Add(packageName, gameSettingsActuator);
      }
      return gameSettingsActuator;
    }
    /// <summary>
    /// Restore default settings.
    /// </summary>
    public static void ResetDefaultSettings()
    {
      PlayerPrefs.DeleteAll();

      foreach (var actuator in settingsActuators)
        actuator.Value.NotifyAll();
    }

    internal static void ListActuators()
    {
      foreach (var i in settingsActuators)
        Log.V("GameSettingsManager", string.Format("{0} => {1}", i.Key, i.Value.ListCallbacks()));
    }
    internal static void Init()
    {
      if (settingsActuators == null)
        settingsActuators = new Dictionary<string, GameSettingsActuator>();
      if (!settingsActuators.ContainsKey(GamePackageManager.SYSTEM_PACKAGE_NAME))
        settingsActuators.Add(GamePackageManager.SYSTEM_PACKAGE_NAME, new GameCoreSettingsActuator());
    }
    internal static void Destroy()
    {
      if(settingsActuators != null) {
        foreach (var key in settingsActuators.Keys)
          settingsActuators[key].Destroy();
        settingsActuators.Clear();
        settingsActuators = null;
      }
    }
  }

  /// <summary>
  /// Actuator managing individual setting entries.
  /// </summary>
  public class GameSettingsActuator
  {
    /// <summary>
    /// Update operation.
    /// </summary>
    public const int ACTION_UPDATE = 1;
    /// <summary>
    /// Load operation.
    /// </summary>
    public const int ACTION_LOAD = 2;

    private string basePackName = "unknow";

    /// <summary>
    /// Create a settings actuator.
    /// </summary>
    /// <param name="packageName">Package identifier owning these settings.</param>
    public GameSettingsActuator(string packageName)
    {
      basePackName = packageName;
    }
    /// <summary>
    /// Set an integer setting.
    /// </summary>
    /// <param name="key">Setting key.</param>
    /// <param name="value">Setting value.</param>
    public virtual void SetInt(string key, int value)
    {
      PlayerPrefs.SetInt(basePackName + "." + key, value);
    }
    /// <summary>
    /// Get an integer setting.
    /// </summary>
    /// <param name="key">Setting key.</param>
    /// <param name="defaultValue">Default returned when no setting exists.</param>
    /// <returns></returns>
    public virtual int GetInt(string key, int defaultValue)
    {
      return PlayerPrefs.GetInt(basePackName + "." + key, defaultValue);
    }
    /// <summary>
    /// Get a KeyCode setting.
    /// </summary>
    /// <param name="key">Setting key.</param>
    /// <param name="defaultValue">Default returned when no setting exists.</param>
    /// <returns></returns>
    public virtual KeyCode GetKeyCode(string key, KeyCode defaultValue)
    {
      return (KeyCode)PlayerPrefs.GetInt(basePackName + "." + key, (int)defaultValue);
    }
    /// <summary>
    /// Set a string setting.
    /// </summary>
    /// <param name="key">Setting key.</param>
    /// <param name="value">Setting value.</param>
    public virtual void SetString(string key, string value)
    {
      PlayerPrefs.SetString(basePackName + "." + key, value);
    }
    /// <summary>
    /// Get a string setting.
    /// </summary>
    /// <param name="key">Setting key.</param>
    /// <param name="defaultValue">Default returned when no setting exists.</param>
    /// <returns></returns>
    public virtual string GetString(string key, string defaultValue)
    {
      return PlayerPrefs.GetString(basePackName + "." + key, defaultValue);
    }
    /// <summary>
    /// Set a floating-point setting.
    /// </summary>
    /// <param name="key">Setting key.</param>
    /// <param name="value">Setting value.</param>
    public virtual void SetFloat(string key, float value)
    {
      PlayerPrefs.SetFloat(basePackName + "." + key, value);
    }
    /// <summary>
    /// Get a floating-point setting.
    /// </summary>
    /// <param name="key">Setting key.</param>
    /// <param name="defaultValue">Default returned when no setting exists.</param>
    /// <returns></returns>
    public virtual float GetFloat(string key, float defaultValue)
    {
      return PlayerPrefs.GetFloat(basePackName + "." + key, defaultValue);
    }
    /// <summary>
    /// Set a boolean setting.
    /// </summary>
    /// <param name="key">Setting key.</param>
    /// <param name="value">Setting value.</param>
    public virtual void SetBool(string key, bool value)
    {
      PlayerPrefs.SetString(basePackName + "." + key, value.ToString());
    }
    /// <summary>
    /// Get a boolean setting.
    /// </summary>
    /// <param name="key">Setting key.</param>
    /// <param name="defaultValue">Default returned when no setting exists.</param>
    /// <returns></returns>
    public virtual bool GetBool(string key, bool defaultValue = false)
    {
      return bool.Parse(PlayerPrefs.GetString(basePackName + "." + key, defaultValue.ToString()));
    }

    public virtual string GetString(string key)
    {
      return PlayerPrefs.GetString(basePackName + "." + key, "");
    }
    public virtual float GetFloat(string key)
    {
      return PlayerPrefs.GetFloat(basePackName + "." + key, 0);
    }    
    public virtual bool GetBool(string key)
    {
      return bool.Parse(PlayerPrefs.GetString(basePackName + "." + key, "False"));
    }
    public virtual int GetInt(string key)
    {
      return PlayerPrefs.GetInt(basePackName + "." + key, 0);
    }
    public virtual KeyCode GetKeyCode(string key)
    {
      return (KeyCode)GetInt(key);
    }

    private static int settingUpdateCallbackID = 0;
    private List<SettingUpdateCallbackData> settingUpdateCallbacks = new List<SettingUpdateCallbackData>();
    private struct SettingUpdateCallbackData
    {
      public string groupName;
      public GameSettingsCallback callback;
      public int id;

      public SettingUpdateCallbackData(string groupName, GameSettingsCallback callback)
      {
        this.id = settingUpdateCallbackID++;
        this.groupName = groupName;
        this.callback = callback;
      }
    }

    internal void NotifyAll()
    {
      foreach (var v in settingUpdateCallbacks)
        v.callback(v.groupName, ACTION_UPDATE);
    }
    internal string ListCallbacks()
    {
      StringBuilder sb = new StringBuilder("Callbacks: ");
      sb.Append(settingUpdateCallbacks.Count);
      sb.Append(" => ");
      foreach (var v in settingUpdateCallbacks)
      {
        sb.Append(",");
        sb.Append(v.groupName);
      }
      return sb.ToString();
    }
    internal void Destroy()
    {
      settingUpdateCallbacks.Clear();
    }

    /// <summary>
    /// Notify a settings group to load its values.
    /// </summary>
    /// <param name="groupName">Group name; * selects all groups.</param>
    public void RequireSettingsLoad(string groupName)
    {
      if (groupName == "*")
      {
        foreach (var d in settingUpdateCallbacks)
          if (d.callback(d.groupName, ACTION_LOAD)) break;
      }
      else foreach (var d in settingUpdateCallbacks)
          if (d.groupName == groupName)
            if (d.callback(groupName, ACTION_LOAD)) break;
    }
    /// <summary>
    /// Notify a settings group of an update.
    /// </summary>
    /// <param name="groupName">Group name; * selects all groups.</param>
    public void NotifySettingsUpdate(string groupName)
    {
      if (groupName == "*")
      {
        foreach (var d in settingUpdateCallbacks)
          if (d.callback(d.groupName, ACTION_UPDATE)) break;
      }
      else foreach (var d in settingUpdateCallbacks)
          if (d.groupName == groupName)
            d.callback(groupName, ACTION_UPDATE);
    }
    /// <summary>
    /// Register a settings group update callback.
    /// </summary>
    /// <param name="groupName">Group name.</param>
    /// <param name="handler">Callback.</param>
    public int RegisterSettingsUpdateCallback(string groupName, GameSettingsCallback callback)
    {
      var v = new SettingUpdateCallbackData(groupName, callback);
      settingUpdateCallbacks.Add(v);
      return v.id;
    }
    /// <summary>
    /// Unregister a settings group update callback.
    /// </summary>
    /// <param name="id">Callback ID.</param>
    public void UnRegisterSettingsUpdateCallback(int id)
    {
      for (var i = 0; i < settingUpdateCallbacks.Count; i++)
      {
        if (settingUpdateCallbacks[i].id == id)
        {
          settingUpdateCallbacks.RemoveAt(i);
          break;
        }
      }
    }
  }
}
