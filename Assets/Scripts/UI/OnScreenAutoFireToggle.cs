using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Tap to switch auto-fire on or off. When on, the gun keeps firing without holding FIRE.
public class OnScreenAutoFireToggle : MonoBehaviour, IPointerDownHandler
{
    public Text label;
    public Image background;

    void Start()
    {
        Refresh();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        InputBridge.AutoFire = !InputBridge.AutoFire;
        Refresh();
    }

    void Refresh()
    {
        if (label != null) label.text = InputBridge.AutoFire ? "AUTO ON" : "AUTO OFF";
        if (background != null)
            background.color = InputBridge.AutoFire
                ? new Color(0.85f, 0.59f, 0.20f, 0.62f)
                : new Color(1f, 1f, 1f, 0.28f);
    }
}
