using System.Diagnostics;
using System.Text;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* DebugUtils.cs
* 
* Purpose:
* Debug helpers.
*
* Author:
* mengyu
*
*/

namespace Ballance2.Utils
{
  /// <summary>
  /// Debug helpers.
  /// </summary>
  public class DebugUtils
  {
    /// <summary>
    /// Get the current call stack.
    /// </summary>
    /// <param name="skipFrame">Number of frames to skip; zero skips none.</param>
    /// <returns>Detailed call stack.</returns>
    public static string GetStackTrace(int skipFrame)
    {
      StringBuilder stringBuilder = new StringBuilder();
      var stacktrace = new StackTrace(skipFrame + 1, true);
      for (var i = 0; i < stacktrace.FrameCount; i++)
      {
        var frame = stacktrace.GetFrame(i);
        var method = stacktrace.GetFrame(i).GetMethod();
        stringBuilder.Append("\n[");
        stringBuilder.Append(i);
        stringBuilder.Append("] ");
        stringBuilder.Append(method.Name);

        var lineNum = frame.GetFileLineNumber();
        if (lineNum > 0)
        {
          stringBuilder.Append(" in ");
          stringBuilder.Append(frame.GetFileName());
          stringBuilder.Append(" (");
          stringBuilder.Append(lineNum);
          stringBuilder.Append(":");
          stringBuilder.Append(frame.GetFileColumnNumber());
          stringBuilder.Append(")");
        }
      }
      return stringBuilder.ToString();
    }

    /// <summary>
    /// Format a byte array as hexadecimal.
    /// </summary>
    /// <param name="vs">Byte array.</param>
    /// <returns>Formatted string for display or console output.</returns>
    public static string PrintBytes(byte[] vs)
    {
      StringBuilder sb = new StringBuilder();
      int col = 0, line = 0;
      sb.Append("Byte Array Length: ");
      sb.Append(vs.Length);
      sb.Append('\n');
      sb.Append(
          "00000000 | 00 01 02 03  04 05 06 07  08 09 0a 0b  0c 0d 0e 0f\n" +
          "-------- | --------------------------------------------------\n" +
          "00000000 | ");
      for (int i = 0; i < vs.Length; i++)
      {
        sb.Append(vs[i].ToString("X2"));
        sb.Append(' ');
        if (col == 3 || col == 7 || col == 11)
          sb.Append(' ');

        if (col < 0xf)
          col++;
        else
        {
          col = 0;
          line++;
          sb.Append('\n');
          sb.Append((line * 0xf).ToString("X8"));
          sb.Append(" | ");
        }

        if (i > 0xE01F)
        {
          sb.Append('\n');
          sb.Append((vs.Length - i).ToString("X8"));
          sb.Append(" bytes data left...");
          break;
        }
      }

      return sb.ToString();
    }

    /// <summary>
    /// Format source code with line numbers.
    /// </summary>
    /// <param name="code">Source code string.</param>
    /// <returns>Formatted string for display or console output.</returns>
    public static string PrintCodeWithLine(string code)
    {
      string[] lines = code.Split('\n');
      StringBuilder sb = new StringBuilder();
      int lineNum = 0;
      int lineCount = lines.Length, lineCountMumLength = lineCount.ToString().Length + 1;
      string lineNumStr;

      sb.Append(lineCount);
      sb.Append(" Lines ");
      sb.Append(code.Length);
      sb.Append(" Chars\n");

      foreach (string line in lines)
      {
        lineNum++;
        lineNumStr = lineNum.ToString();

        sb.Append(lineNumStr);
        for (int i = lineCountMumLength; i > lineNumStr.Length; i--)
          sb.Append(' ');

        sb.Append(line);
        sb.Append('\n');

        if (lineNum > 2048)
        {
          sb.Append(lineCount - lineNum);
          sb.Append(" lines more...\n");
        }
      }

      return sb.ToString();
    }

