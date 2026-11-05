using UnityEngine;

// Sizes and positions a UI element in dp (density-independent pixels), so touch targets keep the same
// physical size and spacing on any phone. 1 dp = 1 px at 160 dpi, so px = dp * Screen.dpi / 160.
// The anchor (for example bottom-left or bottom-right) is set in the scene; offsetDp is measured from that anchor.
[RequireComponent(typeof(RectTransform))]
public class DpSize : MonoBehaviour
{
    public float widthDp = 64f;
    public float heightDp = 64f;
    public Vector2 offsetDp;
    public Canvas canvas;

    void Start()
    {
        float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
        float pixelsPerDp = dpi / 160f;
        float scale = canvas != null ? canvas.scaleFactor : 1f;

        RectTransform rect = (RectTransform)transform;
        rect.sizeDelta = new Vector2(widthDp * pixelsPerDp, heightDp * pixelsPerDp) / scale;
        rect.anchoredPosition = offsetDp * pixelsPerDp / scale;
    }
}
