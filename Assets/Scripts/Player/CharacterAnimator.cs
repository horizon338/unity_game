using UnityEngine;

// Cycles sprite frames while the transform moves and flips the sprite to face the movement.
// Works for both the player and patrolling enemies (frame 0 is the idle pose).
[RequireComponent(typeof(SpriteRenderer))]
public class CharacterAnimator : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 8f;
    [Tooltip("Enemies set this while aiming so the sprite faces the target instead of the walk direction.")]
    public bool faceDirection;
    public bool faceRight = true;

    SpriteRenderer renderer;
    Vector3 lastPosition;
    int currentFrame = -1;

    void Awake()
    {
        renderer = GetComponent<SpriteRenderer>();
        lastPosition = transform.position;
        if (frames != null && frames.Length > 0) SetFrame(0);
    }

    void LateUpdate()
    {
        if (frames == null || frames.Length == 0) return;

        Vector3 position = transform.position;
        float dx = position.x - lastPosition.x;
        lastPosition = position;

        if (faceDirection) renderer.flipX = !faceRight;
        else if (Mathf.Abs(dx) > 0.0005f) renderer.flipX = dx < 0f;

        bool moving = Mathf.Abs(dx) > 0.0015f;
        int target = 0;
        if (moving && frames.Length > 1)
        {
            int step = Mathf.FloorToInt(Time.time * fps);
            target = 1 + Mathf.Abs(step) % (frames.Length - 1);
        }
        SetFrame(target);
    }

    void SetFrame(int index)
    {
        if (index == currentFrame) return;
        currentFrame = index;
        if (index >= 0 && index < frames.Length) renderer.sprite = frames[index];
    }
}
