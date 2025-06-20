using UnityEngine;

[CreateAssetMenu(menuName = "Items/Consumable Item")]
public abstract class ConsumableItem : Item
{
    [Header("Consumable Details")]
    public string useAnimation;

    [Header("Audio")]
    [SerializeField] private SoundSO onUseSound;

    public abstract void Use(PlayerManager playerManager);

    public void PlaySoundOnUse()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(onUseSound);
        }
    }
}
