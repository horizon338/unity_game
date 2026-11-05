using UnityEngine;
using UnityEngine.InputSystem;

// Single entry point for player input.
// Keyboard works on desktop; on-screen controls write the static fields on touch devices.
public static class InputBridge
{
    // Written by the on-screen controls (see OnScreenStick / OnScreenButton).
    public static Vector2 OnScreenMove;
    public static bool OnScreenJumpHeld;
    public static bool OnScreenFireHeld;
    public static bool OnScreenJumpDown;

    // Auto-fire toggled by the AUTO button. When off, the gun fires only while FIRE is held.
    public static bool AutoFire;

    // Movement in [-1, 1] on both axes.
    public static Vector2 Move
    {
        get
        {
            Vector2 v = OnScreenMove;
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                float x = 0f;
                float y = 0f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;
                v += new Vector2(x, y);
            }
            return Vector2.ClampMagnitude(v, 1f);
        }
    }

    public static bool JumpHeld
    {
        get
        {
            Keyboard kb = Keyboard.current;
            return OnScreenJumpHeld || (kb != null && kb.spaceKey.isPressed);
        }
    }

    public static bool FireHeld
    {
        get
        {
            Keyboard kb = Keyboard.current;
            return OnScreenFireHeld || (kb != null && kb.jKey.isPressed);
        }
    }

    // True once per press. Call from Update only.
    public static bool ConsumeJumpPressed()
    {
        Keyboard kb = Keyboard.current;
        bool pressed = OnScreenJumpDown || (kb != null && kb.spaceKey.wasPressedThisFrame);
        OnScreenJumpDown = false;
        return pressed;
    }
}
