using UnityEngine;

/// <summary>
/// Applies the selected character (stats, weapon, tint) and the lobby's permanent bonuses to the
/// player at the start of a run. All weapons live on the player disabled except the chosen one,
/// so adding a character is: a weapon component, a CharacterData asset, nothing else.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class CharacterLoadout : MonoBehaviour
{
    [Tooltip("Used when MetaProgression is absent (tests, bare scene).")]
    [SerializeField] private CharacterData fallbackCharacter;

    private PlayerController player;

    void Start()
    {
        player = GetComponent<PlayerController>();
        ApplyLoadout();
    }

    public void ApplyLoadout()
    {
        if (player == null || player.stats == null) return;

        var meta = MetaProgression.Instance;
        var character = meta != null ? meta.SelectedCharacter() : null;
        if (character == null) character = fallbackCharacter;

        if (character != null) {
            player.stats.ApplyCharacter(character);
            player.SetCharacterId(character.characterId);
        }
        if (meta != null)
        {
            player.stats.SetMetaBonuses(meta.Bonus(MetaStat.MaxHP), meta.Bonus(MetaStat.Damage), meta.Bonus(MetaStat.MoveSpeed));
        }

        player.GetHealth(out var health);
        if (health != null)
        {
            health.SetMaxHP(player.stats.currentMaxHP);
            health.ResetHealth();
        }
        GetComponent<PlayerMovement>()?.UpdateSpeed(player.stats.currentSpeed);

        var hive = GetComponent<BeeHive>();
        if (hive != null) hive.Bind(character != null ? character.characterId : "");
        if (character != null && character.extraComponents != null)
            foreach (var name in character.extraComponents)
                EnableExtra(name);

        SelectWeapon(character != null ? character.weaponId : "flamethrower");
        var sprite = GetComponent<SpriteRenderer>();
        if (sprite != null && character != null) sprite.color = character.tint;
        GameEvents.PlayerStatsChanged(player.stats);
    }

    private void EnableExtra(string componentName)
    {
        if (string.IsNullOrEmpty(componentName)) return;
        foreach (var c in GetComponents<MonoBehaviour>())
            if (c.GetType().Name == componentName && !c.enabled) c.enabled = true;
    }

    private void SelectWeapon(string weaponId)
    {
        IWeaponAttack chosen = null;
        foreach (var component in GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (!(component is IWeaponAttack weapon)) continue;
            bool match = weapon.WeaponId == weaponId;
            component.enabled = match;
            if (match) chosen = weapon;
        }
        // A weapon id with no matching component falls back to whatever exists first.
        if (chosen == null)
        {
            foreach (var component in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component is IWeaponAttack weapon) { component.enabled = true; chosen = weapon; break; }
            }
        }

        GetComponent<PlayerAttack>()?.SetWeapon(chosen);
        GetComponent<PlayerAiming>()?.RefreshWeapon(chosen);
    }
}
