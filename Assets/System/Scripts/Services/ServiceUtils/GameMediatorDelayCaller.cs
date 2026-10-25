using System.Collections.Generic;
using Ballance2.Services;
using UnityEngine;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* GameMediatorDelayCaller.cs
* 
* Purpose:
* Provide delayed event dispatch for GameMediator.
*
* Author:
* mengyu
*/

namespace Ballance2.Utils.ServiceUtils
{

  public class GameMediatorDelayCaller : MonoBehaviour
  {
    public GameMediator GameMediator;

    private void FixedUpdate()
    {
      if (data.Count > 0)
      {
        for (int i = data.Count - 1; i >= 0; i--)
        {
          var d = data[i];
          d.tick--;
          if (d.tick <= 0)
          {
            try {
              switch (d.type)
              {
                case GameMediatorDelayCallType.NormalEvent:
                  GameMediator.DispatchGlobalEvent(d.name, d.args);
                  break;
                case GameMediatorDelayCallType.SingleEvent:
                  GameMediator.NotifySingleEvent(d.name, d.args);
                  break;
              }
            } catch(System.Exception e) {
              Log.W("GameMediatorDelayCaller", "Call Delay event " + d.name + " failed because Exception: " + e);
            }
            data.RemoveAt(i);
          }
        }
      }
    }

    private enum GameMediatorDelayCallType
    {
      SingleEvent,
      NormalEvent
    }
    private List<DelayCallData> data = new List<DelayCallData>();
    private class DelayCallData
    {
      public GameMediatorDelayCallType type;
      public string name;
      public int tick;
      public object[] args;
    }

    public void AddDelayCallSingle(string name, float delayeSecond, object[] args)
    {
      var d = new DelayCallData();
      d.type = GameMediatorDelayCallType.SingleEvent;
      d.name = name;
      d.tick = (int)(delayeSecond / Time.deltaTime);
      d.args = args;
      data.Add(d);
    }
    public void AddDelayCallNormal(string name, float delayeSecond, object[] args)
    {
      var d = new DelayCallData();
      d.type = GameMediatorDelayCallType.NormalEvent;
      d.name = name;
      d.tick = (int)(delayeSecond / Time.deltaTime);
      d.args = args;
      data.Add(d);
    }
  }
}