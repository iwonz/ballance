using Ballance2.Game.GamePlay;
using Ballance2.Menu;
using Ballance2.Services.I18N;
using UnityEngine;
using UnityEngine.UI;

namespace Ballance2.Game.Remaster
{
  public sealed class RemasterHud : MonoBehaviour
  {
    private Text lives;
    private int previousLives = int.MinValue;
    private CanvasGroup visibility;

    private static void Contrast(Text text)
    {
      var shadow = text.GetComponent<Shadow>() ?? text.gameObject.AddComponent<Shadow>();
      shadow.effectColor = new Color(.025f, .055f, .075f, .85f);
      shadow.effectDistance = new Vector2(0, -1.5f);
      var outline = text.GetComponent<Outline>() ?? text.gameObject.AddComponent<Outline>();
      outline.effectColor = new Color(.025f, .055f, .075f, .45f);
      outline.effectDistance = new Vector2(.7f, -.7f);
      text.raycastTarget = false;
    }

    private static void Counter(RectTransform root, Text value, string caption, RemasterIcon symbol, bool right)
    {
      float left = right ? 0 : .22f, end = right ? .78f : 1;
      var alignment = right ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
      RemasterTheme.Place(value.rectTransform, left, 0, end, .72f);
      value.font = RemasterTheme.Font; value.fontSize = 32; value.fontStyle = FontStyle.Normal;
      value.resizeTextForBestFit = true; value.resizeTextMinSize = 18; value.resizeTextMaxSize = 32;
      value.color = new Color(.97f, .98f, .97f); value.alignment = alignment;
      Contrast(value);
      var label = RemasterTheme.Label(root, "Counter caption", 11, alignment);
      label.resizeTextMinSize = 9; label.text = caption.ToUpperInvariant();
      label.color = new Color(.79f, .89f, .86f);
      RemasterTheme.Place(label.rectTransform, left, .70f, end, 1); Contrast(label);
      var icon = RemasterIcons.Add(root, "Counter icon", symbol);
      RemasterTheme.Place(icon.rectTransform, right ? .82f : 0, .03f, right ? 1 : .18f, .72f);
    }

    public static void Install(GamePlayUIControl control)
    {
      var hud = control.gameObject.AddComponent<RemasterHud>();
      hud.visibility = control.GetComponent<CanvasGroup>() ?? control.gameObject.AddComponent<CanvasGroup>();
      var score = control._ScoreText.rectTransform.parent as RectTransform;
      RemasterTheme.Place(score, .028f, .89f, .19f, .975f);
      score.GetComponent<Image>().enabled = false;
      control._ScoreBoardActive.sprite = null;
      control._ScoreBoardActive.color = new Color(.55f, .83f, .74f, .8f);
      RemasterTheme.Place(control._ScoreBoardActive.rectTransform, .22f, 0, 1, .025f);
      Counter(score, control._ScoreText, I18N.Tr("core.ui.Remaster.HudScore"), RemasterIcon.Score, false);
      // Keep serialized life controls alive for existing game events, but replace their presentation.
      foreach (string name in new[] { "LifeBoard", "LifeBoardBaffleHost", "LifeBalls" }) {
        var child = control.transform.Find(name);
        if (child != null) child.gameObject.SetActive(false);
      }
      var lifePanel = new GameObject("Life summary", typeof(RectTransform)).GetComponent<RectTransform>();
      lifePanel.SetParent(control.transform, false);
      RemasterTheme.Place(lifePanel, .81f, .89f, .972f, .975f);
      hud.lives = RemasterTheme.Label(lifePanel, "Lives", 32, TextAnchor.MiddleRight);
      Counter(lifePanel, hud.lives, I18N.Tr("core.ui.Remaster.HudLives"), RemasterIcon.Life, true);

    }

    private void Update()
    {
      var game = GamePlayManager.Instance;
      if (game == null || lives == null) return;
      visibility.alpha = game._IsGamePlaying ? 1 : 0;
      visibility.blocksRaycasts = game._IsGamePlaying;
      if (game.CurrentLife != previousLives) {
        previousLives = game.CurrentLife;
        lives.text = previousLives < 0 ? "∞" : previousLives.ToString();
      }

    }
  }
}
