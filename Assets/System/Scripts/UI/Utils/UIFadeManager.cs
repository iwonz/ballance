using System.Collections.Generic;
using Ballance2.Utils;
using UnityEngine;
using UnityEngine.UI;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* UIFadeManager.cs
* 
* Purpose:
* Fade manager.
* Manage and execute automated fades.
* GameUIManager owns an instance available through GameUIManager.UIFadeManager.
*
* Author:
* mengyu
*/

/*

Fade objects in/out and optionally show or hide them.
Supports Material, Material arrays, Image, Text, and AudioSource.

Fading a shared material affects all its users. Assign a separate material when only one object should fade.

Usage:

// Create a fade for this object and hide it when the fade finishes.
// A null final argument automatically finds the material on the object's MeshRenderer.
var fadeObject = AddFadeOut(gameObject, 1.0f, true, null);

// The returned fadeObject can be stopped or assigned a current fade value.
fadeObject.Delete(); // Stop.
fadeObject.ResetTo(0.5f); // Set the current fade value to 0.5.

*/

namespace Ballance2.UI.Utils
{
  /// <summary>
  /// Fade Material, Material arrays, Image, Text, or AudioSource, optionally showing or hiding objects.
  /// </summary>
  /// <remarks>
  /// Access the instance owned by GameUIManager through GameUIManager.UIFadeManager.
  /// Fading a shared material affects all its users; use a separate material for an independent fade.
  /// </remarks>
  /// <example>
  /// // Fade this object and hide it on completion.
  /// // A null final argument uses the material on this object's MeshRenderer.
  /// var fadeObject = GameUIManager.UIFadeManager.AddFadeOut(this.gameObject, 1.0f, true, null);
  /// // Stop the returned fadeObject or set its current fade value at any time.
  /// fadeObject.Delete(); // Stop.
  /// fadeObject.ResetTo(0.5f); // Set the current fade value to 0.5.
  /// </example>
  public class UIFadeManager : MonoBehaviour
  {
    public List<FadeObject> fadeObjects = new List<FadeObject>();

    private void Update()
    {
      if (fadeObjects.Count > 0)
      {
        FadeObject fadeObject = null;
        for (int i = fadeObjects.Count - 1; i >= 0; i--)
        {
          fadeObject = fadeObjects[i];
          if (fadeObject.runEnd) fadeObjects.Remove(fadeObject);
          else
          {
            if (fadeObject.fadeType == FadeType.FadeIn)
            {
              if (fadeObject.alpha < 1)
              {
                fadeObject.alpha += Time.deltaTime / fadeObject.timeInSecond * 1;
                if (fadeObject.material != null)
                  fadeObject.material.color = new Color(fadeObject.material.color.r, fadeObject.material.color.g, fadeObject.material.color.b, fadeObject.alpha);
                else if (fadeObject.materials != null && fadeObject.materials.Length > 0)
                {
                  foreach (Material m in fadeObject.materials)
                    m.color = new Color(m.color.r, m.color.g, m.color.b, fadeObject.alpha);
                }
                else if (fadeObject.image != null)
                  fadeObject.image.color = new Color(fadeObject.image.color.r, fadeObject.image.color.g, fadeObject.image.color.b, fadeObject.alpha);
                else if (fadeObject.text != null)
                  fadeObject.text.color = new Color(fadeObject.text.color.r, fadeObject.text.color.g, fadeObject.text.color.b, fadeObject.alpha);
                else if (fadeObject.audio != null)
                  fadeObject.audio.volume = fadeObject.alpha;
              }
              else fadeObject.runEnd = true;
            }
            else if (fadeObject.fadeType == FadeType.FadeOut)
            {
              if (fadeObject.alpha > 0)
              {
                fadeObject.alpha -= Time.deltaTime / fadeObject.timeInSecond * 1;
                if (fadeObject.material != null)
                  fadeObject.material.color = new Color(fadeObject.material.color.r, fadeObject.material.color.g, fadeObject.material.color.b, fadeObject.alpha);
                else if (fadeObject.materials != null && fadeObject.materials.Length > 0)
                {
                  foreach (Material m in fadeObject.materials)
                    m.color = new Color(m.color.r, m.color.g, m.color.b, fadeObject.alpha);
                }
                else if (fadeObject.image != null)
                  fadeObject.image.color = new Color(fadeObject.image.color.r, fadeObject.image.color.g, fadeObject.image.color.b, fadeObject.alpha);
                else if (fadeObject.text != null)
                  fadeObject.text.color = new Color(fadeObject.text.color.r, fadeObject.text.color.g, fadeObject.text.color.b, fadeObject.alpha);
                else if (fadeObject.audio != null)
                  fadeObject.audio.volume = fadeObject.alpha;
              }
              else
              {
                if (fadeObject.endReactive)
                {
                  if (fadeObject.gameObject != null)
                    fadeObject.gameObject.SetActive(false);
                }
                fadeObject.runEnd = true;

                // Restore the original material rendering mode.
                if(fadeObject.material != null && (fadeObject.material.shader == null || fadeObject.material.shader.name == "Standard")) {
                  if (fadeObject.material != null && fadeObject.oldMatRenderMode != MaterialUtils.RenderingMode.Fade)
                  {
                    MaterialUtils.SetMaterialRenderingMode(fadeObject.material, fadeObject.oldMatRenderMode);
                    fadeObject.oldMatRenderMode = MaterialUtils.RenderingMode.Fade;
                  }
                }
              }
            }
          }
        }
      }
    }

