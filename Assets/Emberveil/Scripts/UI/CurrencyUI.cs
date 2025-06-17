using UnityEngine;
using TMPro;

public class CurrencyUI : MonoBehaviour
{
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private PlayerStats playerStats;

    private void Awake()
    {
        if (currencyText == null)
        {
            currencyText = GetComponent<TMP_Text>();
        }
        if (currencyText == null)
        {
            Debug.LogError("CurrencyUI: TextMeshPro component not assigned or found!", this);
            enabled = false;
        }
        if (playerStats == null)
        {
            Debug.LogError("Player Stats is not assigned for Currency UI", this);
        }
    }

    private void OnEnable()
    {
        PlayerStats.OnCurrencyChanged += UpdateCurrencyText;

        if (playerStats != null)
        {
            UpdateCurrencyText(playerStats.currentCurrency);
        }
    }

    private void OnDisable()
    {
        PlayerStats.OnCurrencyChanged -= UpdateCurrencyText;
    }

    private void UpdateCurrencyText(int newAmount)
    {
        currencyText.text = newAmount.ToString();
    }
}
