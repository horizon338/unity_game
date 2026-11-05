using UnityEngine;

// Side-scrolling camera: follows the target horizontally and keeps a little headroom vertically.
public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.15f;
    public float minX = 0f;
    public float maxX = 80f;
    public float minY = 0f;
    public float maxY = 6f;

    Vector3 velocity;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 goal = new Vector3(
            Mathf.Clamp(target.position.x, minX, maxX),
            Mathf.Clamp(target.position.y + 1f, minY, maxY),
            transform.position.z);

        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
    }
}