    /// <summary>
    /// Format source code with line numbers.
    /// </summary>
    /// <param name="code">Source code lines.</param>
    /// <returns>Formatted string for display or console output.</returns>
    public static string PrintCodeWithLine(byte[] code)
    {
      return PrintCodeWithLine(Encoding.UTF8.GetString(code));
    }

    /// <summary>
    /// Format an array as a string.
    /// </summary>
    /// <param name="any">Array to convert.</param>
    /// <returns>Formatted string for display or console output.</returns>
    public static string PrintArrVar(object[] any)
    {
      return StringUtils.ValueArrayToString(any);
    }

    /// <summary>
    /// Validate and retrieve a string argument at a given index.
    /// </summary>
    /// <remarks>
    /// Designed for reading arguments in custom console commands.
    /// 
    /// A missing required argument automatically produces an error message.
    /// </remarks>
    /// <example>
    /// // Register a test console command that reads user arguments:
    /// GameDebugCommandServer.RegisterCommand("mycommand", (keyword, fullCmd, argsCount, args) => {
    ///   // The first result indicates whether retrieval succeeded.
    ///   // The second result is the argument value.
    ///   var ox = DebugUtils.CheckIntDebugParam(0, args, out var nx, true, 0)
    ///   if (!ox) return false;
    ///     Debug.Log("User entered: " + nx);
    ///   return true;
    /// }, 1, "mycommand <count:number> > Test console command")
    /// </example>
    /// <param name="index">Zero-based index of the argument to retrieve.</param>
    /// <param name="arr">User argument array.</param>
    /// <param name="value">Retrieved argument.</param>
    /// <param name="required">Required arguments return false when absent.</param>
    /// <param name="defaultValue">Default returned when an optional argument is absent.</param>
    /// <returns>Whether argument retrieval succeeded.</returns>
    public static bool CheckDebugParam(int index, string[] arr, out string value, bool required = true, string defaultValue = "")
    {
      if (arr.Length <= index)
      {
        if (required)
          Log.E("Debug", "Missing argument [{0}].", index);
        value = defaultValue;
        return false;
      }
      value = arr[index];
      return true;
    }

    /// <summary>
    /// Validate and retrieve an integer argument at a given index.
    /// </summary>
    /// <param name="index">Index of the argument to retrieve.</param>
    /// <param name="arr">User argument array.</param>
    /// <param name="value">Retrieved argument.</param>
    /// <param name="required">Required arguments return false when absent.</param>
    /// <param name="defaultValue">Default returned when an optional argument is absent.</param>
    /// <returns>Whether argument retrieval succeeded.</returns>
    public static bool CheckIntDebugParam(int index, string[] arr, out int value, bool required = true, int defaultValue = 0)
    {
      if (arr.Length <= index)
      {
        if (required)
          Log.E("Debug", "Missing argument [{0}].", index);
        value = defaultValue;
        return false;
      }
      if (!int.TryParse(arr[index], out value))
      {
        Log.E("Debug", "Argument [{0}] is not a valid int.", index);
        return false;
      }
      return true;
    }

    /// <summary>
    /// Validate and retrieve a float argument at a given index.
    /// </summary>
    /// <param name="index">Index of the argument to retrieve.</param>
    /// <param name="arr">User argument array.</param>
    /// <param name="value">Retrieved argument.</param>
    /// <param name="required">Required arguments return false when absent.</param>
    /// <param name="defaultValue">Default returned when an optional argument is absent.</param>
    /// <returns>Whether argument retrieval succeeded.</returns>
    public static bool CheckFloatDebugParam(int index, string[] arr, out float value, bool required = true, float defaultValue = 0)
    {
      if (arr.Length <= index)
      {
        if (required)
          Log.E("Debug", "Missing argument [{0}].", index);
        value = defaultValue;
        return false;
      }
      if (!float.TryParse(arr[index], out value))
      {
        Log.E("Debug", "Argument [{0}] is not a valid float.", index);
        return false;
      }
      return true;
    }

