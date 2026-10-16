using UnityEngine;

/// <summary>
/// A glowing document left in document-cache and hidden-cache rooms. Touching it opens the text
/// popup and consumes the pickup. Bobbing and a blink warn before it despawns.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class DocumentPickup : MonoBehaviour
{
    [Tooltip("Empty = draw a random unread document when picked up.")]
    public string documentId = "";
    [SerializeField] private float magnetRange = 2.2f;
    [SerializeField] private float magnetSpeed = 7f;
    [SerializeField] private float lifetime = 30f;

    private float dieAt;
    private float bobSeed;
    private Vector3 basePosition;
    private bool taken;

    void Awake()
    {
        GetComponent<CircleCollider2D>().isTrigger = true;
        bobSeed = Random.value * 6.28f;
    }

    void OnEnable()
    {
        dieAt = Time.time + lifetime;
        basePosition = transform.position;
        taken = false;
    }

    void Update()
    {
        if (taken) return;
        if (Time.time >= dieAt) { Destroy(gameObject); return; }
        var player = PlayerController.Instance;
        if (player != null)
        {
            Vector2 to = (Vector2)player.transform.position - (Vector2)transform.position;
            if (to.magnitude < magnetRange)
            {
                transform.position += (Vector3)(to.normalized * magnetSpeed * Time.deltaTime);
                basePosition = transform.position;
            }
            else
            {
                transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * 3f + bobSeed) * 0.08f);
            }
        }
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = Time.time < dieAt - 3f || Mathf.FloorToInt(Time.time * 8f) % 2 == 0;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (taken) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;
        taken = true;
        if (LoreSystem.Instance != null) LoreSystem.Instance.ShowDocument(documentId);
        else GameEvents.ShowNotification("UNFILED PAPERWORK");
        Destroy(gameObject);
    }
}
