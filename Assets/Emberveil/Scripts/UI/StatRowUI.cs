using UnityEngine;
using TMPro;

public class StatRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text statNameText;
    [SerializeField] private TMP_Text currentValueText;
    [SerializeField] private TMP_Text nextValueText;
    [SerializeField] private GameObject rightArrowIcon;

    public void UpdateRow(float currentValue, float nextValue)
    {
        currentValueText.text = currentValue.ToString();

        if (nextValue > currentValue)
        {
            nextValueText.text = Mathf.RoundToInt(nextValue).ToString();
            rightArrowIcon.SetActive(true);
            nextValueText.gameObject.SetActive(true);
        }
        else
        {
            rightArrowIcon.SetActive(false);
            nextValueText.gameObject.SetActive(false);
        }
    }
}