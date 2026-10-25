using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace BallancePhysics.Api
{
  [Serializable, StructLayout(LayoutKind.Sequential)]
  public struct SurfaceState
  {
    public float wetness, snow, ice, sand, vegetation;
    public static SurfaceState Lerp(SurfaceState a, SurfaceState b, float t) => new SurfaceState {
      wetness = Mathf.Lerp(a.wetness, b.wetness, t), snow = Mathf.Lerp(a.snow, b.snow, t),
      ice = Mathf.Lerp(a.ice, b.ice, t), sand = Mathf.Lerp(a.sand, b.sand, t),
      vegetation = Mathf.Lerp(a.vegetation, b.vegetation, t)
    };
  }

  public enum SurfaceMaterial { None, WoodBall, StoneBall, PaperBall, Stone, Wood, Metal, Rubber }

  [Serializable, StructLayout(LayoutKind.Sequential)]
  public struct SurfaceRegion
  {
    public Vector3 center, extent;
    public float snow, ice, sand, vegetation;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct SurfaceContact
  {
    public SurfaceState state;
    public Vector3 point, normal;
    public float friction, resistance;
    public int supportId;
  }
}
