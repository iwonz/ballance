using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ballance2.Game.Remaster
{
  public static class RemasterPreferences
  {
    public const string Prefix = "ballance.remaster.v1.";
    public const string IndexKey = Prefix + "settings-index";
    [Serializable] private class KeyIndex { public List<string> keys = new List<string>(); }
#if UNITY_EDITOR
    private static string editorTestPrefix;
    private static string StoragePrefix => editorTestPrefix ?? Prefix;
#else
    private const string StoragePrefix = Prefix;
#endif
    private static string StorageIndex => StoragePrefix + "settings-index";

    public static int GetInt(string key, int fallback = 0) => PlayerPrefs.GetInt(StoragePrefix + key, fallback);
    public static float GetFloat(string key, float fallback = 0) => PlayerPrefs.GetFloat(StoragePrefix + key, fallback);
    public static string GetString(string key, string fallback = "") => PlayerPrefs.GetString(StoragePrefix + key, fallback);
    public static void SetInt(string key, int value) { PlayerPrefs.SetInt(StoragePrefix + key, value); Remember(key); }
    public static void SetFloat(string key, float value) { PlayerPrefs.SetFloat(StoragePrefix + key, value); Remember(key); }
    public static void SetString(string key, string value) { PlayerPrefs.SetString(StoragePrefix + key, value); Remember(key); }
    public static void Save() => PlayerPrefs.Save();

    private static KeyIndex ReadIndex()
    {
      try {
        var index = JsonUtility.FromJson<KeyIndex>(PlayerPrefs.GetString(StorageIndex, "{}"));
        return index != null && index.keys != null ? index : new KeyIndex();
      } catch (ArgumentException) { return new KeyIndex(); }
    }

    private static void Remember(string key)
    {
      var index = ReadIndex();
      string stored = StoragePrefix + key;
      if (!index.keys.Contains(stored)) {
        index.keys.Add(stored);
        PlayerPrefs.SetString(StorageIndex, JsonUtility.ToJson(index));
      }
      Save();
    }

    public static void Reset()
    {
      foreach (string key in ReadIndex().keys)
        if (key != null && key.StartsWith(StoragePrefix, StringComparison.Ordinal)) PlayerPrefs.DeleteKey(key);
      PlayerPrefs.DeleteKey(StorageIndex);
      Save();
    }

    public static EnvironmentPreset Preset
    {
      get {
        var value = (EnvironmentPreset)GetInt("environment.preset");
        return Enum.IsDefined(typeof(EnvironmentPreset), value) ? value : EnvironmentPreset.Standard;
      }
      set {
        if (!Enum.IsDefined(typeof(EnvironmentPreset), value)) throw new ArgumentOutOfRangeException(nameof(value));
        SetInt("environment.preset", (int)value);
      }
    }
  }
}
