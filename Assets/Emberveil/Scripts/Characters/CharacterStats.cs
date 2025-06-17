using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    public int characterLevel = 1;

    [Header("Health Stats")]
    public int maxHealth;
    public int currentHealth;

    [Header("Stamina Stats")]
    public float maxStamina;
    public float currentStamina;

    [Header("Combat Stats")]
    public int baseAttackPower = 10;
    public int baseDefense = 2;

    public bool isDead = false;
}
