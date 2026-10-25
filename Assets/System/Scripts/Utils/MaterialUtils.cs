using UnityEngine;

/*
* Copyright(c) 2021 imengyu
*
* Module:
* MaterialUtils.cs
* 
* Purpose:
* Material helpers.
*
* Author:
* mengyu
*/

namespace Ballance2.Utils
{
  /// <summary>
  /// Material utility methods.
  /// </summary>
  
  
  public static class MaterialUtils
  {
    /// <summary>
    /// Material rendering mode matching the four Unity inspector modes.
    /// </summary>
    public enum RenderingMode
    {
      Opaque,
      Cutout,
      Fade,
      Transparent,
    }

    /// <summary>
    /// Set a material's rendering mode.
    /// </summary>
    /// <param name="material">Material to update.</param>
    /// <param name="renderingMode">Rendering mode.</param>
    public static void SetMaterialRenderingMode(Material material, RenderingMode renderingMode)
    {
      switch (renderingMode)
      {
        case RenderingMode.Opaque:
          material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
          material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
          material.SetInt("_ZWrite", 1);
          material.DisableKeyword("_ALPHATEST_ON");
          material.DisableKeyword("_ALPHABLEND_ON");
          material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
          material.renderQueue = -1;
          break;
        case RenderingMode.Cutout:
          material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
          material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
          material.SetInt("_ZWrite", 1);
          material.EnableKeyword("_ALPHATEST_ON");
          material.DisableKeyword("_ALPHABLEND_ON");
          material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
          material.renderQueue = 2450;
          break;
        case RenderingMode.Fade:
          material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
          material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
          material.SetInt("_ZWrite", 0);
          material.DisableKeyword("_ALPHATEST_ON");
          material.EnableKeyword("_ALPHABLEND_ON");
          material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
          material.renderQueue = 3000;
          break;
        case RenderingMode.Transparent:
          material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
          material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
          material.SetInt("_ZWrite", 0);
          material.DisableKeyword("_ALPHATEST_ON");
          material.DisableKeyword("_ALPHABLEND_ON");
          material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
          material.renderQueue = 3000;
          break;
      }
    }
    /// <summary>
    /// Get a material's rendering mode.
    /// </summary>
    /// <param name="material">Material to inspect.</param>
    /// <returns>Material rendering mode.</returns>
    public static RenderingMode GetMaterialRenderingMode(Material material) {
      if(material.renderQueue == -1) return RenderingMode.Opaque;
      else if(material.renderQueue == 2450) return RenderingMode.Cutout;
      else {
        var _SrcBlend = material.renderQueue == 0 ? 0 : material.GetInt("_SrcBlend");
        var _DstBlend = material.renderQueue == 0 ? 0 : material.GetInt("_DstBlend");
        if(_SrcBlend == (int)UnityEngine.Rendering.BlendMode.SrcAlpha && _DstBlend == (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha)
          return RenderingMode.Fade;
        else if(_SrcBlend == (int)UnityEngine.Rendering.BlendMode.One && _DstBlend == (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha)
          return RenderingMode.Fade;
        return RenderingMode.Opaque;
      }
    } 
  }
}