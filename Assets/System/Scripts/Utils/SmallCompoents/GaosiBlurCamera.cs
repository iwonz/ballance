using UnityEngine;

namespace Ballance2.Utils.SmallCompoents
{
  public class GaosiBlurCamera : MonoBehaviour
  {
    public Shader gaosiShader;

    private Material gaosiMaterial;
    // Blur pass count.
    [Range(0, 4)]
    public int iterations = 3;

    // Higher values increase blur.
    [Range(0.2f, 3.0f)]
    public float blurSpread = 0.6f;

    // Reduce blur sampling resolution to improve performance at the cost of quality.
    [Range(1, 8)]
    public int downSample = 2;

    private void GaosiBlur(RenderTexture source, RenderTexture destination)
    {
      int rtW = source.width / downSample;
      int rtH = source.height / downSample;

      RenderTexture buffer0 = RenderTexture.GetTemporary(rtW, rtH, 0);
      buffer0.filterMode = FilterMode.Bilinear;

      Graphics.Blit(source, buffer0);

      for (int i = 0; i < iterations; i++)
      {
        gaosiMaterial.SetFloat("_BlurSize", 1.0f + i * blurSpread);

        RenderTexture buffer1 = RenderTexture.GetTemporary(rtW, rtH, 0);

        // Render the vertical pass
        Graphics.Blit(buffer0, buffer1, gaosiMaterial, 0);

        RenderTexture.ReleaseTemporary(buffer0);
        buffer0 = buffer1;
        buffer1 = RenderTexture.GetTemporary(rtW, rtH, 0);

        // Render the horizontal pass
        Graphics.Blit(buffer0, buffer1, gaosiMaterial, 1);

        RenderTexture.ReleaseTemporary(buffer0);
        buffer0 = buffer1;
      }

      Graphics.Blit(buffer0, destination);
      RenderTexture.ReleaseTemporary(buffer0);
    }

  }
}