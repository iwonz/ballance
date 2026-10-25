using Ballance2.Services.InputManager;
using UnityEngine;
using UnityEngine.UI;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* Toggle.cs
* 
* Purpose:
* Toggle component distinct from the standard checkbox.
*
* Author:
* mengyu
*/

namespace Ballance2.UI.Core.Controls
{
  /// <summary>
  /// Toggle component.
  /// </summary>
  [ExecuteInEditMode]
  [AddComponentMenu("Ballance/UI/Controls/ToggleEx")]
  public class ToggleEx : MonoBehaviour
  {
    public RectTransform Drag;
    public RectTransform Background;
    public Image DragImage;
    public Sprite ActiveImage;
    public Sprite DeactiveImage;
    public Image CheckedButton;
    public Image UnCheckedButton;
    public I18NText Text;

    public Toggle.ToggleEvent onValueChanged;
    public bool UseButtonStyle = false;

    void Start()
    {
      if (Background != null)
        EventTriggerListener.Get(Background.gameObject).onClick = (go) => { isOn = !isOn; };
      if (CheckedButton != null)
      {
        var btn = UnCheckedButton.gameObject.GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(() => isOn = false);
        btn = CheckedButton.gameObject.GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(() => isOn = true);
      }
      UpdateOn();
    }

    [SerializeField, HideInInspector]
    private bool on = false;

    public void UpdateOn()
    {
      var active = Ballance2.Game.Remaster.RemasterTheme.Accent;
      var inactive = Ballance2.Game.Remaster.RemasterTheme.Control;
      if (!UseButtonStyle)
      {
        Ballance2.Game.Remaster.RemasterTheme.Surface(Background.GetComponent<Image>(), inactive);
        Ballance2.Game.Remaster.RemasterIcons.Set(DragImage, on ? Ballance2.Game.Remaster.RemasterIcon.Check : Ballance2.Game.Remaster.RemasterIcon.Minus);
        DragImage.color = on ? Color.white : new Color(.55f, .63f, .65f);
        Drag.anchoredPosition = new Vector2(
            on ? 31.6f : 10.5f,
            Drag.anchoredPosition.y
        );
      }
      else
      {
        SetButtonColor(CheckedButton, on, active, inactive);
        SetButtonColor(UnCheckedButton, !on, active, inactive);
      }
    }

    private static void SetButtonColor(Image image, bool selected, Color active, Color inactive)
    {
      Ballance2.Game.Remaster.RemasterTheme.Surface(image, Color.white);
      var button = image.GetComponent<Button>();
      var colors = button.colors; colors.normalColor = selected ? active : inactive;
      colors.highlightedColor = colors.selectedColor = new Color(.4f, .62f, .6f);
      button.colors = colors;
      var label = image.GetComponentInChildren<Text>();
      if (label != null) label.color = selected ? new Color(.035f, .085f, .11f) : new Color(.91f, .95f, .94f);
    }

    public bool isOn
    {
      get { return on; }
      set
      {
        if (on != value)
        {
          on = value;
          if (onValueChanged != null)
            onValueChanged.Invoke(on);
          UpdateOn();
        }
      }
    }
  }
}
