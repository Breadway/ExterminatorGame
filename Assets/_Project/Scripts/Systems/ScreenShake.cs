using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Camera feedback: noise-driven shake plus a quick zoom punch. Uses unscaled time so effects
/// started during a freeze or pause still settle. Both respect the player's screen shake setting.
/// </summary>
public class ScreenShake : MonoBehaviour
{
    [SerializeField] private CinemachineBasicMultiChannelPerlin noise;
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private float maxAmplitude = 2.2f;
    [SerializeField] private float decayPerSecond = 3.5f;
    [Tooltip("How much the lens narrows at full punch (orthographic size units).")]
    [SerializeField] private float maxZoomPunch = 0.35f;
    [SerializeField] private float zoomRecoverPerSecond = 2.6f;

    private float energy;
    private float punch;
    private float baseSize = -1f;

    void OnEnable()
    {
        GameEvents.OnScreenShake += Kick;
        GameEvents.OnZoomPunch += Punch;
    }

    void OnDisable()
    {
        GameEvents.OnScreenShake -= Kick;
        GameEvents.OnZoomPunch -= Punch;
        if (noise != null) noise.AmplitudeGain = 0f;
        RestoreLens();
    }

    void Start()
    {
        if (noise == null) noise = FindFirstObjectByType<CinemachineBasicMultiChannelPerlin>();
        if (virtualCamera == null) virtualCamera = FindFirstObjectByType<CinemachineCamera>();
        if (noise != null) noise.AmplitudeGain = 0f;
        if (virtualCamera != null) baseSize = virtualCamera.Lens.OrthographicSize;
    }

    private void Kick(float strength)
    {
        if (!SaveSystem.Data.screenShake) return;
        energy = Mathf.Min(1f, energy + Mathf.Clamp01(strength));
    }

    private void Punch(float strength)
    {
        if (!SaveSystem.Data.screenShake) return;
        punch = Mathf.Max(punch, Mathf.Clamp01(strength));
    }

    private void RestoreLens()
    {
        if (virtualCamera != null && baseSize > 0f)
        {
            var lens = virtualCamera.Lens;
            lens.OrthographicSize = baseSize;
            virtualCamera.Lens = lens;
        }
    }

    void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        if (noise != null)
        {
            energy = Mathf.MoveTowards(energy, 0f, decayPerSecond * dt);
            noise.AmplitudeGain = energy * maxAmplitude;
        }
        if (virtualCamera != null && baseSize > 0f)
        {
            punch = Mathf.MoveTowards(punch, 0f, zoomRecoverPerSecond * dt);
            var lens = virtualCamera.Lens;
            lens.OrthographicSize = baseSize - punch * maxZoomPunch;
            virtualCamera.Lens = lens;
        }
    }
}
