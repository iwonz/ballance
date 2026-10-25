using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Ballance2.Game.Remaster;
using BallancePhysics.Wapper;

namespace Ballance2.Editor
{
  public static class RemasterMaterialAuthoring
  {
    private static readonly Dictionary<string, string> families = new Dictionary<string, string>();
    private static void Family(string family, string names) { foreach (string name in names.Split(' ')) families.Add(name, family); }

    public static void Apply()
    {
      if (families.Count == 0) {
        Family("Sandstone", "Floor_Side Floor_Top_Boderless Floor_Top_Border Floor_Top_Checkpoint Floor_Top_Flat Floor_Top_Profile Floor_Top_ProfileFlat Tower_Floor_Side Tower_Floor_Top_Flat_illu Colums_beige P_Modul_08_Stone Modul_15_Stone P_Modul_03_Floor P_Modul_03_Walls");
        Family("Oak", "Ball_Wood Ball_Wood_Pieces P_Ball_Wood_MFMat Floor_Wood Floor_Stopper_Wood Halfpipe_Wood Modul_11_Wood01 Modul_11_Wood02 Modul_13_Wood P_Modul_01_Wood P_Modul_08_Wood P_Modul_25_Wood P_Modul_19_Flaps P_Modul_19_FlapsBackboards P_Modul_30_Board P_Modul_37_Wood01 P_Modul_37_WoodAxis P_Modul_37_WoodMisc P_Modul_41_BoxWood P_Box_MFMat PE_Balloon_SuspBridge");
        Family("Granite", "Ball_Stone Ball_Stone_Pieces P_Ball_Stone_MFMat");
        Family("Metal", "Metall PE_UFO_Arm Rail Stick_Rail TowerRail MetalButtons Misc_MetalButtons P_Modul37_Misc_MetalButtons Modul_13_Metal P_Modul_08_Metal P_Modul_25_Hinge P_Modul_25_HingeSide P_Modul_37_Metall_1 P_Modul_41_BoxMetal PE_Balloon_Metall Laterne_Metal");
        Family("Paper", "Ball_Paper Ball_Paper_Pieces P_Ball_Paper_MFMat");
        Family("Rope", "P_Modul_26_Rope");
        Family("Concrete", "Modul01_Floor P_Modul_01_Stone");
        Family("Brass", "Misc_Schelle P_Modul_08_Schelle PE_Balloonl_Halter P_Modul_26_Halter");
        Family("Rubber", "P_Dome_MFMat");
        Family("Glass", "Laterne_Glas");
      }
      var shader = Shader.Find("Ballance/Remaster/Surface");
      if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Remaster surface shader failed compilation");
      int count = 0;
      foreach (string folder in new[] { "Assets/Game/Common/Materials/Game", "Assets/Game/MenuLevel/Prefabs/Materials" })
        foreach (string path in Directory.GetFiles(folder, "*.mat")) {
          var material = AssetDatabase.LoadAssetAtPath<Material>(path);
          string name = material.name.StartsWith("I_") ? material.name.Substring(2) : material.name;
          if (!families.TryGetValue(name, out string family)) continue;
          if (family == "Glass") {
            material.shader = Shader.Find("Standard");
            material.SetFloat("_Mode", 3); material.SetFloat("_Metallic", 0); material.SetFloat("_Glossiness", .94f);
            material.SetColor("_Color", new Color(.88f, .94f, 1, .4f));
            material.SetColor("_EmissionColor", new Color(.34f, .25f, .10f));
            material.SetTexture("_BumpMap",Resources.Load<Texture2D>("Remaster/GlassNormal"));
            material.SetTexture("_ParallaxMap",Resources.Load<Texture2D>("Remaster/GlassMask"));
            material.SetTexture("_MetallicGlossMap",Resources.Load<Texture2D>("Remaster/GlassMask"));
            material.SetFloat("_Parallax",.003f); material.SetFloat("_GlossMapScale",1);
            material.EnableKeyword("_NORMALMAP"); material.EnableKeyword("_PARALLAXMAP"); material.EnableKeyword("_METALLICGLOSSMAP");
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0); material.SetOverrideTag("RenderType", "Transparent");
            material.DisableKeyword("_ALPHATEST_ON"); material.DisableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON"); material.EnableKeyword("_EMISSION");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(material); count++; continue;
          }
          material.shader = shader;
          material.SetTexture("_ReliefTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/Remaster/Resources/Remaster/" + family + "Relief.png"));
          material.SetFloat("_HeightScale", family == "Metal" || family == "Brass" ? .006f : family == "Paper" ? .012f : .025f);
          foreach (string covering in new[] { "Snow", "Ice", "Sand", "Moss" }) {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/Remaster/Resources/Remaster/" + covering + ".png");
            if (texture == null) throw new InvalidOperationException("Missing covering texture: " + covering);
            material.SetTexture("_" + covering + "Tex", texture);
            material.SetTexture("_" + covering + "Relief", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/Remaster/Resources/Remaster/" + covering + "Relief.png"));
          }
          material.SetColor("_Emission", Color.black);
          material.SetFloat("_Metallic", family == "Metal" || family == "Brass" ? 0.85f : 0);
          material.SetFloat("_Smoothness", family == "Metal" ? 0.68f : family == "Brass" ? .55f : family == "Oak" ? 0.36f : family == "Rubber" ? .22f : 0.2f);
          material.SetFloat("_DetailScale", name.StartsWith("Ball_") ? 2 : 4);
          bool natural = family == "Sandstone" || family == "Oak" || family == "Granite" || family == "Paper" || family == "Concrete";
          material.SetFloat("_NaturalBlend", !natural ? 0 : family == "Paper" ? 1 : name == "Ball_Wood" ? 0.4f : family == "Granite" ? 0.75f : family == "Concrete" ? .7f : 0.42f);
          if (natural) material.SetTexture("_NaturalTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/Remaster/Resources/Remaster/" + family + ".png"));
          if (family == "Brass") material.SetColor("_Color", new Color(.85f, .63f, .28f));
          if (family == "Rubber") material.SetColor("_Color", new Color(.34f, .35f, .36f));
          material.enableInstancing = true;
          EditorUtility.SetDirty(material);
          count++;
        }
      AssetDatabase.SaveAssets();
      CheckBallVisuals();
      Debug.Log("REMASTER_MATERIALS_AUTHORED: " + count + " shared materials; meshes and GUIDs unchanged.");
    }

    private static void CheckBallVisuals()
    {
      foreach (string kind in new[] { "Wood", "Stone", "Paper" }) {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Import/Balls/Ball_" + kind + ".obj").GetComponentInChildren<MeshFilter>().sharedMesh;
        Vector3[] original = source.vertices;
        var go = new GameObject("Ball" + kind); go.SetActive(false);
        try {
          var filter = go.AddComponent<MeshFilter>(); filter.sharedMesh = source;
          var body = go.AddComponent<PhysicsObject>(); body.DoNotAutoCreateAtAwake = true;
          body.UseBall = kind != "Paper"; body.BallRadius = 2;
          RemasterBallVisual.Install(body, go.name);
          if (filter.sharedMesh == source || filter.sharedMesh.triangles.Length <= source.triangles.Length)
            throw new InvalidOperationException("Missing improved ball silhouette: " + kind);
          if (kind == "Paper" && (body.Convex.Count != 1 || body.Convex[0] != source))
            throw new InvalidOperationException("Paper collision mesh was replaced by its visual mesh");
          var after = source.vertices;
          for (int i = 0; i < original.Length; i++)
            if (original[i] != after[i]) throw new InvalidOperationException("Imported collision vertices changed");
          foreach (var vertex in filter.sharedMesh.vertices)
            if (float.IsNaN(vertex.sqrMagnitude) || vertex.magnitude > 2.3f)
              throw new InvalidOperationException("Invalid visual ball vertex");
          Debug.Log("BALL_VISUAL_CHECK: " + kind + " source=" + source.vertexCount + " visual=" + filter.sharedMesh.vertexCount + " collision=preserved");
        } finally { UnityEngine.Object.DestroyImmediate(go); }
      }
    }

    public static void RenderCoverings()
    {
      Apply();
      UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
      var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
      floor.transform.localScale = new Vector3(4, 1, 4);
      var material = new Material(Shader.Find("Ballance/Remaster/Surface"));
      material.mainTexture = Resources.Load<Texture2D>("Remaster/Sandstone");
      material.SetTexture("_NaturalTex", material.mainTexture);
      material.SetTexture("_ReliefTex", Resources.Load<Texture2D>("Remaster/SandstoneRelief"));
      foreach (string name in new[] { "Snow", "Ice", "Sand", "Moss" }) {
        var texture = Resources.Load<Texture2D>("Remaster/" + name);
        if (texture == null || texture.wrapMode != TextureWrapMode.Repeat || texture.mipmapCount < 2)
          throw new InvalidOperationException("Covering must repeat with mipmaps: " + name);
        material.SetTexture("_" + name + "Tex", texture);
        material.SetTexture("_" + name + "Relief", Resources.Load<Texture2D>("Remaster/" + name + "Relief"));
      }
      material.SetInt("_RegionCount", 1);
      material.SetVectorArray("_RegionCenters", new[] { Vector4.zero });
      material.SetVectorArray("_RegionExtents", new[] { new Vector4(100, 100, 100, 0) });
      material.SetVectorArray("_RegionWeights", new[] { Vector4.one });
      material.SetMatrix("_WorldToSurface", Matrix4x4.identity);
      floor.GetComponent<Renderer>().sharedMaterial = material;
      var light = new GameObject("Fixture light").AddComponent<Light>();
      light.type = LightType.Directional; light.intensity = .8f;
      light.transform.rotation = Quaternion.Euler(65, -30, 0);
      RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
      RenderSettings.ambientLight = Color.gray;
      var camera = new GameObject("Fixture camera").AddComponent<Camera>();
      camera.transform.SetPositionAndRotation(new Vector3(0, 40, 0), Quaternion.Euler(90, 0, 0));
      camera.orthographic = true; camera.orthographicSize = 20;
      camera.clearFlags = CameraClearFlags.SolidColor;
      var target = new RenderTexture(1024, 1024, 24); camera.targetTexture = target;
      var image = new Texture2D(1024, 1024, TextureFormat.RGB24, false);
      Directory.CreateDirectory("Builds/checks/coverings");
      try {
        foreach (string name in new[] { "Snow", "Ice", "Sand", "Moss" }) {
          material.SetVector("_Surface", new Vector4(0, name == "Snow" ? 1 : 0, name == "Ice" ? 1 : 0, name == "Sand" ? 1 : 0));
          material.SetFloat("_Vegetation", name == "Moss" ? 1 : 0);
          camera.Render(); RenderTexture.active = target;
          image.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0); image.Apply();
          File.WriteAllBytes("Builds/checks/coverings/" + name + ".png", image.EncodeToPNG());
        }
      } finally {
        RenderTexture.active = null; camera.targetTexture = null; target.Release();
        foreach (var value in new UnityEngine.Object[] { image, target, material, floor, camera.gameObject, light.gameObject })
          UnityEngine.Object.DestroyImmediate(value);
      }
      Debug.Log("COVERING_RENDER_CHECKS_PASSED: four repeated PBR covering previews; inspect their seams and scale.");
    }
  }
}
