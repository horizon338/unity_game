using UnityEngine;
using UnityEngine.EventSystems;

public enum OnScreenButtonType
{
    Jump,
    Fire
}

// Touch button for jump or fire. Holding it sets the held flag; pressing sets a one-shot jump request.
public class OnScreenButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public OnScreenButtonType type;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (type == OnScreenButtonType.Jump)
        {
            InputBridge.OnScreenJumpHeld = true;
            InputBridge.OnScreenJumpDown = true;
        }
        else
        {
            InputBridge.OnScreenFireHeld = true;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (type == OnScreenButtonType.Jump) InputBridge.OnScreenJumpHeld = false;
        else InputBridge.OnScreenFireHeld = false;
    }
}
