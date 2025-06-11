using UnityEngine;

[CreateAssetMenu(menuName = "Items/Consumable Item")]
public class ConsumableItem : Item
{
    public GameObject itemPrefab; // Prefab to instantiate on use (e.g., throwing pot)
    public string useAnimation;   // Animation to play when used

    [Header("Consumable Effects")]
    public int healthRestoreAmount;
    public int staminaRestoreAmount;
    public float temporaryBuffDuration;

    public virtual void Use(PlayerManager playerManager)
    {
        Debug.Log($"Using {name}. Restoring {healthRestoreAmount} health.");
        // Logic to apply health/stamina restore would go here, calling methods on PlayerStats
        // playerManager.GetComponent<PlayerStats>().Heal(healthRestoreAmount);
    }
}
