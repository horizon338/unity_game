using UnityEngine;

// Falling snow for the winter stage: a box of light sprite flakes follows the camera,
// and each flake drifts down and wraps back to the top when it leaves the box.
public class Snowfall : MonoBehaviour
{
    public Sprite flakeSprite;
    public int count = 90;
    public float width = 30f;
    public float height = 16f;
    public float speedMin = 0.5f;
    public float speedMax = 1.5f;

    Transform camTransform;
    Transform[] flakes;
    float[] speeds;
    float[] phases;
    float[] drifts;

    void Start()
    {
        camTransform = Camera.main != null ? Camera.main.transform : null;

        flakes = new Transform[count];
        speeds = new float[count];
        phases = new float[count];
        drifts = new float[count];

        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("Flake");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(
                Random.Range(-width * 0.5f, width * 0.5f),
                Random.Range(-height * 0.5f, height * 0.5f),
                0f);

            float scale = Random.Range(0.5f, 1.1f);
            go.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = flakeSprite;
            renderer.sortingOrder = 30;
            renderer.color = new Color(1f, 1f, 1f, Random.Range(0.35f, 0.85f));

            flakes[i] = go.transform;
            speeds[i] = Random.Range(speedMin, speedMax);
            phases[i] = Random.Range(0f, Mathf.PI * 2f);
            drifts[i] = Random.Range(0.15f, 0.6f);
        }
    }

    void LateUpdate()
    {
        if (flakes == null) return;

        // Keep the flake box centred on the camera so snow is always on screen.
        if (camTransform != null)
        {
            transform.position = new Vector3(camTransform.position.x, camTransform.position.y + 1f, 0f);
        }

        float t = Time.time;
        for (int i = 0; i < flakes.Length; i++)
        {
            Vector3 p = flakes[i].localPosition;
            p.y -= speeds[i] * Time.deltaTime;
            p.x += Mathf.Sin(t * 0.8f + phases[i]) * drifts[i] * Time.deltaTime;

            if (p.y < -height * 0.5f)
            {
                p.y += height;
                p.x = Random.Range(-width * 0.5f, width * 0.5f);
            }
            if (p.x > width * 0.5f) p.x -= width;
            else if (p.x < -width * 0.5f) p.x += width;

            flakes[i].localPosition = p;
        }
    }
}
