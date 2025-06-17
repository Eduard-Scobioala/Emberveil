using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelUpWindowUI : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private PlayerStats playerStats;

    [Header("Header Info")]
    [SerializeField] private TMP_Text playerLevelText;
    [SerializeField] private TMP_Text currencyText;

    [Header("Stat Rows")]
    [SerializeField] private StatRowUI healthStatRow;
    [SerializeField] private StatRowUI staminaStatRow;
    [SerializeField] private StatRowUI attackStatRow;
    [SerializeField] private StatRowUI defenseStatRow;

    [Header("Footer Info")]
    [SerializeField] private TMP_Text levelUpCostText;
    [SerializeField] private Button confirmButton;

    private void Awake()
    {
        if (playerStats == null)  Debug.LogError("Player Stats missing on LEVEL UP window.");
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmLevelUp);
        }
    }

    private void OnEnable()
    {
        PlayerStats.OnStatsRecalculated += RefreshUI;
        PlayerStats.OnCurrencyChanged += (newAmount) => RefreshUI();

        // Refresh the UI as soon as the window is opened
        RefreshUI();
    }

    private void OnDisable()
    {
        PlayerStats.OnStatsRecalculated -= RefreshUI;
        PlayerStats.OnCurrencyChanged -= (newAmount) => RefreshUI();
    }

    private void RefreshUI()
    {
        if (playerStats == null) return;

        // Update Header
        playerLevelText.text = playerStats.characterLevel.ToString();
        currencyText.text = playerStats.currentCurrency.ToString();

        // Get stat increase preview
        var (nextHealth, nextStamina, nextAttack, nextDefense) = playerStats.GetStatPreviewForNextLevel();

        // Update Stat Rows
        healthStatRow.UpdateRow(playerStats.maxHealth, nextHealth);
        staminaStatRow.UpdateRow(playerStats.maxStamina, nextStamina);
        attackStatRow.UpdateRow(playerStats.baseAttackPower, nextAttack);
        defenseStatRow.UpdateRow(playerStats.baseDefense, nextDefense);

        // Update Footer
        int cost = playerStats.GetLevelUpCost();
        levelUpCostText.text = cost.ToString();

        // Enable or disable the confirm button based on whether the player can afford it
        if (confirmButton != null)
        {
            confirmButton.interactable = playerStats.currentCurrency >= cost;
        }
    }

    private void OnConfirmLevelUp()
    {
        if (playerStats != null)
        {
            playerStats.LevelUp();
            // The UI will automatically refresh because LevelUp triggers OnStatsRecalculated
        }
    }
}