using Ballance2.Utils;
using UnityEngine;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* UnityLogCatcher.cs
* 
* Purpose:
* Capture Unity logs for the in-game console.
*
* Author:
* mengyu
*/

namespace Ballance2.Utils
{
  class UnityLogCatcher
  {
    public static void Init()
    {
      Application.logMessageReceived += Application_logMessageReceived;
    }
    public static void Destroy()
    {
      Application.logMessageReceived -= Application_logMessageReceived;
    }

    private static void Application_logMessageReceived(string condition, string stackTrace, LogType type)
    {
      switch (type)
      {
        case LogType.Error: Log.LogWrite(LogLevel.Error, "Application", condition, stackTrace); break;
        case LogType.Assert: Log.LogWrite(LogLevel.Error, "Assert", condition, stackTrace); break;
        case LogType.Warning: Log.LogWrite(LogLevel.Warning, "Application", condition, stackTrace); break;
        case LogType.Log: Log.LogWrite(LogLevel.Debug, "Application", condition, stackTrace); break;
        case LogType.Exception: Log.LogWrite(LogLevel.Error, "Exception", condition, stackTrace); break;
      }
    }
  }
}
