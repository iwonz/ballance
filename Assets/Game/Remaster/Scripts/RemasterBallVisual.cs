using System;
using System.Collections.Generic;
using BallancePhysics.Wapper;
using UnityEngine;

namespace Ballance2.Game.Remaster
{
  public sealed class RemasterBallVisual : MonoBehaviour
  {
    private MeshFilter filter;
    private Mesh original, visual;

    public static void Install(PhysicsObject body, string name)
    {
      if (name != "BallWood" && name != "BallStone" && name != "BallPaper") return;
      var filter = body.GetComponent<MeshFilter>();
      if (filter == null || filter.sharedMesh == null || body.GetComponent<RemasterBallVisual>() != null) return;
      var view = body.gameObject.AddComponent<RemasterBallVisual>();
      view.filter = filter; view.original = filter.sharedMesh;
      // Paper's native convex hull must continue using the original imported vertices.
      if (!body.UseBall && body.Convex.Count == 0 && body.Concave.Count == 0) body.Convex.Add(view.original);
      view.visual = Build(view.original, name == "BallPaper");
      filter.sharedMesh = view.visual;
    }

    public static Mesh Build(Mesh source, bool paper)
    {
      if (source.subMeshCount != 1) throw new InvalidOperationException("Expected a single authored ball material");
      var vertices = new List<Vector3>(source.vertices);
      var uv = new List<Vector2>(source.uv);
      var indices = new List<int>(source.triangles);
      if (vertices.Count != uv.Count) throw new InvalidOperationException("Ball mesh is missing texture coordinates");
      Vector3 center = source.bounds.center;
      for (int pass = 0; pass < 2; pass++) {
        var next = new List<int>(indices.Count * 4);
        var midpoints = new Dictionary<long, int>();
        int Midpoint(int a, int b) {
          long key = ((long)Math.Min(a, b) << 32) | (uint)Math.Max(a, b);
          if (midpoints.TryGetValue(key, out int cached)) return cached;
          Vector3 first = vertices[a] - center, second = vertices[b] - center;
          Vector3 direction = (first + second).normalized;
          float radius = (first.magnitude + second.magnitude) * .5f;
          int index = vertices.Count;
          vertices.Add(center + direction * radius); uv.Add((uv[a] + uv[b]) * .5f);
          midpoints.Add(key, index); return index;
        }
        for (int i = 0; i < indices.Count; i += 3) {
          int a = indices[i], b = indices[i + 1], c = indices[i + 2];
          int ab = Midpoint(a, b), bc = Midpoint(b, c), ca = Midpoint(c, a);
          next.AddRange(new[] { a, ab, ca, ab, b, bc, ca, bc, c, ab, bc, ca });
        }
        indices = next;
      }
      var normals = new List<Vector3>(vertices.Count);
      if (paper) {
        var folded = new List<Vector3>(indices.Count);
        var foldedUV = new List<Vector2>(indices.Count);
        foreach (int index in indices) {
          Vector3 p = vertices[index] - center;
          float crease = Mathf.Sin(p.x * 17 + p.y * 11) * Mathf.Sin(p.z * 13 - p.y * 7) * .012f;
          folded.Add(center + p * (1 + crease)); foldedUV.Add(uv[index]);
        }
        vertices = folded; uv = foldedUV;
        for (int i = 0; i < indices.Count; i++) indices[i] = i;
      } else {
        foreach (var vertex in vertices) normals.Add((vertex - center).normalized);
      }
      var mesh = new Mesh { name = source.name + " Remaster visual", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
      mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(indices, 0);
      if (paper) mesh.RecalculateNormals(); else mesh.SetNormals(normals);
      mesh.RecalculateTangents(); mesh.RecalculateBounds();
      return mesh;
    }

    private void OnDestroy()
    {
      if (filter != null && filter.sharedMesh == visual) filter.sharedMesh = original;
      if (visual != null) Destroy(visual);
    }
  }
}
