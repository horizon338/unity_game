using UnityEngine;

// Designer-tunable weapon numbers. Create via Assets > Create > Contra > Weapon Data.
[CreateAssetMenu(menuName = "Contra/Weapon Data", fileName = "WeaponData")]
public class WeaponData : ScriptableObject
{
    [Tooltip("Shots per second.")]
    public float fireRate = 8f;

    [Tooltip("Bullet speed in world units per second.")]
    public float bulletSpeed = 12f;

    [Tooltip("Seconds before an unused bullet is returned to the pool.")]
    public float bulletLifetime = 1.2f;

    [Tooltip("Damage per bullet.")]
    public int damage = 1;
}
