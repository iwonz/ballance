
using System.Collections.Generic;
using Ballance2.Services.Debug;
using UnityEngine;

namespace Ballance2.Menu
{
  /// <summary>
  /// Manage mobile keyboard types and custom keyboard registration.
  /// </summary>
  public class KeypadUIManager
  {
    public struct KeypadUIInfo {
      public KeypadUIInfo(GameObject prefab, Sprite image, string explain) {
        this.prefab = prefab;
        this.image = image;
        this.explain = explain;
      }
      public GameObject prefab;
      public Sprite image;
      public string explain;
    }

    private const string TAG = "KeypadUIManager";
    private static Dictionary<string, KeypadUIInfo> keypadData = new Dictionary<string, KeypadUIInfo>();

    /// <summary>
    /// Register a keyboard.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <param name="prefab">Keyboard prefab.</param>
    /// <param name="image">Keyboard preview image shown in the menu.</param>
    /// <returns>True on success; false on failure, such as a duplicate keyboard name.</returns>
    public static bool AddKeypad(string name, GameObject prefab, Sprite image, string explain)
    {
      if (keypadData.ContainsKey(name)) {
        GameErrorChecker.SetLastErrorAndLog(GameError.AlreadyRegistered, TAG, $"Keypad {name} already registered!");
        return false;
      }

      if (prefab == null) {
        GameErrorChecker.SetLastErrorAndLog(GameError.AlreadyRegistered, TAG, $"AddKeypad {name} fail because prefab is null");
        return false;
      }

      keypadData.Add(name, new KeypadUIInfo(prefab, image, explain));  
      return true;
    }
    /// <summary>
    /// Whether a keyboard is registered.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <returns></returns>
    public static bool GetKeypadRegistered(string name)
    {
      return keypadData.ContainsKey(name);
    } 
    /// <summary>
    /// Get a registered keyboard, or null if absent.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <returns></returns>
    public static KeypadUIInfo GetKeypad(string name)
    {
      if(keypadData.TryGetValue(name, out var result))
        return result;
      return default(KeypadUIInfo);
    } 
    /// <summary>
    /// Get all registered keyboards.
    /// </summary>
    public static Dictionary<string, KeypadUIInfo> GetAllKeypad()
    {
      return keypadData;
    } 
    /// <summary>
    /// Unregister a keyboard.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <returns>True on success; false on failure.</returns>
    public static bool DeletetKeypad(string name)  
    {
      if (!keypadData.ContainsKey(name)) {
        GameErrorChecker.SetLastErrorAndLog(GameError.NotRegister, TAG, $"Keypad {name} not register!");
        return false;
      }
      keypadData.Remove(name);
      return true;
    } 
  }
}