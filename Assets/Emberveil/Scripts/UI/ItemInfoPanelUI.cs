using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemInfoPanelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemDescriptionText;
    [SerializeField] private Image itemIcon; // Optional, for larger icon

    public void DisplayItemInfo(Item item)
    {
        if (item != null)
        {
            itemNameText.text = item.name;
            itemDescriptionText.text = item.itemText; // Assumes itemText is the description
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

    public void ClearPanel()
    {
        itemNameText.text = "";
        itemDescriptionText.text = "";
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }
    }
}
