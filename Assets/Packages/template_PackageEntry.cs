using Ballance2.Package;
using Ballance2;

namespace ModNamespace {

  /// <summary>
  /// Example mod entry point.
  /// </summary>
  public class PackageEntry {
    const string TAG = "MyMod";

    /// <summary>
    /// The mod's main entry point must return its package entry information.
    /// </summary>
    /// <returns></returns>
    public static GamePackageEntry Main() {
      GamePackageEntry entry = new GamePackageEntry();
      entry.OnLoad = (package) => {
        Log.D(TAG, "Package entry point!");

        // Add package initialization here.
        //
        // See the developer-tools and package-management sections of docs/architecture.md for the current integration points.
        //
        // Return true when initialization succeeds; otherwise false.
        return true;
      };
      entry.OnBeforeUnLoad = (package) => {
        // Release owned resources here.
        Log.D(TAG, "Called before the package is unloaded!");
        return true;
      };
      entry.Version = 1; // Return the version number.
      return entry;
    }
  }
}