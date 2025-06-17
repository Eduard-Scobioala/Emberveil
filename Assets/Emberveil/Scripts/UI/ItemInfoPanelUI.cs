using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemInfoPanelUI : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemDescriptionText;
    [SerializeField] private TMP_Text itemStatsText;

    public void DisplayItemInfo(Item item)
    {
        if (item != null)
        {
            gameObject.SetActive(true);
            itemNameText.text = item.itemName;
            itemDescriptionText.text = item.itemDescription;
            itemStatsText.text = item.GetItemStatsText();
            if (itemIcon != null)
            {
                itemIcon.sprite = item.itemIcon;
                itemIcon.enabled = true;
            }
        }
        else
        {
            ClearPanel();
        }
    }

    public void DisplayUnequipInfo()
    {
        gameObject.SetActive(true);
        itemNameText.text = "Unequip";
        itemDescriptionText.text = "Remove the item from this slot.";
        itemStatsText.text = "";
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }
    }

    public void ClearPanel()
    {
        itemNameText.text = "";
        itemDescriptionText.text = "";
        itemStatsText.text = "";
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }
        gameObject.SetActive(false);
    }
}
