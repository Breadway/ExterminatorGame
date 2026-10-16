using UnityEngine;

public enum MetaStat
{
    MaxHP,
    Damage,
    MoveSpeed,
    XPGain,
    ScrapGain
}

/// <summary>
/// A small permanent upgrade bought in the lobby with scrap. Assets live in Resources/Meta.
/// </summary>
[CreateAssetMenu(fileName = "NewMetaUpgrade", menuName = "Exterminator/Meta Upgrade")]
public class MetaUpgradeData : ScriptableObject
{
    public string upgradeId;
    public string displayName;
    [TextArea] public string description;
    public MetaStat stat;
    [Tooltip("Bonus per level as a fraction, e.g. 0.05 = +5%.")]
    public float perLevel = 0.05f;
    [Min(1)] public int maxLevel = 5;
    public int baseCost = 40;
    public float costGrowth = 1.5f;

    public int CostForLevel(int currentLevel)
    {
        return Mathf.RoundToInt(baseCost * Mathf.Pow(Mathf.Max(1f, costGrowth), currentLevel));
    }
}
