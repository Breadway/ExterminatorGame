using System.Collections;
using UnityEngine;

/// <summary>
/// Brief freeze frames on heavy impacts. The freeze is measured in real time and restores whatever
/// time scale was active, so it never fights a pause: if a menu opens during a freeze, the menu wins.
/// </summary>
public class HitStop : MonoBehaviour
{
    [SerializeField] private float maxSeconds = 0.12f;
    [SerializeField] private float minGapSeconds = 0.05f;

    private Coroutine running;
    private float lastEnd;
    private float savedScale = 1f;
    private bool frozen;

    void OnEnable() { GameEvents.OnHitStop += Freeze; }

    void OnDisable()
    {
        GameEvents.OnHitStop -= Freeze;
        Release();
    }

    private void Freeze(float seconds, float scale)
    {
        // Only freeze live gameplay, and never stack freezes into a stutter
        if (!SaveSystem.Data.screenShake) return;
        if (Time.timeScale <= 0f && !frozen) return;
        if (Time.unscaledTime - lastEnd < minGapSeconds) return;
        seconds = Mathf.Clamp(seconds, 0.01f, maxSeconds);
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Run(seconds, scale));
    }

    private IEnumerator Run(float seconds, float scale)
    {
        if (!frozen) savedScale = Time.timeScale;
        frozen = true;
        Time.timeScale = Mathf.Clamp(scale, 0f, 1f);
        float end = Time.unscaledTime + seconds;
        while (Time.unscaledTime < end)
        {
            // A menu or pause took over: stop restoring a stale scale
            if (GameManager.Instance != null && GameManager.Instance.gameState != GameState.Playing) { frozen = false; running = null; yield break; }
            yield return null;
        }
        Release();
    }

    private void Release()
    {
        if (frozen)
        {
            var state = GameManager.Instance != null ? GameManager.Instance.gameState : GameState.Playing;
            Time.timeScale = state == GameState.Playing ? Mathf.Max(savedScale, 0.0001f) : 0f;
            if (state == GameState.Playing && savedScale > 0.0001f) Time.timeScale = savedScale;
        }
        frozen = false;
        running = null;
        lastEnd = Time.unscaledTime;
    }
}
