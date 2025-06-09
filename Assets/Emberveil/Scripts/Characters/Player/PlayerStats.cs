using UnityEngine;

public class PlayerStats : CharacterStats
{
    [SerializeField] private StatUIBar healthBar;
    [SerializeField] private StatUIBar staminaBar;

    private PlayerAnimator playerAnimator;
    private PlayerManager playerManager;

    [Header("Stats Settings")]
    [SerializeField] private int baseHealthAmout = 300;
    [SerializeField] private int baseStaminaAmout = 100;
    [SerializeField] private float staminaRegenAmount = 10;
    [SerializeField] private float staminaRegenDelay = 1;

    [Header("Hit Animation Names")]
    [SerializeField] private string frontHitAnimation = "Damage_Front_01";
    [SerializeField] private string backHitAnimation = "Damage_Back_01";
    [SerializeField] private string genericHitAnimation = "Damage_01"; // Fallback if direction is ambiguous or not needed

    private float staminaRegenTimer = 0;

    private void Awake()
    {
        playerAnimator = GetComponentInChildren<PlayerAnimator>();
        playerManager = GetComponent<PlayerManager>();
    }

    private void Start()
    {
        maxHealth = GetMaxHealthBasedOnHealthLevel();
        currentHealth = maxHealth;
        if (healthBar != null) healthBar.SetMaxSliderValue(maxHealth);
        else Debug.LogWarning("PlayerStats: HealthBar reference not set.");

        maxStamina = GetMaxStaminaBasedOnStaminaLevel();
        currentStamina = maxStamina;
        if (staminaBar != null) staminaBar.SetMaxSliderValue(maxStamina);
        else Debug.LogWarning("PlayerStats: StaminaBar reference not set.");
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

    public void TakeDamange(int damange, Transform attackerTransform = null)
    {
        if (isDead)
            return;

        if (playerAnimator.IsInvulnerable)
            return;

        currentHealth -= damange;
        healthBar.SetCurrentStatValue(currentHealth);

        if (!playerManager.isBeingCriticallyHit && currentHealth > 0)
        {
            string hitAnimToPlay = genericHitAnimation;

            if (attackerTransform != null)
            {
                Vector3 directionFromAttacker = (playerManager.transform.position - attackerTransform.position).normalized;
                directionFromAttacker.y = 0; // Ignore vertical difference for front/back determination

                float dotProduct = Vector3.Dot(playerManager.transform.forward, directionFromAttacker);

                if (dotProduct < -0.3f)
                {
                    hitAnimToPlay = frontHitAnimation;
                }
                else if (dotProduct > 0.3f)
                {
                    hitAnimToPlay = backHitAnimation;
                }
                // else, if dotProduct is close to 0, it's a side hit.
            }

            if (string.IsNullOrEmpty(hitAnimToPlay) || (hitAnimToPlay == frontHitAnimation && string.IsNullOrEmpty(frontHitAnimation)) || (hitAnimToPlay == backHitAnimation && string.IsNullOrEmpty(backHitAnimation)))
            {
                hitAnimToPlay = genericHitAnimation;
            }

            // Reset bools that may conflict because were interuped by taking dmg
            playerAnimator.IsDodging = false;
            playerAnimator.IsCrouching = false;

            playerAnimator.PlayTargetAnimation(hitAnimToPlay, false, rootMotion: true);
        }

        if (currentHealth <= 0 && !isDead)
        {
            currentHealth = 0;
            isDead = true;

            playerAnimator.anim.SetBool("isDead", true);

            // If not already in a critical hit (backstab), play normal death.
            // If being critically hit, the backstab victim animation sequence will handle death.
            if (!playerManager.isBeingCriticallyHit)
            {
                playerAnimator.PlayTargetAnimation("Death_01", true);
            }
            
            //playerManager.RaiseDeath();
        }
    }

    public bool CanPerformStaminaConsumingAction()
    {
        return currentStamina > 0;
    }
    
    public void ConsumeStamina(float staminaToConsume)
    {
        if (staminaToConsume <= 0) return;

        currentStamina -= staminaToConsume;
        if (currentStamina < 0)
        {
            currentStamina = 0;
        }

        if (staminaBar != null) staminaBar.SetCurrentStatValue(currentStamina);

        staminaRegenTimer = 0;
    }

    public void RegenerateStamina()
    {
        if (currentStamina >= maxStamina)
            return;

        if (playerManager.playerAnimator.IsInMidAction && !playerManager.playerAnimator.IsInAir) // while falling stamina is regenerating
        {
            staminaRegenTimer = 0;
            return;
        }

        staminaRegenTimer += Time.deltaTime;

        if (staminaRegenTimer > staminaRegenDelay)
        {
            currentStamina += staminaRegenAmount * Time.deltaTime;
            if (currentStamina > maxStamina)
            {
                currentStamina = maxStamina;
            }
            if (staminaBar != null) staminaBar.SetCurrentStatValue(currentStamina);
        }
    }
}
