using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* StringUtils.cs
* 
* Purpose:
* String helpers.
*
* Author:
* mengyu
*
*/

namespace Ballance2.Utils
{
  /// <summary>
  /// String helpers.
  /// </summary>
  /// <remarks>
  /// String processing utilities.
  /// </remarks>
  public static class StringUtils
  {
    /// <summary>
    /// Check whether a string is empty.
    /// </summary>
    /// <param name="text">String to check.</param>
    /// <returns>True if null or empty; otherwise false.</returns>
    public static bool isNullOrEmpty(string text)
    {
      return string.IsNullOrEmpty(text);
    }
    /// <summary>
    /// Check whether a string is empty or whitespace.
    /// </summary>
    /// <param name="text">String to check.</param>
    /// <returns>True if null, empty, or whitespace; otherwise false.</returns>
    public static bool IsNullOrWhiteSpace(string text)
    {
      return string.IsNullOrWhiteSpace(text);
    }
    /// <summary>
    /// Check whether a string is a URL.
    /// </summary>
    /// <param name="text">String to check.</param>
    /// <returns>True for a valid URL; otherwise false.</returns>
    public static bool IsUrl(string text)
    {
      return !isNullOrEmpty(text) && Regex.IsMatch(text, "(https?|ftp|file)://[-A-Za-z0-9+&@#/%?=~_|!:,.;]+[-A-Za-z0-9+&@#/%=~_|]");
    }
    /// <summary>
    /// Check whether a string is an integer.
    /// </summary>
    /// <param name="text">String to check.</param>
    /// <returns>True for a valid integer string; otherwise false.</returns>
    public static bool IsNumber(string text)
    {
      return !isNullOrEmpty(text) && Regex.IsMatch(text, "^-?[1-9]\\d*$");
    }
    /// <summary>
    /// Check whether a string is a floating-point number.
    /// </summary>
    /// <param name="text">String to check.</param>
    /// <returns>True for a valid floating-point string; otherwise false.</returns>
    public static bool IsFloatNumber(string text)
    {
      return !isNullOrEmpty(text) && Regex.IsMatch(text, "^(-?\\d+)(\\.\\d+)?");
    }
    /// <summary>
    /// Check whether a string is a package identifier.
    /// </summary>
    /// <param name="text">String to check.</param>
    /// <returns>True for a valid package identifier; otherwise false.</returns>
    public static bool IsPackageName(string text)
    {
      if (isNullOrEmpty(text)) return false;
      return Regex.IsMatch(text, "^([a-zA-Z]+[.][a-zA-Z]+)[.]*.*");
    }
    /// <summary>
    /// Compare two version strings.
    /// </summary>
    /// <param name="version1">Version 1.</param>
    /// <param name="version2">Version 2.</param>
    /// <returns>-1 if version 1 is earlier, 1 if later, and 0 if equal.</returns>
    public static int CompareTwoVersion(string version1, string version2)
    {
      if (version1 == version2) return 0;
      long ver1 = 0, ver2 = 0;

      string[] vf = version1.Split('.');
      int v = 0;
      for (int i = 0; i < vf.Length; i++)
      {
        if (int.TryParse(vf[i], out v))
          ver1 += v * (int)Math.Pow(10, i);
      }
      vf = version2.Split('.');
      for (int i = 0; i < vf.Length; i++)
      {
        if (int.TryParse(vf[i], out v))
          ver2 += v * (int)Math.Pow(10, i);
      }

      if (ver1 == ver2) return 0;
      return ver1 < ver2 ? -1 : 1;
    }
    /// <summary>
    /// Replace &lt;br&gt; with a newline.
    /// </summary>
    /// <param name="str">String to process.</param>
    /// <returns>Processed string.</returns>
    public static string ReplaceBrToLine(string str)
    {
      return str.Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n"); ;
    }
    /// <summary>
    /// Convert a color string to Color.
    /// 
    /// <br/>Supports named colors defined by Color, listed below:
    /// <br/>* black ： Color.black
    /// <br/>* blue ： Color.blue
    /// <br/>* clear ： Color.clear
    /// <br/>* cyan ： Color.cyan
    /// <br/>* gray ： Color.gray
    /// <br/>* green ： Color.green
    /// <br/>* magenta ： Color.magenta
    /// <br/>* red ： Color.red
    /// <br/>* white ： Color.white
    /// <br/>* yellow ： Color.yellow
    /// <br/>Also supports hexadecimal strings such as #ffffff and RGB strings such as 255,255,255.
    /// </summary>
    /// <param name="color">Color string to convert.</param>
    /// <returns>Parsed color; Color.black on failure.</returns>
    public static Color StringToColor(string color)
    {
      switch (color)
      {
        case "black": return Color.black;
        case "blue": return Color.blue;
        case "clear": return Color.clear;
        case "cyan": return Color.cyan;
        case "gray": return Color.gray;
        case "green": return Color.green;
        case "magenta": return Color.magenta;
        case "red": return Color.red;
        case "white": return Color.white;
        case "yellow": return Color.yellow;
        default:
          Color nowColor;
          if (ColorUtility.TryParseHtmlString(color, out nowColor))
            return nowColor;
          else if (color.Contains(","))
          {
            string[] s = color.Split(',');
            if (s.Length >= 3)
            {
              int r, g, b, a = 255;
              int.TryParse(s[0], out r);
              int.TryParse(s[1], out g);
              int.TryParse(s[2], out b);

              if (s.Length >= 4) int.TryParse(s[2], out a);

              nowColor = new Color((float)r / 255, (float)g / 255, (float)b / 255, (float)a / 255);
              return nowColor;
            }
          }
          break;
      }
      return Color.black;
    }
    
