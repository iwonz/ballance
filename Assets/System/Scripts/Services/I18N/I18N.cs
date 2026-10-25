
/*
* Copyright(c) 2021  mengyu
*
* Module:
* I18N.cs
* 
* Purpose:
* Look up localized strings for the current game language.
*
* Author:
* mengyu
*/

namespace Ballance2.Services.I18N
{
  /// <summary>
  /// Provide localized strings for the current game language.
  /// </summary>
  /// <remark>
  /// Two ways to supply language resources:
  /// * Add PackageLanguageRes.xml to the mod directory; package loading imports its strings automatically.
  /// * Call I18NProvider.LoadLanguageResources explicitly.
  /// Language resource XML format:
  /// ```xml
  /// <I18n>
  ///   <Language name="ChineseSimplified">
  ///     <Text name="core.ui.RestartLevel">Example translation for the selected language</Text>
  ///   </Language>
  ///   <Language name="English">
  ///     <Text name="core.ui.RestartLevel">Restart Level</Text>
  ///   </Language>
  ///   <Language name="ChineseTraditional">
  ///     <Text name="core.ui.RestartLevel">Example translation for another language</Text>
  ///   </Language>
  /// </I18n>
  /// ```
  /// Language names come from UnityEngine.SystemLanguage; multiple language sections are supported.
  /// Text keys are global across the game. Give each mod a unique prefix to avoid collisions.
  /// </remark>
  public static class I18N
  {
    /// <summary>
    /// Get a localized string.
    /// </summary>
    /// <param name="key">String key.</param>
    /// <returns>The string, or [Key xxx not found!] when missing.</returns>
    public static string Tr(string key)
    {
      var str = I18NProvider.GetLanguageString(key);
      if (str != null)
        return str;
      return string.Format("[Key {0} not found!]", key);
    }
    /// <summary>
    /// Get a localized string.
    /// </summary>
    /// <param name="key">String key.</param>
    /// <param name="defaultString">Fallback when the localized key is missing.</param>
    /// <returns></returns>
    public static string Tr(string key, string defaultString)
    {
      var str = I18NProvider.GetLanguageString(key);
      if (str != null)
        return str;
      return defaultString;
    }
    /// <summary>
    /// Get a localized string with format arguments.
    /// </summary>
    /// <param name="key">String key.</param>
    /// <param name="formatParams">Format arguments.</param>
    /// <returns></returns>
    public static string TrF(string key, params object[] formatParams)
    {
      var str = I18NProvider.GetLanguageString(key);
      if (str != null)
        return string.Format(str, formatParams);
      return string.Format("[Key {0} not found!]", key);
    }
    /// <summary>
    /// Get a localized string with format arguments.
    /// </summary>
    /// <param name="key">String key.</param>
    /// <param name="defaultString">Fallback format string when the localized key is missing.</param>
    /// <param name="formatParams">Format arguments.</param>
    /// <returns></returns>
    public static string TrF(string key, string defaultString, params object[] formatParams)
    {
      var str = I18NProvider.GetLanguageString(key);
      try {
        return string.Format(str != null ? str : defaultString, formatParams);
      } catch (System.Exception e) {
        return "Failed to format " + key + " Value: " + str + " Exception: " + e.ToString();
      }
    }
  }
}
