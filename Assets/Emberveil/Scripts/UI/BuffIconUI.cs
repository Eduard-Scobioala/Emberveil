using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuffIconUI : MonoBehaviour
{
    [SerializeField] private Image buffIcon;
    [SerializeField] private TMP_Text buffNameText;
    [SerializeField] private TMP_Text durationText;

    public void SetBuff(ActiveBuffInfo buffInfo)
    {
        if (buffIcon != null) buffIcon.sprite = buffInfo.BuffIcon;
        if (buffNameText != null) buffNameText.text = buffInfo.BuffName;
        if (durationText != null)
        {
            // Show duration, rounding up to nearest second for display
            durationText.text = Mathf.CeilToInt(buffInfo.RemainingDuration).ToString();
        }
    }
}
