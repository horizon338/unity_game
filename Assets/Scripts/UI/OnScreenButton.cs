using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum OnScreenButtonType
{
    Jump,
    Fire
}

// Touch button for jump or fire. Holding it sets the held flag; pressing sets a one-shot jump request.
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
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (image != null) image.color = pressedColor;
        if (type == OnScreenButtonType.Jump)
        {
            InputBridge.OnScreenJumpHeld = true;
            InputBridge.OnScreenJumpDown = true;
        }
        else
        {
            InputBridge.OnScreenFireHeld = true;
            InputBridge.OnScreenFireDown = true;
            InputBridge.OnScreenFireHoldTime = 0f;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (image != null) image.color = normalColor;
        if (type == OnScreenButtonType.Jump)
        {
            InputBridge.OnScreenJumpHeld = false;
        }
        else
        {
            InputBridge.OnScreenFireHeld = false;
            InputBridge.OnScreenFireHoldTime = 0f;
        }
    }

    void Update()
    {
        // Tracks how long FIRE has been held so taps stay single-shot while holding auto-fires.
        if (type == OnScreenButtonType.Fire && InputBridge.OnScreenFireHeld)
        {
            InputBridge.OnScreenFireHoldTime += Time.unscaledDeltaTime;
        }
    }
}
