using System;
using Ballance2.Services.I18N;

namespace Ballance2.Menu
{
  // Display labels only: level IDs, paths and saved high scores stay unchanged.
  public static class LevelDisplayName
  {
    private static int Number(string id)
    {
      int number;
      return id != null && id.StartsWith("level", StringComparison.OrdinalIgnoreCase)
        && int.TryParse(id.Substring(5), out number) && number >= 1 && number <= 13
        ? number : 0;
    }

    public static string Get(string id)
    {
      var number = Number(id);
      return number == 0 ? id : I18N.Tr("core.level." + number, id);
    }

    public static string Introduction(string id, string original)
    {
      var number = Number(id);
      return number == 0 ? original : I18N.TrF("core.ui.OriginalLevel", number);
    }
  }
}