    private FadeObject FindFadeObjectByGameObject(GameObject gameObject, FadeType fadeType)
    {
      foreach (FadeObject o in fadeObjects)
      {
        if (o.gameObject == gameObject && fadeType == o.fadeType)
          return o;
      }
      return null;
    }
    private FadeObject FindFadeObjectByImage(Image image, FadeType fadeType)
    {
      foreach (FadeObject o in fadeObjects)
      {
        if (o.image == image && fadeType == o.fadeType)
          return o;
      }
      return null;
    }
    private FadeObject FindFadeObjectByText(Text text, FadeType fadeType)
    {
      foreach (FadeObject o in fadeObjects)
      {
        if (o.text == text && fadeType == o.fadeType)
          return o;
      }
      return null;
    }
    private FadeObject FindFadeObjectByAudio(AudioSource audio, FadeType fadeType)
    {
      foreach (FadeObject o in fadeObjects)
      {
        if (o.audio == audio && fadeType == o.fadeType)
          return o;
      }
      return null;
    }

    /// <summary>
    /// Run a fade-out animation.
    /// </summary>
    /// <param name="gameObject">Target object.</param>
    /// <param name="timeInSecond">Duration.</param>
    /// <param name="hidden">Deactivate the object when finished.</param>
    /// <param name="material">Target material.</param>
    public FadeObject AddFadeOut(GameObject gameObject, float timeInSecond, bool hidden, Material material)
    {
      FadeObject fadeObject = FindFadeObjectByGameObject(gameObject, FadeType.FadeOut);
      if (fadeObject != null)
        fadeObjects.Remove(fadeObject);

      fadeObject = new FadeObject(this);
      fadeObject.gameObject = gameObject;
      fadeObject.timeInSecond = timeInSecond;
      fadeObject.alpha = 1;
      if (material == null)
      {
        var meshRender = gameObject.GetComponent<MeshRenderer>();
        fadeObject.material = meshRender.material;
      }
      else fadeObject.material = material;
      fadeObject.fadeType = FadeType.FadeOut;
      if (fadeObject.material != null)
      {

        fadeObject.material.color = new Color(fadeObject.material.color.r, fadeObject.material.color.g, fadeObject.material.color.b, 1);
        if(fadeObject.material.shader == null || fadeObject.material.shader.name == "Standard") {
          // Switch the standard material to Fade mode if necessary.
          var oldRenderMode = MaterialUtils.GetMaterialRenderingMode(fadeObject.material);
          if (oldRenderMode != MaterialUtils.RenderingMode.Fade)
          {
            fadeObject.oldMatRenderMode = oldRenderMode;
            MaterialUtils.SetMaterialRenderingMode(fadeObject.material, MaterialUtils.RenderingMode.Fade);
          }
        }
      }
      fadeObject.endReactive = hidden;
      fadeObjects.Add(fadeObject);
      return fadeObject;
    }

