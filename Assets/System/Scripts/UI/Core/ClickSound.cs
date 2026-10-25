using UnityEngine.EventSystems;
using UnityEngine;
using Ballance2.Services;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* ClickSound.cs
* 
* Purpose:
* Play a click sound on UI controls.
*
* Author:
* mengyu
*/

namespace Ballance2.UI.Core
{
  /// <summary>
  /// Click sound component.
  /// </summary>
  [Tooltip("Play sound on click.")]
  [AddComponentMenu("Ballance/UI/Controls/ClickSound")]
  public class ClickSound : UIBehaviour, ISelectHandler, IPointerClickHandler
  {
    [Tooltip("Sound resource name in the format accepted by GameSoundManager.")]
    public string SoundName = "";
    [Tooltip("Play sound when selected.")]
    public bool HasSelectSound = true;
    [Tooltip("Play sound when clicked.")]
    public bool HasClickSound = true;

    private GameSoundManager GameSoundManager;
    private bool LastSelect = false;

    protected override void Start()
    {
      GameSoundManager = GameSystem.GetSystemService<GameSoundManager>();
    }

    public void OnSelect(BaseEventData eventData)
    {
      if (HasSelectSound && !string.IsNullOrEmpty(SoundName)) {
        GameSoundManager.PlayFastVoice(SoundName, GameSoundType.UI);
        LastSelect = true;
      }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
      if(LastSelect) {
        LastSelect = false;
        return;
      }
      if (HasClickSound && !string.IsNullOrEmpty(SoundName))
        GameSoundManager.PlayFastVoice(SoundName, GameSoundType.UI);
    }
  }
}