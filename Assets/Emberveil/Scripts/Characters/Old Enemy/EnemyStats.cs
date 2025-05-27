using System;
using UnityEngine;

public class EnemyStats2 : CharacterStats
{
    [SerializeField] private int baseHealthAmout = 0;

    public event Action<int> OnDamageReceived;
    public event Action OnDeath;

    private EnemyManager enemyManager;

    private void Awake()
    {
        enemyManager = GetComponent<EnemyManager>();
        if (enemyManager == null)
        {
            Debug.LogError("EnemyStats could not find EnemyManager component.", this);
        }
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

    public void TakeDamage(int damage, bool isBackstab = false)
    {
        if (isDead)
            return;

        if (enemyManager != null && !isBackstab && enemyManager.isInvulnerable)
            return;

        currentHealth -= damage;
        OnDamageReceived?.Invoke(damage);

        if (currentHealth <= 0 && !isDead)
        {
            currentHealth = 0;
            isDead = true;

            enemyManager.enemyAnimator.anim.SetBool("isDead", true);
             
            OnDeath?.Invoke();
        }
    }
}
