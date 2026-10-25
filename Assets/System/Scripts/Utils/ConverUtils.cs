/*
* Copyright(c) 2021  mengyu
*
* Module:
* ConverUtils.cs
* 
* Purpose:
* String conversion helpers.
*
* Author:
* mengyu
*
*/

using System;

namespace Ballance2.Utils
{
  /// <summary>
  /// String conversion helpers.
  /// </summary>
  public static class ConverUtils
  {
    private static readonly string TAG = "ConverUtils";

    private static void LogErr(string paramName, string stringValue, string valType, string defaultValue)
    {
      Log.W(TAG, "Failed to parse param {0} because \"{1}\" is not a {2} value, use default value {3} insted.",
              paramName, stringValue, valType, defaultValue);
    }

    /// <summary>
    /// Convert a string to an enum.
    /// </summary>
    /// <param name="stringValue">String to convert.</param>
    /// <param name="defaultValue">Default value.</param>
    /// <param name="paramName">Parameter name used in error logs.</param>
    /// <returns>Parsed enum value, or the default if parsing fails.</returns>
    public static T StringToEnum<T>(string stringValue, T defaultValue, string paramName = "") where T : struct
    {
      if (string.IsNullOrEmpty(stringValue))
        return defaultValue;
      T outValue;
      if (!Enum.TryParse<T>(stringValue, out outValue))
      {
        if (!string.IsNullOrEmpty(paramName))
          LogErr(paramName, stringValue, typeof(T).Name, defaultValue.ToString());
        outValue = defaultValue;
      }
      return outValue;
    }
    /// <summary>
    /// Convert a string to an integer.
    /// </summary>
    /// <param name="stringValue">String to convert.</param>
    /// <param name="defaultValue">Default value.</param>
    /// <param name="paramName">Parameter name used in error logs.</param>
    /// <returns>Parsed integer, or the default if parsing fails.</returns>
    public static int StringToInt(string stringValue, int defaultValue, string paramName = "")
    {
      if (string.IsNullOrEmpty(stringValue))
        return defaultValue;
      int outValue;
      if (!int.TryParse(stringValue, out outValue))
      {
        if (!string.IsNullOrEmpty(paramName))
          LogErr(paramName, stringValue, "int", defaultValue.ToString());
        outValue = defaultValue;
      }
      return outValue;
    }
    /// <summary>
    /// Convert a string to a long integer.
    /// </summary>
    /// <param name="stringValue">String to convert.</param>
    /// <param name="defaultValue">Default value.</param>
    /// <param name="paramName">Parameter name used in error logs.</param>
    /// <returns>Parsed long integer, or the default if parsing fails.</returns>
    public static long StringToLong(string stringValue, long defaultValue, string paramName = "")
    {
      if (string.IsNullOrEmpty(stringValue))
        return defaultValue;
      long outValue;
      if (!long.TryParse(stringValue, out outValue))
      {
        if (!string.IsNullOrEmpty(paramName))
          LogErr(paramName, stringValue, "long", defaultValue.ToString());
        outValue = defaultValue;
      }
      return outValue;
    }
    /// <summary>
    /// Convert a string to a boolean.
    /// </summary>
    /// <param name="stringValue">String to convert.</param>
    /// <param name="defaultValue">Default value.</param>
    /// <param name="paramName">Parameter name used in error logs.</param>
    /// <returns>Parsed boolean, or the default if parsing fails.</returns>
    public static bool StringToBoolean(string stringValue, bool defaultValue, string paramName = "")
    {
      if (string.IsNullOrEmpty(stringValue))
        return defaultValue;
      bool outValue;
      if (stringValue.ToLower() == "true" || stringValue == "1")
        return true;
      if (!bool.TryParse(stringValue, out outValue))
      {
        if (!string.IsNullOrEmpty(paramName))
          LogErr(paramName, stringValue, "bool", defaultValue.ToString());
        outValue = defaultValue;
      }
      return outValue;
    }
    /// <summary>
    /// Convert a string to a float.
    /// </summary>
    /// <param name="stringValue">String to convert.</param>
    /// <param name="defaultValue">Default value.</param>
    /// <param name="paramName">Parameter name used in error logs.</param>
    /// <returns>Parsed float, or the default if parsing fails.</returns>
    public static float StringToFloat(string stringValue, float defaultValue, string paramName = "")
    {
      if (string.IsNullOrEmpty(stringValue))
        return defaultValue;
      float outValue;
      if (!float.TryParse(stringValue, out outValue))
      {
        if (!string.IsNullOrEmpty(paramName))
          LogErr(paramName, stringValue, "float", defaultValue.ToString());
        outValue = defaultValue;
      }
      return outValue;
    }
  }
}
