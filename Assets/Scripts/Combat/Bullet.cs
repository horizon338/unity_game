using UnityEngine;
using UnityEngine.Pool;

// A pooled bullet. It flies straight, hits enemies or ground, then returns to its pool.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    ObjectPool<Bullet> pool;
    Rigidbody2D body;
    int damage;
    float lifetime;
    float age;
    bool released;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    public void Launch(ObjectPool<Bullet> owner, Vector2 direction, float speed, float life, int dmg)
    {
        pool = owner;
        damage = dmg;
        lifetime = life;
        age = 0f;
        released = false;
        transform.right = direction;
        body.velocity = direction * speed;
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= lifetime) Release();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (released) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Release();
        }
        else if (other.CompareTag("Ground"))
        {
            Release();
        }
    }

    void Release()
    {
        if (released) return;
        released = true;
        body.velocity = Vector2.zero;

        if (pool != null) pool.Release(this);
        else Destroy(gameObject);
    }
}
