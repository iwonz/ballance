using System.Collections;
using Ballance2.Services;
using Ballance2.Utils;
using UnityEngine;

namespace Ballance2.Game.GamePlay.Balls
{
  /// <summary>
  /// Interface for scattering and collecting ball fragments.
  /// </summary>
  public interface IBallPiecesControllManager
  {
    /// <summary>
    /// Start scattering fragments.
    /// </summary>
    /// <param name="data">Fragment data.</param>
    /// <param name="pos">Scatter position.</param>
    /// <param name="minForce">Minimum force.</param>
    /// <param name="maxForce">Maximum force.</param>
    /// <param name="timeLive">Fragment lifetime in seconds.</param>
    void ThrowPieces(BallPiecesData data, Vector3 pos,float minForce, float maxForce, float timeLive);
    /// <summary>
    /// Collect fragments.
    /// </summary>
    /// <param name="data">Fragment data.</param>
    void ResetPieces(BallPiecesData data);
  }

  /// <summary>
  /// Default controller for ball fragment scattering and collection effects.
  /// </summary>
  public class BallPiecesControllManager : MonoBehaviour, IBallPiecesControllManager
  {
    private const string TAG = "BallPiecesControllManager";

    /// <summary>
    /// Start scattering fragments.
    /// </summary>
    /// <param name="data">Fragment data.</param>
    /// <param name="pos">Scatter position.</param>
    /// <param name="minForce">Minimum force.</param>
    /// <param name="maxForce">Maximum force.</param>
    /// <param name="timeLive">Fragment lifetime in seconds.</param>
    public void ThrowPieces(
      BallPiecesData data, 
      Vector3 pos,
      float minForce, 
      float maxForce, 
      float timeLive)
    {
      var FadeManager = GameUIManager.Instance.UIFadeManager;
      var parent = data.parent;

      // Remove physics.
      foreach (var body in data.bodys) {
        if (body.IsPhysicalized)
          body.UnPhysicalize(true);
      }

      parent.SetActive(true);

      // Restore the initial state.
      ObjectStateBackupUtils.RestoreObjectAndChilds(parent);
      // Set position.
      data.parent.transform.position = pos;
      data.throwed = true;

      // Clear the previous delay.
      if (data.delayHideTimerID > 0)
        GameManager.Instance.DeleteTimer(data.delayHideTimerID);
      if (data.fadeOutTimerID > 0)
        GameManager.Instance.DeleteTimer(data.fadeOutTimerID);

      // Force-stop the unfinished fade.
      if (data.fadeObjects.Count > 0) 
      {
        foreach(var value in data.fadeObjects) {
          value.ResetTo(1);
          value.Delete();
        }
        data.fadeObjects.Clear();
      }
      else
      {
        // Show immediately.
        for (int i = 0; i < parent.transform.childCount; i++)
          FadeManager.AddFadeIn(parent.transform.GetChild(i).gameObject, 0.1f, null);
      }

      // Disappear after a delay.
      data.delayHideTimerID = GameTimer.Delay(timeLive * 1000, () => {
        data.delayHideTimerID = 0;
        ResetPieces(data);
      });

      // Create physics bodies and scatter fragments.
      StartCoroutine(PhysicsAndThrowPeices(data, minForce, maxForce));
    }

    /// <summary>
    /// Collect fragments.
    /// </summary>
    /// <param name="data">Fragment data.</param>
    public void ResetPieces(BallPiecesData data)
    {
      var FadeManager = GameUIManager.Instance.UIFadeManager;
      var parent = data.parent;  
      // Clear the previous delay.
      if (data.delayHideTimerID > 0)
        GameTimer.DeleteDelay(data.delayHideTimerID);

      if (data.fadeObjects.Count > 0)
        data.fadeObjects.Clear();

      // Fade out the materials.
      for (int i = 0; i < parent.transform.childCount; i++)
        data.fadeObjects.Add(FadeManager.AddFadeOut(parent.transform.GetChild(i).gameObject, 3, true, null));

      data.throwed = false;

      if (data.fadeOutTimerID > 0)
        GameTimer.DeleteDelay(data.fadeOutTimerID);

      // Delay.
      data.fadeOutTimerID = GameTimer.Delay(2990, () => {
        
        if (data.fadeObjects.Count > 0)
          data.fadeObjects.Clear();
  
        // Remove physics.
        foreach (var body in data.bodys) 
          body.UnPhysicalize(true);

        parent.SetActive(false); // Hide.
      });
    }

    // Create physics bodies and scatter fragments.
    private IEnumerator PhysicsAndThrowPeices(BallPiecesData data, float minForce, float maxForce) {
      var count = 0;
      foreach (var body in data.bodys)
      {
        // Avoid scattering too many fragments at once.
        count++;
        if (count > 3) 
        {
          yield return new WaitForSeconds(0.01f);
          count = 0;
        }
        body.Physicalize(); // Physics.
      }
      foreach (var body in data.bodys)
      {
        count++;
        if (count > 3) 
        {
          yield return new WaitForSeconds(0.01f);
          count = 0;
        }

        var forceDir = body.transform.localPosition;
        body.gameObject.SetActive(true);
        forceDir.y += 2;
        forceDir.Normalize();
        body.Impluse(forceDir * CommonUtils.RandomFloat(minForce, maxForce)); // Apply force.
      }
    }
  }
}