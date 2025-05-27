using System;
using UnityEngine;

public class EnemyStats : CharacterStats
{
    public event Action<int, DamageType, Transform> OnDamagedEvent;
    public event Action OnDeathEvent;

    private EnemyManager enemyManager;

    [Header("Enemy Specific Stats")]
    [SerializeField] private int baseHealthAmount = 100;
    public float poise = 100f;
    public float currentPoise;

    protected void Awake()
    {
        //base.Awake();
        enemyManager = GetComponent<EnemyManager>();
        if (enemyManager == null)
        {
            Debug.LogError("EnemyStats could not find EnemyManager component.", this);
        }
    }

    private void Start()
    {
        maxHealth = CalculateMaxHealth();
        currentHealth = maxHealth;
        currentPoise = poise;
        isDead = false;
    }

    private int CalculateMaxHealth()
    {
        int levelBasedGainedHealth = 0;

        if (healthLevel < 20)
        {
            levelBasedGainedHealth = 15 * healthLevel;
        }
        else if (healthLevel >= 20 && healthLevel <= 40)
        {
            levelBasedGainedHealth = 300 + 10 * (healthLevel - 19);
        }
        else if (healthLevel > 40)
        {
            levelBasedGainedHealth = 510 + 5 * (healthLevel - 40);
        }

        return baseHealthAmount + levelBasedGainedHealth;
    }

    public void TakeDamage(int damage, DamageType damageType, Transform attacker)
    {
        if (isDead) return;
        if (enemyManager.isInvulnerable && damageType != DamageType.BackstabCritical) // Backstabs should always hit
        {
            // TODO: Maybe some damage types bypass invulnerability
            Debug.Log($"{gameObject.name} is invulnerable. Damage blocked.");
            return;
        }

        currentHealth -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage. Current health: {currentHealth}/{maxHealth}");

        OnDamagedEvent?.Invoke(damage, damageType, attacker); // Notify EnemyManager

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            if (!isDead) // Ensure OnDeathEvent is invoked only once
            {
                isDead = true;
                OnDeathEvent?.Invoke(); // Notify EnemyManager
            }
        }
        // else
        // {
        //    // Handle poise damage
        //    // currentPoise -= poiseDamageFromAttack;
        //    // if (currentPoise <= 0) { // Trigger PoiseBreak state }
        // }
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
    }
}
