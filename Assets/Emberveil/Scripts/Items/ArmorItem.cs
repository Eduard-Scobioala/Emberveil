using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ArmorMeshData
{
    [Tooltip("The name of the body part this mesh should be applied to (e.g., 'Torso', 'Hips', 'Head'). Must match the names in PlayerEquipmentManager.")]
    public string targetBodyPartName;
    public Mesh mesh;
}

[CreateAssetMenu(menuName = "Items/Armor Item")]
public class ArmorItem : Item
{
    public ArmorType armorType;

    [Header("Visuals")]
    [Tooltip("A list of meshes that make up this piece of armor.")]
    public List<ArmorMeshData> armorMeshes = new ();

    [Header("Defense Stats")]
    public float physicalDefense;
    public float magicDefense;
    // TODO: Add other resistances...
}
