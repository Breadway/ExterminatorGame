using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes Unknown rooms more than a stat modifier. Each Unknown room rolls one event: a floor
/// hazard (acid rain, shock plates, webs, leaking fuel), an ambush, a supply cache, or a document
/// cache with lore. Hidden caches use evocative room names and always contain a document.
/// Runs after the arena is built and before the wave spawns, so hazards and pickups are in place.
/// </summary>
public class RoomEventDirector : MonoBehaviour
{
    public static RoomEventDirector Instance { get; private set; }

    [Header("Pickups")]
    [SerializeField] private GameObject healPrefab;
    [SerializeField] private GameObject scrapPrefab;
    [SerializeField] private GameObject documentPrefab;

    public string LastEventLabel { get; private set; } = "";

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Roll an event id for an Unknown room. Hidden caches are rare by design.
    /// Rooms 1-2 only get friendly events: no hazards or ambushes while learning.</summary>
    public static string RollEventId(int number = 99)
    {
        if (number <= 2) return Random.value < 0.5f ? "supply_cache" : "document_cache";
        float roll = Random.value;
        if (roll < 0.16f) return "acid_rain";
        if (roll < 0.30f) return "web_nest";
        if (roll < 0.43f) return "shock_plates";
        if (roll < 0.56f) return "fuel_leak";
        if (roll < 0.68f) return "ambush";
        if (roll < 0.80f) return "supply_cache";
        if (roll < 0.93f) return "document_cache";
        return "hidden_cache";
    }

    public static string Label(string eventId)
    {
        switch (eventId)
        {
            case "acid_rain": return "CAUSTIC RESIDUE";
            case "web_nest": return "WEB NEST";
            case "shock_plates": return "EXPOSED WIRING";
            case "fuel_leak": return "FUEL LEAK";
            case "ambush": return "MOVEMENT DETECTED";
            case "supply_cache": return "SUPPLY CACHE";
            case "document_cache": return "UNFILED PAPERWORK";
            case "hidden_cache": return "DO NOT ENTER";
            default: return "";
        }
    }

    /// <summary>Mutate the wave spec and seed the arena. Call after the layout is built.</summary>
    public void Apply(RoomDefinition room, WaveSpec spec, Rect arena)
    {
        LastEventLabel = "";
        if (room == null || spec == null || string.IsNullOrEmpty(room.eventId)) return;
        LastEventLabel = Label(room.eventId);
        var field = HazardField.Instance;

        switch (room.eventId)
        {
            case "acid_rain":
                Scatter(field, HazardKind.Acid, 7, 1.2f, 30f, 6f, true, true, 0.8f, arena);
                GameEvents.ShowNotification("CAUSTIC RESIDUE DETECTED");
                break;
            case "web_nest":
                Scatter(field, HazardKind.Web, 4, 1.4f, 30f, 0f, true, true, 0.45f, arena);
                spec.enemyCount += 2;
                GameEvents.ShowNotification("WEB NEST");
                break;
            case "shock_plates":
                Scatter(field, HazardKind.Shock, 5, 1.0f, 30f, 8f, true, true, 1f, arena);
                GameEvents.ShowNotification("EXPOSED WIRING");
                break;
            case "fuel_leak":
                Scatter(field, HazardKind.Fuel, 6, 1.0f, 40f, 0f, false, false, 1f, arena);
                GameEvents.ShowNotification("FUEL LEAK - DO NOT IGNITE");
                break;
            case "ambush":
                spec.enemyCount = Mathf.RoundToInt(spec.enemyCount * 1.6f);
                spec.xpMultiplier *= 1.2f;
                GameEvents.ShowNotification("MOVEMENT DETECTED");
                break;
            case "supply_cache":
                Drop(healPrefab, 2, arena);
                Drop(scrapPrefab, 2, arena, 5f);
                break;
            case "document_cache":
                DropDocument(arena, false);
                Drop(healPrefab, 1, arena);
                break;
            case "hidden_cache":
                room.roomName = LoreSystem.HiddenRoomName();
                DropDocument(arena, true);
                spec.enemyCount += 2;
                spec.healthMultiplier *= 1.3f;
                break;
        }
    }

    private static void Scatter(HazardField field, HazardKind kind, int count, float radius, float lifetime,
        float dps, bool hurtsPlayer, bool hurtsEnemies, float slow, Rect arena)
    {
        if (field == null) return;
        for (int i = 0; i < count; i++)
            field.Spawn(kind, RandomPoint(arena), radius * Random.Range(0.8f, 1.2f), lifetime, dps, hurtsPlayer, hurtsEnemies, slow);
    }

    private void Drop(GameObject prefab, int count, Rect arena, float amount = 0f)
    {
        if (prefab == null) return;
        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(prefab, (Vector3)RandomPoint(arena), Quaternion.identity);
            var pickup = go.GetComponent<Pickup>();
            if (pickup != null && amount > 0f) pickup.amount = amount;
        }
    }

    private void DropDocument(Rect arena, bool rare)
    {
        if (documentPrefab == null || LoreSystem.Instance == null) return;
        var go = Instantiate(documentPrefab, (Vector3)RandomPoint(arena), Quaternion.identity);
        var doc = go.GetComponent<DocumentPickup>();
        if (doc != null) doc.documentId = LoreSystem.Instance.DrawDocument(rare);
    }

    private static Vector2 RandomPoint(Rect arena)
    {
        const float margin = 2.5f;
        return new Vector2(
            Random.Range(arena.xMin + margin, arena.xMax - margin),
            Random.Range(arena.yMin + margin, arena.yMax - margin));
    }
}
