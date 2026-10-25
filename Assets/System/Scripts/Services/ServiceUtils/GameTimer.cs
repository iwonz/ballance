using static Ballance2.Services.GameManager;

namespace Ballance2.Services
{
  /// <summary>
  /// Timer service wrapping GameManager.
  /// </summary>
  public static class GameTimer
  {
    /// <summary>
    /// Schedule a delayed callback.
    /// </summary>
    /// <param name="sec">Delay in seconds.</param>
    /// <param name="callback">Callback.</param>
    public static int Delay(float sec, VoidDelegate callback)
    {
      return GameManager.Instance.Delay(sec, callback);
    }

    /// <summary>
    /// Cancel a delayed callback by ID.
    /// </summary>
    /// <param name="timerId">ID returned by Delay.</param>
    /// <returns>Whether removal succeeded.</returns>
    public static bool DeleteDelay(int timerId)
    {
      return GameManager.Instance.DeleteDelay(timerId);
    }

    /// <summary>
    /// Create a repeating timer.
    /// </summary>
    /// <param name="loopSec">Timer interval.</param>
    /// <param name="callback">Callback.</param>
    /// <returns>Timer ID; pass it to DeleteTimer to remove the timer.</returns>
    public static int Timer(float loopSec, VoidDelegate callback)
    {
      return GameManager.Instance.Timer(loopSec, callback);
    }

    /// <summary>
    /// Create a repeating timer.
    /// </summary>
    /// <param name="delaySec">Delay before the first timer invocation.</param>
    /// <param name="loopSec">Timer interval.</param>
    /// <param name="callback">Callback.</param>
    /// <returns>Timer ID; pass it to DeleteTimer to remove the timer.</returns>
    public static int Timer(float loopSec, float delaySec, VoidDelegate callback)
    {
      return GameManager.Instance.Timer(loopSec, delaySec, callback);
    }
       
    /// <summary>
    /// Delete a timer by ID.
    /// </summary>
    /// <param name="timerId">ID returned by Timer.</param>
    /// <returns>Whether removal succeeded.</returns>
    public static bool DeleteTimer(int timerId)
    {
      return GameManager.Instance.DeleteTimer(timerId);
    }
  }
}