    /// <summary>
    /// Run a fade-in animation.
    /// </summary>
    /// <param name="gameObject">Target object.</param>
    /// <param name="timeInSecond">Duration.</param>
    /// <param name="material">Target material.</param>
    public FadeObject AddFadeIn(GameObject gameObject, float timeInSecond, Material material)
    {
      FadeObject fadeObject = FindFadeObjectByGameObject(gameObject, FadeType.FadeIn);
      if (fadeObject != null)
        fadeObjects.Remove(fadeObject);

      fadeObject = new FadeObject(this);
      fadeObject.gameObject = gameObject;
      fadeObject.timeInSecond = timeInSecond;
      fadeObject.alpha = 0;
      if (material == null)
      {
        var meshRender = gameObject.GetComponent<MeshRenderer>();
        fadeObject.material = meshRender.material;
      }
      else fadeObject.material = material;
      fadeObject.fadeType = FadeType.FadeIn;
      if (fadeObject.material != null)
      {
        fadeObject.material.color = new Color(fadeObject.material.color.r, fadeObject.material.color.g, fadeObject.material.color.b, 0);
        if(fadeObject.material.shader == null || fadeObject.material.shader.name == "Standard") {
          // Set material rendering mode to Fade.
          var oldRenderMode = MaterialUtils.GetMaterialRenderingMode(fadeObject.material);
          if (oldRenderMode != MaterialUtils.RenderingMode.Fade)
          {
            fadeObject.oldMatRenderMode = oldRenderMode;
            MaterialUtils.SetMaterialRenderingMode(fadeObject.material, MaterialUtils.RenderingMode.Fade);
          }
        }
      }

      if (!gameObject.activeSelf)
        gameObject.SetActive(true);
      fadeObjects.Add(fadeObject);
      return fadeObject;
    }

    /// <summary>
    /// Run a fade-out animation.
    /// </summary>
    /// <param name="gameObject">Target object.</param>
    /// <param name="timeInSecond">Duration.</param>
    /// <param name="hidden">Deactivate the object when finished.</param>
    /// <param name="materials">Target materials.</param>
    public FadeObject AddFadeOut2(GameObject gameObject, float timeInSecond, bool hidden, Material[] materials)
    {
      FadeObject fadeObject = FindFadeObjectByGameObject(gameObject, FadeType.FadeOut);
      if (fadeObject != null)
        fadeObjects.Remove(fadeObject);

      fadeObject = new FadeObject(this);
      fadeObject.gameObject = gameObject;
      fadeObject.timeInSecond = timeInSecond;
      fadeObject.alpha = 1;
      if (materials == null)
      {
        var meshRender = gameObject.GetComponent<MeshRenderer>();
        fadeObject.materials = meshRender.materials;
      }
      else fadeObject.materials = materials;
      fadeObject.fadeType = FadeType.FadeOut;
      if (fadeObject.materials != null && fadeObject.materials.Length > 0)
        foreach (Material m in fadeObject.materials)
          m.color = new Color(m.color.r, m.color.g, m.color.b, 1);
      fadeObject.endReactive = hidden;
      fadeObjects.Add(fadeObject);
      return fadeObject;
    }

    /// <summary>
    /// Run a fade-in animation.
    /// </summary>
    /// <param name="gameObject">Target object.</param>
    /// <param name="timeInSecond">Duration.</param>
    /// <param name="material">Target material.</param>
    public FadeObject AddFadeIn2(GameObject gameObject, float timeInSecond, Material[] materials)
    {
      FadeObject fadeObject = FindFadeObjectByGameObject(gameObject, FadeType.FadeIn);
      if (fadeObject != null)
        fadeObjects.Remove(fadeObject);

      fadeObject = new FadeObject(this);
      fadeObject.gameObject = gameObject;
      fadeObject.timeInSecond = timeInSecond;
      fadeObject.alpha = 0;
      if (materials == null)
      {
        var meshRender = gameObject.GetComponent<MeshRenderer>();
        fadeObject.materials = meshRender.materials;
      }
      else fadeObject.materials = materials;
      fadeObject.fadeType = FadeType.FadeIn;
      if (fadeObject.materials != null && fadeObject.materials.Length > 0)
        foreach (Material m in fadeObject.materials)
          m.color = new Color(m.color.r, m.color.g, m.color.b, 0);
      if (!gameObject.activeSelf)
        gameObject.SetActive(true);
      fadeObjects.Add(fadeObject);
      return fadeObject;
    }

