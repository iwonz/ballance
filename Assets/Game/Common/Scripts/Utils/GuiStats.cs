using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/*
 * Copyright (c) 2022 mengyu
 * 
 * Module:
 * GuiStats.cs
 * 
 * Purpose:
 * Display status values in the GUI.
 * 
 * Author:
 * mengyu
 */

namespace Ballance2.Game
{
  /// <summary>
  /// Display status values in the GUI.
  /// </summary>
  public class GuiStats : MonoBehaviour {
    private LinkedList<GuiStatsValue> stats = new LinkedList<GuiStatsValue>();

    [Tooltip("Target Text component when using Text mode.")]
    public Text text;
    [Tooltip("Use a Text component for the final statistics; otherwise draw them directly with GUI.")]
    public bool SetToText = false;

    /// <summary>
    /// Add a status entry.
    /// </summary>
    /// <param name="name">Entry name.</param>
    /// <returns>Entry instance used to update its data.</returns>
    public GuiStatsValue AddStat(string name) {
      GuiStatsValue stat = new GuiStatsValue(this, name);
      stats.AddLast(stat);
      return stat;
    }
    /// <summary>
    /// Remove a status entry.
    /// </summary>
    /// <param name="stat">Entry.</param>
    public void DeleteStat(GuiStatsValue stat) {
      stats.Remove(stat);
    }

    public float UpdateTime = 1;

    [Tooltip("Display rectangle when using GUI mode.")]
    public Rect DisplayArea = new Rect(0, 200, 300, 500);

    private float tick = 0;

    private void Update() {
      if(SetToText) {
        tick += Time.deltaTime;
        if(tick >= UpdateTime) {
          StringBuilder sb = new StringBuilder();

          LinkedListNode<GuiStatsValue> value = stats.First;
          while(value != null) {
            sb.AppendLine(value.Value.NameValue);
            value = value.Next;
          }

          text.text = sb.ToString();
          tick = 0;
        }
      }
    }
    private void OnGUI() {
      if(!SetToText) {
        GUILayout.BeginArea(DisplayArea);
        GUILayout.BeginVertical();

        LinkedListNode<GuiStatsValue> value = stats.First;
        while(value != null) {
          GUILayout.Label(value.Value.NameValue);
          value = value.Next;
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
      }
    }
  }

  public class GuiStatsValue {

    private GuiStats stats;
    
    public GuiStatsValue(GuiStats stats, string name) {
      this.stats = stats;
      Name = name;
    }

    /// <summary>
    /// Get this entry's name.
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// Get this entry's formatted string.
    /// </summary>
    public string NameValue { get; set; } 
    /// <summary> 
    /// Set this entry's data.
    /// </summary>
    public string Value {
      set {
        NameValue = Name + ": " + value;
      }
      get {
        return NameValue;
      }
    }
    
    public void SetVector4Value(Vector4 v) { Value = v.ToString(); }
    public void SetVector3Value(Vector3 v) { Value = v.ToString(); }
    public void SetVector2Value(Vector2 v) { Value = v.ToString(); }

    /// <summary>
    /// Remove this entry.
    /// </summary>
    public void Delete() {
      stats.DeleteStat(this);
    }
  }
}