using UnityEngine;

public class PlayerStats : CharacterStats
{
    [SerializeField] private StatUIBar healthBar;
    [SerializeField] private StatUIBar staminaBar;

    private AnimatorHandler animatorHandler;
    private PlayerManager playerManager;

    [Header("Stats Settings")]
    [SerializeField] private int baseHealthAmout = 300;
    [SerializeField] private int baseStaminaAmout = 100;
    [SerializeField] private float staminaRegenAmount = 10;
    [SerializeField] private float staminaRegenDelay = 1;

    private float staminaRegenTimer = 0;

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

    private void Update()
    {
        RegenerateStamina();
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

        // Play damage animation ONLY if not in a critical hit sequence (like being backstabbed)
        // and if damage is not lethal (lethal damage will trigger backstab death or regular death anim)
        if (!playerManager.isBeingCriticallyHit && currentHealth > 0)
        {
            animatorHandler.PlayTargetAnimation("Damage_01", true);
        }
        else
        {
            animatorHandler.PlayTargetAnimation("Backstab_Main_Victim_01", true);
        }

        if (currentHealth <= 0 && !isDead)
        {
            currentHealth = 0;
            isDead = true;

            animatorHandler.anim.SetBool("isDead", true);

            // If not already in a critical hit (backstab), play normal death.
            // If being critically hit, the backstab victim animation sequence will handle death.
            if (!playerManager.isBeingCriticallyHit)
            {
                animatorHandler.PlayTargetAnimation("Death_01", true);
            }
            
            //playerManager.RaiseDeath();
        }
    }

    public void ConsumeStamina(int stamina)
    {
        currentStamina -= stamina;

        if (currentStamina <= 0) currentStamina = 0;

        staminaBar.SetCurrentStatValue(currentStamina);

        // Reset regen timer after consuming stamina
        staminaRegenTimer = 0;
    }

    public void RegenerateStamina()
    {
        if (playerManager.isInMidAction)
            return;

        staminaRegenTimer += Time.deltaTime;

        if (currentStamina < maxStamina && staminaRegenTimer > staminaRegenDelay)
        {
            currentStamina += staminaRegenAmount * Time.deltaTime;

            if (currentStamina > maxStamina)
            {
                currentStamina = maxStamina;
            }
            
            staminaBar.SetCurrentStatValue(currentStamina);
        }
    }
}
