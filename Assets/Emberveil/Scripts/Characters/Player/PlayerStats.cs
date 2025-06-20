using System;
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

public class PlayerStats : CharacterStats, ISavable
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

    [Header("Leveling Stats")]
    public int currentHealthLevel;
    public int currentStaminaLevel;
    public static event Action OnStatsRecalculated;

    [Header("Stats Settings")]
    [SerializeField] private int baseHealthAmount = 300;
    [SerializeField] private int baseStaminaAmount = 100;
    [SerializeField] private float staminaRegenAmount = 10;
    [SerializeField] private float staminaRegenDelay = 1;

    [Header("Currency")]
    public int currentCurrency = 0;
    public static event Action<int> OnCurrencyChanged;

    [Header("Audio")]
    [SerializeField] private SoundSO gotHitSound;
    private SoundEmitter soundEmitter;

    // Buffs
    public int CurrentAttackBuff { get; private set; } = 0;
    public float CurrentStaminaRegenBuff { get; private set; } = 0f;

    private Dictionary<string, Coroutine> activeBuffCoroutines = new ();
    private readonly List<ActiveBuffInfo> activeBuffsForUI = new ();

    public float FinalStaminaRegen => staminaRegenAmount + CurrentStaminaRegenBuff;
    public int TotalAttackPower => CalculateAttackDamage(playerManager.playerInventory.EquippedRightWeapon, PlayerAttackType.LightAttack);
    public int TotalDefense
    {
        get
        {
            int total = baseDefense;
            if (playerInventory.headArmor != null) total += (int)playerInventory.headArmor.physicalDefense;
            if (playerInventory.bodyArmor != null) total += (int)playerInventory.bodyArmor.physicalDefense;
            if (playerInventory.handArmor != null) total += (int)playerInventory.handArmor.physicalDefense;
            if (playerInventory.legArmor != null) total += (int)playerInventory.legArmor.physicalDefense;
            return total;
        }
    }


    private float staminaRegenTimer = 0;

    private void Awake()
    {
        playerAnimator = GetComponentInChildren<PlayerAnimator>();
        playerManager = GetComponent<PlayerManager>();
        playerInventory = GetComponent<PlayerInventory>();

        if (!TryGetComponent<SoundEmitter>(out soundEmitter))
        {
            soundEmitter = gameObject.AddComponent<SoundEmitter>();
        }
    }

    private void Start()
    {
        RecalculateStats();
        OnCurrencyChanged?.Invoke(currentCurrency);
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
        float healthPercent = (maxHealth > 0) ? (float)currentHealth / maxHealth : 1f;
        float staminaPercent = (maxStamina > 0) ? currentStamina / maxStamina : 1f;

        baseAttackPower = GetBaseAttackPowerForLevel(characterLevel);
        baseDefense = GetBaseDefenseForLevel(characterLevel);
        maxHealth = GetMaxHealthForLevel(characterLevel);
        maxStamina = GetMaxStaminaForLevel(characterLevel);

        currentHealthLevel = characterLevel;
        currentStaminaLevel = characterLevel;


        // Apply talisman bonuses AFTER calculating base values
        ApplyTalismanBonuses();

        currentHealth = Mathf.RoundToInt(maxHealth * healthPercent);
        currentStamina = maxStamina * staminaPercent;

        if (healthBar != null) healthBar.SetMaxSliderValue(maxHealth);
        if (staminaBar != null) staminaBar.SetMaxSliderValue(maxStamina);
        if (healthBar != null) healthBar.SetCurrentStatValue(currentHealth);
        if (staminaBar != null) staminaBar.SetCurrentStatValue(currentStamina);

        OnStatsRecalculated?.Invoke();
        Debug.Log($"Stats recalculated for Level {characterLevel}. HP:{maxHealth}, Stamina:{maxStamina}, Atk:{base.baseAttackPower}, Def:{base.baseDefense}");
    }

    private void ApplyTalismanBonuses()
    {
        float totalHealthMultiplier = 1.0f;
        float totalStaminaMultiplier = 1.0f;

        foreach (TalismanItem talisman in playerInventory.talismanSlots)
        {
            if (talisman != null)
            {
                totalHealthMultiplier += talisman.healthBonusMultiplier;
                totalStaminaMultiplier += talisman.staminaBonusMultiplier;
            }
        }

        maxHealth = Mathf.RoundToInt(maxHealth * totalHealthMultiplier);
        maxStamina = Mathf.RoundToInt(maxStamina * totalStaminaMultiplier);
    }

    #region Stat Calculation Formulas

    public int GetMaxHealthForLevel(int level)
    {
        int levelBasedHealth = 0;
        if (level < 27) levelBasedHealth = 20 * level;
        else if (level >= 27 && level <= 49) levelBasedHealth = 540 + 13 * (level - 26);
        else if (level > 49) levelBasedHealth = 858 + 5 * (level - 49);
        return baseHealthAmount + levelBasedHealth;
    }

    public float GetMaxStaminaForLevel(int level)
    {
        return baseStaminaAmount + level * 5;
    }

    public int GetBaseAttackPowerForLevel(int level)
    {
        return baseAttackPower + ((level - 1) / 2);
    }

    public int GetBaseDefenseForLevel(int level)
    {
        // Adds +1 for every 2 levels (level 2, 4, 6...)
        return baseDefense + (level - 1) / 2;
    }

    #endregion

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

    public void RestoreVitals()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;

        if (healthBar != null) healthBar.SetCurrentStatValue(currentHealth);
        if (staminaBar != null) staminaBar.SetCurrentStatValue(currentStamina);

        playerInventory.RefillFlasks();

        Debug.Log("Player vitals and flasks have been restored.");
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

            PlaySoundOnHit();
            playerAnimator.PlayTargetAnimation(hitAnimToPlay, false, rootMotion: true);
        }

        if (currentHealth <= 0 && !isDead)
        {
            currentHealth = 0;
            isDead = true;

            playerManager.HandleDeath();
        }
    }

    public void HandleDeathPenalty()
    {
        currentCurrency /= 2;
        OnCurrencyChanged?.Invoke(currentCurrency);
        Debug.Log($"Player died. Currency halved. New amount: {currentCurrency}");
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
            var duplicateBuff = activeBuffsForUI.Find(buff => buff.BuffName == sourceItem.itemName);
            activeBuffsForUI.Remove(duplicateBuff);
            StopCoroutine(activeBuffCoroutines[sourceItem.itemName]);
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
            var duplicateBuff = activeBuffsForUI.Find(buff => buff.BuffName == sourceItem.itemName);
            activeBuffsForUI.Remove(duplicateBuff);
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

    public void AddCurrency(int amount)
    {
        if (amount <= 0) return;
        currentCurrency += amount;
        OnCurrencyChanged?.Invoke(currentCurrency);
    }

    public bool SpendCurrency(int amount)
    {
        if (amount <= 0) return false;

        if (currentCurrency >= amount)
        {
            currentCurrency -= amount;
            OnCurrencyChanged?.Invoke(currentCurrency);
            return true;
        }

        Debug.Log("Not enough currency!");
        return false;
    }

    #region Leveling System Logic

    public int GetLevelUpCost()
    {
        // A formula that increases the cost with each level.
        return Mathf.RoundToInt(50 + (characterLevel * 100) + Mathf.Pow(characterLevel, 2) * 5);
    }

    public (int health, float stamina, int attack, int defense) GetStatPreviewForNextLevel()
    {
        int nextLevel = characterLevel + 1;
        int nextHealth = GetMaxHealthForLevel(nextLevel);
        float nextStamina = GetMaxStaminaForLevel(nextLevel);
        int nextAttack = GetBaseAttackPowerForLevel(nextLevel);
        int nextDefense = GetBaseDefenseForLevel(nextLevel);
        return (nextHealth, nextStamina, nextAttack, nextDefense);
    }

    public void LevelUp()
    {
        int cost = GetLevelUpCost();
        if (!SpendCurrency(cost))
        {
            Debug.Log("Not enough currency to level up!");
            return;
        }

        characterLevel++;

        RecalculateStats();
        RestoreVitals();
    }

    #endregion

    #region Saving and Loading

    // A struct to hold all the data we want to save for the player.
    [System.Serializable]
    private struct PlayerSaveData
    {
        public int playerLevel;
        public int currentCurrency;
        public Vector3 position;
        public Quaternion rotation;
        // We don't save health/stamina, as they are restored on load/rest.
    }

    public string GetUniqueIdentifier()
    {
        // The player is unique, so we can use a simple, constant ID.
        return "PlayerCharacter";
    }

    public object CaptureState()
    {
        return new PlayerSaveData
        {
            playerLevel = characterLevel,
            currentCurrency = currentCurrency,
            position = transform.position,
            rotation = transform.rotation
        };
    }

    public void RestoreState(object state)
    {
        if (state is PlayerSaveData saveData)
        {
            characterLevel = saveData.playerLevel;
            currentCurrency = saveData.currentCurrency;

            // We use the Rigidbody to move the character to avoid physics issues.
            // Disable and re-enable the character controller/rigidbody if you have issues with teleporting.
            playerManager.GetComponent<Rigidbody>().position = saveData.position;
            transform.rotation = saveData.rotation;

            // Recalculate all stats based on the loaded level
            RecalculateStats();
            // Fully restore vitals on load
            RestoreVitals();

            OnCurrencyChanged?.Invoke(currentCurrency); // Update UI
            Debug.Log($"Player state restored. Level: {characterLevel}, Position: {saveData.position}");
        }
    }
    #endregion

    #region Sounds

    private void PlaySoundOnHit()
    {
        if(soundEmitter != null)
        {
            soundEmitter.PlaySFX(gotHitSound);
        }
    }

    #endregion
}
