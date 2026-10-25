using Ballance2.Package;
using Ballance2.Config;
using Ballance2.Menu;
using Ballance2.Game.LevelBuilder;
using Ballance2.Game.GamePlay;
using Ballance2.Services;
using Ballance2.Base;
using Ballance2.Services.Debug;
using Ballance2.Game.GamePlay.Debug;

namespace Ballance2.Game {
  // Core game package entry point.
  public class GameCorePackageEntry {

    private static string TAG = "GameCore";

    public static GamePackageEntry Main() {
      GamePackageEntry entry = new GamePackageEntry();
      // Initialize.
      entry.OnLoad = (package) => {
        IntroManager.Init(package);
        MenuLevelManager.Init(package);
        MenuManager.Init();
        LevelBuilderInit.Init();
        HighscoreManager.Init();
        HighscoreManager.Instance.Load();

        var SystemPackage = GamePackage.GetSystemPackage();;
        var GameMediatorInstance = GameMediator.Instance;

        // Debug entry control.
        if (GameManager.DebugMode) {
          GameMediatorInstance.RegisterEventHandler(SystemPackage, "CoreDebugLevelBuliderEntry", TAG, (evtName, param) => {
            LevelBuilderInit.CoreDebugLevelBuliderEntry();
            return false;
          });
          GameMediatorInstance.RegisterEventHandler(SystemPackage, "CoreDebugLevelEnvironmentEntry", TAG, (evtName, param) => {
            LevelBuilderInit.CoreDebugLevelEnvironmentEntry();
            return false;
          });
          GameMediatorInstance.RegisterEventHandler(SystemPackage, "ModulCustomDebug", TAG, (evtName, param) => {
            GamePlayModulDebugManager.Init();
            return false;
          });
          GameMediatorInstance.RegisterEventHandler(SystemPackage, "LevelCustomDebug", TAG, (evtName, param) => {
            GamePlayLevelCustomDebugManager.Init();
            return false;
          });
        }

        // Level loading control.
        var nextLoadLevel = "";
        var nextLoadLevelIsPreview = false;
        GameMediatorInstance.RegisterEventHandler(SystemPackage, GameEventNames.EVENT_LOGIC_SECNSE_ENTER, TAG, (evtName, param) => {
          var scense = param[0] as string;
          if(scense == "Level") { 
            GameTimer.Delay(0.3f, () => {
              GamePlayInitManager.GamePlayInit(nextLoadLevelIsPreview, () => {
                if (nextLoadLevel != "") {
                  LevelBuilder.LevelBuilder.Instance.LoadLevel(nextLoadLevel, nextLoadLevelIsPreview);
                  nextLoadLevel = "";
                }
              });
            });
          }
          return false;
        });    
        GameMediatorInstance.RegisterEventHandler(SystemPackage, GameEventNames.EVENT_LOGIC_SECNSE_QUIT, TAG, (evtName, param) => {
          var scense = param[0] as string;
          if(scense == "Level")  
            GamePlayInitManager.GamePlayUnload();
          return false;
        });
        
        // Level loading entry point.
        GameMediatorInstance.SubscribeSingleEvent(SystemPackage, "CoreStartLoadLevel", TAG, (evtName, param) => {
          if (param.Length < 1 || !(param[0] is string)) {
            var type = param[0] as string;
            GameErrorChecker.SetLastErrorAndLog(GameError.ParamNotProvide, TAG, $"Param 0 expect string, but got {type}");
            return false;
          } 
          else 
          {
            nextLoadLevelIsPreview = param.Length >= 2 && param[1] is bool ? (bool)param[1] : false;
            nextLoadLevel = param[0] as string;
            Log.D(TAG, $"Start load level {nextLoadLevel} preview {nextLoadLevelIsPreview}");
          }
          GameManager.Instance.RequestEnterLogicScense("Level");
          return false;
        });
        // Quit entry point.
        GameMediatorInstance.RegisterEventHandler(SystemPackage, GameEventNames.EVENT_BEFORE_GAME_QUIT, TAG, (evtName, param) => {
          // Save score data.
          HighscoreManager.Instance.Save();
          return false;
        });

        return true;
      };
      // Unload.
      entry.OnBeforeUnLoad = (package) => {
        IntroManager.Destroy(package);
        MenuLevelManager.Destroy(package);
        LevelBuilderInit.Destroy();
        HighscoreManager.Destroy();
        return true;
      };
      entry.Version = GameConst.GameBulidVersion;
      return entry;
    }
  }
}