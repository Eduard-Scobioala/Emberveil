using UnityEngine;

public class Item : ScriptableObject
{
    [Header("Item Information")]
    public Sprite itemIcon;
    public string itemName;
    [TextArea(4, 8)]
    public string itemDescription;
    public string itemStatsText;

    [Header("Item Properties")]
    public bool isDroppable = true;
    public GameObject itemPickupPrefab;

    public virtual string GetItemStatsText()
    {
        return itemStatsText;
    }
}
