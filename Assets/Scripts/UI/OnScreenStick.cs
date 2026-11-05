using UnityEngine;
using UnityEngine.EventSystems;

// Virtual joystick for touch. Writes InputBridge.OnScreenMove while a finger is on it.
// Unity UI tracks each finger separately, so it works together with the buttons (multi-touch).
public class OnScreenStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform knob;

    RectTransform rect;

    void Awake()
    {
        rect = (RectTransform)transform;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Drag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Drag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        InputBridge.OnScreenMove = Vector2.zero;
        if (knob != null) knob.anchoredPosition = Vector2.zero;
    }

    void Drag(PointerEventData eventData)
    {
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out local))
            return;

        float radius = rect.rect.width * 0.4f;
        Vector2 value = Vector2.ClampMagnitude(local / radius, 1f);
        InputBridge.OnScreenMove = value;
        if (knob != null) knob.anchoredPosition = value * radius;
    }
}
