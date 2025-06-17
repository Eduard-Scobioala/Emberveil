using UnityEngine;

[CreateAssetMenu(menuName = "Items/Armor Item")]
public class ArmorItem : Item
{
    public GameObject modelPrefab;
    public ArmorType armorType;

    [Header("Defense Stats")]
    public float physicalDefense;
    public float magicDefense;
    // TODO: Add other resistances...
}
