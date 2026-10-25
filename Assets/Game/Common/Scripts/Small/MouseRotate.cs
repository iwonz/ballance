#if UNITY_ANDROID && !UNITY_EDITOR
using Input = Ballance2.InputCompatibility.AndroidInput;
#endif
using UnityEngine;

namespace Ballance2.Game
{
  /// <summary>
  /// Rotate an object while dragging with the mouse.
  /// </summary>
  public class MouseRotate : MonoBehaviour
  {
    private float OffsetX = 0;
    private float OffsetY = 0;

    /// <summary>
    /// Rotation speed.
    /// </summary>
    [Tooltip("Rotation speed.")]
    public float Speed = 6f;// Rotation speed.
    /// <summary>
    /// Allow rotation around X.
    /// </summary>
    [Tooltip("Allow rotation around the X axis.")]
    public bool CanRotateX = true;
    /// <summary>
    /// Allow rotation around Y.
    /// </summary>
    [Tooltip("Allow rotation around the Y axis.")]
    public bool CanRotateY = true;

    void Update()
    {
      if (Input.GetMouseButton(0))
      {
        if(CanRotateX)
          OffsetX = Input.GetAxis("Mouse X");
        if(CanRotateY)
          OffsetY= Input.GetAxis("Mouse Y");

        transform.Rotate(new Vector3(OffsetY, -OffsetX, 0) * Speed, Space.World);
      }
    }
  }
}
