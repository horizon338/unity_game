using UnityEngine;

// Short-lived sprite debris burst used when an enemy is destroyed. The shards are children of
// this object so the whole burst disappears with a single Destroy.
public class SpriteBurst : MonoBehaviour
{
    public Sprite sprite;
    public int count = 9;
    public float speedMin = 2.5f;
    public float speedMax = 5.5f;
    public float lifetime = 0.5f;
    public float gravity = -18f;
    public Color[] palette;
    public int sortingOrder = 12;

    Transform[] shards;
    Vector2[] velocities;
    float[] ages;

    void Start()
    {
        shards = new Transform[count];
        velocities = new Vector2[count];
        ages = new float[count];

        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("Shard");
            go.transform.SetParent(transform, false);
            go.transform.position = transform.position + (Vector3)(Random.insideUnitCircle * 0.2f);
            float scale = Random.Range(0.5f, 1f);
            go.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.color = palette != null && palette.Length > 0
                ? palette[Random.Range(0, palette.Length)]
                : Color.white;

            shards[i] = go.transform;
            velocities[i] = new Vector2(Random.Range(-1f, 1f), Random.Range(0.4f, 1f)).normalized
                * Random.Range(speedMin, speedMax);
            ages[i] = 0f;
        }
    }

    void Update()
    {
        if (shards == null) return;

        float dt = Time.deltaTime;
        bool anyAlive = false;

        for (int i = 0; i < shards.Length; i++)
        {
            if (shards[i] == null) continue;

            ages[i] += dt;
            float k = ages[i] / lifetime;
            if (k >= 1f)
            {
                Destroy(shards[i].gameObject);
                shards[i] = null;
                continue;
            }

            anyAlive = true;
            velocities[i].y += gravity * dt;
            shards[i].position += (Vector3)(velocities[i] * dt);

            SpriteRenderer renderer = shards[i].GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Color c = renderer.color;
                c.a = 1f - k;
                renderer.color = c;
            }
        }

        if (!anyAlive) Destroy(gameObject);
    }
}
