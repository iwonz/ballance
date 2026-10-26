using System;
using System.IO;
using Ballance2.Game.Remaster;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ballance2.Editor
{
  public static class RemasterArtChecks
  {
    public static void Render() { Render(false); }
    public static void RenderClouds() { RemasterReliefAuthoring.GenerateCloudNoise(); Render(true); }
    private static void Render(bool cloudsOnly)
    {
      EnvironmentChecks.Run();
      RemasterMaterialAuthoring.Apply();
      const string folder = "Assets/Game/Remaster/Resources/Remaster/";
      foreach (string family in RemasterReliefAuthoring.Families) {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(folder+family+"Relief.png");
        var importer = (TextureImporter)AssetImporter.GetAtPath(folder+family+"Relief.png");
        if (texture == null || texture.width != 1024 || texture.mipmapCount < 2 || importer.sRGBTexture || texture.wrapMode != TextureWrapMode.Repeat)
          throw new InvalidOperationException("Invalid linear periodic relief import: "+family);
      }
      var scene = EditorSceneManager.NewPreviewScene();
      var root = new GameObject("Art inspection");
      UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
      var camera = new GameObject("Camera").AddComponent<Camera>(); camera.transform.SetParent(root.transform);
      camera.scene = scene; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.15f,.2f,.24f);
      camera.transform.position = new Vector3(0,.9f,-2.8f); camera.transform.LookAt(new Vector3(0,0,0)); camera.fieldOfView = 42;
      var target = new RenderTexture(1600,900,24); camera.targetTexture = target;
      var capture = new Texture2D(1600,900,TextureFormat.RGB24,false);
      var light = new GameObject("Sun").AddComponent<Light>(); light.transform.SetParent(root.transform);
      light.type = LightType.Directional; light.intensity = .7f; light.transform.rotation = Quaternion.Euler(45,-35,0);
      var ambientMode = RenderSettings.ambientMode; var ambientColor = RenderSettings.ambientLight;
      RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.18f,.19f,.2f);
      var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere); ball.transform.SetParent(root.transform); ball.transform.localPosition = new Vector3(-.6f,0,0);
      var block = GameObject.CreatePrimitive(PrimitiveType.Cube); block.transform.SetParent(root.transform);
      block.transform.localPosition = new Vector3(.6f,-.2f,0); block.transform.localScale = new Vector3(.9f,.25f,1.2f);
      var material = new Material(Shader.Find("Ballance/Remaster/Surface"));
      var sky = new Material(Resources.Load<Shader>("Remaster/LayeredSky"));
      Directory.CreateDirectory("Builds/checks/remaster-art");
      void Save(string name) {
        camera.Render();
        var skybox = camera.GetComponent<Skybox>();
        if (skybox != null && skybox.material != sky) throw new InvalidOperationException("Sky capture did not restore its source material");
        var previous = RenderTexture.active; RenderTexture.active = target;
        try { capture.ReadPixels(new Rect(0,0,1600,900),0,0); capture.Apply(); File.WriteAllBytes("Builds/checks/remaster-art/"+name+".png",capture.EncodeToPNG()); }
        finally { RenderTexture.active = previous; }
      }
      try {
        ball.GetComponent<Renderer>().sharedMaterial = block.GetComponent<Renderer>().sharedMaterial = material;
        material.SetFloat("_NaturalBlend",1); material.SetFloat("_DetailScale",1); material.SetFloat("_HeightScale",.025f);
        if (!cloudsOnly) foreach (string family in RemasterReliefAuthoring.Families) {
          var albedo = Resources.Load<Texture2D>("Remaster/"+family);
          material.SetTexture("_NaturalTex", albedo == null ? Texture2D.whiteTexture : albedo);
          material.SetTexture("_ReliefTex",Resources.Load<Texture2D>("Remaster/"+family+"Relief"));
          material.SetFloat("_Metallic",family == "Metal" || family == "Brass" ? .85f : 0);
          material.SetColor("_Color",family == "Brass" ? new Color(.85f,.63f,.28f) : family == "Rubber" ? new Color(.12f,.13f,.14f) : family == "Rope" ? new Color(.64f,.5f,.31f) : Color.white);
          foreach (int angle in new[] { -35, 65 }) { light.transform.rotation = Quaternion.Euler(45,angle,0); Save(family+"-light"+angle); }
        }
        ball.SetActive(false);
        if (!cloudsOnly) {
          block.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
          block.transform.localScale = new Vector3(60,2,100);
          camera.transform.position = new Vector3(55,80,-100); camera.transform.LookAt(Vector3.zero);
          material.SetTexture("_NaturalTex",Resources.Load<Texture2D>("Remaster/Sandstone"));
          material.SetTexture("_ReliefTex",Resources.Load<Texture2D>("Remaster/SandstoneRelief"));
          material.SetFloat("_Metallic",0); material.SetColor("_Color",Color.white); material.SetFloat("_DetailScale",12);
          foreach (string family in new[] { "Snow", "Ice", "Sand", "Moss" }) {
            material.SetTexture("_"+family+"Tex",Resources.Load<Texture2D>("Remaster/"+family));
            material.SetTexture("_"+family+"Relief",Resources.Load<Texture2D>("Remaster/"+family+"Relief"));
          }
          var regions = EnvironmentSurfaces.CreateRegions(new Bounds(Vector3.zero,block.transform.localScale),Vector3.one,.32f,123);
          var centers = new Vector4[8]; var extents = new Vector4[8]; var weights = new Vector4[8];
          for (int i=0; i<regions.Length; i++) {
            centers[i] = regions[i].center; extents[i] = regions[i].extent;
            weights[i] = new Vector4(regions[i].snow,regions[i].ice,regions[i].sand,regions[i].vegetation);
          }
          material.SetInt("_RegionCount",regions.Length); material.SetMatrix("_WorldToSurface",Matrix4x4.identity);
          material.SetVectorArray("_RegionCenters",centers); material.SetVectorArray("_RegionExtents",extents); material.SetVectorArray("_RegionWeights",weights);
          foreach (var preset in new[] { EnvironmentPreset.Standard,EnvironmentPreset.Moscow,EnvironmentPreset.Sahara,EnvironmentPreset.HongKong }) {
            var state = EnvironmentState.For(preset);
            material.SetVector("_Surface",new Vector4(state.surface.wetness,state.surface.snow,state.surface.ice,state.surface.sand));
            material.SetFloat("_Vegetation",state.surface.vegetation);
            Save("Coverings-"+preset);
          }
        }
        block.SetActive(false);
        foreach (string side in new[] { "Front", "Back", "Left", "Right", "Down" })
          sky.SetTexture("_"+side+"Tex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Packages/core.assets.skys/Sky_A_"+side+".BMP"));
        sky.SetTexture("_UpTex",sky.GetTexture("_FrontTex"));
        sky.SetTexture("_CloudVolume",Resources.Load<Texture3D>("Remaster/CloudDensity"));
        sky.SetTexture("_CloudGroups",Resources.Load<Texture2D>("Remaster/CloudGroups"));
        sky.SetFloat("_CloudBase",-600); sky.SetVector("_CloudSun",new Vector4(.4f,.6f,-.2f,0));
        camera.gameObject.AddComponent<Skybox>().material = sky;
        var skyCache = camera.gameObject.AddComponent<RemasterSkyRenderer>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.transform.SetPositionAndRotation(new Vector3(0,100,0),Quaternion.Euler(25,0,0)); camera.fieldOfView = 55;
        foreach (EnvironmentPreset preset in Enum.GetValues(typeof(EnvironmentPreset))) {
          var state = preset == EnvironmentPreset.Weather ? LevelEnvironmentProfile.Parse(File.ReadAllText(folder+"EnvironmentProfiles.json"))["level01"].Resolve(preset,3) : EnvironmentState.For(preset);
          sky.SetVector("_Weather",new Vector4(state.cloud,state.fog,state.warmth,state.rain));
          sky.SetVector("_ThemeSky",state.skyTheme);
          foreach (int quality in new[] { 1,2,3 }) {
            sky.SetFloat("_CloudQuality",quality); sky.SetFloat("_WeatherTime",0); Save("Clouds-"+preset+"-q"+quality);
            if (preset == EnvironmentPreset.Moscow && quality == 2) {
              skyCache.enabled = false; Save("Clouds-Moscow-direct"); skyCache.enabled = true;
            }
          }
        }
        sky.SetFloat("_WeatherTime",90); Save("Clouds-motion-90s");
        sky.SetFloat("_WeatherTime",0);
        Save("Clouds-distribution-origin");
        var originalClouds = capture.GetPixels32();
        camera.transform.position += Vector3.right*(240/.23f);
        Save("Clouds-distribution-old-period");
        var translatedClouds = capture.GetPixels32();
        double difference = 0;
        for (int pixel=0; pixel<originalClouds.Length; pixel++) {
          var a = originalClouds[pixel]; var b = translatedClouds[pixel];
          difference += Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b);
        }
        difference /= originalClouds.Length*3*255.0;
        if (difference < .015) throw new InvalidOperationException("Cloud deck repeats at the old texture period: "+difference);
        Debug.Log("REMASTER_CLOUD_TRANSLATION_DIFFERENCE: "+difference);
        camera.transform.position = new Vector3(0,100,0);
        foreach (int heading in new[] { 45, 135, 225, 315 }) {
          camera.transform.rotation = Quaternion.Euler(25,heading,0);
          Save("Clouds-heading-"+heading);
        }
        camera.transform.rotation = Quaternion.Euler(25,0,0);
        sky.SetVector("_Weather",Vector4.zero); sky.SetVector("_ThemeSky",Vector4.zero);
        for (char stock='A'; stock<='M'; stock++) {
          sky.SetTexture("_FrontTex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Packages/core.assets.skys/Sky_"+stock+"_Front.BMP"));
          Save("Stock-sky-"+stock);
        }
        Debug.Log("REMASTER_ART_PREVIEWS_RENDERED: " + (cloudsOnly ? "cloud iteration" : "fourteen materials under two lights") + "; seven cloud presets at all qualities, thirteen stock skies, headings and motion.");
      } finally {
        camera.targetTexture = null; target.Release();
        RenderSettings.ambientMode = ambientMode; RenderSettings.ambientLight = ambientColor;
        foreach (var asset in new UnityEngine.Object[] { material, sky, capture, target, root }) UnityEngine.Object.DestroyImmediate(asset);
        EditorSceneManager.ClosePreviewScene(scene);
      }
    }
  }
}
