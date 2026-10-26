using UnityEngine;
using UnityEngine.UI;

/*
* Copyright(c) 2022  mengyu
*
* Module:
* GameGlobalIngameLoading.cs
* 
* Purpose:
* Initial loading object behavior.
*
* Author:
* mengyu
*/

namespace Ballance2.UI.Utils
{
  public class GameGlobalIngameLoading : MonoBehaviour {
    public Text TextStatus;

    /* private int currentLogObserver = 0;

    private void OnEnable() {
      TextStatus.text = "";
      currentLogObserver = Log.RegisterLogObserver((level, tag, message, stackTrace) => {
        TextStatus.text = message;
      }, LogLevel.All);
    }
    private void OnDisable() {
      if(currentLogObserver > 0) {
        Log.UnRegisterLogObserver(currentLogObserver);
        currentLogObserver = 0;
      }
    } */
  }
}