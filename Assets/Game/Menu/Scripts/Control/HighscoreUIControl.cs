using System.Collections.Generic;
using Ballance2.Base;
using Ballance2.Game.GamePlay;
using Ballance2.Game.Remaster;
using Ballance2.Services.I18N;
using UnityEngine;
using UnityEngine.UI;

namespace Ballance2.Menu
{
  public class HighscoreUIControl : GameSingletonBehavior<HighscoreUIControl>  {
    public Text TextLevelName;

    private int _CurrentIndex = 0;
    private List<TextItemData> _TextItem = new List<TextItemData>();
    private List<string> LevelNames;
    private Text title;
    private bool styled;

    private class TextItemData {
      public Text textName;
      public Text testScore;
    }

    public void Install() {
      if (styled) return;
      styled = true;
      var layout = GetComponent<VerticalLayoutGroup>();
      if (layout != null) layout.enabled = false;
      foreach (Transform child in transform) if (child.name == "Space") child.gameObject.SetActive(false);
      title = transform.Find("Panel/Text").GetComponent<Text>();
      title.transform.SetParent(transform, false); title.fontSize = 36;
      _TextItem.Clear();
      for (int i = 1; i <= 10; i++)
      {
         _TextItem.Add(new TextItemData {
          textName = transform.Find($"ItemHighscore{i}/TextValue").GetComponent<Text>(),
          testScore = transform.Find($"ItemHighscore{i}/TextScoreValue").GetComponent<Text>()
        });
        var row = transform.Find($"ItemHighscore{i}");
        RemasterTheme.Surface(row.GetComponent<Image>(), i <= 3 ? new Color(.16f, .28f, .29f) : new Color(.08f, .16f, .19f, i % 2 == 0 ? .9f : .5f));
        var rank = row.Find("Text").GetComponent<Text>();
        rank.text = i.ToString("00"); rank.color = i <= 3 ? new Color(.94f, .78f, .47f) : new Color(.54f, .67f, .69f);
        foreach (var text in row.GetComponentsInChildren<Text>()) {
          text.fontSize = 22; text.resizeTextForBestFit = true; text.resizeTextMinSize = 14; text.resizeTextMaxSize = 22;
        }
        RemasterTheme.Place(rank.rectTransform, .03f, 0, .13f, 1);
        RemasterTheme.Place(_TextItem[i-1].textName.rectTransform, .17f, 0, .70f, 1);
        RemasterTheme.Place(_TextItem[i-1].testScore.rectTransform, .72f, 0, .97f, 1);
        _TextItem[i-1].textName.alignment = TextAnchor.MiddleLeft;
        _TextItem[i-1].testScore.alignment = TextAnchor.MiddleRight;
      }
      RemasterTheme.Arrow(transform.Find("Panel/ButtonPageLeft").GetComponent<Button>(), "‹");
      RemasterTheme.Arrow(transform.Find("Panel/ButtonPageRight").GetComponent<Button>(), "›");
      Arrange();
    }

    private void Start() { Install(); }
    private void OnRectTransformDimensionsChange() { if (styled) Arrange(); }
    private void Arrange() {
      var root = (RectTransform)transform;
      float inset = root.rect.width > root.rect.height ? .20f : .06f;
      RemasterTheme.Place(title.rectTransform, inset, .85f, 1-inset, .96f);
      RemasterTheme.Place((RectTransform)transform.Find("Panel"), inset, .73f, 1-inset, .80f);
      RemasterTheme.Place(TextLevelName.rectTransform, .15f, 0, .85f, 1);
      RemasterTheme.Place((RectTransform)transform.Find("Panel/ButtonPageLeft"), 0, 0, .10f, 1);
      RemasterTheme.Place((RectTransform)transform.Find("Panel/ButtonPageRight"), .90f, 0, 1, 1);
      for (int i = 1; i <= 10; i++) {
        float top = .71f - (i-1) * .055f;
        RemasterTheme.Place((RectTransform)transform.Find($"ItemHighscore{i}"), inset, top-.049f, 1-inset, top);
      }
      RemasterTheme.Place((RectTransform)transform.Find("ButtonBack"), inset, .05f, 1-inset, .12f);
    }

    public void LoadLevelData(int index) {
      LevelNames = HighscoreManager.Instance.GetLevelNames();
      LoadLevelData(LevelNames.Count > 0 ? LevelNames[Mathf.Clamp(index, 0, LevelNames.Count-1)] : "");
    }
    public void LoadLevelData(string name) {
      Install();
      LevelNames = HighscoreManager.Instance.GetLevelNames();
      _CurrentIndex = Mathf.Max(0, LevelNames.IndexOf(name));

      var data = HighscoreManager.Instance.GetData(name);
      if (data != null) {

        for (int i = 0; i < 10; i++) {
          var item = i < data.Count ? data[i] : null;
          if (item != null) {
            _TextItem[i].textName.text = item.name;
            _TextItem[i].testScore.text = item.score.ToString();
          } else {
            _TextItem[i].textName.text = "—";
            _TextItem[i].testScore.text = "—";
          }
        }    

        TextLevelName.text = LevelDisplayName.Get(name);
      } 
      else 
      {
        for (int i = 0; i < 10; i++) {
          _TextItem[i].textName.text = "—";
          _TextItem[i].testScore.text = "—";
        }  
        TextLevelName.text = string.IsNullOrEmpty(name) ? I18N.Tr("core.ui.NoLevelData") : LevelDisplayName.Get(name);
      }
    }

    public void Next() {
      if (_CurrentIndex < LevelNames.Count - 1)
        _CurrentIndex++;
      else
        _CurrentIndex = 0;
      LoadLevelData(_CurrentIndex);
    }
    public void Prev() {
      if( _CurrentIndex > 0)
        _CurrentIndex--;
      else
        _CurrentIndex = LevelNames.Count - 1;
      LoadLevelData(_CurrentIndex);
    }
  }
}
