using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ballance2.Game.Remaster
{
  public sealed class EnvironmentVegetation : IDisposable
  {
    private readonly List<GameObject> plants = new List<GameObject>();
    private readonly Material material;
    private readonly Mesh mesh;

    public EnvironmentVegetation(LevelEnvironmentProfile profile)
    {
      material = new Material(Resources.Load<Shader>("Remaster/Foliage")) { enableInstancing = true };
      material.mainTexture = Resources.Load<Texture2D>("Remaster/Fern");
      mesh = CreateFronds();
      var covered = new HashSet<string>(profile.coverings, StringComparer.Ordinal);
      foreach (var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
        if (plants.Count == 96) break;
        if (!covered.Contains(filter.name) || filter.sharedMesh == null || !filter.sharedMesh.isReadable) continue;
        if (!TryFindWall(filter.sharedMesh, out var point, out var normal)) continue;
        var plant = new GameObject("Side fern");
        plant.transform.SetParent(filter.transform, false);
        var direction = filter.transform.TransformDirection(normal);
        direction.y = 0;
        if (direction.sqrMagnitude < .1f) { Destroy(plant); continue; }
        plant.transform.SetPositionAndRotation(filter.transform.TransformPoint(point) + direction.normalized * .4f - Vector3.up * 2,
          Quaternion.LookRotation(direction.normalized, Vector3.up));
        plant.transform.localScale = Vector3.one;
        plant.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = plant.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        plants.Add(plant); plant.SetActive(false);
      }
    }

    private static bool TryFindWall(Mesh source, out Vector3 point, out Vector3 normal)
    {
      point = normal = Vector3.zero;
      var vertices = source.vertices; var triangles = source.triangles;
      float best = 2;
      for (int i = 0; i < triangles.Length; i += 3) {
        var a = vertices[triangles[i]]; var b = vertices[triangles[i + 1]]; var c = vertices[triangles[i + 2]];
        var cross = Vector3.Cross(b - a, c - a);
        float area = cross.magnitude;
        if (area <= best || Mathf.Abs(cross.normalized.y) > .15f) continue;
        var center = (a + b + c) / 3;
        if (center.y > source.bounds.max.y - .2f) continue;
        best = area; point = center; normal = cross.normalized;
      }
      return best > 2;
    }

    private static Mesh CreateFronds()
    {
      const int segments = 8;
      var vertices = new Vector3[3*(segments+1)*2]; var uv = new Vector2[vertices.Length]; var triangles = new int[3*segments*6];
      for (int i = 0; i < 3; i++) {
        var rotation = Quaternion.Euler(0, (i - 1) * 32, (i - 1) * 18);
        for (int j=0; j<=segments; j++) {
          float height = (float)j/segments;
          for (int side=0; side<2; side++) {
            int index = i*(segments+1)*2+j*2+side;
            vertices[index] = rotation*new Vector3(side == 0 ? -1 : 1,height*3,height*height*.6f);
            uv[index] = new Vector2(side,height);
          }
          if (j == segments) continue;
          int v = i*(segments+1)*2+j*2, t = (i*segments+j)*6;
          triangles[t] = v; triangles[t+1] = v+2; triangles[t+2] = v+1;
          triangles[t+3] = v+2; triangles[t+4] = v+3; triangles[t+5] = v+1;
        }
      }
      var result = new Mesh { name = "Side fern fronds", vertices = vertices, uv = uv, triangles = triangles };
      result.RecalculateNormals(); result.RecalculateTangents(); result.RecalculateBounds(); return result;
    }

    public void Apply(float vegetation, float time, float wetness = 0)
    {
      material.SetFloat("_Growth", vegetation); material.SetFloat("_WeatherTime", time);
      material.SetFloat("_Wetness",wetness);
      foreach (var plant in plants) if (plant != null && plant.activeSelf != (vegetation > .01f)) plant.SetActive(vegetation > .01f);
    }

    public void Dispose()
    {
      foreach (var plant in plants) if (plant != null) Destroy(plant);
      Destroy(mesh); Destroy(material); plants.Clear();
    }

    private static void Destroy(UnityEngine.Object value)
    {
      if (Application.isPlaying) UnityEngine.Object.Destroy(value);
      else UnityEngine.Object.DestroyImmediate(value);
    }
  }
}
