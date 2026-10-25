using Ballance2.Utils;
using Ballance2.Res;
using Ballance2.Services;
using Ballance2.Services.Debug;
using Ballance2.Entry;
using System.Collections.Generic;
using UnityEngine;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* GameSystem.cs
* 
* Purpose:
* Base game system and entry management.
* Initialize the base system and services.
* GameManager manages higher-level services; this class manages the base framework.
*
* Author:
* mengyu
*
*/

/*

Wrap framework initialization; the implementation is in
Assets/System/Scripts/Services/Init/GameSystemInit.cs.

Also registers and resolves required system services, like GameManager.GetSystemService.

The entry point starts the framework through GameSystem.Init and GameSystem.PreInit.

*/

namespace Ballance2
{
  /// <summary>
  /// Base system.
  /// </summary>
  public static class GameSystem
  {
    private const string TAG = "GameSystem";

    #region System entry points

    /// <summary>
    /// System lifecycle handler.
    /// </summary>
    /// <param name="act">Current operation.</param>
    public delegate void SysHandler(int act);

    private static SysHandler sysHandler = null;

    public const int ACTION_INIT = 1;
    public const int ACTION_DESTROY = 2;
    public const int ACTION_FORCE_INT = 3;
    public const int ACTION_PRE_INT = 4;

    /// <summary>
    /// Register the system lifecycle handler.
    /// </summary>
    /// <param name="handler">System lifecycle handler.</param>
    public static void RegSysHandler(SysHandler handler)
    {
      if (sysHandler != null)
      {
        Log.E("GameSystemInit", "SysHandler already set ");
        GameErrorChecker.LastError = GameError.AccessDenined;
        return;
      }
      sysHandler = handler;
    }
    /// <summary>
    /// Quit the player.
    /// </summary>
    public static void QuitPlayer()
    {
#if UNITY_EDITOR
      UnityEditor.EditorApplication.isPlaying = false;
#else
      Application.Quit();
#endif
    }

    #endregion

    #region System services

    // System service registry.
    private static Dictionary<string, object> systemService = new Dictionary<string, object>();

    /// <summary>
    /// Register a system service.
    /// </summary>
    /// <param name="name">Service name.</param>
    /// <param name="classObject">Service object.</param>
    /// <returns></returns>
    public static bool RegSystemService<T>() where T : GameService<T>
    {
      GameObject newManager = CloneUtils.CreateEmptyObject("NewManagerObject");
      T manager = newManager.AddComponent<T>();
      newManager.name = manager.Name;

      if (systemService.ContainsKey(manager.Name))
      {
        GameErrorChecker.LastError = GameError.AlreadyRegistered;
        return false;
      }

      //init
      if (!manager.Initialize())
      {
        Log.E(TAG, "Service {0} init failed ! {1}({2})", manager.Name, GameErrorChecker.LastError, GameErrorChecker.GetLastErrorMessage());
        return false;
      }

      systemService.Add(manager.Name, manager);

      return true;
    }
    /// <summary>
    /// Unregister a system service.
    /// </summary>
    /// <param name="name">Service.</param>
    /// <returns></returns>
    public static bool UnRegSystemService(string name)
    {
      if (!systemService.ContainsKey(name))
      {
        GameErrorChecker.LastError = GameError.NotRegister;
        return false;
      }

      // Release resources.
      var gameService = systemService[name] as GameService<System.Object>;
      gameService.Destroy();

      return systemService.Remove(name);
    }
    /// <summary>
    /// Get a system service.
    /// </summary>
    /// <param name="name">Service name.</param>
    /// <returns></returns>
    public static T GetSystemService<T>() where T : GameService<T>
    {
      if (!systemService.TryGetValue(typeof(T).Name, out var o))
        GameErrorChecker.LastError = GameError.ClassNotFound;
      return o as T;
    }
    

    #endregion

    #region Debug providers

    /// <summary>
    /// System debug provider.
    /// </summary>
    public interface SysDebugProvider
    {
      bool StartDebug();
    }
    public delegate SysDebugProvider SysDebugProviderCheck();

