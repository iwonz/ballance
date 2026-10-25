using Ballance2.Package;
using Ballance2.Services;
using Ballance2.Utils;
using UnityEngine;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* SkyBoxUtils.cs
* 
* Purpose:
* Skybox generation helpers.
*
* Author:
* mengyu
*/

namespace Ballance2.Game.Utils
{
  /// <summary>
  /// Skybox generator.
  /// </summary>
  public static class SkyBoxUtils
  {
    private const string TAG = "SkyBoxUtils";

    private static GamePackage skyAssetPack = null;
    private static GamePackageManager GamePackageManager = null;

    /// <summary>
    /// Create a predefined skybox.
    /// </summary>
    /// <param name="s">Skybox name A-K, matching the original game's eleven skies.</param>
    /// <returns>The generated skybox material.</returns>
    public static Material MakeSkyBox(string s, bool remaster = false)
    {
      if (GamePackageManager == null) GamePackageManager = GameManager.GetSystemService<GamePackageManager>();
      if (skyAssetPack == null) skyAssetPack = GamePackageManager.FindPackage("core.assets.skys");
      if (skyAssetPack == null)
      {
        Log.E(TAG, "MakeSkyBox failed because skybase pack core.assets.skys not load !");
        return null;
      }

      Texture SkyLeft = skyAssetPack.GetAsset<Texture>("Sky_" + s + "_Left.BMP");
      Texture SkyRight = skyAssetPack.GetAsset<Texture>("Sky_" + s + "_Right.BMP");
      Texture SkyFront = skyAssetPack.GetAsset<Texture>("Sky_" + s + "_Front.BMP");
      Texture SkyBack = skyAssetPack.GetAsset<Texture>("Sky_" + s + "_Back.BMP");
      Texture SkyDown = skyAssetPack.GetAsset<Texture>("Sky_" + s + "_Down.BMP");

      var material = MakeCustomSkyBox(SkyLeft, SkyRight, SkyFront, SkyBack, SkyDown, null);
      if (remaster) {
        material.shader = Resources.Load<Shader>("Remaster/LayeredSky");
        material.SetTexture("_UpTex",SkyFront);
        material.SetTexture("_CloudVolume",Resources.Load<Texture3D>("Remaster/CloudDensity"));
        material.SetTexture("_CloudGroups",Resources.Load<Texture2D>("Remaster/CloudGroups"));
        material.SetFloat("_CloudBase",-300);
        material.SetVector("_CloudSun",-GameManager.GameLight.transform.forward);
      }
      return material;
    }
    /// <summary>
    /// Create a custom skybox.
    /// </summary>
    /// <param name="SkyLeft">Left image.</param>
    /// <param name="SkyRight">Right image.</param>
    /// <param name="SkyFront">Front image.</param>
    /// <param name="SkyBack">Back image.</param>
    /// <param name="SkyDown">Bottom image.</param>
    /// <param name="SkyTop">Top image.</param>
    public static Material MakeCustomSkyBox(Texture SkyLeft, Texture SkyRight, Texture SkyFront, Texture SkyBack, Texture SkyDown, Texture SkyTop)
    {
      Material m = new Material(Shader.Find("Skybox/6 Sided"));
      m.SetTexture("_FrontTex", SkyFront);
      m.SetTexture("_BackTex", SkyBack);
      m.SetTexture("_LeftTex", SkyRight);
      m.SetTexture("_RightTex", SkyLeft);
      m.SetTexture("_DownTex", SkyDown);
      m.SetTexture("_TopTex", SkyTop);
      return m;
    }
  }
}
