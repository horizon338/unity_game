using UnityEngine;

// Side-scrolling player: run, jump (variable height), 8-direction aim, auto-fire.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4.5f;
    public float jumpSpeed = 9.5f;
    [Tooltip("Vertical speed kept when the jump button is released early (short hop).")]
    [Range(0f, 1f)] public float jumpCutFactor = 0.5f;

    [Header("Ground")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.12f;

    [Header("Combat")]
    public Gun gun;
    public Transform muzzle;
    // Auto-fire is controlled by the AUTO button (InputBridge.AutoFire); FIRE fires while held.

    [Header("Safety")]
    [Tooltip("Respawn when the player falls below this height.")]
    public float killHeight = -8f;

    Rigidbody2D body;
    Vector3 spawnPosition;
    bool facingRight = true;
    bool grounded;
    bool jumpRequested;
    bool jumping;
    bool jumpCut;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.freezeRotation = true;
        spawnPosition = transform.position;
    }

    void Update()
    {
        Vector2 move = InputBridge.Move;
        if (Mathf.Abs(move.x) > 0.1f) facingRight = move.x > 0f;

        // Keep the muzzle at the gun tip of the sprite, mirrored while facing left.
        if (muzzle != null)
        {
            Vector3 p = muzzle.localPosition;
            p.x = Mathf.Abs(p.x) * (facingRight ? 1f : -1f);
            muzzle.localPosition = p;
        }

        if (InputBridge.ConsumeJumpPressed() && grounded)
        {
            jumpRequested = true;
        }

        if (gun != null && (InputBridge.AutoFire || InputBridge.FireHeld))
        {
            gun.TryFire(AimDirection(move), facingRight);
        }

        if (transform.position.y < killHeight) Respawn();
    }

    void FixedUpdate()
    {
        grounded = groundCheck != null && IsGrounded();
        if (grounded)
        {
            jumping = false;
            jumpCut = false;
        }

        Vector2 v = body.velocity;
        v.x = InputBridge.Move.x * moveSpeed;

        if (jumpRequested)
        {
            v.y = jumpSpeed;
            jumpRequested = false;
            jumping = true;
            jumpCut = false;
        }
        else if (jumping && !jumpCut && !InputBridge.JumpHeld && v.y > 0f)
        {
            v.y *= jumpCutFactor;
            jumpCut = true;
        }

        body.velocity = v;
    }

    // Stick up = aim up; stick up + side = diagonal; otherwise aim where the player faces.
    Vector2 AimDirection(Vector2 move)
    {
        Vector2 dir = Vector2.zero;
        if (move.y > 0.5f) dir.y = 1f;
        if (Mathf.Abs(move.x) > 0.5f) dir.x = Mathf.Sign(move.x);
        if (dir == Vector2.zero) dir = facingRight ? Vector2.right : Vector2.left;
        return dir.normalized;
    }

    bool IsGrounded()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.attachedRigidbody == body) continue;
            if (hit.CompareTag("Ground")) return true;
        }
        return false;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.GetComponent<Enemy>() != null) Respawn();
    }

    void Respawn()
    {
        transform.position = spawnPosition;
        body.velocity = Vector2.zero;
        jumpRequested = false;
        jumping = false;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null) Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
