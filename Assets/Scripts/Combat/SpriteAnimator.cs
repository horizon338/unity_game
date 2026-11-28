using UnityEngine;

// Cycles sprite frames for simple looping effects such as the player bullet glow.
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimator : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 25f;

    SpriteRenderer spriteRenderer;
    float offset;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        // Desync pooled bullets so they do not all shimmer in lockstep.
        if (frames != null && frames.Length > 0) offset = Random.value * frames.Length;
    }

    void Update()
    {
        if (frames == null || frames.Length == 0 || spriteRenderer == null) return;
        int index = Mathf.FloorToInt(Time.time * fps + offset) % frames.Length;
        spriteRenderer.sprite = frames[index];
    }
}
