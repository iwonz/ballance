using UnityEngine;

/// <summary>
/// Set targetFrameRate.
/// </summary>
public class SetFps : MonoBehaviour
{
  [Tooltip("Target FPS.")]
  public int FPS = 60;
  
  private void Start() {
    Application.targetFrameRate = FPS;
  }
}