using System.Collections.Generic;
using Ballance2.Utils;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* Log.cs
* 
* Purpose:
* Static logging helpers write to the console or a file, and
* allow observers to receive log entries for custom output or processing.
* 
* Log observer example:
* Log.RegisterLogObserver((level, tag, message, stackTrace) => {
*   // Capture Warning and Error entries.
* }, LogLevel.Warning | LogLevel.Error);
*
* Logging examples:
* Log.V(tag, message, ...) writes verbose diagnostic details.
* Log.D(tag, message, ...) writes debug information.
* Log.I(tag, message, ...) writes information useful for understanding runtime activity.
* Log.W(tag, message, ...) writes warnings.
* Log.E(tag, message, ...) writes errors.
*
* Author:
* mengyu
*
*/

namespace Ballance2
{
  /// <summary>
  /// Base logging class.
  /// </summary>
  public static class Log
  {
    private static string TAG = "Log";

    /// <summary>
    /// Write verbose diagnostic details.
    /// </summary>
    /// <param name="tag">Tag.</param>
    /// <param name="message">Log message.</param>
    public static void V(string tag, string message)
    {
      LogInternal(LogLevel.Verbose, tag, message);
    }
    /// <summary>
    /// Write a formatted log message.
    /// </summary>
    /// <param name="tag">Tag.</param>
    /// <param name="format">Format string using string.Format syntax.</param>
    /// <param name="param">Format arguments.</param>
    public static void V(string tag, string format, params object[] param)
    {
      V(tag, string.Format(format, param));
    }
    /// <summary>
    /// Write debug information.
    /// </summary>
    /// <param name="tag">Tag.</param>
    /// <param name="message">Log message.</param>
    public static void D(string tag, string message)
    {
      LogInternal(LogLevel.Debug, tag, message);
    }
    /// <summary>
    /// Write formatted debug information.
    /// </summary>
    /// <param name="tag">Tag.</param>
    /// <param name="format">Format string using string.Format syntax.</param>
    /// <param name="param">Format arguments.</param>
    public static void D(string tag, string format, params object[] param)
    {
      D(tag, string.Format(format, param));
    }
    /// <summary>
    /// Write an informational message.
    /// </summary>
    /// <param name="tag">Tag.</param>
    /// <param name="message">Log message.</param>
    public static void I(string tag, string message)
    {
      LogInternal(LogLevel.Info, tag, message);
    }
    /// <summary>
    /// Write a formatted informational message.
    /// </summary>
    /// <param name="tag">Tag.</param>
    /// <param name="format">Format string using string.Format syntax.</param>
    /// <param name="param">Format arguments.</param>
    public static void I(string tag, string format, params object[] param)
    {
      I(tag, string.Format(format, param));
    }
    /// <summary>
    /// Write a warning.
    /// </summary>
    /// <param name="tag">Tag.</param>
    /// <param name="message">Log message.</param>
    public static void W(string tag, string message)
    {
      LogInternal(LogLevel.Warning, tag, message);
    }
    /// <summary>
    /// Write a formatted warning.
    /// </summary>
    /// <param name="tag">Tag.</param>
    /// <param name="format">Format string using string.Format syntax.</param>
    /// <param name="param">Format arguments.</param>
    public static void W(string tag, string format, params object[] param)
    {
      W(tag, string.Format(format, param));
    }
    /// <summary>
    /// Write an error.
    /// </summary>
    /// <param name="tag">Tag.</param>
    /// <param name="message">Log message.</param>
    public static void E(string tag, string message)
    {
      LogInternal(LogLevel.Error, tag, message);
    }
    /// <summary>
    /// Write a formatted error.
    /// </summary>
    /// <param name="tag">Tag.</param>
    /// <param name="format">Format string using string.Format syntax.</param>
    /// <param name="param">Format arguments.</param>
    public static void E(string tag, string format, params object[] param)
    {
      E(tag, string.Format(format, param));
    }

    private static void LogInternal(LogLevel level, string tag, string message)
    {
      LogWrite(level, tag, message, DebugUtils.GetStackTrace(2));
    }

    /// <summary>
    /// Write a log entry manually.
    /// </summary>
    /// <param name="level">Log level.</param>
    /// <param name="tag">Tag.</param>
    /// <param name="message">Message.</param>
    /// <param name="stackTrace">Stack trace.</param>
    public static void LogWrite(LogLevel level, string tag, string message, string stackTrace)
    {
      if (logWriteLock)
      {
        logWriteLock = false;
        return;
      }

      // Also send logs to the Unity console in the editor.
      //#if UNITY_EDITOR
      logWriteLock = true;

      var str = string.Format("[{0}] {1}", tag, message);
      switch (level)
      {
        case LogLevel.Debug:
        case LogLevel.Verbose:
        case LogLevel.Info: UnityEngine.Debug.Log(str); break;
        case LogLevel.Warning: UnityEngine.Debug.LogWarning(str); break;
        case LogLevel.Error: UnityEngine.Debug.LogError(str); break;
      }

      logWriteLock = false;
      //#endif

      if (!logTemporaryForeachLock)
      {
        LogTemporaryData data = new LogTemporaryData();
        data.level = level;
        data.message = message;
        data.tag = tag;
        data.stackTrace = stackTrace;
        logTemporary.Add(data);
      }

      if (logTemporary.Count > 256)
        logTemporary.RemoveAt(0);

      observers.ForEach((observer) =>
      {
        if ((observer.AcceptLevel & level) != LogLevel.None)
          observer.Observer(level, tag, message, stackTrace);
      });
    }

