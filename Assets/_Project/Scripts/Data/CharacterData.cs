using UnityEngine;

/// <summary>
/// One playable exterminator. Stats here replace PlayerStats' defaults; the weapon is chosen by id
/// and the matching IWeaponAttack component on the player is enabled (see CharacterLoadout).
/// Assets live in Resources/Characters so the lobby and the game scene can both find them.
/// </summary>
[CreateAssetMenu(fileName = "NewCharacter", menuName = "Exterminator/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Identity")]
    public string characterId = "firebug";
    public string displayName = "The Firebug";
    [TextArea] public string tagline;
    public Color tint = Color.white;

    [Header("Kit")]
    public string weaponId = "flamethrower";
    public string weaponName = "Leak-Prone Flamethrower";
    public string powerName;
    [TextArea] public string powerText;
    public string constraintName;
    [TextArea] public string constraintText;

    [Header("Base stats")]
    public float maxHP = 100f;
    public float damage = 5f;
    public float speed = 15f;
    public float attackSpeed = 1.2f;

    [Header("Unlock")]
    public bool unlockedByDefault;
    public int unlockCost = 150;

    [Header("Kit")]
    [Tooltip("Always-enabled companion components for this character (Backdraft, BeeHive).")]
    public string[] extraComponents;
}
