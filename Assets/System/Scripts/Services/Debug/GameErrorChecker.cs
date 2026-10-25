using Ballance2.Entry;
using Ballance2.UI.CoreUI;
using Ballance2.Utils;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* GameErrorChecker.cs
* 
* Purpose:
* Error checker.
* Retrieve errors from game API calls.
* Also displays global error dialogs for debugging.
*
* Author:
* mengyu
*/

namespace Ballance2.Services.Debug
{
  /// <summary>
  /// Error checker.
  /// </summary>
  public class GameErrorChecker
  {
    private static GameGlobalErrorUI gameGlobalErrorUI;
    private static List<string> gameErrorLogPools;

    internal static void SetGameErrorUI(GameGlobalErrorUI errorUI)
    {
      gameGlobalErrorUI = errorUI;
    }

    /// <summary>
    /// Raise a fatal game error and stop the game, similar to a system crash screen.
    /// </summary>
    /// <param name="code">Error code.</param>
    /// <param name="message">Exception details.</param>
    public static void ThrowGameError(GameError code, string message)
    {
      StringBuilder stringBuilder = new StringBuilder("Error code: ");
      stringBuilder.Append(code.ToString());
      stringBuilder.Append("\n");
      stringBuilder.Append(string.IsNullOrEmpty(message) ? GameErrorInfo.GetErrorMessage(code) : message);
      stringBuilder.Append("\n");
      stringBuilder.Append(DebugUtils.GetStackTrace(1));
      stringBuilder.Append("\n");

      if(gameErrorLogPools != null && gameErrorLogPools.Count > 0) {
        stringBuilder.Append("Errors:\n");
        foreach (var item in gameErrorLogPools)
        {
          stringBuilder.Append(item);
          stringBuilder.Append("\n");
        }
      }

      GameSystem.ForceInterruptGame();
      HideInternalObjects();
      gameGlobalErrorUI.ShowErrorUI(stringBuilder.ToString());
      UnityEngine.Debug.LogError(stringBuilder.ToString());
    }

    /// <summary>
    /// Get or set the previous operation's error.
    /// </summary>
    public static GameError LastError { get; set; }
    /// <summary>
    /// Get the previous operation's error description.
    /// </summary>
    /// <returns></returns>
    public static string GetLastErrorMessage()
    {
      return GameErrorInfo.GetErrorMessage(LastError);
    }

    /// <summary>
    /// Set an error code and log a message.
    /// </summary>
    /// <param name="code">Error code.</param>
    /// <param name="tag">Log tag.</param>
    /// <param name="message">Log message format string.</param>
    /// <param name="param">Log format arguments.</param>
    public static void SetLastErrorAndLog(GameError code, string tag, string message, params object[] param)
    {
      LastError = code;
      Log.E(tag, message, param);
    }
    /// <summary>
    /// Set an error code and log a message.
    /// </summary>
    /// <param name="code">Error code.</param>
    /// <param name="tag">TAG</param>
    /// <param name="message">Error message.</param>
    public static void SetLastErrorAndLog(GameError code, string tag, string message)
    {
      LastError = code;
      Log.E(tag, message);
    }

    private static void HideInternalObjects() {
      var GameGlobalMask = GameObject.Find("GameGlobalMask");
      if(GameGlobalMask != null)
        GameGlobalMask.SetActive(false);
      var GameGlobalIngameLoading = GameObject.Find("GameGlobalIngameLoading");
      if(GameGlobalIngameLoading != null)
        GameGlobalIngameLoading.SetActive(false);
    }

    /// <summary>
    /// Show a system error dialog.
    /// </summary>
    /// <param name="message">Error message.</param>
    public static void ShowSystemErrorMessage(string message)
    {
      gameGlobalErrorUI.ShowErrorUI(message);
      HideInternalObjects();
    }
    /// <summary>
    /// Show a script error dialog.
    /// </summary>
    /// <param name="message">Error message.</param>
    public static void ShowScriptErrorMessage(string fileName, string packName, string message)
    {
      GameEntry entry = GameEntry.Instance;
      if(entry) {
        GameSystem.ForceInterruptGame();
        HideInternalObjects();

        string err = string.Format("Code exception:\nFile: {0}, package: {1}\n",fileName, packName) + message;
        entry.GlobalGameScriptErrDialog.Show(err);
        UnityEngine.Debug.LogError(err);
      }
    }
  }
}
