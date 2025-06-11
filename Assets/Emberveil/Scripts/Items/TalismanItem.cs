using UnityEngine;

[CreateAssetMenu(menuName = "Items/Talisman Item")]
public class TalismanItem : Item
{
    [Header("Talisman Effects")]
    public float healthBonus;
    public float staminaBonus;
    public float damageMultiplier;
}