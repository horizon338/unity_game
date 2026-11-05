using UnityEngine;

// Snappier arcade gravity than Unity's default -9.81: shorter airtime, less floaty jumps.
public class PhysicsSetup : MonoBehaviour
{
    public float gravity = -28f;

    void Awake()
    {
        Physics2D.gravity = new Vector2(0f, gravity);
    }
}
