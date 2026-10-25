using Ballance2.Entry;
using Ballance2.Services;
using Ballance2.Services.Debug;

namespace Ballance2.Game.GamePlay.Debug
{
  public static class GamePlayLevelCustomDebugManager
  {
    public static void Init() {
      // Validate parameters.
      if (string.IsNullOrEmpty(GameDebugEntry.Instance.LevelName)) {
        GameErrorChecker.ThrowGameError(GameError.ParamNotProvide, "LevelName is not set!");
        return;
      }
      GameManager.GameMediator.NotifySingleEvent("CoreStartLoadLevel", GameDebugEntry.Instance.LevelName);
    } 
  }
}