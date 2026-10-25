using System.Collections.Generic;
using Ballance2.Res;
using Ballance2.Services;
using Ballance2.Services.InputManager;
using Ballance2.UI.Utils;
using Ballance2.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/*
* Copyright(c) 2022  mengyu
*
* Module:
* DebugConsole.cs
* 
* Purpose:
* Debug console logic.
*
* Author:
* mengyu
*/

namespace Ballance2.DebugTools 
{
  public class DebugConsole : MonoBehaviour 
  {
    public ScrollRect ConsoleScrollView;
    public RectTransform ConsoleContainerContent;
    public Button ExecButton;
    public InputField CommandInputField;
    public GameObject DebugLogItemPrefab;
    public Toggle CheckBoxAutoScroll;
    public Toggle CheckBoxWarningAndErr;
    public Toggle CheckBoxMessages;
    public InputField FilterLogInputField;
    public Text CommandHelpText;

    private struct LogItem
    {
      public GameObject go;
      public string message;
      public LogLevel level;
      public string stackTrace;
    }
    private int logObserver = 0;
    private bool logAutoScroll = true;
    private bool logShowErrAndWarn = true;
    private bool logShowMessage = true;
    private List<LogItem> logItems = new List<LogItem>();
    private bool logForceDisable = true;
    private int logMaxCount = 256;
    private string logFilter = "";

    private int commandHistoryIndex = 0;
    private List<string> commandHistory = new List<string>();
    private GameDebugCommandServer commandServer = null;
    private int commandInputListenKey1;
    private int commandInputListenKey2;
    private GameUIManager GameUIManager;

    private void Start()
    {
      this.GameUIManager = GameManager.GetSystemService<GameUIManager>();

      // Register a log observer.
      this.logObserver = Log.RegisterLogObserver((level, tag, message, stackTrace) =>
      {
        if (this.logForceDisable)
          return;

        if (this.logItems.Count > this.logMaxCount)
        {
          var nitem = logItems[0];
          this.logItems.RemoveAt(0);
          UnityEngine.Object.Destroy(nitem.go);
        }

        if (message.Length > 8192)
          message = message.Substring(0, 8192) + "...";

        var logColor = Log.GetLogColor(level);
        var t = string.Format("<color=#999>{1}/{2}</color> <color=#{0}>{3}</color>{4}", 
          Log.GetLogColor(level), 
          Log.LogLevelToString(level), 
          tag, 
          message, 
          level >= LogLevel.Warning ? $"\n{stackTrace}" : ""
        );

        var newEle = CloneUtils.CloneNewObjectWithParent(DebugLogItemPrefab, this.ConsoleContainerContent);
        var text = newEle.GetComponent<Text>();
        text.text = t;
        UIAnchorPosUtils.SetUIAnchor((RectTransform)newEle.transform, UIAnchor.Left, UIAnchor.Top);
        UIAnchorPosUtils.SetUIPivot((RectTransform)newEle.transform, UIPivot.TopLeft);

        var item = new LogItem();
        item.go = newEle;
        item.message = message;
        item.level = level;
        item.stackTrace = stackTrace;
        this.logItems.Add(item);

        // Rebuild layout.
        this.RelayoutLogContent();

        text.gameObject.SetActive(true);
        // Scroll to the end.
        if (this.logAutoScroll)
          this.ConsoleScrollView.normalizedPosition = Vector2.zero;
      }, LogLevel.All);

      this.logForceDisable = false;

      // Prepare the command service.
      this.commandServer = GameManager.Instance.GameDebugCommandServer;
      commandServer.RegisterCommand("clear", (keyword, fullCmd, argsCount, args) => {
        ClearCommand();
        commandHistory.Clear();
        commandHistoryIndex = 0;
        return true;
      }, 0, "clear > Clear the console");

      // Replay logs not yet captured.
      Log.SendLogsInTemporary();

      commandHistory.Add("");
      commandInputListenKey1 = this.GameUIManager.ListenKey(KeyCode.UpArrow, (KeyCode key, bool downed) => {
        if(downed && CommandInputField.isFocused) UpCommandHistory();
      });
      commandInputListenKey2 = this.GameUIManager.ListenKey(KeyCode.DownArrow, (KeyCode key, bool downed) => {
        if(downed && CommandInputField.isFocused) DownCommandHistory();
      });
    }
    private void OnDestroy()
    {
      logForceDisable = true;
      Log.UnRegisterLogObserver(logObserver);
      if(this.GameUIManager != null) {
        this.GameUIManager.DeleteKeyListen(commandInputListenKey1);
        this.GameUIManager.DeleteKeyListen(commandInputListenKey2);
      }
    }

