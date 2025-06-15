using System;
using UnityEngine;
using UnityEngine.UI;

public class QuickSlotsUI : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private Image rightWeaponIcon;
    // [SerializeField] Image leftWeaponIcon; // For shield later

    private void Awake()
    {
        if (inventory == null) Debug.LogError("Inventory reference not set for Quick Slots UI");
        PlayerInventory.OnEquipmentUpdated += UpdateEquippedWeaponUI;
    }

    private void OnDestroy()
    {
        PlayerInventory.OnEquipmentUpdated -= UpdateEquippedWeaponUI;
    }

    private void UpdateEquippedWeaponUI()
    {
        var equippedWeapon = inventory.EquippedRightWeapon;

        if (equippedWeapon != null && !equippedWeapon.isUnarmed && equippedWeapon.itemIcon != null)
        {
            rightWeaponIcon.sprite = equippedWeapon.itemIcon;
            rightWeaponIcon.enabled = true;
        }
        else
        {
            // Optionally show a "fist" icon for unarmed, or just disable
            if (equippedWeapon != null && equippedWeapon.isUnarmed && equippedWeapon.itemIcon != null)
            {
                rightWeaponIcon.sprite = equippedWeapon.itemIcon;
                rightWeaponIcon.enabled = true;
            }
            else
            {
                rightWeaponIcon.sprite = null;
                rightWeaponIcon.enabled = false;
            }
        }
    }

    public void RefreshAllQuickSlots()
    {
        UpdateEquippedWeaponUI();
    }
}
