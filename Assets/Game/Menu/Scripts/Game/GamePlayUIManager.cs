using Ballance2.Game.Remaster;
using Ballance2.Game.GamePlay;
using Ballance2.Package;
using Ballance2.Services;
using Ballance2.Services.I18N;
using UnityEngine;
using UnityEngine.UI;

namespace Ballance2.Menu
{
  public class GamePlayUIManager {
    public static void Create() {
      var package = GamePackage.GetSystemPackage();
      var GameUIManager = GameManager.GetSystemService<GameUIManager>();
      var MessageCenter = MenuManager.MessageCenter;

      var PageGamePause = GameUIManager.RegisterPage("PageGamePause", "PageCommon");
      var PageGameFail = GameUIManager.RegisterPage("PageGameFail", "PageCommon");
      var PageGameQuitAsk = GameUIManager.RegisterPage("PageGameQuitAsk", "PageCommon");
      var PageGameRestartAsk = GameUIManager.RegisterPage("PageGameRestartAsk", "PageCommon");
      var PageGameWinRestartAsk = GameUIManager.RegisterPage("PageGameWinRestartAsk", "PageCommon");
      var PageGameWin = GameUIManager.RegisterPage("PageGameWin", "PageCommon");
      var PageEndScore = GameUIManager.RegisterPage("PageEndScore", "PageTransparent");
      var PageHighscoreEntry = GameUIManager.RegisterPage("PageHighscoreEntry", "PageCommon");
      var PageGamePreviewPause = GameUIManager.RegisterPage("PageGamePreviewPause", "PageCommon");
      var PageGamePreviewQuitAsk = GameUIManager.RegisterPage("PageGamePreviewQuitAsk", "PageCommon");
      
      PageGamePause.CreateContent(package);
      PageGameQuitAsk.CreateContent(package);
      PageGameRestartAsk.CreateContent(package);
      PageGamePause.CreateContent(package);
      PageGamePause.CanEscBack = false;
      PageGameWin.CreateContent(package);
      PageGameWinRestartAsk.CreateContent(package);
      PageGameWin.CanEscBack = false;
      PageEndScore.CreateContent(package);
      PageEndScore.CanEscBack = false;
      PageHighscoreEntry.CreateContent(package);
      PageHighscoreEntry.CanEscBack = false;
      PageGameFail.CreateContent(package);
      PageGameFail.CanEscBack = false;
      PageGamePreviewPause.CanEscBack = false;
      PageGamePreviewPause.CreateContent(package);
      PageGamePreviewQuitAsk.CreateContent(package);

      MessageCenter.SubscribeEvent("BtnGameHomeClick", () => GamePlayManager.Instance.QuitLevel());
      MessageCenter.SubscribeEvent("BtnNextLevellick", () => GamePlayManager.Instance.NextLevel());
      MessageCenter.SubscribeEvent("BtnGameRestartClick", () => GameUIManager.GoPage("PageGameRestartAsk"));
      MessageCenter.SubscribeEvent("BtnGameWinRestartClick", () => GameUIManager.GoPage("PageGameWinRestartAsk"));
      MessageCenter.SubscribeEvent("BtnGameQuitClick", () => GameUIManager.GoPage("PageGameQuitAsk"));
      MessageCenter.SubscribeEvent("BtnGamePreviewQuitClick", () => GameUIManager.GoPage("PageGamePreviewQuitAsk"));
      MessageCenter.SubscribeEvent("BtnPauseSettingsClick", () => GameUIManager.GoPage("PageSettingsInGame"));
      MessageCenter.SubscribeEvent("BtnGameFailRestartClick", () => {
        GameUIManager.HideCurrentPage();
        GamePlayManager.Instance.RestartLevel();
      });
      MessageCenter.SubscribeEvent("BtnGameQuitSureClick", () => {
        GameUIManager.HideCurrentPage();
        GamePlayManager.Instance.QuitLevel();
      });
      MessageCenter.SubscribeEvent("BtnGameFailQuitClick", () => {
        GameUIManager.HideCurrentPage();
        GamePlayManager.Instance.QuitLevel();
      });
      MessageCenter.SubscribeEvent("BtnResumeClick", () => GamePlayManager.Instance.ResumeLevel());
      MessageCenter.SubscribeEvent("BtnPreviewResumeClick", () => GamePreviewManager.Instance.ResumeLevel());
      MessageCenter.SubscribeEvent("BtnGamePreviewQuitSureClick", () => {
        GameUIManager.HideCurrentPage();
        GamePreviewManager.Instance.QuitLevel();
      });

      // Default high-score data.
      var HighscoreEntryName = PageHighscoreEntry.Content.Find("InputField").GetComponent<InputField>();
      HighscoreEntryName.text = RemasterPreferences.GetString("LastEnterHighscoreEntry", I18N.Tr("core.ui.PlayerName"));

      // Next-level button after completion.
      var ButtonNext = PageGameWin.Content.Find("ButtonNext").gameObject;
      PageGameWin.OnShow = (p) => {
        ButtonNext.SetActive(GamePlayManager.Instance.NextLevelName != "");
      };

      MessageCenter.SubscribeEvent("BtnHighscrollEnterClick", () => {
        RemasterPreferences.SetString("LastEnterHighscoreEntry", HighscoreEntryName.text);
        WinScoreUIControl.Instance.SaveHighscore(HighscoreEntryName.text);

        // Show the high-score page for a new high score; otherwise show the next-level menu.
        GameUIManager.GoPage("PageGameWin");
        if (WinScoreUIControl.Instance.ThisTimeHasNewHighscore) {
          GameTimer.Delay(0.2f, () => {
            GameUIManager.GoPage("PageHighscore");
            // Load the current level's data on the high-score page.
            HighscoreUIControl.Instance.LoadLevelData(GamePlayManager.Instance.CurrentLevelName);
          });
        }
      });

      KeypadUIManager.AddKeypad("BaseCenter", package.GetPrefabAsset("KeypadCenter.prefab"), package.GetSpriteAsset("keypad_c.png"), I18N.Tr("core.ui.SettingsControlKeypadBaseCenter"));
      KeypadUIManager.AddKeypad("BaseLeft", package.GetPrefabAsset("KeypadLeft.prefab"), package.GetSpriteAsset("keypad_l.png"), I18N.Tr("core.ui.SettingsControlKeypadBaseLeft"));
      KeypadUIManager.AddKeypad("BaseRight", package.GetPrefabAsset("KeypadRight.prefab"), package.GetSpriteAsset("keypad_r.png"), I18N.Tr("core.ui.SettingsControlKeypadBaseRight"));
      KeypadUIManager.AddKeypad("BaseJoyLeft", package.GetPrefabAsset("KeypadJoyLeft.prefab"), package.GetSpriteAsset("keypad_joy_l.png"), I18N.Tr("core.ui.SettingsControlKeypadBaseJoyLeft"));
      KeypadUIManager.AddKeypad("BaseJoyRight", package.GetPrefabAsset("KeypadJoyRight.prefab"), package.GetSpriteAsset("keypad_joy_r.png"), I18N.Tr("core.ui.SettingsControlKeypadBaseJoyRight"));
    }
  }
}
