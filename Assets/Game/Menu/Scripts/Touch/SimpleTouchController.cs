using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ballance2.Menu.Touch
{
  public class SimpleTouchController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, ICancelHandler
  {
    public delegate void TouchDelegate(Vector2 value);
    public event TouchDelegate TouchEvent;
    public delegate void TouchStateDelegate(bool touchPresent);
    public event TouchStateDelegate TouchStateEvent;
    [SerializeField] private RectTransform joystickArea;
    private int? pointer;
    private Vector2 movementVector;
    public Vector2 GetTouchPosition => movementVector;
    public bool IsHeld => pointer.HasValue;

    private void Awake()
    {
      // Serialized callbacks remain compatible; direct pointer ownership replaces ScrollRect motion.
      if (TryGetComponent<ScrollRect>(out var scroll)) scroll.enabled = false;
      if (TryGetComponent<EventTrigger>(out var events)) events.enabled = false;
    }

    public static Vector2 Normalize(Vector2 position, float radius)
    {
      var value = Vector2.ClampMagnitude(position / Mathf.Max(1, radius), 1);
      float length = value.magnitude;
      return length <= .12f ? Vector2.zero : value.normalized * ((length - .12f) / .88f);
    }

    public void OnPointerDown(PointerEventData data)
    {
      if (pointer.HasValue || data.button != PointerEventData.InputButton.Left) return;
      pointer = data.pointerId;
      BeginDrag();
      OnDrag(data);
    }

    public void OnDrag(PointerEventData data)
    {
      if (pointer != data.pointerId) return;
      var area = (RectTransform)transform;
      if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, data.position, data.pressEventCamera, out var point)) return;
      float radius = Mathf.Min(area.rect.width, area.rect.height) * .35f;
      movementVector = Normalize(point - area.rect.center, radius);
      if (joystickArea != null) joystickArea.anchoredPosition = movementVector * radius;
      TouchEvent?.Invoke(movementVector);
    }

    public void OnPointerUp(PointerEventData data) { if (pointer == data.pointerId) EndDrag(); }
    public void OnCancel(BaseEventData data) { EndDrag(); }
    public void BeginDrag() { TouchStateEvent?.Invoke(true); }
    public void EndDrag()
    {
      pointer = null;
      movementVector = Vector2.zero;
      if (joystickArea != null) joystickArea.anchoredPosition = Vector2.zero;
      TouchEvent?.Invoke(Vector2.zero);
      TouchStateEvent?.Invoke(false);
    }
    public void OnValueChanged(Vector2 value) { if (IsHeld) TouchEvent?.Invoke(Normalize((Vector2.one * .5f - value) * 2, 1)); }
    private void OnDisable() { EndDrag(); }
    private void OnApplicationFocus(bool focused) { if (!focused) EndDrag(); }
    private void OnApplicationPause(bool paused) { if (paused) EndDrag(); }
  }
}
