using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum EquipmentSlotCategory { RightHand, LeftHand, Head, Body, Hands, Legs, Talisman, Consumable }

public class EquipmentSlotUI : MonoBehaviour
{
    [Header("Slot Info")]
    public EquipmentSlotCategory slotCategory;
    public int slotIndex; // 0-2 for RightHand, 0-3 for Talisman, etc.

    [Header("UI References")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text itemNameText; // Optional

    private Item currentItem;

    public void UpdateSlot(Item item)
    {
        currentItem = item;
        if (currentItem != null && currentItem.itemIcon != null)
        {
            icon.sprite = currentItem.itemIcon;
            icon.enabled = true;
            if (itemNameText != null) itemNameText.text = currentItem.name;
        }
        else
        {
            icon.sprite = null;
            icon.enabled = false;
            if (itemNameText != null) itemNameText.text = "";
        }
    }

    public void OnSlotClicked()
    {
        // Tell the UIManager or EquipmentWindow to open the filtered inventory
        // for this slot type and index.
        Debug.Log($"Clicked on slot: {slotCategory}, index: {slotIndex}");
        // FindObjectOfType<UIManager>().ShowFilteredInventoryForSlot(this);
    }
}
