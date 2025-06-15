using UnityEngine;

[CreateAssetMenu(menuName = "Items/Talisman Item")]
public class TalismanItem : Item
{
    [Header("Talisman Effects")]
    [Tooltip("Percentage bonus to max health. 0.1 = 10% bonus.")]
    public float healthBonusMultiplier;

    [Tooltip("Percentage bonus to max stamina. 0.05 = 5% bonus.")]
    public float staminaBonusMultiplier;

    [Tooltip("Multiplier for ALL outgoing damage. 1.0 = no change, 1.1 = 10% bonus damage.")]
    public float totalDamageMultiplier = 1.0f;

    public override string GetItemStatsText()
    {
        return $"{itemStatsText}";//: {GetStatValue(healthBonusMultiplier)}{GetStatValue(staminaBonusMultiplier)}{GetStatValue(totalDamageMultiplier)}";
    }

    private string GetStatValue(float value)
    {
        return value != 0 ? value.ToString() : "";
    }
}