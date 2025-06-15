using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Struct to hold information about an active buff for the UI
public struct ActiveBuffInfo
{
    public string BuffName;
    public Sprite BuffIcon;
    public float RemainingDuration;
}

public class PlayerStats : CharacterStats
{
    [SerializeField] private StatUIBar healthBar;
    [SerializeField] private StatUIBar staminaBar;

    private PlayerAnimator playerAnimator;
    private PlayerManager playerManager;
    private PlayerInventory playerInventory;

    [Header("Hit Animation Names")]
    [SerializeField] private string frontHitAnimation = "Damage_Front_01";
    [SerializeField] private string backHitAnimation = "Damage_Back_01";
    [SerializeField] private string genericHitAnimation = "Damage_01"; // Fallback if direction is ambiguous or not needed

    [Header("Stats Settings")]
    [SerializeField] private int baseHealthAmout = 300;
    [SerializeField] private int baseStaminaAmout = 100;
    [SerializeField] private float staminaRegenAmount = 10;
    [SerializeField] private float staminaRegenDelay = 1;
    [SerializeField] private int baseDefense = 5;

    // Buffs
    public int CurrentAttackBuff { get; private set; } = 0;
    public float CurrentStaminaRegenBuff { get; private set; } = 0f;

    private Dictionary<string, Coroutine> activeBuffCoroutines = new ();
    private List<ActiveBuffInfo> activeBuffsForUI = new ();

    public float FinalStaminaRegen => staminaRegenAmount + CurrentStaminaRegenBuff;
    public int TotalAttackPower => CalculateAttackDamage(playerManager.playerInventory.EquippedRightWeapon, PlayerAttackType.LightAttack);
    public int TotalDefense => baseDefense; // Later this will be: baseDefense + armorDefense;

    private float staminaRegenTimer = 0;

    private void Awake()
    {
        playerAnimator = GetComponentInChildren<PlayerAnimator>();
        playerManager = GetComponent<PlayerManager>();
        playerInventory = GetComponent<PlayerInventory>();
    }

    private void Start()
    {
        RecalculateStats();
    }

    private void Update()
    {
        RegenerateStamina();
    }

    private void OnEnable()
    {
        PlayerInventory.OnEquipmentUpdated += RecalculateStats;
    }

    private void OnDisable()
    {
        PlayerInventory.OnEquipmentUpdated -= RecalculateStats;
    }

    private void RecalculateStats()
    {
        // Temporarily store current health/stamina percentages
        float healthPercent = (maxHealth > 0) ? (float)currentHealth / maxHealth : 1f;
        float staminaPercent = (maxStamina > 0) ? currentStamina / maxStamina : 1f;

        // --- Calculate Max Health
        maxHealth = GetMaxHealthBasedOnHealthLevel();

        // --- Calculate Max Stamina
        maxStamina = GetMaxStaminaBasedOnStaminaLevel();

        // Apply new max values while retaining percentage
        currentHealth = Mathf.RoundToInt(maxHealth * healthPercent);
        currentStamina = maxStamina * staminaPercent;

        // Update UI bars with new max values
        if (healthBar != null) healthBar.SetMaxSliderValue(maxHealth);
        if (staminaBar != null) staminaBar.SetMaxSliderValue(maxStamina);

        // Update current values on UI
        if (healthBar != null) healthBar.SetCurrentStatValue(currentHealth);
        if (staminaBar != null) staminaBar.SetCurrentStatValue(currentStamina);

        Debug.Log($"Stats recalculated. New MaxHP: {maxHealth}, New MaxStamina: {maxStamina}");
    }

    private int GetMaxHealthBasedOnHealthLevel()
    {
        int levelBasedHealth = 0;

        if (healthLevel < 27)
        {
            levelBasedHealth = 20 * healthLevel;
        }
        else if (healthLevel >= 27 && healthLevel <= 49)
        {
            levelBasedHealth = 540 + 13 * (healthLevel - 26);
        }
        else if (healthLevel > 49)
        {
            levelBasedHealth = 858 + 5 * (healthLevel - 49);
        }

        int baseValue = baseHealthAmout + levelBasedHealth;

        // Apply Talisman Bonuses
        float totalHealthMultiplier = 1.0f;
        foreach (TalismanItem talisman in playerInventory.talismanSlots)
        {
            if (talisman != null)
            {
                totalHealthMultiplier += talisman.healthBonusMultiplier;
            }
        }

        return Mathf.RoundToInt(baseValue * totalHealthMultiplier);
    }

    private float GetMaxStaminaBasedOnStaminaLevel()
    {
        float baseValue = baseStaminaAmout + staminaLevel * 5;

        // Apply Talisman Bonuses ---
        float totalStaminaMultiplier = 1.0f;
        foreach (TalismanItem talisman in playerInventory.talismanSlots)
        {
            if (talisman != null)
            {
                totalStaminaMultiplier += talisman.staminaBonusMultiplier;
            }
        }

        return baseValue * totalStaminaMultiplier;
    }

