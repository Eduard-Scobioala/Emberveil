using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuickSlotsUI : MonoBehaviour
{
    [SerializeField] private PlayerInventory playerInventory;
    [Header("Weapon Slot")]
    [SerializeField] private Image rightWeaponIcon;
    // [SerializeField] Image leftWeaponIcon; // For shield later

    [Header("Consumable Slots")]
    [SerializeField] private Image currentConsumableIcon;
    [SerializeField] private TMP_Text currentConsumableQuantity;
    [SerializeField] private Image nextConsumableIcon;

    private void Awake()
    {
        if (playerInventory == null) Debug.LogError("Inventory reference not set for Quick Slots UI");
        PlayerInventory.OnEquipmentUpdated += RefreshAllSlots;
        PlayerInventory.OnInventoryUpdated += RefreshAllSlots;
    }

    private void OnDestroy()
    {
        PlayerInventory.OnEquipmentUpdated -= RefreshAllSlots;
        PlayerInventory.OnInventoryUpdated += RefreshAllSlots;
    }

    public void RefreshAllSlots()
    {
        if (playerInventory == null) return;

        UpdateEquippedWeaponUI(playerInventory.EquippedRightWeapon);
        UpdateConsumableUI();
    }

    private void UpdateEquippedWeaponUI(WeaponItem equippedWeapon)
    {
        WeaponItem weaponToShow = equippedWeapon ?? playerInventory.unarmedWeaponData;

        if (weaponToShow != null && !weaponToShow.isUnarmed && weaponToShow.itemIcon != null)
        {
            rightWeaponIcon.sprite = weaponToShow.itemIcon;
            rightWeaponIcon.enabled = true;
        }
        else
        {
            rightWeaponIcon.sprite = null;
            rightWeaponIcon.enabled = false;
        }
    }

    private void UpdateConsumableUI()
    {
        InventorySlot currentSlot = playerInventory.CurrentConsumableSlot;
        InventorySlot nextSlot = playerInventory.NextConsumableSlot;

        // --- Update Current Consumable Slot (D-Pad Up) ---
        if (currentSlot != null && currentSlot.item != null)
        {
            currentConsumableIcon.sprite = currentSlot.item.itemIcon;
            currentConsumableIcon.enabled = true;

            if (currentConsumableQuantity != null)
            {
                currentConsumableQuantity.text = currentSlot.quantity.ToString();
                currentConsumableQuantity.enabled = true;
            }
        }
        else
        {
            // No consumable equipped
            currentConsumableIcon.sprite = null;
            currentConsumableIcon.enabled = false;
            if (currentConsumableQuantity != null) currentConsumableQuantity.enabled = false;
        }

        // --- Update Next Consumable Slot (D-Pad Down) ---
        // Hide the "next" icon if there's only one or zero items to cycle through
        if (nextSlot != null && nextSlot.item != null && playerInventory.consumableQuickSlots.Count > 1)
        {
            nextConsumableIcon.sprite = nextSlot.item.itemIcon;
            nextConsumableIcon.enabled = true;
        }
        else
        {
            nextConsumableIcon.sprite = null;
            nextConsumableIcon.enabled = false;
        }
    }
}
