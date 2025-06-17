using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBarUI : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    private Transform cameraToFace;
    private EnemyStats enemyStats;

    public void Initialize(EnemyStats stats)
    {
        enemyStats = stats;
        if (healthSlider == null) healthSlider = GetComponentInChildren<Slider>();

        if (enemyStats != null)
        {
            // Subscribe to the health changed event
            enemyStats.OnHealthChanged += UpdateHealthBar;
            // Also subscribe to death event to properly hide/destroy itself
            enemyStats.OnDeathEvent += HandleDeath;
        }

        // Find the main camera
        if (Camera.main != null) cameraToFace = Camera.main.transform;

        // Start with the health bar hidden
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        // Make the health bar always face the camera
        if (cameraToFace != null)
        {
            transform.LookAt(transform.position + cameraToFace.rotation * Vector3.forward,
                             cameraToFace.rotation * Vector3.up);
        }
    }

    private void OnDestroy()
    {
        if (enemyStats != null)
        {
            enemyStats.OnHealthChanged -= UpdateHealthBar;
            enemyStats.OnDeathEvent -= HandleDeath;
        }
    }

    private void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (healthSlider.maxValue != maxHealth)
        {
            healthSlider.maxValue = maxHealth;
        }
        healthSlider.value = currentHealth;

        // If health is not full, it means damage has been taken.
        if (currentHealth < maxHealth && !gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }
    }

    private void HandleDeath()
    {
        gameObject.SetActive(false);
    }

    public void SetVisibility(bool isVisible)
    {
        // Only show if health is not full
        if (isVisible && enemyStats != null && enemyStats.currentHealth < enemyStats.maxHealth)
        {
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}