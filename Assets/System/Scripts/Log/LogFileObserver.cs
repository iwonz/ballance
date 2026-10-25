using System.IO;
using System.Text;
using Ballance2.Res;
using Ballance2.Utils;
using UnityEngine;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* LogFileObserver.cs
* 
* Purpose:
* Capture log entries and write them to a file.
*
* Author:
* mengyu
*/

namespace Ballance2.Utils
{
  class LogFileObserver : System.IDisposable
  {
    private StreamWriter sw = null;
    private bool disposedValue;

    public LogFileObserver()
    {
      sw = new StreamWriter(GamePathManager.GetResRealPath("logfile", "", true, false), false);
      Log.RegisterLogObserver(LogObserver, LogLevel.All);
    }

    
    void LogObserver(LogLevel level, string tag, string message, string stackTrace) {
      StringBuilder sb = new StringBuilder();
      sb.Append("[");
      sb.Append(tag);
      sb.Append("/");
      sb.Append(level.ToString().Substring(0, 1));
      sb.Append("] ");
      sb.Append(message);
      sw.WriteLine(sb.ToString());
      sw.Write("    ");
      sw.Write(stackTrace);
      sw.Write("\n");
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          sw.Close();
          sw.Dispose();
        }
        disposedValue = true;
      }
    }

    public void Dispose()
    {
      // Do not change this method; put cleanup in Dispose(bool disposing).
      Dispose(disposing: true);
      System.GC.SuppressFinalize(this);
    }
  }
}
