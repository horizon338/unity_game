using UnityEngine;

// Patrols a fixed range, stops and shoots when the player is close, and shows a health bar.
public class Enemy : MonoBehaviour
{
    public int maxHealth = 4;
    public float patrolDistance = 2f;
    public float speed = 1.2f;

    [Header("Fire")]
    public EnemyBullet bulletPrefab;
    public Transform muzzle;
    public float fireRange = 10f;
    public float fireInterval = 2.6f;

    [Header("Health bar")]
    public Transform healthFill;

    [Header("Death")]
    public SpriteBurst deathBurst;

    int health;
    float originX;
    int direction = 1;
    float nextFireTime;
    float fullBarScale;
    float fullBarWidth;
    float flashTimer;
    CharacterAnimator animator;
    SpriteRenderer spriteRenderer;

    void Awake()
    {
        health = maxHealth;
        originX = transform.position.x;
        animator = GetComponent<CharacterAnimator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (healthFill != null)
        {
            fullBarScale = healthFill.localScale.x;
            SpriteRenderer fillRenderer = healthFill.GetComponent<SpriteRenderer>();
            float spriteWidth = fillRenderer != null && fillRenderer.sprite != null
                ? fillRenderer.sprite.bounds.size.x
                : 0.5f;
            fullBarWidth = fullBarScale * spriteWidth;
        }
        // Stagger the first shot so a group does not fire in lockstep.
        nextFireTime = Time.time + Random.Range(0.6f, fireInterval);
    }

    void Update()
    {
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f && spriteRenderer != null) spriteRenderer.color = Color.white;
        }

        PlayerController player = PlayerController.Instance;
        bool aiming = player != null
            && Vector2.Distance(transform.position, player.transform.position) <= fireRange;

        if (aiming)
        {
            Face(player.transform.position.x);
            if (bulletPrefab != null && muzzle != null && Time.time >= nextFireTime) Fire(player);
        }
        else
        {
            Patrol();
            if (animator != null) animator.faceDirection = false;
            FaceMuzzle(direction);
        }
    }

    void Patrol()
    {
        transform.Translate(Vector3.right * direction * speed * Time.deltaTime);
        float offset = transform.position.x - originX;
        if (offset > patrolDistance) direction = -1;
        else if (offset < -patrolDistance) direction = 1;
    }

    // While aiming the enemy stands still and turns to the player, so the shot comes from the gun tip.
    void Face(float targetX)
    {
        bool right = targetX >= transform.position.x;
        if (animator != null)
        {
            animator.faceDirection = true;
            animator.faceRight = right;
        }
        FaceMuzzle(right ? 1 : -1);
    }

    void FaceMuzzle(int side)
    {
        if (muzzle == null) return;
        Vector3 p = muzzle.localPosition;
        p.x = Mathf.Abs(p.x) * (side >= 0 ? 1f : -1f);
        muzzle.localPosition = p;
    }

    void Fire(PlayerController player)
    {
        nextFireTime = Time.time + fireInterval * Random.Range(0.8f, 1.25f);

        Vector2 aim = (Vector2)player.transform.position + new Vector2(0f, 0.1f) - (Vector2)muzzle.position;
        if (aim.sqrMagnitude < 0.01f) return;

        EnemyBullet bullet = Instantiate(bulletPrefab, muzzle.position, Quaternion.identity);
        bullet.Launch(aim);
    }

    public void TakeDamage(int amount)
    {
        health -= amount;

        if (health > 0)
        {
            // Brief red tint so hits read clearly even without hit-stop.
            flashTimer = 0.08f;
            if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.55f, 0.55f);
        }
        else if (deathBurst != null)
        {
            Instantiate(deathBurst, transform.position, Quaternion.identity);
        }

        RefreshBar();
        if (health <= 0) gameObject.SetActive(false);
    }

    // The fill is centred on its transform, so the position keeps the left edge fixed as width shrinks.
    void RefreshBar()
    {
        if (healthFill == null || fullBarWidth <= 0f) return;

        float fraction = Mathf.Clamp01((float)health / Mathf.Max(1, maxHealth));
        Vector3 scale = healthFill.localScale;
        scale.x = fullBarScale * fraction;
        healthFill.localScale = scale;

        Vector3 position = healthFill.localPosition;
        position.x = -fullBarWidth * 0.5f * (1f - fraction);
        healthFill.localPosition = position;
        healthFill.gameObject.SetActive(fraction > 0f);
    }
}
