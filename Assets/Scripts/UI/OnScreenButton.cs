using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum OnScreenButtonType
{
    Jump,
    Fire
}

// Touch button for jump or fire. FIRE: a tap fires a single shot, holding for 2 seconds latches
// auto-fire, and tapping again cancels auto-fire back to single-shot mode.
public class OnScreenButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    const float AutoFireHoldTime = 2f;

    public OnScreenButtonType type;
    public Image image;

    Color normalColor;
    Color pressedColor;
    float holdTime;

    void Awake()
    {
        if (image == null) return;
        normalColor = image.color;
        pressedColor = new Color(normalColor.r * 0.75f, normalColor.g * 0.75f, normalColor.b * 0.75f,
            Mathf.Min(1f, normalColor.a + 0.2f));
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

        // A tap while auto-fire is latched cancels it; this tap does not fire a bullet.
        if (InputBridge.OnScreenFireAuto)
        {
            InputBridge.OnScreenFireAuto = false;
            InputBridge.OnScreenFireHeld = false;
            holdTime = 0f;
            RefreshColor();
            return;
        }

        InputBridge.OnScreenFireHeld = true;
        InputBridge.OnScreenFireDown = true;
        holdTime = 0f;
        if (image != null) image.color = pressedColor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (type == OnScreenButtonType.Jump)
        {
            InputBridge.OnScreenJumpHeld = false;
            if (image != null) image.color = normalColor;
            return;
        }

        InputBridge.OnScreenFireHeld = false;
        holdTime = 0f;
        RefreshColor();
    }

    void Update()
    {
        if (type != OnScreenButtonType.Fire) return;

        if (InputBridge.OnScreenFireHeld && !InputBridge.OnScreenFireAuto)
        {
            holdTime += Time.unscaledDeltaTime;
            if (holdTime >= AutoFireHoldTime)
            {
                // Long press latches auto-fire; it keeps firing after release until tapped again.
                InputBridge.OnScreenFireAuto = true;
                holdTime = 0f;
            }
        }

        RefreshColor();
    }

    void RefreshColor()
    {
        if (image == null || type != OnScreenButtonType.Fire) return;

        if (InputBridge.OnScreenFireAuto)
            image.color = new Color(0.95f, 0.62f, 0.16f, 0.85f);
        else if (InputBridge.OnScreenFireHeld)
            image.color = pressedColor;
        else
            image.color = normalColor;
    }
}
