using UnityEngine;

// Simple patrolling enemy. Kinematic body; bullets damage it and the player respawns on contact.
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    public int maxHealth = 2;
    public float patrolDistance = 2f;
    public float speed = 1.2f;

    int health;
    float originX;
    int direction = 1;

    void Awake()
    {
        health = maxHealth;
        originX = transform.position.x;
    }

    void Update()
    {
        transform.Translate(Vector3.right * direction * speed * Time.deltaTime);

        float offset = transform.position.x - originX;
        if (offset > patrolDistance) direction = -1;
        else if (offset < -patrolDistance) direction = 1;
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0) gameObject.SetActive(false);
    }
}