    public int CalculateAttackDamage(WeaponItem weapon, PlayerAttackType attackType)
    {
        if (weapon == null) weapon = playerInventory.unarmedWeaponData; // Ensure we have data

        int baseWeaponDamage = 0;
        switch (attackType)
        {
            case PlayerAttackType.LightAttack: baseWeaponDamage = weapon.lightAttackDmg; break;
            case PlayerAttackType.RollAttack: baseWeaponDamage = weapon.rollAttackDmg; break;
            case PlayerAttackType.BackstepAttack: baseWeaponDamage = weapon.backstepAttackDmg; break;
            case PlayerAttackType.JumpAttack: baseWeaponDamage = weapon.jumpAttackDmg; break;
            default: baseWeaponDamage = weapon.lightAttackDmg; break;
        }

        // Combine base stats and weapon damage
        int preMultiplierDamage = baseAttackPower + baseWeaponDamage + CurrentAttackBuff;

        // Apply Talisman Damage Multiplier
        float totalDamageMultiplier = 1.0f;
        foreach (TalismanItem talisman in playerInventory.talismanSlots)
        {
            if (talisman != null)
            {
                totalDamageMultiplier *= talisman.totalDamageMultiplier; // Multipliers should multiply each other
            }
        }

        return Mathf.RoundToInt(preMultiplierDamage * totalDamageMultiplier);
    }

    public void TakeDamange(int damage, Transform attackerTransform = null)
    {
        if (isDead)
            return;

        if (playerAnimator.IsInvulnerable)
            return;

        int damageAfterDefense = Mathf.Max(1, damage - TotalDefense);

        currentHealth -= damageAfterDefense;
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

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        if (healthBar != null) healthBar.SetCurrentStatValue(currentHealth);
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
            float finalRegenAmount = staminaRegenAmount + CurrentStaminaRegenBuff;

            currentStamina += finalRegenAmount * Time.deltaTime;
            if (currentStamina > maxStamina) currentStamina = maxStamina;
            if (staminaBar != null) staminaBar.SetCurrentStatValue(currentStamina);
        }
    }

    // --- Buff Application Methods ---
    public void ApplyAttackBuff(int attackBonus, float duration, ConsumableItem sourceItem)
    {
        // If a buff of the same type is already active, stop the old one before starting the new one
        if (activeBuffCoroutines.ContainsKey(sourceItem.itemName))
        {
            StopCoroutine(activeBuffCoroutines[sourceItem.itemName]);
            // We need to manually remove the old buff's value before adding the new one
            // This is complex. A simpler approach for now is just to stack or overwrite. Let's stack.
        }
        activeBuffCoroutines[sourceItem.itemName] = StartCoroutine(AttackBuffCoroutine(attackBonus, duration, sourceItem));
    }

    private IEnumerator AttackBuffCoroutine(int attackBonus, float duration, ConsumableItem sourceItem)
    {
        CurrentAttackBuff += attackBonus;
        var buffInfo = new ActiveBuffInfo { BuffName = sourceItem.itemName, BuffIcon = sourceItem.itemIcon, RemainingDuration = duration };
        activeBuffsForUI.Add(buffInfo);

        while (buffInfo.RemainingDuration > 0)
        {
            buffInfo.RemainingDuration -= Time.deltaTime;
            // Update the entry in the list
            int index = activeBuffsForUI.FindIndex(b => b.BuffName == sourceItem.itemName);
            if (index != -1) activeBuffsForUI[index] = buffInfo;
            yield return null;
        }

        CurrentAttackBuff -= attackBonus;
        activeBuffCoroutines.Remove(sourceItem.itemName);
        activeBuffsForUI.RemoveAll(b => b.BuffName == sourceItem.itemName);
    }

    public void ApplyStaminaBuff(float regenBonus, float duration, ConsumableItem sourceItem)
    {
        if (activeBuffCoroutines.ContainsKey(sourceItem.itemName))
        {
            StopCoroutine(activeBuffCoroutines[sourceItem.itemName]);
        }
        activeBuffCoroutines[sourceItem.itemName] = StartCoroutine(StaminaBuffCoroutine(regenBonus, duration, sourceItem));
    }

    private IEnumerator StaminaBuffCoroutine(float regenBonus, float duration, ConsumableItem sourceItem)
    {
        CurrentStaminaRegenBuff += regenBonus;
        var buffInfo = new ActiveBuffInfo { BuffName = sourceItem.itemName, BuffIcon = sourceItem.itemIcon, RemainingDuration = duration };
        activeBuffsForUI.Add(buffInfo);

        while (buffInfo.RemainingDuration > 0)
        {
            buffInfo.RemainingDuration -= Time.deltaTime;
            int index = activeBuffsForUI.FindIndex(b => b.BuffName == sourceItem.itemName);
            if (index != -1) activeBuffsForUI[index] = buffInfo;
            yield return null;
        }

        CurrentStaminaRegenBuff -= regenBonus;
        activeBuffCoroutines.Remove(sourceItem.itemName);
        activeBuffsForUI.RemoveAll(b => b.BuffName == sourceItem.itemName);
    }

    public List<ActiveBuffInfo> GetActiveBuffs()
    {
        return activeBuffsForUI;
    }
}
