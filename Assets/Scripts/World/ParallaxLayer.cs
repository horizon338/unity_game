using UnityEngine;

// Scrolls a background layer slower than the camera to create depth.
// factor 0 = fixed in the world, 1 = glued to the camera. Use ~0.85 for far mountains.
public class ParallaxLayer : MonoBehaviour
{
    public Transform cameraTransform;
    [Range(0f, 1f)] public float factor = 0.85f;

    Vector3 startPosition;
    Vector3 cameraStart;

    void Start()
    {
        startPosition = transform.position;
        if (cameraTransform != null) cameraStart = cameraTransform.position;
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;
        float dx = cameraTransform.position.x - cameraStart.x;
        transform.position = new Vector3(startPosition.x + dx * factor, startPosition.y, startPosition.z);
    }
}
