using UnityEngine;
using UnityEngine.UI;
using Ballance2.Game.Remaster;

/*
* Copyright(c) 2022  mengyu
*
* Module:
* GameGlobalIngameLoading.cs
* 
* Purpose:
* Initial loading object behavior.
*
* Author:
* mengyu
*/

namespace Ballance2.UI.Utils
{
  public class GameGlobalIngameLoading : MonoBehaviour {
    public Text TextStatus;
    private Image activity;

    private void Awake() => Install();

    public void Install() {
      if (activity != null) return;
      foreach (Transform child in transform)
        if (child != TextStatus.transform) child.gameObject.SetActive(false);
      RemasterTheme.Place((RectTransform)transform, 0, 0, 1, 1);
      RemasterTheme.Apply(gameObject, true);
      GetComponent<Image>().color = new Color(.035f, .085f, .11f, 1);
      TextStatus.fontSize = 22; TextStatus.alignment = TextAnchor.MiddleCenter;
      RemasterTheme.Place(TextStatus.rectTransform, .1f, .4f, .9f, .48f);
      var title = RemasterTheme.Label(transform, "Loading title", 48, TextAnchor.MiddleCenter);
      title.text = "BALLANCE / REMASTER";
      RemasterTheme.Place(title.rectTransform, .1f, .52f, .9f, .65f);
      activity = new GameObject("Loading activity", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
      activity.transform.SetParent(transform, false);
      activity.color = new Color(.66f, .84f, .78f);
      activity.raycastTarget = false;
      RemasterTheme.Place(activity.rectTransform, .35f, .495f, .65f, .5f);
    }

    private void Update() {
      var color = activity.color;
      color.a = .55f + Mathf.Sin(Time.unscaledTime * 2) * .25f;
      activity.color = color;
    }
  }
}
