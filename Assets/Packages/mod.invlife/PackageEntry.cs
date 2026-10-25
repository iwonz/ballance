using Ballance2.Package;
using Ballance2.Menu;
using Ballance2.Services;
using Ballance2.Base;
using Ballance2.Game.GamePlay;
using Ballance2.Services.I18N;

namespace mod.invlife {

  public class PackageEntry {
    private static bool enable = false;
    private static string TAG = "invlife";

    public static GamePackageEntry Main() {
      GamePackageEntry entry = new GamePackageEntry();
      entry.OnLoad = (package) => {
        /*
        Listen to GamePlayManager.EventBeforeStart and set lives to -1 (unlimited) before the level starts.
        */ 
        GameEventEmitterHandler eventHandler = null; 
        GameManager.GameMediator.RegisterEventHandler(package, GameEventNames.EVENT_LEVEL_BUILDER_START, TAG, (evtName, param) => {
          var GamePlayManagerInstance = GamePlayManager.Instance;
          eventHandler = GamePlayManagerInstance.EventBeforeStart.On((param) => {
            if (enable) {
              GamePlayManagerInstance.StartLife = -1;
              GamePlayManagerInstance.CurrentLife = -1;
              GamePlayUIControl.Instance.SetLifeBallCount(-1);
            }
          });
          return false;
        });
        GameManager.GameMediator.RegisterEventHandler(package, GameEventNames.EVENT_LEVEL_BUILDER_UNLOAD_START, TAG, (evtName, param) => {
          eventHandler.Off();
          return false;
        });
        return true;
      };
      entry.OnLoadUI = (package) => {
        /*
        Create a settings UI with an Enable infinite life ball toggle,
        allowing the player to enable or disable unlimited lives.
        */
        var setting = GameSettingsManager.GetSettings(package.PackageName);
        var settingsUI = PackageManagerUIControl.Instance.RegisterPackageSettingsUI(package);
        // Add a toggle.
        var contol = settingsUI.AddToggle("Enable", I18N.Tr("mod.invlife.ui.EnableTitle"), (val) => {
          // Save settings when the toggle changes.
          enable = (bool)val;
          setting.SetBool("enable", enable);
        });
        // Load settings when the page opens.
        settingsUI.Page.OnShow += (o) => {
          enable = setting.GetBool("enable");
          contol.ValueBinderSupplierCallback(enable);
        };
        return true;
      };
      entry.OnBeforeUnLoad = (package) => {
        return true;
      };
      entry.Version = 1;
      return entry;
    }
  }
}