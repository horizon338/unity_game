using UnityEngine;
using UnityEngine.Pool;

// Fires pooled bullets from a muzzle point. Numbers come from WeaponData.
public class Gun : MonoBehaviour
{
    public WeaponData weapon;
    public Bullet bulletPrefab;
    public Transform muzzle;
    public MuzzleFlash muzzleFlash;

    ObjectPool<Bullet> pool;
    float nextShotTime;

    void Awake()
    {
        pool = new ObjectPool<Bullet>(
            createFunc: () => Instantiate(bulletPrefab),
            actionOnGet: b => b.gameObject.SetActive(true),
            actionOnRelease: b => b.gameObject.SetActive(false),
            actionOnDestroy: b => Destroy(b.gameObject),
            collectionCheck: false,
            defaultCapacity: 32,
            maxSize: 128);
    }

    public void TryFire(Vector2 direction, bool facingRight)
    {
        if (weapon == null || bulletPrefab == null || muzzle == null) return;
        if (Time.time < nextShotTime) return;

        nextShotTime = Time.time + 1f / Mathf.Max(0.1f, weapon.fireRate);

        Vector2 dir = direction.sqrMagnitude > 0.01f ? direction : (facingRight ? Vector2.right : Vector2.left);
        Bullet bullet = pool.Get();
        bullet.transform.position = muzzle.position;
        bullet.Launch(pool, dir, weapon.bulletSpeed, weapon.bulletLifetime, weapon.damage);
        if (muzzleFlash != null) muzzleFlash.Play();
    }
}
