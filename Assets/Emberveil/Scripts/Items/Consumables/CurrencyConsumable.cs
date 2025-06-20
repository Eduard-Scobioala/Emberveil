using UnityEngine;

[CreateAssetMenu(menuName = "Items/Consumables/Currency Item")]
public class CurrencyConsumable : ConsumableItem
{
    [Header("Currency Effect")]
    public int currencyAmount = 100;

    public override void Use(PlayerManager playerManager)
    {
        PlayerStats playerStats = playerManager.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            Debug.Log($"Using {itemName}. Gained {currencyAmount} currency.");
            playerStats.AddCurrency(currencyAmount);
            PlaySoundOnUse();
        }
    }
}
