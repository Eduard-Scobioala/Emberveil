using UnityEngine;

[CreateAssetMenu(menuName = "Items/Consumables/Health Potion")]
public class HealthConsumable : ConsumableItem
{
    [Header("Potion Effects")]
    public int healthRestoreAmount;
    public bool isFlask; // Is this the main regenerating flask?

    public override void Use(PlayerManager playerManager)
    {
        Debug.Log($"Using {itemName}. Restoring {healthRestoreAmount} health.");
        PlayerStats playerStats = playerManager.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.Heal(healthRestoreAmount);
            // If it's the main flask, PlayerInventory will handle decrementing its count
            if (!isFlask)
            {
                playerManager.playerInventory.RemoveItem(this);
            }
        }
    }
}