    /// <summary>
    /// Validate and retrieve a boolean argument at a given index.
    /// </summary>
    /// <param name="index">Index of the argument to retrieve.</param>
    /// <param name="arr">User argument array.</param>
    /// <param name="value">Retrieved argument.</param>
    /// <param name="required">Required arguments return false when absent.</param>
    /// <param name="defaultValue">Default returned when an optional argument is absent.</param>
    /// <returns>Whether argument retrieval succeeded.</returns>
    public static bool CheckBoolDebugParam(int index, string[] arr, out bool value, bool required = true, bool defaultValue = false)
    {
      if (arr.Length <= index)
      {
        if (required)
          Log.E("Debug", "Missing argument [{0}].", index);
        value = defaultValue;
        return false;
      }
      if (!bool.TryParse(arr[index], out value))
      {
        Log.E("Debug", "Argument [{0}] is not a valid bool.", index);
        return false;
      }
      return true;
    }

    /// <summary>
    /// Validate and retrieve a double argument at a given index.
    /// </summary>
    /// <param name="index">Index of the argument to retrieve.</param>
    /// <param name="arr">User argument array.</param>
    /// <param name="value">Retrieved argument.</param>
    /// <param name="required">Required arguments return false when absent.</param>
    /// <param name="defaultValue">Default returned when an optional argument is absent.</param>
    /// <returns>Whether argument retrieval succeeded.</returns>
    public static bool CheckDoubleDebugParam(int index, string[] arr, out double value, bool required = true, double defaultValue = 0)
    {
      if (arr.Length <= index)
      {
        if (required)
          Log.E("Debug", "Missing argument [{0}].", index);
        value = defaultValue;
        return false;
      }
      if (!double.TryParse(arr[index], out value))
      {
        Log.E("Debug", "Argument [{0}] is not a valid double.", index);
        return false;
      }
      return true;
    }

    /// <summary>
    /// Validate and retrieve a string argument at a given index.
    /// </summary>
    /// <param name="index">Index of the argument to retrieve.</param>
    /// <param name="arr">User argument array.</param>
    /// <param name="value">Retrieved argument.</param>
    /// <param name="required">Required arguments return false when absent.</param>
    /// <param name="defaultValue">Default returned when an optional argument is absent.</param>
    /// <returns>Whether argument retrieval succeeded.</returns>
    public static bool CheckStringDebugParam(int index, string[] arr, bool required = true)
    {
      if (arr.Length <= index)
      {
        if (required)
          Log.E("Debug", "Missing argument [{0}].", index);
        return false;
      }
      if (string.IsNullOrEmpty(arr[index]))
      {
        Log.E("Debug", "Argument [{0}] is not a valid string (empty).", index);
        return false;
      }
      return true;
    }

    /// <summary>
    /// Validate and retrieve an enum argument at a given index.
    /// </summary>
    /// <param name="index">Index of the argument to retrieve.</param>
    /// <param name="arr">User argument array.</param>
    /// <param name="value">Retrieved argument.</param>
    /// <param name="required">Required arguments return false when absent.</param>
    /// <param name="defaultValue">Default returned when an optional argument is absent.</param>
    /// <returns>Whether argument retrieval succeeded.</returns>
    public static bool CheckEnumDebugParam<T>(int index, string[] arr, out T value, bool required = true, T defaultValue = default(T)) where T : struct
    {
      if (arr.Length <= index)
      {
        if (required)
          Log.E("Debug", "Missing argument [{0}].", index);
        value = defaultValue;
        return false;
      }
      if (!System.Enum.TryParse<T>(arr[index], out value))
      {
        Log.E("Debug", "Argument [{0}] is not a valid {1}.", index, typeof(T).Name);
        return false;
      }
      return true;
    }
  }
}
