using UnityEngine;
using UnityEngine.UI;

namespace Ballance2.Game.Remaster
{
  public enum RemasterIcon { Ball, Cloud, Sound, Music, InfiniteLives, Restart, Speed, Score, Life, Right, Plus, Minus, Check, View, Package, Close }

  public static class RemasterIcons
  {
    private static readonly Sprite[] sprites = new Sprite[16];
    private static Texture2D atlas;

    public static Sprite Get(RemasterIcon icon)
    {
      int index = (int)icon;
      if (sprites[index] != null) return sprites[index];
      if (atlas == null) atlas = Resources.Load<Texture2D>("Remaster/InterfaceIcons");
      float width = atlas.width / 4f, height = atlas.height / 4f;
      return sprites[index] = Sprite.Create(atlas, new Rect(index % 4 * width, (3-index / 4) * height, width, height), new Vector2(.5f,.5f));
    }

    public static void Set(Image image, RemasterIcon icon)
    {
      image.sprite = Get(icon); image.overrideSprite = null; image.type = Image.Type.Simple;
      image.color = Color.white; image.preserveAspect = true; image.enabled = true;
    }

    public static Image Add(Transform parent, string name, RemasterIcon icon)
    {
      var image = parent.Find(name)?.GetComponent<Image>();
      if (image == null) {
        image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
      }
      Set(image, icon); image.raycastTarget = false;
      return image;
    }

    private static void Direction(Image image, float rotation)
    {
      bool visible = image.enabled;
      foreach (var child in image.GetComponentsInChildren<Image>(true))
        if (child != image) child.enabled = false;
      Set(image, RemasterIcon.Right);
      var center = image.rectTransform.TransformPoint(image.rectTransform.rect.center);
      image.rectTransform.pivot = new Vector2(.5f, .5f);
      image.rectTransform.position = center;
      image.rectTransform.localRotation = Quaternion.Euler(0, 0, rotation);
      image.enabled = visible;
    }

    public static void Keypad(GameObject root)
    {
      foreach (var pad in root.GetComponentsInChildren<Ballance2.Menu.Touch.SimpleTouchDirectionKeyController>(true)) {
        Direction(pad.ImageRight, 0); Direction(pad.ImageForwardRight, 45);
        Direction(pad.ImageForward, 90); Direction(pad.ImageForwardLeft, 135);
        Direction(pad.ImageLeft, 180); Direction(pad.ImageBackLeft, 225);
        Direction(pad.ImageBack, 270); Direction(pad.ImageBackRight, 315);
      }
      foreach (var stick in root.GetComponentsInChildren<Ballance2.Menu.Touch.KeyPadJoystickController>(true)) {
        Set(stick.JoystickImage, RemasterIcon.Ball);
        stick.Joystick.color = Color.clear;
      }
      foreach (var rect in root.GetComponentsInChildren<RectTransform>(true)) {
        float angle;
        var symbol = RemasterIcon.Right;
        switch (rect.name) {
          case "ButtonCamRight": angle = 0; break;
          case "ButtonCamLeft": angle = 180; break;
          case "ButtonUp": angle = 90; break;
          case "ButtonDown": angle = 270; break;
          case "ButtonSpace":
          case "ButtonSpaceShift": angle = 0; symbol = RemasterIcon.View; break;
          default: continue;
        }
        foreach (var text in rect.GetComponentsInChildren<Text>(true)) text.enabled = false;
        foreach (var image in rect.GetComponentsInChildren<Image>(true))
          if (image.transform != rect) image.enabled = false;
        RemasterTheme.Surface(rect.GetComponent<Image>(), Color.clear);
        var icon = Add(rect, "Direction icon", symbol);
        RemasterTheme.Place(icon.rectTransform, .15f, .15f, .85f, .85f);
        icon.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
      }
    }
  }
}
