using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public int healthLevel = 10;
    public int maxHealth;
    public int currentHealth;

    public HealthBar healthBar;

    private AnimatorHandler animatorHandler;

    [SerializeField] private int baseHealthAmout = 300;

    private void Awake()
    {
        animatorHandler = GetComponentInChildren<AnimatorHandler>();
    }

    private void Start()
    {
        maxHealth = GetMaxHealthBasedOnHealthLevel();
        currentHealth = maxHealth;
        healthBar.SetMaxSliderValue(maxHealth);
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

    public void TakeDamange(int damange)
    {
        currentHealth -= damange;

        healthBar.SetCurrentHealthValue(currentHealth);

        animatorHandler.PlayTargetAnimation("Damage_01", true);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            animatorHandler.PlayTargetAnimation("Death_01", true);

            // Handle Player Death
        }
    }
}