    /// <summary>
    /// Convert a string array to an argument array.
    /// </summary>
    /// <remarks>
    /// Only float, int, bool, and string values are supported.
    /// </remarks>
    /// <param name="arr">String array to convert.</param>
    /// <returns>Converted argument array.</returns>
    public static object[] TryConvertStringArrayToValueArray(string[] arr)
    {
      return TryConvertStringArrayToValueArray(arr, 0);
    }
    /// <summary>
    /// Convert a string array to an argument array.
    /// </summary>
    /// <remarks>
    /// Only float, int, bool, and string values are supported.
    /// </remarks>
    /// <param name="arr">String array to convert.</param>
    /// <param name="startIndex">Starting array index.</param>
    /// <returns>Converted argument array.</returns>
    public static object[] TryConvertStringArrayToValueArray(string[] arr, int startIndex)
    {
      object[] rs = new object[arr.Length];
      if (arr == null) return rs;

      for (int i = startIndex, ix = 0; i < arr.Length; i++, ix++)
        rs[ix] = TryConvertStringToValue(arr[i]);

      return rs;
    }

    /// <summary>
    /// Convert a string to an argument value.
    /// </summary>
    /// <remarks>
    /// Only float, int, bool, and string values are supported.
    /// </remarks>
    /// <param name="value">String to convert.</param>
    /// <returns>The original string if conversion fails.</returns>
    public static object TryConvertStringToValue(string value)
    {
      object rs = null;
      if (IsFloatNumber(value))
      {
        float v = 0;
        if (float.TryParse(value, out v)) rs = v;
      }
      else if (IsNumber(value))
      {
        int v = 0;
        if (int.TryParse(value, out v)) rs = v;
      }
      else if (value.ToLower() == "true")
        rs = true;
      else if (value.ToLower() == "false")
        rs = false;
      else
        rs = value;

      return rs;
    }

    /// <summary>
    /// Convert an argument array to a string.
    /// </summary>
    /// <param name="arr">Arguments to convert.</param>
    /// <returns>Converted string.</returns>
    public static string ValueArrayToString(object[] arr)
    {
      StringBuilder sb = new StringBuilder();
      if (arr == null)
        sb.Append("null");
      else
      {
        foreach (object o in arr)
        {
          sb.Append("\n");
          if (o == null) sb.Append("null");
          else sb.Append(o.ToString());
        }
      }
      return sb.ToString();
    }

    /// <summary>
    /// Compare byte arrays.
    /// </summary>
    /// <param name="inV">First byte array.</param>
    /// <param name="outV">Second byte array.</param>
    /// <returns>Whether the byte arrays are equal.</returns>
    public static bool TestBytesMatch(byte[] inV, byte[] outV)
    {
      bool rs = true;
      if (inV != null && outV != null)
      {
        if (inV.Length == outV.Length)
        {
          for (int i = 0, c = outV.Length; i < c; i++)
          {
            if (inV[i] != outV[i])
            {
              rs = false;
              break;
            }
          }
        }
      }
      return rs;
    }
    /// <summary>
    /// Compare a specified number of bytes.
    /// </summary>
    /// <param name="inV">First byte array.</param>
    /// <param name="outV">Second byte array.</param>
    /// <returns>Whether the byte arrays are equal.</returns>
    public static bool TestBytesMatchN(byte[] inV, byte[] outV, int len)
    {
      bool rs = true;
      if (inV != null && outV != null)
      {
        for (int i = 0, c = outV.Length; i < c && i < len; i++)
        {
          if (inV[i] != outV[i])
          {
            rs = false;
            break;
          }
        }
      }
      return rs;
    }

