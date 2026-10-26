using UnityEngine;

/*
* Copyright(c) 2021 imengyu
*
* Module:
* TextureScroller.cs
* 
* Purpose:
* Scroll material textures; used for moving clouds.
* 
* Author:
* mengyu
*/

namespace Ballance2.Game
{
  /// <summary>
  /// Scroll material textures; used for moving clouds.
  /// </summary>
  public class TextureScroller : MonoBehaviour
  {
    [Tooltip("Material to scroll.")]
    /// <summary>
    /// Material to scroll.
    /// </summary>
    public Material Material;
    [Tooltip("Material scrolling speed.")]
    /// <summary>
    /// Texture scrolling speed.
    /// </summary>
    public Vector2 ScrollSpeed;

    private Vector2 scrollOff;

    private void Start()
    {
      if (Material == null)
      {
        var renderer = GetComponent<Renderer>();
        if (renderer)
          Material = renderer.sharedMaterial;
      }
      if (Material != null)
      {
        scrollOff = Material.GetTextureOffset("_MainTex");
      }
    }
    private void OnEnable() {
      var renderer = GetComponent<Renderer>();
      if (renderer)
        Material = renderer.sharedMaterial;
    }
    private void Update()
    {
      if (Material != null)
      {
        var x = scrollOff.x + ScrollSpeed.x * Time.deltaTime;
        var y = scrollOff.y + ScrollSpeed.y * Time.deltaTime;
        if (x < 1) x += 1; if (x > 1) x -= 1;
        if (y < 1) y += 1; if (y > 1) y -= 1;
        scrollOff = new Vector2(x, y);
        Material.SetTextureOffset("_MainTex", scrollOff);
      }
    }
  }
}