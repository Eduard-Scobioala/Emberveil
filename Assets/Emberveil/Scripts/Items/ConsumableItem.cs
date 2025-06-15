using UnityEngine;

[CreateAssetMenu(menuName = "Items/Consumable Item")]
public abstract class ConsumableItem : Item
{
    [Header("Consumable Details")]
    public string useAnimation;

    public abstract void Use(PlayerManager playerManager);
}
