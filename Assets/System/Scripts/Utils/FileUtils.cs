using System.IO;
using System.Text;
using UnityEngine;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* FileUtils.cs
* 
* Purpose:
* File operation helpers.
*
* Author:
* mengyu
*
*/

namespace Ballance2.Utils
{
  /// <summary>
  /// File helpers.
  /// </summary>
  public class FileUtils
  {
    private static byte[] zipHead = new byte[4] { 0x50, 0x4B, 0x03, 0x04 };
    private static byte[] untyFsHead = new byte[7] { 0x55, 0x6e, 0x69, 0x74, 0x79, 0x46, 0x53 };

    /// <summary>
    /// Check for a ZIP file header.
    /// </summary>
    /// <param name="file">File path to inspect.</param>
    /// <returns>True if the header matches; otherwise false.</returns>
    public static bool TestFileIsZip(string file)
    {
      return TestFileHead(file, zipHead);
    }

    /// <summary>
    /// Check for a UnityFS file header.
    /// </summary>
    /// <param name="file">File path to inspect.</param>
    /// <returns>True if the header matches; otherwise false.</returns>
    public static bool TestFileIsAssetBundle(string file)
    {
      return TestFileHead(file, untyFsHead);
    }

    /// <summary>
    /// Check a custom file header.
    /// </summary>
    /// <param name="file">File path to inspect.</param>
    /// <param name="head">Expected header.</param>
    /// <returns>True if the header matches; otherwise false.</returns>
    public static bool TestFileHead(string file, byte[] head)
    {
      using var stream = File.OpenRead(PathUtils.FixFilePathScheme(file));
      foreach (byte expected in head)
        if (stream.ReadByte() != expected) return false;
      return true;
    }

    /// <summary>
    /// Write a string to a file.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="append">Append to the file.</param>
    /// <param name="data">Content to write.</param>
    public static void WriteFile(string path, bool append, string data)
    {
      if (append) File.AppendAllText(path, data);
      else File.WriteAllText(path, data);
    }
  
    /// <summary>
    /// Check whether a file exists.
    /// </summary>
    /// <param name="path">Path.</param>
    /// <returns></returns>
    public static bool FileExists(string path) { return File.Exists(PathUtils.FixFilePathScheme(path)); }
    
    /// <summary>
    /// Check whether a directory exists.
    /// </summary>
    /// <param name="path">Path.</param>
    /// <returns></returns>
    public static bool DirectoryExists(string path) { return Directory.Exists(path); }
    
    /// <summary>
    /// Create a directory.
    /// </summary>
    /// <param name="path">Path.</param>
    public static void CreateDirectory(string path)
    {
      Directory.CreateDirectory(path);
    }
    
    /// <summary>
    /// Read a file as a string.
    /// </summary>
    /// <param name="path">Path.</param>
    /// <returns></returns>
    public static string ReadFile(string path)
    {
      return File.ReadAllText(path);
    }

    /// <summary>
    /// Read all file contents as bytes.
    /// </summary>
    /// <param name="file">File path.</param>
    /// <returns>Byte array.</returns>
    public static byte[] ReadAllToBytes(string file)
    {
      return File.ReadAllBytes(PathUtils.FixFilePathScheme(file));
    }
    
    /// <summary>
    /// Delete a file.
    /// </summary>
    /// <param name="path">Path.</param>
    public static void RemoveFile(string path)
    {
      if (Directory.Exists(path))
        Directory.Delete(path, true);
      else if (File.Exists(path))
        File.Delete(path);
    }
    
    /// <summary>
    /// Delete a directory.
    /// </summary>
    /// <param name="path">Path.</param>
    public static void RemoveDirectory(string path)
    {
      if (Directory.Exists(path))
        Directory.Delete(path, true);
    }
    
    /// <summary>
    /// Format a byte count as a readable file size.
    /// </summary>
    /// <param name="longFileSize">File size in bytes.</param>
    /// <returns>Readable size, such as 2.5M.</returns>
    public static string GetBetterFileSize(long longFileSize)
    {
      StringBuilder sizeStr = new StringBuilder();
      float fileSize;
      if (longFileSize >= 1073741824)
      {
        fileSize = Mathf.Round(longFileSize / 1073741824 * 100) / 100;
        sizeStr.Append(fileSize);
        sizeStr.Append("G");
      }
      else if (longFileSize >= 1048576)
      {
        fileSize = Mathf.Round(longFileSize / 1048576 * 100) / 100;
        sizeStr.Append(fileSize);
        sizeStr.Append("M");
      }
      else
      {
        fileSize = Mathf.Round(longFileSize / 1024 * 100) / 100;
        sizeStr.Append(fileSize);
        sizeStr.Append("K");
      }
      return sizeStr.ToString();
    }
  }


}