    private static SysDebugProvider sysDebugProvider = null;
    private static SysDebugProviderCheck sysDebugProviderCheck = null;

    /// <summary>
    /// Register a system-level debug provider.
    /// </summary>
    public static void RegSysDebugProvider(SysDebugProviderCheck providerCheck)
    {
      sysDebugProviderCheck = providerCheck;
    }
    /// <summary>
    /// Start system-level debugging.
    /// </summary>
    public static void StartRunDebugProvider()
    {
      if (sysDebugProviderCheck != null)
      {
        sysDebugProvider = sysDebugProviderCheck.Invoke();
        if (sysDebugProvider != null)
          sysDebugProvider.StartDebug();
      }
    }

    #endregion

    #region Initialization

    private static bool sysInit = false;

    /// <summary>
    /// Supply the static resource entry; required before the system can load static resources.
    /// </summary>
    /// <param name="gameEntry"></param>
    internal static void FillResEntry(GameStaticResEntry gameEntry) { gameStaticResEntryInstance = gameEntry; }

    private static GameStaticResEntry gameStaticResEntryInstance = null;

    /// <summary>
    /// Restart mode: when true, restart after Destroy completes.
    /// </summary>
    public static bool IsRestart = false;

    /// <summary>
    /// Pre-initialize only language resources and settings.
    /// </summary>
    public static void PreInit()
    {
      //Call game init
      if (sysHandler == null)
      {
        Log.D(TAG, "Not found SysHandler, did you call RegSysHandler first?");
        GameErrorChecker.ThrowGameError(GameError.ConfigueNotRight, null);
        return;
      }        
      //Call init
      sysHandler(ACTION_PRE_INT);
    }
    /// <summary>
    /// Main initialization entry point.
    /// </summary> 
    public static void Init()
    {
      if (!sysInit)
      {
        sysInit = true;

        try {
          //Init system
          Ballance2.Utils.UnityLogCatcher.Init();
          GameStaticResourcesPool.InitStaticPrefab(gameStaticResEntryInstance.GamePrefab, gameStaticResEntryInstance.GameAssets);

          //Call game init
          if (sysHandler == null)
          {
            Log.D(TAG, "Not found SysHandler, did you call RegSysHandler first?");
            GameErrorChecker.ThrowGameError(GameError.ConfigueNotRight, null);
            return;
          }        
          //Call init
          sysHandler(ACTION_INIT);

          Log.D(TAG, "System init ok");
        } catch(System.Exception e)  {
          GameErrorChecker.ThrowGameError(GameError.ExecutionFailed, "System init failed: \n" + e.ToString());
        }
      }
      else
      {
        Log.D(TAG, "System already init ok");
      }
    }
    /// <summary>
    /// Destroy.
    /// </summary>
    public static void Destroy()
    {
      if (sysInit)
      {
        sysInit = false;

        Log.D(TAG, "System destroy");

        sysHandler?.Invoke(ACTION_DESTROY);

        //Destroy system service
        List<string> serviceNames = new List<string>(systemService.Keys);
        for (int i = serviceNames.Count - 1; i >= 0; i--)
        {
          try
          {
            var service = (systemService[serviceNames[i]] as GameService<System.Object>);
            if (service != null)
              service.Destroy();
          }
          catch (System.Exception e)
          {
            UnityEngine.Debug.LogError("Exception when destroy service " + serviceNames[i] + "," + e.ToString());
          }
        }
        serviceNames.Clear();
        systemService.Clear();

        UnityLogCatcher.Destroy();
        
        if (IsRestart)
        {
          //Restart game
          #if !UNITY_EDITOR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX)
          System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
          #endif
        }
        QuitPlayer();
      }
    }
    /// <summary>
    /// Force the game to stop.
    /// </summary>
    public static void ForceInterruptGame()
    {
      sysHandler?.Invoke(ACTION_FORCE_INT);
    }

    #endregion
  }
}
