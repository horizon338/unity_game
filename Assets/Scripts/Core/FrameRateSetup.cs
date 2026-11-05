using UnityEngine;

public class FrameRateSetup : MonoBehaviour
{
    [Tooltip("Target frame rate. 60 for mid-range phones; lower to 30 if the Profiler shows 60 cannot be held.")]
    public int targetFps = 60;

    void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFps;
    }
}
