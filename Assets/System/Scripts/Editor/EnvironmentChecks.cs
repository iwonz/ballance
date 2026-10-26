using System;
using System.IO;
using Ballance2.Game.Remaster;
using Ballance2.Game.GamePlay;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System.Runtime.InteropServices;
using BallancePhysics.Api;
using BallancePhysics.Wapper;
using UnityEditor;

namespace Ballance2.Editor
{
  public static class EnvironmentChecks
  {
    private static void Require(bool ok, string message)
    {
      if (!ok) throw new InvalidOperationException("Environment check failed: " + message);
    }

    public static void Run()
    {
      var profiles = LevelEnvironmentProfile.Parse(File.ReadAllText("Assets/Game/Remaster/Resources/Remaster/EnvironmentProfiles.json"));
      Require(profiles.Count == 13, "thirteen profiles");
      Require(Marshal.SizeOf<SurfaceState>() == 20 && Marshal.SizeOf<SurfaceRegion>() == 40 && Marshal.SizeOf<SurfaceContact>() == 56,
        "native surface layout matches ABI 2304");
      foreach (var entry in profiles) {
        var level = JObject.Parse(File.ReadAllText("Assets/Game/Levels/Level" + entry.Key.Substring(5) + "/Level.json"));
        Require(entry.Value.sectors.Length == level["level"]["sectorCount"].Value<int>(), "profile covers every authored sector");
        var floors = new HashSet<string>();
        foreach (var floor in level["level"]["floors"])
          foreach (var name in floor["objects"]) floors.Add(name.Value<string>());
        foreach (string name in entry.Value.coverings) Require(floors.Contains(name), "covering references an existing physical floor");
        var mechanisms = new HashSet<string>();
        foreach (var group in level["level"]["groups"])
          foreach (var name in group["objects"]) mechanisms.Add(name.Value<string>());
        foreach (string name in entry.Value.mechanisms) Require(mechanisms.Contains(name), "covering references an existing mechanism instance");
      }
      string development = PlayerSettings.bundleVersion.Contains("-dev.") ? "-dev" : "";
      Require(PlayerBuild.DefaultOutput(BuildTarget.StandaloneOSX) == "Builds/macOS/Reballance" + development + ".app", "native bundle uses the product and development name");
      Require(PlayerBuild.DefaultOutput(BuildTarget.StandaloneWindows64) == "Builds/Windows/Reballance.exe", "Windows executable uses the product name");
      Require(PlayerBuild.DefaultOutput(BuildTarget.StandaloneLinux64) == "Builds/Linux/Reballance.x86_64", "Linux executable uses the product name");
      Require(PlayerBuild.DefaultOutput(BuildTarget.Android) == "Builds/Android/Reballance.apk", "Android package uses the product name");
      CheckMovingSupports();
      CheckVegetation();
      CheckTextureScrolling();
      var rainSamples = EnvironmentAudio.CreateRainSamples();
      Require(rainSamples.Length == 22050 * 8 * 2, "eight-second stereo rain loop");
      double rainEnergy = 0, rainMean = 0;
      foreach (float sample in rainSamples) {
        Require(!float.IsNaN(sample) && Mathf.Abs(sample) <= 1, "finite rain samples without clipping");
        rainEnergy += sample * sample; rainMean += sample;
      }
      Require(rainEnergy / rainSamples.Length > .01 && Math.Abs(rainMean / rainSamples.Length) < .01,
        "rain loop contains signal without DC offset");
      foreach (string shaderName in new[] { "Remaster/Foliage", "Remaster/Surface", "Remaster/LayeredSky", "Remaster/Precipitation" }) {
        var shader = Resources.Load<Shader>(shaderName);
        Require(shader != null && !ShaderUtil.ShaderHasError(shader), "environment shader compiles: " + shaderName);
      }
      foreach (EnvironmentPreset preset in Enum.GetValues(typeof(EnvironmentPreset)))
        for (int i = 1; i <= 13; i++)
        {
          var profile = profiles["level" + i.ToString("00")];
          var run = new EnvironmentRun(profile, preset);
          for (int sector = 1; sector <= profile.sectors.Length; sector++)
          {
            run.EnterSector(sector); run.Advance(8);
            Require(!float.IsNaN(run.Current.fog), "finite preset state");
          }
        }
      var test = new EnvironmentRun(profiles["level01"], EnvironmentPreset.Standard);
      Require(test.Current.skyTheme == Vector4.zero, "standard preserves the authored sky palette");
      test.Select(EnvironmentPreset.Standard);
      Require(test.Time == 0 && test.Current.rain == 0, "same selection is harmless");
      test.Select(EnvironmentPreset.London);
      Require(test.Current.rain == 0, "manual switch starts continuously");
      test.Advance(4);
      Require(Mathf.Abs(test.Current.skyTheme.w - .5f) < .0001f, "sky override blends on the weather clock");
      Require(Mathf.Abs(test.Current.rain - 0.325f) < 0.0001f, "halfway blend");
      float rain = test.Current.rain;
      Vector4 sky = test.Current.skyTheme;
      test.Select(EnvironmentPreset.Moscow);
      Require(test.Current.rain == rain, "interrupt from current blend");
      Require(test.Current.skyTheme == sky, "interrupted sky remains continuous");
      test.Advance(0);
      Require(test.Current.rain == rain, "pause does not advance");
      test.Advance(8);
      Require(test.Current.rain == 0 && test.Current.snowfall > 0, "second blend completes");
      test.RestoreCheckpoint();
      Require(test.Current.rain == rain, "rebased checkpoint restored");
      Require(test.Current.skyTheme == sky, "checkpoint restores the matching sky blend");
      test.Select(EnvironmentPreset.Standard);
      test.Restart(EnvironmentPreset.Standard);
      Require(test.Current.surface.ice == 0, "restart restores baseline");
      var custom = new EnvironmentRun(null, EnvironmentPreset.Moscow);
      Require(custom.Current.snowfall == 0 && custom.Current.surface.ice == 0, "unknown custom level remains dry");
      foreach (string invalid in new[] { "null", "[null]", "[{\"level\":\"x\",\"sectors\":[\"invalid\"]}]",
        "[{\"level\":\"x\",\"sectors\":[\"clear\"],\"coverage\":0.9}]" })
      {
        bool rejected = false;
        try { LevelEnvironmentProfile.Parse(invalid); } catch (InvalidOperationException) { rejected = true; }
        Require(rejected, "invalid profile rejected");
      }
      var coarse = new EnvironmentRun(profiles["level01"], EnvironmentPreset.Standard);
      coarse.Select(EnvironmentPreset.London);
      for (int i = 0; i < 240; i++) coarse.Advance(1f / 30);
      var fine = new EnvironmentRun(profiles["level01"], EnvironmentPreset.Standard);
      fine.Select(EnvironmentPreset.London);
      for (int i = 0; i < 1152; i++) fine.Advance(1f / 144);
      Require(Mathf.Abs(coarse.Current.rain - fine.Current.rain) < 0.00001f, "time-based blend independent of render cadence");
      Require(Ballance2.Res.GamePathManager.ResolvePersistentDataPath("/profiles/iwonz/Reballance", RuntimePlatform.OSXPlayer) == "/profiles/iwonz/Ballance Remaster", "rename preserves desktop files");
      Require(Ballance2.Res.GamePathManager.ResolvePersistentDataPath("/profiles/unity.iwonz.Reballance", RuntimePlatform.OSXPlayer) == "/profiles/unity.iwonz.Ballance Remaster", "legacy macOS path remains stable");
      Require(Ballance2.Res.GamePathManager.ResolvePersistentDataPath("/idbfs/profile", RuntimePlatform.WebGLPlayer) == "/idbfs/profile", "Web URL identity unchanged");
      Require(Ballance2.Res.GamePathManager.ResolvePersistentDataPath("/profiles/Ballance", RuntimePlatform.OSXPlayer) == "/profiles/Ballance", "classic storage never imported");
      CheckRecords();
      CheckPreferences();
      Require(RemasterGraphics.Calculate(3840, 2160, 0, 3) == new Vector2Int(1920, 1080), "Auto caps at Full HD");
      Require(RemasterGraphics.Calculate(1920, 1080, 2, 3) == new Vector2Int(2560, 1440), "QHD can render above display resolution");
      Require(RemasterGraphics.Calculate(1920, 1080, 3, 0) == new Vector2Int(1920, 1080), "4K at half scale");
      Require(RemasterGraphics.Calculate(3440, 1440, 3, 3) == new Vector2Int(3840, 1607), "ultrawide aspect preserved");
      Require(RemasterGraphics.Calculate(720, 1280, 0, 3) == new Vector2Int(608, 1080), "portrait aspect preserved");
      Require(RemasterSkyRenderer.CubeSize(1) == 128 && RemasterSkyRenderer.CubeSize(2) == 256 && RemasterSkyRenderer.CubeSize(3) == 256, "bounded reusable cloud cubemaps");
      Debug.Log("ENVIRONMENT_CHECKS_PASSED: profiles, blends, pause, checkpoint, shared records and invalid input.");
    }