    /// <summary>
    /// Run a fade-out animation.
    /// </summary>
    /// <param name="image">Target image.</param>
    /// <param name="timeInSecond">Duration.</param>
    /// <param name="hidden">Deactivate the object when finished.</param>
    public FadeObject AddFadeOut(Image image, float timeInSecond, bool hidden)
    {
      if (image != null)
      {
        FadeObject fadeObject = FindFadeObjectByImage(image, FadeType.FadeOut);
        if (fadeObject != null)
          fadeObjects.Remove(fadeObject);

        fadeObject = new FadeObject(this);
        fadeObject.gameObject = image.gameObject;
        fadeObject.timeInSecond = timeInSecond;
        fadeObject.alpha = 1;
        fadeObject.material = null;
        fadeObject.image = image;
        fadeObject.fadeType = FadeType.FadeOut;
        fadeObject.endReactive = hidden;
        fadeObjects.Add(fadeObject);

        image.color = new Color(image.color.r, image.color.g, image.color.b, 1);
        return fadeObject;
      }
      return null;
    }

    /// <summary>
    /// Run a fade-in animation.
    /// </summary>
    /// <param name="image">Target image.</param>
    /// <param name="timeInSecond">Duration.</param>
    public FadeObject AddFadeIn(Image image, float timeInSecond)
    {
      if (image != null)
      {
        FadeObject fadeObject = FindFadeObjectByImage(image, FadeType.FadeIn);
        if (fadeObject != null)
          fadeObjects.Remove(fadeObject);

        fadeObject = new FadeObject(this);
        fadeObject.gameObject = image.gameObject;
        fadeObject.timeInSecond = timeInSecond;
        fadeObject.alpha = 0;
        fadeObject.material = null;
        fadeObject.image = image;
        fadeObject.fadeType = FadeType.FadeIn;
        fadeObjects.Add(fadeObject);

        image.color = new Color(image.color.r, image.color.g, image.color.b, 0);
        if (!image.gameObject.activeSelf)
          image.gameObject.SetActive(true);
        return fadeObject;
      }
      return null;
    }

    /// <summary>
    /// Run a fade-out animation.
    /// </summary>
    /// <param name="text">Target text.</param>
    /// <param name="timeInSecond">Duration.</param>
    /// <param name="hidden">Deactivate the object when finished.</param>
    public FadeObject AddFadeOut(Text text, float timeInSecond, bool hidden)
    {
      if (text != null)
      {
        FadeObject fadeObject = FindFadeObjectByText(text, FadeType.FadeOut);
        if (fadeObject != null)
          fadeObjects.Remove(fadeObject);

        fadeObject = new FadeObject(this);
        fadeObject.gameObject = text.gameObject;
        fadeObject.timeInSecond = timeInSecond;
        fadeObject.alpha = 1;
        fadeObject.material = null;
        fadeObject.text = text;
        fadeObject.fadeType = FadeType.FadeOut;
        fadeObject.endReactive = hidden;
        fadeObjects.Add(fadeObject);

        text.color = new Color(text.color.r, text.color.g, text.color.b, 1);
        return fadeObject;
      }
      return null;
    }

    /// <summary>
    /// Run a fade-in animation.
    /// </summary>
    /// <param name="text">Target text.</param>
    /// <param name="timeInSecond">Duration.</param>
    public FadeObject AddFadeIn(Text text, float timeInSecond)
    {
      if (text != null)
      {
        FadeObject fadeObject = FindFadeObjectByText(text, FadeType.FadeIn);
        if (fadeObject != null)
          fadeObjects.Remove(fadeObject);

        fadeObject = new FadeObject(this);
        fadeObject.gameObject = text.gameObject;
        fadeObject.timeInSecond = timeInSecond;
        fadeObject.alpha = 0;
        fadeObject.material = null;
        fadeObject.text = text;
        fadeObject.fadeType = FadeType.FadeIn;
        fadeObjects.Add(fadeObject);

        text.color = new Color(text.color.r, text.color.g, text.color.b, 0);
        if (!text.gameObject.activeSelf)
          text.gameObject.SetActive(true);
        return fadeObject;
      }
      return null;
    }

