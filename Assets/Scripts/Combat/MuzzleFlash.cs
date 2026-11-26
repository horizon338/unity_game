using UnityEngine;

// Brief muzzle flash: shows a bright star sprite for a few frames whenever the gun fires.
public class MuzzleFlash : MonoBehaviour
{
    public SpriteRenderer flash;
    public float duration = 0.05f;

    float timer;

    public void Play()
    {
        if (flash == null) return;
        timer = duration;
        flash.enabled = true;
        flash.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        float scale = Random.Range(0.85f, 1.15f);
        flash.transform.localScale = new Vector3(scale, scale, 1f);
    }

    void Update()
    {
        if (timer <= 0f) return;
        timer -= Time.deltaTime;
        if (timer <= 0f && flash != null) flash.enabled = false;
    }
}
