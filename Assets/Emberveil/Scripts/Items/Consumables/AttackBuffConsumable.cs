using UnityEngine;

[CreateAssetMenu(menuName = "Items/Consumables/Attack Buff")]
public class AttackBuffConsumable : ConsumableItem
{
    [Header("Buff Effects")]
    public int attackBonus = 20;    // Flat damage bonus
    public float buffDuration = 30f;

    public override void Use(PlayerManager playerManager)
    {
        Debug.Log($"Using {itemName}. Applying attack buff for {buffDuration}s.");
        PlayerStats playerStats = playerManager.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            // PlayerStats will need a method to handle this
            playerStats.ApplyAttackBuff(attackBonus, buffDuration, this);
            PlaySoundOnUse();
        }
    }
}
