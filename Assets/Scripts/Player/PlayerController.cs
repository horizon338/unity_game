using UnityEngine;

// Side-scrolling player: run, jump (variable height), 8-direction aim, auto-fire, health with
// a short invulnerability window after each hit.
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
    public int maxHealth = 5;
    [Tooltip("Seconds of blinking invulnerability after taking a hit.")]
    public float invulnerableTime = 1.2f;
    // Auto-fire is controlled by the AUTO button (InputBridge.AutoFire); FIRE fires while held.

    [Header("Safety")]
    [Tooltip("Respawn when the player falls below this height.")]
    public float killHeight = -8f;

    public static PlayerController Instance { get; private set; }

    public int Health { get; private set; }
    public int MaxHealth { get { return maxHealth; } }
    public float HealthFraction { get { return maxHealth > 0 ? (float)Health / maxHealth : 0f; } }

    Rigidbody2D body;
    SpriteRenderer spriteRenderer;
    CharacterAnimator animator;
    CameraFollow2D cameraFollow;
    Vector3 spawnPosition;
    Vector3 safePosition;
    bool facingRight = true;
    bool grounded;
    bool jumpRequested;
    bool jumping;
    bool jumpCut;
    bool invulnerable;
    float invulnerableTimer;
    float blinkTimer;
    float knockbackTimer;

    void Awake()
    {
        Instance = this;
        body = GetComponent<Rigidbody2D>();
        body.freezeRotation = true;
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<CharacterAnimator>();
        cameraFollow = FindObjectOfType<CameraFollow2D>();
        spawnPosition = transform.position;
        safePosition = spawnPosition;
        Health = maxHealth;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        UpdateInvulnerability();

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
        if (animator != null) animator.airborne = !grounded;
        if (grounded)
        {
            jumping = false;
            jumpCut = false;
            // Remember the last safe footing so a fall into a pit or an enemy hit does not
            // send the player all the way back to the start of the level.
            safePosition = transform.position;
        }

        if (knockbackTimer > 0f) knockbackTimer -= Time.fixedDeltaTime;

        Vector2 v = body.velocity;
        // Input is ignored for a moment after a hit so the knockback is visible.
        if (knockbackTimer <= 0f) v.x = InputBridge.Move.x * moveSpeed;

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
        ContactDamage(collision);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        ContactDamage(collision);
    }

    // Enemies hurt on touch instead of killing instantly; invulnerability limits the rate.
    void ContactDamage(Collision2D collision)
    {
        Enemy enemy = collision.collider.GetComponent<Enemy>();
        if (enemy != null) TakeDamage(1, enemy.transform.position.x);
    }

    public void TakeDamage(int amount, float sourceX)
    {
        if (amount <= 0 || invulnerable) return;

        Health = Mathf.Max(0, Health - amount);
        invulnerable = true;
        invulnerableTimer = invulnerableTime;
        blinkTimer = 0f;

        float away = transform.position.x >= sourceX ? 1f : -1f;
        body.velocity = new Vector2(away * 5f, 7f);
        knockbackTimer = 0.18f;
        if (cameraFollow != null) cameraFollow.Shake(0.22f, 0.22f);

        if (Health <= 0)
        {
            // Out of health: continue from the last footing with a full bar.
            Health = maxHealth;
            Respawn();
        }
    }

    void UpdateInvulnerability()
    {
        if (!invulnerable) return;

        invulnerableTimer -= Time.deltaTime;
        blinkTimer += Time.deltaTime;
        if (spriteRenderer != null) spriteRenderer.enabled = Mathf.FloorToInt(blinkTimer * 14f) % 2 == 0;

        if (invulnerableTimer <= 0f)
        {
            invulnerable = false;
            if (spriteRenderer != null) spriteRenderer.enabled = true;
        }
    }

    void Respawn()
    {
        transform.position = safePosition;
        body.velocity = Vector2.zero;
        jumpRequested = false;
        jumping = false;
        knockbackTimer = 0f;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null) Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
