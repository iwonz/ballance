using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Ballance2.Game.GamePlay;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ballance2.Game.Remaster
{
  public sealed class RemasterRecords
  {
    public sealed class Scores
    {
      public Dictionary<string, List<HighscoreDataItem>> Data = new Dictionary<string, List<HighscoreDataItem>>();
      public List<string> Names = new List<string>(DefaultHighscoreDataGenerator.DefaultHighscoreLevelNamesData);
    }

    public Dictionary<string, bool> Unlocks { get; private set; }
    public Scores Current { get; private set; }
    private readonly string directory;
    private string ScorePath => Path.Combine(directory, "Highscores.json");

    public RemasterRecords(string persistentRoot)
    {
      directory = Path.Combine(persistentRoot, "Remaster");
      string path = Path.Combine(directory, "LevelPassStateData.json");
      Unlocks = File.Exists(path) ? JsonConvert.DeserializeObject<Dictionary<string, bool>>(File.ReadAllText(path))
        ?? throw new InvalidDataException("Invalid Reballance unlock data") : new Dictionary<string, bool>();
      Current = File.Exists(ScorePath) ? Read(ScorePath) : new Scores();
      if (File.Exists(ScorePath) || !Directory.Exists(directory)) return;

      var legacy = Directory.GetFiles(directory, "Highscores-r*-*.json")
        .Where(file => Regex.IsMatch(Path.GetFileName(file), @"^Highscores-r[1-9][0-9]*-[0-6]\.json$"))
        .OrderBy(file => file, StringComparer.Ordinal).ToArray();
      foreach (string file in legacy) {
        var scores = Read(file);
        foreach (string name in scores.Names.Concat(scores.Data.Keys))
          if (!Current.Names.Contains(name)) Current.Names.Add(name);
        foreach (var entry in scores.Data) {
          if (!Current.Data.TryGetValue(entry.Key, out var items))
            Current.Data.Add(entry.Key, items = new List<HighscoreDataItem>());
          items.AddRange(entry.Value);
        }
      }
      foreach (string level in Current.Data.Keys.ToArray())
        Current.Data[level] = Current.Data[level].GroupBy(item => new { item.name, item.score, item.date })
          .Select(group => group.First()).OrderByDescending(item => item.score).ToList();
      // The shared file is the migration marker; preserve every legacy source for rollback.
      if (legacy.Length > 0) Save();
    }

    private static Scores Read(string path)
    {
      var obj = JObject.Parse(File.ReadAllText(path));
      var scores = new Scores {
        Data = JsonConvert.DeserializeObject<Dictionary<string, List<HighscoreDataItem>>>(obj.Value<string>("data")
          ?? throw new InvalidDataException("Missing Reballance scores")),
        Names = JsonConvert.DeserializeObject<List<string>>(obj.Value<string>("names")
          ?? throw new InvalidDataException("Missing Reballance level names"))
      };
      if (scores.Data == null || scores.Names == null || scores.Names.Any(string.IsNullOrEmpty) ||
          scores.Data.Any(entry => string.IsNullOrEmpty(entry.Key) || entry.Value == null ||
            entry.Value.Any(item => item == null || item.name == null || item.date == null)))
        throw new InvalidDataException("Invalid Reballance scores: " + path);
      return scores;
    }

    public void Save()
    {
      Directory.CreateDirectory(directory);
      var obj = new JObject {
        ["data"] = JsonConvert.SerializeObject(Current.Data),
        ["names"] = JsonConvert.SerializeObject(Current.Names)
      };
      Write(ScorePath, obj.ToString());
      Write(Path.Combine(directory, "LevelPassStateData.json"), JsonConvert.SerializeObject(Unlocks));
    }

    private static void Write(string path, string text)
    {
      string temporary = path + ".tmp";
      File.WriteAllText(temporary, text);
      if (File.Exists(path)) File.Replace(temporary, path, null);
      else File.Move(temporary, path);
    }
  }
}
