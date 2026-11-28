using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum OnScreenButtonType
{
    Jump,
    Fire
}

// Touch button for jump or fire. FIRE toggles continuous fire on and off; the button turns
// orange while firing.
public class OnScreenButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public OnScreenButtonType type;
    public Image image;

    Color normalColor;
    Color pressedColor;

    void Awake()
    {
        if (image == null) return;
        normalColor = image.color;
        pressedColor = new Color(normalColor.r * 0.75f, normalColor.g * 0.75f, normalColor.b * 0.75f,
            Mathf.Min(1f, normalColor.a + 0.2f));
        RefreshColor();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (type == OnScreenButtonType.Jump)
        {
            InputBridge.OnScreenJumpHeld = true;
            InputBridge.OnScreenJumpDown = true;
            if (image != null) image.color = pressedColor;
            return;
        }

        // Toggle continuous fire on or off.
        InputBridge.OnScreenFireAuto = !InputBridge.OnScreenFireAuto;
        RefreshColor();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (type == OnScreenButtonType.Jump)
        {
            InputBridge.OnScreenJumpHeld = false;
            if (image != null) image.color = normalColor;
            return;
        }

        RefreshColor();
    }

    void RefreshColor()
    {
        if (image == null || type != OnScreenButtonType.Fire) return;
        image.color = InputBridge.OnScreenFireAuto
            ? new Color(0.95f, 0.62f, 0.16f, 0.85f)
            : normalColor;
    }
}