    /// <summary>
    /// Remove a UTF-8 BOM.
    /// </summary>
    /// <param name="buffer">Content buffer.</param>
    /// <returns>Processed buffer.</returns>
    public static byte[] FixUtf8BOM(byte[] buffer)
    {
      byte[] bomBuffer = new byte[] { 0xef, 0xbb, 0xbf };
      if (
        buffer.Length > 3
        && TestBytesMatchN(buffer, bomBuffer, 3)
      )
        return buffer.Skip(3).ToArray();
      return buffer;
    }

    /// <summary>
    /// Calculate a string's MD5 hash.
    /// </summary>
    /// <param name="buffer">String.</param>
    /// <returns>MD5 hash.</returns>
    public static string MD5String(string input)
    {
      byte[] inputBytes = Encoding.UTF8.GetBytes(input);
      return MD5(inputBytes);
    }
    /// <summary>
    /// Calculate a byte array's MD5 hash.
    /// </summary>
    /// <param name="inputBytes">Byte array.</param>
    /// <returns>MD5 hash.</returns>
    public static string MD5(byte[] inputBytes)
    {
      // Step 1, calculate MD5 hash from input
      MD5 md5 = System.Security.Cryptography.MD5.Create();
      byte[] hashBytes = md5.ComputeHash(inputBytes);

      // Step 2, convert byte array to hex string
      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < hashBytes.Length; i++)
      {
        sb.Append(hashBytes[i].ToString("X2"));
      }
      return sb.ToString();
    }

    /// <summary>
    /// Wrapper for Encoding.ASCII.GetString.
    /// </summary>
    /// <param name="inputBytes">Byte array.</param>
    /// <returns>Decoded string.</returns>
    public static string GetASCIIBytes(byte[] inputBytes)
    {
      return Encoding.ASCII.GetString(inputBytes);
    }
    /// <summary>
    /// Wrapper for Encoding.UTF8.GetString.
    /// </summary>
    /// <param name="inputBytes">Byte array.</param>
    /// <returns>Decoded string.</returns>
    public static string GetUtf8Bytes(byte[] inputBytes)
    {
      return Encoding.UTF8.GetString(inputBytes);
    }
    /// <summary>
    /// Wrapper for Encoding.Unicode.GetString.
    /// </summary>
    /// <param name="inputBytes">Byte array.</param>
    /// <returns>Decoded string.</returns>
    public static string GetUnicodeBytes(byte[] inputBytes)
    {
      return Encoding.Unicode.GetString(inputBytes);
    }
    /// <summary>
    /// Wrapper for Encoding.ASCII.GetBytes.
    /// </summary>
    /// <param name="input">Input string.</param>
    /// <returns>Byte array.</returns>
    public static byte[] StringToASCIIBytes(string input)
    {
      return Encoding.ASCII.GetBytes(input);
    }
    /// <summary>
    /// Wrapper for Encoding.UTF8.GetBytes.
    /// </summary>
    /// <param name="input">Input string.</param>
    /// <returns>Byte array.</returns>
    public static byte[] StringToUtf8Bytes(string input)
    {
      return Encoding.UTF8.GetBytes(input);
    }
    /// <summary>
    /// Wrapper for Encoding.Unicode.GetBytes.
    /// </summary>
    /// <param name="input">Input string.</param>
    /// <returns>Byte array.</returns>
    public static byte[] StringToUnicodeBytes(string input)
    {
      return Encoding.Unicode.GetBytes(input);
    }
    /// <summary>
    /// Find a substring and return the remainder of input after its end.
    /// </summary>
    /// <param name="input">Input string to slice.</param>
    /// <param name="find">Substring to find.</param>
    /// <returns></returns>
    public static string RemoveStringByStringStart(string input, string find)
    {
      int index = input.IndexOf(find);
      if(index > 0) 
        return input.Substring(index + find.Length);
      return input;
    }
  }
}
