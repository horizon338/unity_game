using UnityEngine;
using UnityEngine.UI;

// Drives the HUD health bar from the player's current health.
public class PlayerHealthBar : MonoBehaviour
{
    public PlayerController player;
    public Image fill;

    void Update()
    {
        if (player == null || fill == null) return;
        fill.fillAmount = player.HealthFraction;
    }
}
