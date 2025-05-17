using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    [Header("Health Stats")]
    public int healthLevel = 10;
    public int maxHealth;
    public int currentHealth;

    [Header("Stamina Stats")]
    public int staminaLevel = 10;
    public float maxStamina;
    public float currentStamina;

    public bool isDead = false;
}
