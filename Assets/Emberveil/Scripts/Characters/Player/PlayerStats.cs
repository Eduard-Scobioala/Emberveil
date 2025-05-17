using UnityEngine;

public class PlayerStats : CharacterStats
{
    [SerializeField] private StatUIBar healthBar;
    [SerializeField] private StatUIBar staminaBar;

    private AnimatorHandler animatorHandler;
    private PlayerManager playerManager;

    [SerializeField] private int baseHealthAmout = 300;
    [SerializeField] private int baseStaminaAmout = 100;

    private void Awake()
    {
        animatorHandler = GetComponentInChildren<AnimatorHandler>();
        playerManager = GetComponent<PlayerManager>();
    }

    private void Start()
    {
        maxHealth = GetMaxHealthBasedOnHealthLevel();
        currentHealth = maxHealth;
        healthBar.SetMaxSliderValue(maxHealth);

        maxStamina = GetMaxStaminaBasedOnStaminaLevel();
        currentStamina = maxStamina;
        staminaBar.SetMaxSliderValue(maxStamina);
    }

    private int GetMaxHealthBasedOnHealthLevel()
    {
        int levelBasedGainedHealth = 0;

        if (healthLevel < 27)
        {
            levelBasedGainedHealth = 20 * healthLevel;
        }
        else if (healthLevel >= 27 && healthLevel <= 49)
        {
            levelBasedGainedHealth = 540 + 13 * (healthLevel - 26);
        }
        else if (healthLevel > 49)
        {
            levelBasedGainedHealth = 858 + 5 * (healthLevel - 49);
        }
        
        return baseHealthAmout + levelBasedGainedHealth;
    }

    private float GetMaxStaminaBasedOnStaminaLevel()
    {
        return baseStaminaAmout + staminaLevel * 5;
    }

    public void TakeDamange(int damange)
    {
        if (isDead)
            return;

        if (playerManager.isInvulnerable)
            return;

        currentHealth -= damange;
        healthBar.SetCurrentStatValue(currentHealth);

        animatorHandler.PlayTargetAnimation("Damage_01", true);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            animatorHandler.PlayTargetAnimation("Death_01", true);

            // Handle Player Death
            isDead = true;
        }
    }

    public void ConsumeStamina(int stamina)
    {
        currentStamina -= stamina;

        if (currentStamina <= 0) currentHealth = 0;

        staminaBar.SetCurrentStatValue(currentStamina);
    }

    public void RegenerateStamina()
    {

    }
}
