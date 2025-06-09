using UnityEngine;
using UnityEngine.UI;

public class QuickSlotsUI : MonoBehaviour
{
    [SerializeField] Image rightWeaponIcon;
    // [SerializeField] Image leftWeaponIcon; // For shield later

    private void OnEnable()
    {
        PlayerInventory.OnEquippedWeaponChanged += UpdateEquippedWeaponUI;
        // If you have access to PlayerInventory at start, you can do an initial update
        // PlayerInventory inv = FindObjectOfType<PlayerInventory>();
        // if(inv) UpdateEquippedWeaponUI(inv.EquippedWeapon);
    }

    private void OnDisable()
    {
        PlayerInventory.OnEquippedWeaponChanged -= UpdateEquippedWeaponUI;
    }

    private void UpdateEquippedWeaponUI(WeaponItem equippedWeapon)
    {
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

    public void RefreshAllQuickSlots(PlayerInventory playerInventory)
    {
        UpdateEquippedWeaponUI(playerInventory.EquippedWeapon);
    }
}
