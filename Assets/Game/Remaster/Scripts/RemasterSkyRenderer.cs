using UnityEngine;
using UnityEngine.Rendering;

namespace Ballance2.Game.Remaster
{
  [RequireComponent(typeof(Camera))]
  [ExecuteAlways]
  public sealed class RemasterSkyRenderer : MonoBehaviour
  {
    private Camera source, skyCamera;
    private Skybox sourceSky, skyOnly;
    private Material original, cached, snapshot, capturedSource;
    private RenderTexture displayed, pending;
    private Vector3 capturePosition;
    private Vector4 capturedWeather, capturedTheme;
    private float capturedTime, capturedQuality, blendStart;
    private int nextFace = -1, lastFrame = -1;
    private bool initialized;
    private const float BlendDuration = .18f;

    public static int CubeSize(int quality) => quality <= 1 ? 128 : 256;

    private RenderTexture MakeCube(int size)
    {
      var texture = new RenderTexture(size,size,0,RenderTextureFormat.ARGB32) {
        dimension = TextureDimension.Cube, name = "Reballance cloud cache",
        filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
      };
      texture.Create(); return texture;
    }

    private void BeginCapture(Material material)
    {
      snapshot.CopyPropertiesFromMaterial(material);
      capturedSource = material; capturePosition = source.transform.position;
      capturedWeather = material.GetVector("_Weather"); capturedTheme = material.GetVector("_ThemeSky");
      capturedTime = material.GetFloat("_WeatherTime");
      capturedQuality = material.GetFloat("_CloudQuality");
      skyCamera.transform.SetPositionAndRotation(capturePosition,Quaternion.identity);
      skyOnly.material = snapshot; nextFace = 0;
    }

    private void Capture(int faces)
    {
      if (!skyCamera.RenderToCubemap(pending,faces))
        throw new System.InvalidOperationException("Cloud cubemap capture failed");
    }

    private void CompleteCapture()
    {
      var previous = displayed; displayed = pending; pending = previous;
      cached.SetTexture("_MainTex",displayed); cached.SetTexture("_PreviousTex",pending);
      blendStart = Time.unscaledTime; nextFace = -1; initialized = true;
    }

    private void OnPreCull()
    {
      if (source == null) { source = GetComponent<Camera>(); sourceSky = GetComponent<Skybox>(); }
      if (sourceSky == null || !sourceSky.enabled || source.clearFlags != CameraClearFlags.Skybox) return;
      var material = sourceSky.material;
      if (material == null || material.shader.name != "Ballance/Remaster/LayeredSky" || material.GetFloat("_CloudQuality") <= 0) return;
      if (skyCamera == null) {
        var holder = new GameObject("Reballance sky capture") { hideFlags = HideFlags.HideAndDontSave };
        skyCamera = holder.AddComponent<Camera>(); skyCamera.enabled = false;
        skyCamera.cullingMask = 0; skyCamera.clearFlags = CameraClearFlags.Skybox;
        skyCamera.allowHDR = false; skyCamera.allowMSAA = false;
        skyOnly = holder.AddComponent<Skybox>();
        snapshot = new Material(material) { hideFlags = HideFlags.HideAndDontSave };
        cached = new Material(Resources.Load<Shader>("Remaster/CachedSky")) { hideFlags = HideFlags.HideAndDontSave };
      }
      int size = CubeSize(Mathf.RoundToInt(material.GetFloat("_CloudQuality")));
      if (displayed == null || displayed.width != size) {
        ReleaseTextures(); displayed = MakeCube(size); pending = MakeCube(size); initialized = false;
      }
      if (!initialized || !Application.isPlaying) {
        BeginCapture(material); Capture(63); CompleteCapture(); blendStart -= BlendDuration;
      } else if (lastFrame != Time.frameCount) {
        lastFrame = Time.frameCount;
        if (nextFace < 0 && Time.unscaledTime-blendStart >= BlendDuration &&
            (capturedSource != material || material.GetFloat("_CloudQuality") != capturedQuality ||
             (source.transform.position-capturePosition).sqrMagnitude > .25f ||
             Mathf.Abs(material.GetFloat("_WeatherTime")-capturedTime) > .25f ||
             (material.GetVector("_Weather")-capturedWeather).sqrMagnitude > .00001f ||
             (material.GetVector("_ThemeSky")-capturedTheme).sqrMagnitude > .00001f)) BeginCapture(material);
        if (nextFace >= 0) {
          Capture(1 << nextFace);
          if (++nextFace == 6) CompleteCapture();
        }
      }
      cached.SetFloat("_Blend",Mathf.Clamp01((Time.unscaledTime-blendStart)/BlendDuration));
      original = material; sourceSky.material = cached;
    }

    private void OnPostRender()
    {
      if (sourceSky != null && sourceSky.material == cached) sourceSky.material = original;
      original = null;
    }

    private void ReleaseTextures()
    {
      foreach (var texture in new[] { displayed,pending })
        if (texture != null) { texture.Release(); Release(texture); }
      displayed = pending = null;
    }

    private void OnDisable()
    {
      OnPostRender(); ReleaseTextures();
      if (skyCamera != null) Release(skyCamera.gameObject);
      if (cached != null) Release(cached);
      if (snapshot != null) Release(snapshot);
      skyCamera = null; cached = snapshot = null; initialized = false;
    }

    private static void Release(Object resource)
    {
      if (Application.isPlaying) Destroy(resource); else DestroyImmediate(resource);
    }
  }
}
