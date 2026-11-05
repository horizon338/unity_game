using UnityEngine;

// Slow, aimed projectile fired by enemies. It spawns inside the shooter's collider (and can
// overlap other enemies), so enemy colliders are ignored; only the player and terrain stop it.
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBullet : MonoBehaviour
{
    public float speed = 5.5f;
    public float lifetime = 5f;
    public int damage = 1;

    Rigidbody2D body;
    float age;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    public void Launch(Vector2 direction)
    {
        transform.right = direction;
        body.velocity = direction.normalized * speed;
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= lifetime) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Enemy>() != null) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            player.TakeDamage(damage, transform.position.x);
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Ground")) Destroy(gameObject);
    }
}
