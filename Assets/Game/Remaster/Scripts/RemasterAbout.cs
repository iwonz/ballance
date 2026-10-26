using UnityEngine;
using UnityEngine.UI;

namespace Ballance2.Game.Remaster
{
  public sealed class RemasterAbout : MonoBehaviour
  {
    private RectTransform root, artwork;
    private bool ready;

    public static void ReplaceIllustration(RectTransform root)
    {
      var old = root.Find("Scroll View/Viewport/Content/Image");
      if (old == null) return;
      old.gameObject.SetActive(false);
      var holder = new GameObject("Remaster illustration", typeof(RectTransform), typeof(LayoutElement));
      holder.transform.SetParent(old.parent, false); holder.transform.SetSiblingIndex(old.GetSiblingIndex());
      holder.GetComponent<LayoutElement>().preferredHeight = 180;
      RemasterIntro.Artwork(holder.transform);
      if (!old.parent.TryGetComponent(out VerticalLayoutGroup layout)) layout = old.parent.gameObject.AddComponent<VerticalLayoutGroup>();
      layout.childControlHeight = layout.childControlWidth = true;
      layout.childForceExpandHeight = false; layout.childForceExpandWidth = true; layout.spacing = 18;
      if (!old.parent.TryGetComponent(out ContentSizeFitter fit)) fit = old.parent.gameObject.AddComponent<ContentSizeFitter>();
      fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
      foreach (Transform child in old.parent) if (child.name == "Space") child.gameObject.SetActive(false);
      var text = old.parent.Find("Text").GetComponent<Text>();
      text.fontSize = 17; text.resizeTextForBestFit = false; text.alignment = TextAnchor.UpperLeft;
    }

    public static void Install(RectTransform root)
    {
      var view = root.gameObject.AddComponent<RemasterAbout>(); view.root = root;
      root.GetComponent<VerticalLayoutGroup>().enabled = false;
      foreach (Transform child in root)
        if (child.name == "Panel" || child.name == "ImageLogo3D" || child.name == "Space" || child.name == "Text") child.gameObject.SetActive(false);
      view.artwork = new GameObject("Title illustration", typeof(RectTransform)).GetComponent<RectTransform>();
      view.artwork.SetParent(root, false);
      RemasterIntro.Artwork(view.artwork);
      view.ready = true; view.Arrange();
    }

    private void OnRectTransformDimensionsChange() { if (ready) Arrange(); }
    private void Arrange()
    {
      bool portrait = root.rect.width < root.rect.height;
      RemasterTheme.Place(artwork, .04f, portrait ? .60f : .2f, portrait ? .96f : .61f, .94f);
      string[] buttons = { "ButtonProject", "ButtonCreators", "ButtonOpenSourceLicense", "ButtonVersion", "ButtonBack" };
      float left = portrait ? .08f : .65f, right = .92f, top = portrait ? .56f : .82f;
      float step = portrait ? .078f : .12f;
      for (int i = 0; i < buttons.Length; i++) {
        var rect = root.Find(buttons[i]) as RectTransform;
        if (rect != null) RemasterTheme.Place(rect, left, top - i * step - step * .78f, right, top - i * step);
      }
    }
  }
}
