using System;
using System.Collections.Generic;
using BallancePhysics;
using BallancePhysics.Api;
using BallancePhysics.Wapper;
using Ballance2.Game.GamePlay;
using UnityEngine;

namespace Ballance2.Game.Remaster
{
  public sealed class EnvironmentSurfaces : IDisposable
  {
    private sealed class Binding
    {
      public PhysicsObject body;
      public Transform transform;
      public Renderer[] renderers;
      public SurfaceRegion[] regions;
      public bool applied;
      public Matrix4x4 matrix;
    }
    private readonly List<Binding> bindings = new List<Binding>();
    private readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
    private readonly Vector4[] centers = new Vector4[8], extents = new Vector4[8], weights = new Vector4[8];

    public EnvironmentSurfaces(LevelEnvironmentProfile profile, bool preview)
    {
      var covered = new HashSet<string>(profile.coverings, StringComparer.Ordinal);
      var mechanisms = new HashSet<string>(profile.mechanisms, StringComparer.Ordinal);
      if (preview) {
        foreach (var mesh in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
          if (mesh.sharedMesh != null && covered.Contains(mesh.name)) bindings.Add(new Binding {
            transform = mesh.transform,
            regions = CreateRegions(mesh.sharedMesh.bounds, mesh.transform.lossyScale, profile.coverage, StableSeed(mesh.name)),
            renderers = mesh.GetComponents<Renderer>()
          });
      }
      foreach (var body in UnityEngine.Object.FindObjectsByType<PhysicsObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
        var mechanism = body.GetComponentInParent<ModulBase>(true);
        bool moving = mechanism != null && mechanisms.Contains(mechanism.name) && IsMovingSupport(body.name);
        SurfaceMaterial material = body.name == "P_Dome_MF" ? SurfaceMaterial.Rubber : body.Layer == 11 ? SurfaceMaterial.Stone : body.Layer == 12 ? SurfaceMaterial.Metal :
          body.Layer == 13 || body.Layer == 14 || moving ? SurfaceMaterial.Wood : SurfaceMaterial.None;
        if (material == SurfaceMaterial.None || preview && !moving) continue;
        var regions = (moving || covered.Contains(body.name)) && TryGetCollisionBounds(body, out Bounds bounds)
          ? CreateRegions(bounds, body.transform.lossyScale, profile.coverage, StableSeed(body.name)) : Array.Empty<SurfaceRegion>();
        if (!preview) body.ConfigureSurface(material, regions);
        var renderers = new List<Renderer>();
        foreach (var renderer in body.GetComponentsInChildren<Renderer>(true))
          if (renderer.GetComponentInParent<PhysicsObject>() == body) renderers.Add(renderer);
        bindings.Add(new Binding { body = body, transform = body.transform, regions = regions, renderers = renderers.ToArray() });
      }
    }

    public static bool IsMovingSupport(string name) => name == "P_Modul_08_Schaukel" || name == "P_Modul_30_Wippe" ||
      name == "P_Modul_03_Floor" || name == "P_Modul_19_Flaps" || name == "P_Modul_25_Bridge" || name == "P_Modul_37_Bridge";

    public static bool TryGetCollisionBounds(PhysicsObject body, out Bounds bounds)
    {
      bounds = default;
      bool found = false;
      foreach (var meshes in new[] { body.Convex, body.Concave })
        foreach (var mesh in meshes) if (mesh != null) {
          if (found) bounds.Encapsulate(mesh.bounds); else bounds = mesh.bounds;
          found = true;
        }
      if (!found) {
        var mesh = body.GetComponent<MeshFilter>();
        if (mesh != null && mesh.sharedMesh != null) { bounds = mesh.sharedMesh.bounds; found = true; }
      }
      return found;
    }

    private static int StableSeed(string name)
    {
      unchecked { int seed = 17; foreach (char letter in name) seed = seed * 31 + letter; return seed; }
    }

    public static SurfaceRegion[] CreateRegions(Bounds mesh, Vector3 scale, float coverage, int seed = 0)
    {
      // shortcut: authored per-sector playthroughs must still tune the coverage budget before release.
      Vector3 center = Vector3.Scale(mesh.center, scale);
      Vector3 size = Vector3.Scale(mesh.extents, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
      int thin = size.x < size.y && size.x < size.z ? 0 : size.z < size.y ? 2 : 1;
      int across = (thin + 1) % 3, along = (thin + 2) % 3;
      if (size[across] > size[along]) { int swap = across; across = along; along = swap; }
      if (size[across] < 2 || size[along] < 2 || coverage <= 0) return Array.Empty<SurfaceRegion>();
      var random = new System.Random(seed ^ Mathf.RoundToInt(center.x * 31 + center.z * 17));
      var regions = new SurfaceRegion[6];
      for (int i = 0; i < regions.Length; i++) {
        Vector3 position = center, extent = size;
        position[across] += size[across] * ((float)random.NextDouble() * 1.4f - .7f);
        position[along] += size[along] * ((float)random.NextDouble() * 1.5f - .75f);
        float radius = size[across] * Mathf.Sqrt(coverage) * Mathf.Lerp(.55f, .95f, (float)random.NextDouble());
        extent[across] = radius;
        extent[along] = Mathf.Min(size[along] * .4f, radius * Mathf.Lerp(.8f, 1.65f, (float)random.NextDouble()));
        extent[thin] = Mathf.Max(.1f, size[thin] + .1f);
        regions[i] = new SurfaceRegion {
          center = position, extent = extent, snow = i % 2 == 0 ? 1 : 0, ice = i % 2 == 1 ? 1 : 0,
          sand = 1, vegetation = Mathf.Lerp(.65f, 1, (float)random.NextDouble())
        };
      }
      return regions;
    }

    public void Apply()
    {
      foreach (var binding in bindings) {
        if (binding.transform == null) continue;
        var transform = binding.transform;
        var matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).inverse;
        if (binding.applied && matrix == binding.matrix) continue;
        for (int i = 0; i < binding.regions.Length; i++) {
          var region = binding.regions[i];
          centers[i] = region.center; extents[i] = region.extent;
          weights[i] = new Vector4(region.snow, region.ice, region.sand, region.vegetation);
        }
        foreach (var renderer in binding.renderers) if (renderer != null) {
          renderer.GetPropertyBlock(properties);
          if (!binding.applied) {
            properties.SetInt("_RegionCount", binding.regions.Length);
            properties.SetVectorArray("_RegionCenters", centers);
            properties.SetVectorArray("_RegionExtents", extents);
            properties.SetVectorArray("_RegionWeights", weights);
          }
          properties.SetMatrix("_WorldToSurface", matrix);
          renderer.SetPropertyBlock(properties);
        }
        binding.applied = true; binding.matrix = matrix;
      }
    }

    public void Dispose()
    {
      foreach (var binding in bindings) {
        if (binding.body != null) binding.body.ConfigureSurface(SurfaceMaterial.None, null);
        foreach (var renderer in binding.renderers) if (renderer != null) {
          renderer.GetPropertyBlock(properties); properties.SetInt("_RegionCount", 0); renderer.SetPropertyBlock(properties);
        }
      }
      bindings.Clear();
    }
  }
}
