using UnityEngine;
using UnityEngine.UI;

// Shows the average FPS over a short window, plus optional debug info (camera, player, visible renderers).
// Used for performance checks on the phone.
[RequireComponent(typeof(Text))]
public class FpsCounter : MonoBehaviour
{
    public float updateInterval = 0.5f;
    public bool showDebugInfo = true;
    public Camera worldCamera;
    public Transform player;

    Text label;
    int frames;
    float timer;

    void Awake()
    {
        label = GetComponent<Text>();
    }

    void Update()
    {
        frames++;
        timer += Time.unscaledDeltaTime;
        if (timer >= updateInterval)
        {
            string text = "FPS " + (frames / timer).ToString("0");
            if (showDebugInfo) text += "\n" + DebugInfo();
            label.text = text;
            frames = 0;
            timer = 0f;
        }
    }

    string DebugInfo()
    {
        string cam = worldCamera != null
            ? worldCamera.transform.position.ToString("F1") + " ortho " + worldCamera.orthographicSize.ToString("F2")
            : "no camera";
        string pl = player != null ? player.position.ToString("F1") : "no player";

        int spritesVisible = 0;
        int spritesTotal = 0;
        foreach (SpriteRenderer sr in FindObjectsOfType<SpriteRenderer>())
        {
            spritesTotal++;
            if (sr.isVisible) spritesVisible++;
        }

        int tilemapsVisible = 0;
        int tilemapsTotal = 0;
        foreach (UnityEngine.Tilemaps.TilemapRenderer tr in FindObjectsOfType<UnityEngine.Tilemaps.TilemapRenderer>())
        {
            tilemapsTotal++;
            if (tr.isVisible) tilemapsVisible++;
        }

        return "cam " + cam + "\nplayer " + pl +
               "\nsprites visible " + spritesVisible + "/" + spritesTotal +
               "  tilemaps visible " + tilemapsVisible + "/" + tilemapsTotal;
    }
}