    /// <summary>
    /// Replay buffered log entries.
    /// </summary>
    public static void SendLogsInTemporary()
    {
      logTemporaryForeachLock = true;
      logTemporary.ForEach((data) =>
      {
        observers.ForEach((observer) =>
        {
          if ((observer.AcceptLevel & data.level) != LogLevel.None)
            observer.Observer(data.level, data.tag, data.message, data.stackTrace);
        });
      });
      logTemporary.Clear();
      logTemporaryForeachLock = false;
    }

    /// <summary>
    /// Register a log observer.
    /// </summary>
    /// <param name="observer">Observer callback.</param>
    /// <param name="acceptLevel">Log levels accepted by the observer.</param>
    /// <returns>Positive observer ID on success; -1 on error.</returns>
    /// <example>
    /// Log.RegisterLogObserver((level, tag, message, stackTrace) => {
    ///   // Capture Warning and Error entries.
    /// }, LogLevel.Warning | LogLevel.Error);
    /// </example>
    public static int RegisterLogObserver(LogObserver observer, LogLevel acceptLevel)
    {
      if (acceptLevel == LogLevel.None)
      {
        E(TAG, "At least one LogLevel is required for LogObserver ! ");
        return -1;
      }

      LogObserverInternal logObserverInternal = observers.Find((o) => o.Observer == observer);
      if (logObserverInternal != null)
      {
        E(TAG, "Can not register LogObserver {0} because it already registered! ", observer.GetHashCode());
        return -1;
      }

      logObserverInternal = new LogObserverInternal();
      logObserverInternal.Id = CommonUtils.GenAutoIncrementID();
      logObserverInternal.AcceptLevel = acceptLevel;
      logObserverInternal.Observer = observer;

      observers.Add(logObserverInternal);
      return logObserverInternal.Id;
    }
    /// <summary>
    /// Unregister a log observer.
    /// </summary>
    /// <param name="id">Observer ID returned by RegisterLogObserver.</param>
    public static void UnRegisterLogObserver(int id)
    {
      LogObserverInternal logObserverInternal = observers.Find((o) => o.Id == id);
      if (logObserverInternal == null)
        return;
      observers.Remove(logObserverInternal);
    }
    /// <summary>
    /// Get a log observer.
    /// </summary>
    /// <param name="id">Observer ID returned by RegisterLogObserver.</param>
    /// <returns>The observer, or null if not found.</returns>
    public static LogObserver GetLogObserver(int id)
    {
      LogObserverInternal logObserverInternal = observers.Find((o) => o.Id == id);
      if (logObserverInternal != null)
        return logObserverInternal.Observer;
      return null;
    }

    /// <summary>
    /// Convert a log level to its display string.
    /// </summary>
    /// <param name="logLevel">Log level.</param>
    /// <returns></returns>
    public static string LogLevelToString(LogLevel logLevel)
    {
      switch (logLevel)
      {
        case LogLevel.None: return "N";
        case LogLevel.Verbose: return "V";
        case LogLevel.Debug: return "D";
        case LogLevel.Info: return "I";
        case LogLevel.Warning: return "W";
        case LogLevel.Error: return "E";
        case LogLevel.All: return "A";
      }
      return logLevel.ToString();
    }

    private struct LogTemporaryData
    {
      public LogLevel level;
      public string tag;
      public string message;
      public string stackTrace;
    }

    /// <summary>
    /// Get the text color for a log level.
    /// </summary>
    /// <param name="level">Log level.</param>
    /// <returns>Hex color string, such as ffffff.</returns>
    public static string GetLogColor(LogLevel level)
    {
      switch (level)
      {
        case LogLevel.Info: return "67CCFF";
        case LogLevel.Verbose: return "FFFFFF";
        case LogLevel.Warning: return "FFCE00";
        case LogLevel.Error: return "FF1B00";
        default: return "FFFFFF";
      }
    }

    private static List<LogObserverInternal> observers = new List<LogObserverInternal>();
    private static List<LogTemporaryData> logTemporary = new List<LogTemporaryData>();
    private static bool logTemporaryForeachLock = false;
    private static bool logWriteLock = false;

    /// <summary>
    /// Internal observer storage.
    /// </summary>
    private class LogObserverInternal
    {
      public int Id;
      public LogObserver Observer;
      public LogLevel AcceptLevel;
    }

    internal static void StartLogFile() {
      new LogFileObserver();
    }
  }

  /// <summary>
  /// Log level.
  /// </summary>
  public enum LogLevel
  {
    /// <summary>
    /// None.
    /// </summary>
    None = 0,
    /// <summary>
    /// Verbose debug details.
    /// </summary>
    Verbose = 0x1,
    /// <summary>
    /// Debug information.
    /// </summary>
    Debug = 0x2,
    /// <summary>
    /// Information.
    /// </summary>
    Info = 0x4,
    /// <summary>
    /// Warning.
    /// </summary>
    Warning = 0x8,
    /// <summary>
    /// Error.
    /// </summary>
    Error = 0x10,
    /// <summary>
    /// All log levels.
    /// </summary>
    All = Verbose | Debug | Info | Warning | Error,
  }

  /// <summary>
  /// Log observer interface.
  /// </summary>
  public delegate void LogObserver(LogLevel level, string tag, string message, string stackTrace);
}
