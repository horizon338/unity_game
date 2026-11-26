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
    [Tooltip("How many leading frames form the walk cycle. 0 uses every frame.")]
    public int walkFrameCount;
    [Tooltip("Frame shown while airborne. -1 disables the airborne pose.")]
    public int airborneFrame = -1;
    public bool airborne;

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
        int cycle = walkFrameCount > 0 ? Mathf.Min(walkFrameCount, frames.Length) : frames.Length;
        int target = 0;
        if (moving && cycle > 1)
        {
            int step = Mathf.FloorToInt(Time.time * fps);
            target = 1 + Mathf.Abs(step) % (cycle - 1);
        }
        if (airborne && airborneFrame >= 0 && airborneFrame < frames.Length) target = airborneFrame;
        SetFrame(target);
    }

    void SetFrame(int index)
    {
        if (index == currentFrame) return;
        currentFrame = index;
        if (index >= 0 && index < frames.Length) renderer.sprite = frames[index];
    }
}
