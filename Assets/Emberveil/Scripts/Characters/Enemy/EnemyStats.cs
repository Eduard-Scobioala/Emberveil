using System;
using UnityEngine;

public class EnemyStats : CharacterStats
{
    [SerializeField] private int baseHealthAmout = 0;

    private Animator animator;

    public event Action<int> OnDamageReceived;
    public event Action OnDeath;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        maxHealth = GetMaxHealthBasedOnHealthLevel();
        currentHealth = maxHealth;
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

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0)
            return; // Already dead

        currentHealth -= damage;

        OnDamageReceived?.Invoke(damage);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            // Invoke death event
            OnDeath?.Invoke();
        }
    }
}
