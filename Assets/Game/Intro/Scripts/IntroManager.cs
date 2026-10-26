using Ballance2.Base;
using Ballance2;
using Ballance2.Package;
using Ballance2.Services;
using UnityEngine;

namespace Ballance2.Game {

  /// <summary>
  /// Intro management.
  /// </summary>
  public class IntroManager : MonoBehaviour {

    const string TAG = "IntroManager";

    private static RectTransform IntroUI = null;

    // Enter the Intro logical scene.
    private static void OnEnterIntro(GamePackage thisGamePackage) {

      Log.D(TAG, "Into intro ui");

      var GameUIManager = GameManager.GetSystemService<GameUIManager>();
      var GameSoundManager = GameManager.GetSystemService<GameSoundManager>();

      if (IntroUI == null) {
        IntroUI = GameUIManager.InitViewToCanvas(thisGamePackage.GetPrefabAsset("IntroUI.prefab"), "IntroUI", true);
        IntroUI.SetAsFirstSibling();  
      }

      GameUIManager.MaskBlackFadeIn(0.3f);

      // Start intro music.
      GameSoundManager.PlayFastVoice("core.sounds.music:Music_Theme_4_1.wav", GameSoundType.Background);

      GameTimer.Delay(5, () => {
        // Fade in from black.
        GameUIManager.MaskBlackFadeIn(1);
        GameTimer.Delay(1, () => {
          // Enter the menu.
          GameManager.Instance.RequestEnterLogicScense("MenuLevel");
        });
      });
    }
    private static void OnQuitIntro() {
      Log.D(TAG, "Quit intro ui");
      if (IntroUI != null) {
        IntroUI.gameObject.SetActive(false);
      }
    }

    public static void Init(GamePackage package) {
      // Register logical scene transitions.
      GameManager.GameMediator.RegisterEventHandler(package, GameEventNames.EVENT_LOGIC_SECNSE_ENTER, "Intro", (evtName, param) => {
        var scense = param[0];
        if(scense.ToString() == "Intro") 
          OnEnterIntro(package);
        return false;
      }); 
      GameManager.GameMediator.RegisterEventHandler(package, GameEventNames.EVENT_LOGIC_SECNSE_QUIT, "Intro", (evtName, param) => {
        var scense = param[0];
        if (scense.ToString() == "Intro") 
          OnQuitIntro();
        return false;
      });
    }

    public static void Destroy(GamePackage package) {
      if (IntroUI != null) {
        GameObject.Destroy(IntroUI.gameObject);
        IntroUI = null;
      }
    }
  }
}