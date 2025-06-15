using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class StatusWindowUI : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerInventory playerInventory;

    [Header("Stat Text Fields")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text staminaText;
    [SerializeField] private TMP_Text staminaRegenText;
    [SerializeField] private TMP_Text attackPowerText;
    [SerializeField] private TMP_Text defenseText;

    [Header("Buffs Display")]
    [SerializeField] private Transform buffsContainer;
    [SerializeField] private GameObject buffIconPrefab; // Prefab for displaying a single buff

    [Header("Display Refresh")]
    [SerializeField] private float refreshRate = 1.0f;
    private float refreshTimer = 0.0f;

    private void OnEnable()
    {
        // Find references if not assigned
        if (playerStats == null) playerStats = FindObjectOfType<PlayerStats>();
        if (playerInventory == null) playerInventory = FindObjectOfType<PlayerInventory>();

        RefreshAllStats();
    }

    private void Update()
    {
        refreshTimer += Time.deltaTime;
        if (refreshTimer >= refreshRate)
        {
            refreshTimer = 0.0f;
            RefreshAllStats();
        }
    }

    public void RefreshAllStats()
    {
        if (playerStats == null || playerInventory == null)
        {
            Debug.LogError("StatusWindowUI: Missing PlayerStats or PlayerInventory reference!");
            return;
        }

        // Update Main Stats
        healthText.text = $"{playerStats.currentHealth} / {playerStats.maxHealth}";
        staminaText.text = $"{(int)playerStats.currentStamina} / {(int)playerStats.maxStamina}";
        staminaRegenText.text = playerStats.FinalStaminaRegen.ToString("F1"); // One decimal place

        // Attack Power
        attackPowerText.text = playerStats.TotalAttackPower.ToString();

        defenseText.text = playerStats.TotalDefense.ToString();

        // Update Buffs
        UpdateBuffsDisplay();
    }

    private void UpdateBuffsDisplay()
    {
        // Clear old buff icons
        foreach (Transform child in buffsContainer)
        {
            Destroy(child.gameObject);
        }

        // Get active buffs and create UI elements for them
        List<ActiveBuffInfo> activeBuffs = playerStats.GetActiveBuffs();
        foreach (var buffInfo in activeBuffs)
        {
            GameObject buffGO = Instantiate(buffIconPrefab, buffsContainer);
            BuffIconUI buffUI = buffGO.GetComponent<BuffIconUI>();
            if (buffUI != null)
            {
                buffUI.SetBuff(buffInfo);
            }
        }
    }
}
