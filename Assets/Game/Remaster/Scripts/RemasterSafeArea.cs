using UnityEngine;

namespace Ballance2.Game.Remaster
{
  public sealed class RemasterSafeArea : MonoBehaviour
  {
    private RectTransform rect;
    private void Awake() => rect = GetComponent<RectTransform>();
    private void LateUpdate()
    {
      if (Screen.width < 1 || Screen.height < 1) return;
      var area = Screen.safeArea;
      rect.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
      rect.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
    }
  }
}
