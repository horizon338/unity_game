using UnityEngine;

// Side-scrolling camera: follows the target horizontally and keeps a little headroom vertically.
// Supports short screen shakes for hit feedback.
public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.15f;
    public float minX = 0f;
    public float maxX = 80f;
    public float minY = 0f;
    public float maxY = 6f;

    Vector3 velocity;
    Vector3 basePosition;
    float shakeTimer;
    float shakeDuration = 1f;
    float shakeMagnitude;

    void Awake()
    {
        basePosition = transform.position;
    }

    public void Shake(float duration, float magnitude)
    {
        shakeDuration = Mathf.Max(0.0001f, duration);
        shakeTimer = shakeDuration;
        shakeMagnitude = magnitude;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 goal = new Vector3(
            Mathf.Clamp(target.position.x, minX, maxX),
            Mathf.Clamp(target.position.y + 1f, minY, maxY),
            basePosition.z);

        basePosition = Vector3.SmoothDamp(basePosition, goal, ref velocity, smoothTime);

        Vector3 shake = Vector3.zero;
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float k = Mathf.Clamp01(shakeTimer / shakeDuration);
            Vector2 offset = Random.insideUnitCircle * shakeMagnitude * k;
            shake = new Vector3(offset.x, offset.y, 0f);
        }

        transform.position = basePosition + shake;
    }
}
