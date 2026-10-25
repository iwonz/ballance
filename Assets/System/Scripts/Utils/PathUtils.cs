using System.Collections.Generic;
using System.IO;
using System.Text;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* PathUtils.cs
* 
* Purpose:
* Path string helpers.
*
* Author:
* mengyu
*
*/

namespace Ballance2.Utils
{
  /// <summary>
  /// Path processing helpers.
  /// </summary>
  public static class PathUtils
  {
    /// <summary>
    /// Check whether a path is absolute.
    /// </summary>
    /// <param name="path">Path.</param>
    /// <returns>Whether the path is absolute.</returns>
    public static bool IsAbsolutePath(string path)
    {
      return Path.IsPathRooted(path) || IsRemotePath(path) || path.StartsWith("file:");
    }

    public static bool IsRemotePath(string path)
    {
      return path.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("jar:", System.StringComparison.OrdinalIgnoreCase);
    }

    public static string ToRequestUrl(string path)
    {
      return IsRemotePath(path) || path.StartsWith("file:") ? path : new System.Uri(Path.GetFullPath(path)).AbsoluteUri;
    }
    /// <summary>
    /// Convert an absolute path to a project-relative path.
    /// </summary>
    /// <param name="path">Absolute path.</param>
    /// <returns></returns>
    public static string ReplaceAbsolutePathToRelativePath(string path)
    {
      path = path.Replace("\\", "/");
      if(IsAbsolutePath(path)) {
        var dir = Directory.GetCurrentDirectory().Replace("\\", "/");
        var index = path.IndexOf(dir);
        return path.Substring(index + dir.Length);
      } else if(path.StartsWith("Assets/"))
        return path.Substring(7);
      else if(path.StartsWith("/Assets/")) 
        return path.Substring(8);

      return path;
    }
#if UNITY_EDITOR
    /**
      * Convert a filesystem path to a project path.
      */
    public static string AbsPathToRelPath(string path) {
      path = path.Replace('\\', '/');
      if(PathUtils.IsAbsolutePath(path)) {
        var current = System.IO.Directory.GetCurrentDirectory().Replace('\\', '/');
        var startIndex = path.IndexOf(current);
        if(startIndex == 0)
          path = path.Substring(current.Length);
        if(path.EndsWith("/"))
          path = path.Remove(path.Length - 1);
        if(path.StartsWith("/"))
          path = path.Remove(0, 1);
      }
      return path;
    }
#endif
    /// <summary>
    /// Combine paths, resolving current-directory and parent-directory segments.
    /// </summary>
    /// <param name="path1">Base path.</param>
    /// <param name="path2">Relative path.</param>
    /// <returns>Resolved full path.</returns>
    public static string JoinTwoPath(string path1, string path2)
    {
      StringBuilder sb = new StringBuilder();
      List<string> dirs1 = new List<string>(path1.Split('/'));
      List<string> dirs2 = new List<string>(path2.Split('/'));

      for (int i = 0; i < dirs2.Count; i++)
      {
        string n = dirs2[i];
        if (n == ".")
          continue;
        else if (n == "..")
          dirs1.RemoveAt(dirs1.Count - 1); // Remove a directory segment.
        else
          dirs1.Add(n); // Add.
      }
      for (int i = 0; i < dirs1.Count; i++)
      {
        sb.Append(dirs1[i]);
        if (i != dirs1.Count - 1 || path2.EndsWith("/"))
          sb.Append('/');
      }
      return sb.ToString();
    }
    /// <summary>
    /// Get the filename without its extension.
    /// </summary>
    /// <param name="path">Path.</param>
    /// <returns></returns>
    public static string GetFileNameWithoutExt(string path)
    {
      bool chooseRight = path.Contains("/") && !path.Contains("\\");
      int lastPos = path.LastIndexOf(chooseRight ? '/' : '\\');
      if (lastPos > 0)
      {
        if (lastPos == path.Length - 1)
        {
          path = path.Remove(path.Length - 1);
          lastPos = path.LastIndexOf(chooseRight ? '/' : '\\');
        }
        path = path.Substring(lastPos + 1);
        if (path.Contains("."))
        {
          lastPos = path.LastIndexOf('.');
          path = path.Substring(0, lastPos);
        }
      }
      return path;
    }
    /// <summary>
    /// Get the filename including its extension.
    /// </summary>
    /// <param name="path">Path.</param>
    /// <returns></returns>
    public static string GetFileName(string path)
    {
      bool chooseRight = path.Contains("/") && !path.Contains("\\");
      int lastPos = path.LastIndexOf(chooseRight ? '/' : '\\');
      if (lastPos > 0)
      {
        if (lastPos == path.Length - 1)
        {
          path = path.Remove(path.Length - 1);
          lastPos = path.LastIndexOf(chooseRight ? '/' : '\\');
        }
        path = path.Substring(lastPos + 1);
      }
      return path;
    }
    /// <summary>
    /// Normalize a file:/// path prefix.
    /// </summary>
    /// <param name="path">Path.</param>
    /// <returns>Path without the file:/// prefix.</returns>
    public static string FixFilePathScheme(string path)
    {
      if (path.StartsWith("file:"))
        return new System.Uri(path).LocalPath;
      return path;
    }

  }
}