    /// <summary>
    /// Fade audio out.
    /// </summary>
    /// <param name="audio">Target audio source.</param>
    /// <param name="timeInSecond">Duration.</param>
    public FadeObject AddAudioFadeOut(AudioSource audio, float timeInSecond)
    {
      if (audio != null)
      {
        FadeObject fadeObject = FindFadeObjectByAudio(audio, FadeType.FadeOut);
        if (fadeObject != null)
          fadeObjects.Remove(fadeObject);

        fadeObject = new FadeObject(this);
        fadeObject.gameObject = audio.gameObject;
        fadeObject.timeInSecond = timeInSecond;
        fadeObject.alpha = 1;
        fadeObject.material = null;
        fadeObject.audio = audio;
        fadeObject.fadeType = FadeType.FadeOut;
        fadeObject.endReactive = false;
        fadeObjects.Add(fadeObject);

        audio.volume = 1;
        return fadeObject;
      }
      return null;
    }

    /// <summary>
    /// Fade audio in.
    /// </summary>
    /// <param name="audio">Target audio source.</param>
    /// <param name="timeInSecond">Duration.</param>
    public FadeObject AddAudioFadeIn(AudioSource audio, float timeInSecond)
    {
      if (audio != null)
      {
        FadeObject fadeObject = FindFadeObjectByAudio(audio, FadeType.FadeIn);
        if (fadeObject != null)
          fadeObjects.Remove(fadeObject);

        fadeObject = new FadeObject(this);
        fadeObject.gameObject = audio.gameObject;
        fadeObject.timeInSecond = timeInSecond;
        fadeObject.alpha = 0;
        fadeObject.material = null;
        fadeObject.audio = audio;
        fadeObject.fadeType = FadeType.FadeIn;
        fadeObjects.Add(fadeObject);

        audio.volume = 0;
        if (!audio.gameObject.activeSelf)
          audio.gameObject.SetActive(true);
        return fadeObject;
      }
      return null;
    }

  }

  /// <summary>
  /// Fade type.
  /// </summary>
  public enum FadeType
  {
    None,
    FadeIn,
    FadeOut,
  }

  /// <summary>
  /// Fade operation state.
  /// </summary>
  public class FadeObject
  {
    internal FadeObject(UIFadeManager fadeManager) {
      this.fadeManager = fadeManager;
    }

    
    public UIFadeManager fadeManager { get; }
    
    public GameObject gameObject;
    
    public Material material;
    
    public Material[] materials;
    
    public Image image;
    
    public Text text;
    
    public AudioSource audio;
    
    public float alpha;
    
    public float timeInSecond;
    
    public bool endReactive;
    
    public bool runEnd = false;
    
    public MaterialUtils.RenderingMode oldMatRenderMode = MaterialUtils.RenderingMode.Fade;
    
    public FadeType fadeType;

    /// <summary>
    /// Force the fade alpha to a value.
    /// </summary>
    /// <param name="alpha">Alpha value.</param>
    public void ResetTo(float alpha) {
      this.alpha = alpha;

      if (material != null)
        material.color = new Color(material.color.r, material.color.g, material.color.b, alpha);
      else if (materials != null && materials.Length > 0)
      {
        foreach (Material m in materials)
          m.color = new Color(m.color.r, m.color.g, m.color.b, alpha);
      }
      else if (image != null)
        image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
      else if (text != null)
        text.color = new Color(text.color.r, text.color.g, text.color.b, alpha);
      else if (audio != null)
        audio.volume = alpha;
    }

    /// <summary>
    /// Remove this fade.
    /// </summary>
    public void Delete() {
      fadeManager.fadeObjects.Remove(this);
    }
  }
}