    // Lay out the log view.
    private void RelayoutLogContent()
    {
      var transform = ConsoleContainerContent;
      var logFilterEmpty = logFilter == "";
      float h = 0.0f, w = transform.sizeDelta.x;
      for (var i = 0; i < transform.childCount; i++)
      {
        var child = transform.GetChild(i) as RectTransform;
        if(i >= logItems.Count)
          break;
        var log = logItems[i];
        if (
          (logShowErrAndWarn && (log.level == LogLevel.Warning || log.level == LogLevel.Error)) ||
          (logShowMessage && !(log.level == LogLevel.Warning || log.level == LogLevel.Error)) &&
          (logFilterEmpty || log.message.Contains(logFilter))
        )
        {
          child.gameObject.SetActive(true);
          child.anchoredPosition = new Vector2(5, -h);
          if (child.sizeDelta.x > w) 
            w = child.sizeDelta.x;
          h = h + LayoutUtility.GetPreferredSize(child, 1);
        }
        else
          child.gameObject.SetActive(false);
      }
      transform.sizeDelta = new Vector2(w, h < 200 ? 200 : h);
    }
    // Clear log entries.
    private void ClearLogContent()
    {
      var transform = ConsoleContainerContent;
      var c = transform.childCount;
      for (var i = c - 1; i >= 0; i--)
        UnityEngine.Object.Destroy(transform.GetChild(i).gameObject);
      transform.sizeDelta = new Vector2(transform.sizeDelta.x, 300);
    }
    
    // AutoScroll changed.
    public void OnAutoScrollChanged()
    {
      logAutoScroll = CheckBoxAutoScroll.isOn;
    }
    // Error visibility checkbox changed.
    public void OnShowErrCheckChange()
    {
      logShowErrAndWarn = CheckBoxWarningAndErr.isOn;
      RelayoutLogContent();
      SwitchCommandHelp(false);
    }
    // Information visibility checkbox changed.
    public void OnShowMessagesCheckChange()
    {
      logShowMessage = CheckBoxMessages.isOn;
      RelayoutLogContent();
      SwitchCommandHelp(false);
    }
    // Filter input completed.
    public void OnFilterInputEndInput()
    {
      logFilter = FilterLogInputField.text;
      RelayoutLogContent();
      SwitchCommandHelp(false);
    }
    
    private void UpCommandHistory() {
      if(commandHistoryIndex == 0) {
        commandHistory[0] = CommandInputField.text;
        commandHistoryIndex = commandHistory.Count - 1;
      } else {
        if(commandHistoryIndex > 1) {
          commandHistory[commandHistoryIndex] = CommandInputField.text;
          commandHistoryIndex--;
          CommandInputField.text = commandHistory[commandHistoryIndex];
        }
      }
    }    
    private void DownCommandHistory() {
      if(commandHistoryIndex > 0) {
        if(commandHistoryIndex < commandHistory.Count - 1) {
          commandHistory[commandHistoryIndex] = CommandInputField.text;
          commandHistoryIndex ++;
          CommandInputField.text = commandHistory[commandHistoryIndex];
        } else {
          commandHistoryIndex = 0;
          CommandInputField.text = commandHistory[0];
        }
      }
    }
    private void SwitchCommandHelp(bool show) {
      if (show) {
        CommandHelpText.gameObject.SetActive(true);
        ConsoleScrollView.gameObject.SetActive(false);
      } else {
        CommandHelpText.gameObject.SetActive(false);
        ConsoleScrollView.gameObject.SetActive(true);
      }
    }
    private void GenCommandHelp(string text) {
      // Generate command help.
      CommandHelpText.text = commandServer.GenCommandHelp(text);
    }

    // Focus the input field.
    public void OnConsoleShow() {
      RelayoutLogContent();
      EventSystem.current.SetSelectedGameObject(null);
      EventSystem.current.SetSelectedGameObject(CommandInputField.gameObject);
    }
    // Input event.
    public void OnCommandInputFieldChange() {
      string text = CommandInputField.text;
      if (text.StartsWith('/')) {
        SwitchCommandHelp(true);
        GenCommandHelp(text.Substring(1));
      } else {
        SwitchCommandHelp(false);
      }
    }
    // Force garbage collection.
    public void ForceGC() {
      System.GC.Collect();
      Log.D("Debug", "GC Finish");
    }
    // Execute the command.
    public void ExecCommand()
    {
      string enter = CommandInputField.text;
      if (string.IsNullOrEmpty(enter))
        return;
      if (enter.StartsWith('/')) 
      {
        if (commandServer.ExecuteCommand(enter.Substring(1)))
        {
          commandHistory.Add(CommandInputField.text);
          commandHistoryIndex = 0;
          CommandInputField.text = "";
          SwitchCommandHelp(false);
        }
      } 
      else 
      {
        Log.I("User", enter);
        CommandInputField.text = "";
        SwitchCommandHelp(false);
      }
    }
    // Clear the command window.
    public void ClearCommand()
    {
      // Clear log entries.
      ClearLogContent();
      GameManager.GameMediator.NotifySingleEvent("DebugToolsClear");
      // Rebuild layout.
      RelayoutLogContent();
    }
    // Close the command window.
    public void CloseWindow()
    {
      DebugManager.SwitchConsoleVisible();
    }
  }
}