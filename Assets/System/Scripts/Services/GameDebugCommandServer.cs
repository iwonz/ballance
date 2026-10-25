using System.Collections.Generic;
using System.Text;
using Ballance2.Services.Debug;
using Ballance2.Utils;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* GameDebugCommandServer.cs
* 
* Purpose:
* Manage custom game debug commands.
*
* Author:
* mengyu
*/

/*

Press <kbd>F12</kbd> in debug mode to open the console and run commands.

Register custom commands with RegisterCommand. Example:
```csharp
GameDebugCommandServer.RegisterCommand("mycommand", (keyword, fullCmd, argsCount, args) => {
  // Read arguments with the DebugUtils helpers.
  var ox = DebugUtils.CheckIntDebugParam(0, args, out var nx, true, 0);
  if (ox == null) 
    return false;

  // Run custom logic after successfully reading arguments.
  return true;
}, 1, "mycommand <count:number> > Test debug command");
```
*/

namespace Ballance2.Services
{
  /// <summary>
  /// Debug command service for registering custom commands.
  /// </summary>
  public class GameDebugCommandServer
  {   
    private const string TAG = "DebugCommand";
    private List<CmdItem> commands = new List<CmdItem>();
    private class CmdItem
    {
      public int Id;
      public string Keyword;
      public int LimitArgCount;
      public string HelpText;
      public string Handler;
      public CommandDelegate Callback;
    }

    public GameDebugCommandServer()
    {
      RegisterSystemCommands();
    }

    /// <summary>
    /// Get command help matching the given text.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public string GenCommandHelp(string text) {
      StringBuilder sb = new StringBuilder();

      if (string.IsNullOrEmpty(text)) {
        for (var i = 0; i < commands.Count && i < 10; i++) {
          CmdItem cmdItem = commands[i];
          sb.Append(cmdItem.Keyword);
          sb.Append(' ');
          sb.AppendLine(cmdItem.HelpText);
        }
      } else {
        var count = 0;
        for (var i = 0; i < commands.Count && count < 10; i++) {
          CmdItem cmdItem = commands[i];
          if (cmdItem.Keyword.StartsWith(text)) {
            sb.Append("<color=#fff>");
            sb.Append(text);
            sb.Append("</color><color=#ddd>");
            sb.Append(cmdItem.Keyword.Substring(text.Length));
            sb.Append("</color>");
            sb.Append(' ');
            sb.AppendLine(cmdItem.HelpText);
          }
        }
      }
      return sb.ToString();
    }

    /// <summary>
    /// Parse and execute a command string.
    /// </summary>
    /// <param name="cmd">Command string.</param>
    /// <returns>Whether the operation succeeded.</returns>
    public bool ExecuteCommand(string cmd)
    {
      if (string.IsNullOrEmpty(cmd))
        return false;

      StringSpliter sp = new StringSpliter(cmd, ' ', true);
      if (sp.Count >= 1)
      {
        foreach (CmdItem cmdItem in commands)
        {
          if (cmdItem.Keyword == sp.Result[0])
          {
            //arg
            if (cmdItem.LimitArgCount > 0 && sp.Count - 1 < cmdItem.LimitArgCount)
            {
              Log.E(TAG, "Command {0} requires at least {1} arguments.", sp.Result[0], cmdItem.LimitArgCount);
              return false;
            }

            List<string> arglist = new List<string>(sp.Result);
            arglist.RemoveAt(0);

            //Kernel hander
            if (cmdItem.Callback != null)
              return cmdItem.Callback(sp.Result[0], cmd, arglist.Count, arglist.ToArray());
            else
            {
              Log.W(TAG, "Command {0}({1}) has no receiver.", cmdItem.Keyword, cmdItem.Id);
              return false;
            }
          }
        }
        Log.W(TAG, "Command {0} was not found.", sp.Result[0]);
      }
      return false;
    }

    /// <summary>
    /// Register a debug command.
    /// </summary>
    /// <param name="keyword">Command keyword without spaces.</param>
    /// <param name="callback">Command callback.</param>
    /// <param name="limitArgCount">Minimum argument count; default 0 imposes no minimum.</param>
    /// <param name="helpText">Command help text.</param>
    /// <returns>Command ID on success; -1 on failure.</returns>
    public int RegisterCommand(string keyword, CommandDelegate callback, int limitArgCount, string helpText)
    {
      if (!IsCommandRegistered(keyword))
      {
        CmdItem item = new CmdItem();
        item.Keyword = keyword;
        item.LimitArgCount = limitArgCount;
        item.HelpText = helpText;
        item.Handler = "";
        item.Callback = callback;
        item.Id = CommonUtils.GenNonDuplicateID();

        commands.Add(item);
        return item.Id;
      }
      else
      {
        GameErrorChecker.SetLastErrorAndLog(GameError.AlreadyRegistered, TAG, "Command {0} already registered", keyword);
        return -1;
      }
    }

    /// <summary>
    /// Unregister a command.
    /// </summary>
    /// <param name="cmdId">Command ID.</param>
    public void UnRegisterCommand(int cmdId)
    {
      CmdItem removeItem = null;
      foreach (CmdItem cmdItem in commands)
      {
        if (cmdItem.Id == cmdId)
        {
          removeItem = cmdItem;
          break;
        }
      }
      if (removeItem != null)
        commands.Remove(removeItem);
    }

    /// <summary>
    /// Whether a command is registered.
    /// </summary>
    /// <param name="keyword">Command keyword.</param>
    /// <returns>Whether the command is registered.</returns>
    public bool IsCommandRegistered(string keyword)
    {
      foreach (CmdItem cmdItem in commands)
      {
        if (cmdItem.Keyword == keyword)
          return true;
      }
      return false;
    }

    private bool OnCommandHelp(string keyword, string fullCmd, int argsCount, string[] args)
    {
      string helpText = "Command help:\n";
      foreach (CmdItem cmdItem in commands)
        helpText += cmdItem.Keyword + " <color=#adadad>" + cmdItem.HelpText + "</color>\n";
      Log.V(TAG, helpText);
      return true;
    }

    private void RegisterSystemCommands()
    {
      // Register built-in base commands.
      RegisterCommand("help", OnCommandHelp, 0, "help > Show command help");
      RegisterCommand("e", (keyword, fullCmd, argsCount, args) =>
      {
        Log.V("echo", fullCmd.Substring(2));
        return true;
      }, 1, "e [any] > Test command");
    }
  }

  /// <summary>
  /// Debug command callback.
  /// </summary>
  /// <param name="keyword">Command keyword.</param>
  /// <param name="fullCmd">Full command.</param>
  /// <param name="args">Command arguments.</param>
  /// <returns>Return whether execution succeeded.</returns>
  public delegate bool CommandDelegate(string keyword, string fullCmd, int argsCount, string[] args);
}