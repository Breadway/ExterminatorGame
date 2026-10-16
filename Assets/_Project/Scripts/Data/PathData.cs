using UnityEngine;

/// <summary>
/// A level-up path (specialization). It is an UpgradeData, so stats, validation and the card UI
/// all work unchanged. Tier 1 is offered at level 10, tier 2 at 20 (requiring a tier 1 path via
/// requiredUpgrades) and tier 3 at 30 in endless mode.
/// </summary>
[CreateAssetMenu(fileName = "NewPath", menuName = "Exterminator/Path Data")]
public class PathData : UpgradeData
{
    [Header("Path")]
    [Range(1, 3)] public int tier = 1;
    [Tooltip("Character this path belongs to. Empty means every character can take it.")]
    public string characterId = "";
}