    public static void CheckShutdown()
    {
      Require(PhysicsEnvironment.PhysicsWorlds.Count == 0, "shutdown fixture requires an empty editor world");
      var scheduler = typeof(Ballance2.Services.GameManager).GetField("_GameTimeMachine",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
      var previousScheduler = scheduler.GetValue(null);
      var root = new GameObject("Physics shutdown fixture");
      try {
        scheduler.SetValue(null, root.AddComponent<Ballance2.Services.GameTimeMachine>());
        BallancePhysics.PhysicsSystemInit.DoInit();
        var world = root.AddComponent<PhysicsEnvironment>();
        world.AutoCreate = false; world.Create();
        var floor = new GameObject("Floor").AddComponent<PhysicsObject>();
        floor.transform.SetParent(root.transform); floor.transform.position = Vector3.down * 100;
        floor.UseBall = true; floor.BallRadius = 100; floor.Fixed = true; floor.Physicalize();
        var ball = new GameObject("Ball").AddComponent<PhysicsObject>();
        ball.transform.SetParent(root.transform); ball.transform.position = Vector3.up * 3;
        ball.UseBall = true; ball.Physicalize(); ball.EnableCollisionEvent = true;
        for (int i = 0; i < 120; i++) BallancePhysics.PhysicsApi.API.environment_simulate_dtime(world.Handle, 1f / 60);
        Require(BallancePhysics.PhysicsApi.API.physics_get_speed(ball.Handle) < .1f, "shutdown fixture has a settled contact");
        BallancePhysics.PhysicsSystemInit.DoDestroy();
        Require(world.Handle == IntPtr.Zero && !floor.IsPhysicalized && !ball.IsPhysicalized,
          "shutdown destroys bodies and world before the vector pool");
        Require(PhysicsEnvironment.PhysicsWorlds.Count == 0, "shutdown clears world ownership");
        foreach (var type in new[] { typeof(GamePlayManager), typeof(BallManager), typeof(CamManager),
          typeof(SectorManager), typeof(MusicManager), typeof(Ballance2.Game.GamePlay.Balls.BallSoundManager),
          typeof(Ballance2.Game.GamePlay.Tranfo.TranfoAminControl), typeof(Ballance2.Game.GamePlay.Other.TutorialController) }) {
          var owner = new GameObject(type.Name + " shutdown fixture");
          owner.transform.SetParent(root.transform);
          var component = owner.AddComponent(type);
          type.GetMethod("OnDestroy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(component, null);
        }
        Debug.Log("PHYSICS_SHUTDOWN_CHECK_PASSED: active contact, global shutdown, subsequent component cleanup.");
      } finally {
        if (PhysicsEnvironment.PhysicsWorlds.Count > 0) BallancePhysics.PhysicsSystemInit.DoDestroy();
        UnityEngine.Object.DestroyImmediate(root);
        scheduler.SetValue(null, previousScheduler);
        BallancePhysics.PhysicsSystemInit.DoInit();
      }
    }

    private static void CheckTextureScrolling()
    {
      var material = new Material(Shader.Find("Unlit/Texture"));
      material.mainTextureOffset = new Vector2(.1f, .2f);
      var plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
      try {
        var renderer = plane.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        var scroller = plane.AddComponent<Ballance2.Game.TextureScroller>();
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        typeof(Ballance2.Game.TextureScroller).GetMethod("OnEnable", flags).Invoke(scroller, null);
        typeof(Ballance2.Game.TextureScroller).GetField("scrollOff", flags).SetValue(scroller, new Vector2(.3f, .4f));
        typeof(Ballance2.Game.TextureScroller).GetMethod("Update", flags).Invoke(scroller, null);
        var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
        var offset = block.GetVector("_MainTex_ST");
        Require(Mathf.Abs(offset.z - .3f) < .0001f && Mathf.Abs(offset.w - .4f) < .0001f, "scrolling reaches only the renderer");
        Require(material.mainTextureOffset == new Vector2(.1f, .2f) && renderer.sharedMaterial == material,
          "cloud scrolling preserves the shared source material");
      } finally {
        UnityEngine.Object.DestroyImmediate(plane); UnityEngine.Object.DestroyImmediate(material);
      }
    }

    private static void CheckVegetation()
    {
      var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
      floor.name = "Vegetation fixture";
      var mesh = UnityEngine.Object.Instantiate(floor.GetComponent<MeshFilter>().sharedMesh);
      var vertices = mesh.vertices;
      for (int i = 0; i < vertices.Length; i++) vertices[i] *= 10;
      mesh.vertices = vertices; mesh.RecalculateBounds();
      floor.GetComponent<MeshFilter>().sharedMesh = mesh;
      floor.transform.SetPositionAndRotation(new Vector3(30, 8, 20), Quaternion.Euler(0, 90, 0));
      try {
        using (var vegetation = new EnvironmentVegetation(new LevelEnvironmentProfile { coverings = new[] { floor.name } })) {
          Require(floor.transform.childCount == 1, "one fern on the covered floor wall");
          var plant = floor.transform.GetChild(0);
          Require(plant.GetComponent<MeshFilter>().sharedMesh.vertexCount == 54, "segmented fronds support curved motion");
          Require(!plant.gameObject.activeSelf, "vegetation starts hidden in the dry preset");
          Require(plant.localPosition.y + 3 < mesh.bounds.max.y, "fronds remain below the walkable top");
          Require(Mathf.Max(Mathf.Abs(plant.localPosition.x), Mathf.Abs(plant.localPosition.z)) > 5,
            "fern is outside the collision wall");
          Require(floor.GetComponentsInChildren<Collider>(true).Length == 1, "vegetation adds no collision geometry");
          vegetation.Apply(1, 10);
          Require(plant.gameObject.activeSelf, "tropical vegetation becomes visible");
          Vector3 localPoint = plant.localPosition;
          floor.transform.position += Vector3.right * 20;
          Require(Vector3.Distance(plant.position, floor.transform.TransformPoint(localPoint)) < .0001f,
            "vegetation follows its support");
          vegetation.Apply(0, 11);
          Require(!plant.gameObject.activeSelf, "return to dry hides vegetation");
        }
        Require(floor.transform.childCount == 0, "vegetation cleanup removes owned objects");
      } finally { UnityEngine.Object.DestroyImmediate(floor); UnityEngine.Object.DestroyImmediate(mesh); }
    }

    private static void CheckMovingSupports()
    {
      foreach (string name in new[] { "03", "08", "19", "25", "30", "37" }) {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/GamePlay/Prefabs/Moduls/P_Modul_" + name + ".prefab");
        Require(prefab != null, "moving support prefab exists");
        int supports = 0;
        foreach (var body in prefab.GetComponentsInChildren<PhysicsObject>(true)) {
          if (!EnvironmentSurfaces.IsMovingSupport(body.name)) continue;
          supports++;
          Require(EnvironmentSurfaces.TryGetCollisionBounds(body, out Bounds bounds), "covering uses authored collision bounds");
          var regions = EnvironmentSurfaces.CreateRegions(bounds, body.transform.lossyScale, .2f);
          var repeated = EnvironmentSurfaces.CreateRegions(bounds, body.transform.lossyScale, .2f);
          var varied = EnvironmentSurfaces.CreateRegions(bounds, body.transform.lossyScale, .2f, 123);
          Require(regions.Length == 6, "moving support has distinct snow and ice zones: " + body.name + " " + bounds + " scale=" + body.transform.lossyScale);
          Require(regions[0].center == repeated[0].center && regions[5].extent == repeated[5].extent && regions[0].center != varied[0].center,
            "coverage is reproducible and varies between physical objects");
          Require(EnvironmentSurfaces.CreateRegions(bounds, body.transform.lossyScale, 0).Length == 0, "zero coverage remains clear");
          var pose = Matrix4x4.TRS(new Vector3(40, 12, -25), Quaternion.Euler(12, 43, 8), Vector3.one);
          foreach (var region in regions)
            Require(Vector3.Distance(pose.inverse.MultiplyPoint3x4(pose.MultiplyPoint3x4(region.center)), region.center) < .0001f,
              "surface coordinates follow translated and rotated mechanism");
        }
        Require(supports == 1, "exactly one configured walkable body per mechanism");
      }
    }

    private static void CheckRecords()
    {
      string directory = Path.Combine(Path.GetTempPath(), "ballance-record-check-" + Guid.NewGuid());
      string dataDirectory = Path.Combine(directory, "Remaster");
      Directory.CreateDirectory(dataDirectory);
      string original = Path.Combine(directory, "HighscoreData.json");
      File.WriteAllText(original, "original sentinel");
      string Legacy(string name, params HighscoreDataItem[] items) {
        string path = Path.Combine(dataDirectory, name);
        var payload = new Newtonsoft.Json.Linq.JObject {
          ["data"] = Newtonsoft.Json.JsonConvert.SerializeObject(new Dictionary<string, HighscoreDataItem[]> { ["level01"] = items }),
          ["names"] = "[\"level01\"]"
        };
        File.WriteAllText(path, payload.ToString()); return path;
      }
      GameObject host = null;
      try {
        var first = new HighscoreDataItem("first", 123, "2026/10/25");
        string older = Legacy("Highscores-r1-0.json", first);
        string newer = Legacy("Highscores-r2-2.json", first, new HighscoreDataItem("second", 456, "2026/10/25"));
        string before = File.ReadAllText(older), beforeNewer = File.ReadAllText(newer);
        File.WriteAllText(Path.Combine(dataDirectory, "LevelPassStateData.json"), "{\"level02\":true}");
        var records = new RemasterRecords(directory);
        Require(records.Current.Data["level01"].Count == 2 && records.Current.Data["level01"][0].score == 456,
          "legacy preset/rules scores merge, sort and deduplicate");
        Require(File.ReadAllText(older) == before && File.ReadAllText(newer) == beforeNewer, "migration preserves source scores");
        Require(records.Unlocks["level02"], "migration preserves unlocks");
        Require(File.ReadAllText(original) == "original sentinel", "Original file preserved");

        Require(HighscoreManager.Instance == null && EnvironmentController.Active == null, "record check needs an isolated scene");
        host = new GameObject("Shared record check");
        var manager = host.AddComponent<HighscoreManager>();
        typeof(HighscoreManager).GetField("records", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
          .SetValue(manager, records);
        var environment = host.AddComponent<EnvironmentController>();
        var run = new EnvironmentRun(null, EnvironmentPreset.Standard);
        typeof(EnvironmentController).GetProperty("Run").SetValue(environment, run);
        typeof(EnvironmentController).GetProperty("Active").SetValue(null, environment);
        int count = 0;
        foreach (EnvironmentPreset preset in Enum.GetValues(typeof(EnvironmentPreset))) {
          run.Select(preset);
          Require(manager.CheckLevelHighScore("level01", 1000), "weather change never blocks a record");
          manager.AddItem("level01", preset.ToString(), 600 + count++);
        }
        Require(manager.GetData("level01").Count == 9 && records.Unlocks["level01"], "all switched runs save to one table and unlock");
        var loaded = new RemasterRecords(directory);
        Require(loaded.Current.Data["level01"].Count == 9 && loaded.Current.Data["level01"][0].score == 606, "shared scores reload");
        loaded.Current.Data.Clear(); loaded.Save();
        Require(new RemasterRecords(directory).Current.Data.Count == 0, "cleared shared records do not reimport legacy scores");
        File.WriteAllText(Path.Combine(dataDirectory, "Highscores.json"), "{\"data\":\"null\",\"names\":\"[]\"}");
        bool rejected = false;
        try { new RemasterRecords(directory); } catch (InvalidDataException) { rejected = true; }
        Require(rejected, "invalid shared score file cannot silently overwrite data");
        File.Delete(Path.Combine(dataDirectory, "Highscores.json"));
        File.WriteAllText(newer, "{\"data\":\"null\",\"names\":\"[]\"}");
        rejected = false;
        try { new RemasterRecords(directory); } catch (InvalidDataException) { rejected = true; }
        Require(rejected && !File.Exists(Path.Combine(dataDirectory, "Highscores.json")), "invalid legacy input cannot save a partial migration");
        Require(File.ReadAllText(older) == before, "failed migration preserves valid sources");
      } finally {
        if (host != null) UnityEngine.Object.DestroyImmediate(host);
        Directory.Delete(directory, true);
      }
    }

    private static void CheckPreferences()
    {
      string key = "environment-check-" + Guid.NewGuid();
      bool hadIndex = PlayerPrefs.HasKey(RemasterPreferences.IndexKey);
      string index = PlayerPrefs.GetString(RemasterPreferences.IndexKey);
      try {
        PlayerPrefs.SetString(RemasterPreferences.IndexKey, "{}");
        PlayerPrefs.SetInt(key, 71);
        Require(RemasterPreferences.GetInt(key, 5) == 5, "Original preferences are not imported");
        RemasterPreferences.SetInt(key, 19);
        Require(PlayerPrefs.GetInt(key) == 71 && RemasterPreferences.GetInt(key) == 19, "preference namespaces isolated");
        PlayerPrefs.SetString(RemasterPreferences.IndexKey, "{\"keys\":[\"" + key + "\",\"" + RemasterPreferences.Prefix + key + "\"]}");
        RemasterPreferences.Reset();
        Require(PlayerPrefs.GetInt(key) == 71 && RemasterPreferences.GetInt(key, 5) == 5, "reset cannot erase Original keys");
      } finally {
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.DeleteKey(RemasterPreferences.Prefix + key);
        if (hadIndex) PlayerPrefs.SetString(RemasterPreferences.IndexKey, index);
        else PlayerPrefs.DeleteKey(RemasterPreferences.IndexKey);
        PlayerPrefs.Save();
      }
    }
  }
}
