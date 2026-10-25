using UnityEngine;
using UnityEngine.UI;

namespace Ballance2.Game.Remaster
{
  public sealed class RemasterIntro : MonoBehaviour
  {
    private RawImage left, right;
    private float elapsed;

    public static RawImage Artwork(Transform parent)
    {
      var image = new GameObject("Remaster artwork", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter)).GetComponent<RawImage>();
      image.transform.SetParent(parent, false);
      image.texture = Resources.Load<Texture2D>("Remaster/TitleArtwork");
      image.raycastTarget = false;
      var fit = image.GetComponent<AspectRatioFitter>();
      fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
      fit.aspectRatio = (float)image.texture.width / image.texture.height;
      return image;
    }

    public static void Install(RectTransform root)
    {
      foreach (var animator in root.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
      foreach (Transform child in root) child.gameObject.SetActive(false);
      root.GetComponent<Image>().color = new Color(.035f, .085f, .11f);
      Artwork(root);
      var intro = root.gameObject.AddComponent<RemasterIntro>();
      intro.left = intro.Cloud("Cloud left", false);
      intro.right = intro.Cloud("Cloud right", true);
      intro.Animate(0);
    }

    private RawImage Cloud(string name, bool mirror)
    {
      var cloud = new GameObject(name, typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
      cloud.transform.SetParent(transform, false);
      cloud.texture = Resources.Load<Texture2D>("Remaster/IntroCloud");
      cloud.uvRect = mirror ? new Rect(1, 0, -1, 1) : new Rect(0, 0, 1, 1);
      cloud.raycastTarget = false;
      return cloud;
    }

    public void Animate(float seconds)
    {
      float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01((seconds - .4f) / 3.5f));
      RemasterTheme.Place(left.rectTransform, -.3f - t * .7f, -.15f, .8f - t * .7f, 1.15f);
      RemasterTheme.Place(right.rectTransform, .2f + t * .7f, -.15f, 1.3f + t * .7f, 1.15f);
      left.color = right.color = new Color(1, 1, 1, 1 - t);
    }

    private void Update() { elapsed += Time.unscaledDeltaTime; Animate(elapsed); }
  }
}
