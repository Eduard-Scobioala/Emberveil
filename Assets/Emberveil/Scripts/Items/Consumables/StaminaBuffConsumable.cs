using UnityEngine;

[CreateAssetMenu(menuName = "Items/Consumables/Stamina Buff")]
public class StaminaBuffConsumable : ConsumableItem
{
    [Header("Buff Effects")]
    public float staminaRegenBonus = 15f; // Extra regen per second
    public float buffDuration = 30f;      // How long the buff lasts

    public override void Use(PlayerManager playerManager)
    {
        Debug.Log($"Using {itemName}. Applying stamina buff for {buffDuration}s.");
        PlayerStats playerStats = playerManager.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            // PlayerStats will need a method to handle this
            playerStats.ApplyStaminaBuff(staminaRegenBonus, buffDuration, this);
            playerManager.playerInventory.RemoveItem(this); // Consumed on use
        }
    }
}
