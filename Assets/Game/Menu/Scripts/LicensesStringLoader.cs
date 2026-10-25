using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/*
 * Copyright (c) 2020  mengyu
 * 
 * Module:
 * LicensesStringLoader.cs
 * 
 * Purpose:
 * Load license text resources.
 * 
 * Author:
 * mengyu
 */

namespace Ballance2.Menu
{
  public class LicensesStringLoader : MonoBehaviour
  {
    [SerializeField]
    public List<string> names = new List<string>();

    void Start()
    {
      LoadStrings();
    }
    void LoadStrings()
    {
      StringBuilder sb = new StringBuilder();
      Text text = GetComponent<Text>();
      foreach (string name in names)
      {
        var con = Resources.Load<TextAsset>("ThirdPartAllLincnse/" + name + ".LICENSE");
        if (con != null)
        {

          sb.AppendLine("");
          sb.AppendLine("------------------");
          sb.AppendLine(name);
          sb.AppendLine("------------------");
          sb.AppendLine("");

          sb.AppendLine(con.text);
        }
      }
      text.text = sb.ToString();
    }
  }
}