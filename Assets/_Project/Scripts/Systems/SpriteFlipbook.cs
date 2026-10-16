using UnityEngine;

/// <summary>
/// Cycles a SpriteRenderer through a list of sprites at a fixed rate, with a random start offset so
/// a crowd of identical flames does not flicker in lockstep. Uses unscaled time so effects keep
/// animating through hit-stop. No Animator needed.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFlipbook : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 12f;
    [SerializeField] private bool randomizeStart = true;
    [Tooltip("Gentle size pulse on top of the frame animation.")]
    [SerializeField] private float pulse = 0.08f;

    private SpriteRenderer sr;
    private float offset;
    private Vector3 baseScale;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
    }

    void OnEnable()
    {
        offset = randomizeStart ? Random.value * 10f : 0f;
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;
        float t = Time.unscaledTime * framesPerSecond + offset * framesPerSecond;
        sr.sprite = frames[(int)t % frames.Length];
        if (pulse > 0f)
        {
            float s = 1f + Mathf.Sin(Time.unscaledTime * 9f + offset) * pulse;
            transform.localScale = new Vector3(baseScale.x * s, baseScale.y * (2f - s), baseScale.z);
        }
    }

    public void SetFrames(Sprite[] newFrames, float fps)
    {
        frames = newFrames;
        framesPerSecond = fps;
    }
}
