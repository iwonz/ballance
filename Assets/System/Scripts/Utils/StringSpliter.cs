using System;
using System.Collections.Generic;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* StringSpliter.cs
* 
* Purpose:
* String splitter.
*
* Split strings according to the configured delimiters.
*
* Author:
* mengyu
*
*/

namespace Ballance2.Utils
{
  /// <summary>
  /// String splitter.
  /// </summary>
  public class StringSpliter
  {
    /// <summary>
    /// Create a string splitter.
    /// </summary>
    /// <param name="s">String to split.</param>
    /// <param name="sp">Delimiter.</param>
    public StringSpliter(string s, char sp)
    {
      if (s.Contains(sp.ToString()))
      {
        buf = s.Split(sp);
        count = buf.Length;
      }
      else
      {
        buf = new string[] { s };
        count = 1;
      }
    }
    /// <summary>
    /// Create a string splitter.
    /// </summary>
    /// <param name="s">String to split.</param>
    /// <param name="sp">Delimiter.</param>
    /// <param name="excludeQuotes">Ignore double quotes.</param>
    public StringSpliter(string s, char sp, bool excludeQuotes)
    {
      if (s.Contains(sp.ToString()))
      {
        if (excludeQuotes)
        {
          string[] bufbuf = s.Split(new char[] { sp }, StringSplitOptions.RemoveEmptyEntries);
          count = bufbuf.Length;
          if (count > 0)
          {
            bool excludeing = false;
            string excludeingCache = null;
            List<string> cache = new List<string>();
            for (int i = 0; i < count; i++)
            {
              if (bufbuf[i].StartsWith("\"") && !excludeing)
              {
                excludeing = true;
                excludeingCache = bufbuf[i];
              }
              else if (bufbuf[i].EndsWith("\""))
              {
                if (excludeing)
                {
                  excludeingCache += " " + bufbuf[i];
                  cache.Insert(cache.Count, excludeingCache);
                  excludeingCache = null;
                  excludeing = false;
                }
                else cache.Insert(cache.Count, bufbuf[i]);
              }
              else
              {
                if (excludeing) excludeingCache += " " + bufbuf[i];
                else cache.Insert(cache.Count, bufbuf[i]);
              }
            }

            buf = cache.ToArray();
            cache.Clear();
            cache = null;
            bufbuf = null;
          }
        }
        else
        {
          buf = s.Split(sp);
          count = buf.Length;
        }
      }
      else
      {
        buf = new string[] { s };
        count = 1;
      }
    }
    /// <summary>
    /// Create a string splitter.
    /// </summary>
    /// <param name="s">String to split.</param>
    /// <param name="sp">Delimiter.</param>
    /// <param name="sp2">Second delimiter.</param>
    public StringSpliter(string s, char sp, char sp2)
    {
      if (s.Contains(sp.ToString()))
      {
        buf = s.Split(new char[] { sp, sp2 }, StringSplitOptions.RemoveEmptyEntries);
        count = buf.Length;
      }
      else
      {
        buf = new string[] { s };
        count = 1;
      }
    }

    private string[] buf = null;
    private int count = 0;

    /// <summary>
    /// Whether splitting succeeded.
    /// </summary>
    public bool Success { get { return count != 0; } }
    /// <summary>
    /// Number of split results.
    /// </summary>
    public int Count
    {
      get
      {
        return count;
      }
    }
    /// <summary>
    /// Split results.
    /// </summary>
    public string[] Result { get { return buf; } }

  }

}