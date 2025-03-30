using UnityEngine;
using UnityEngine.UI;

public class WeaponInventorySlot : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private WeaponSlotManager weaponSlotManager;
    [SerializeField] private EquipmentWindowUI equipmentWindowUI;
    [SerializeField] private Image weaponIcon;
    private WeaponItem weaponItem;

    public void AddItem(WeaponItem weaponToBeAssigned) {
        weaponItem = weaponToBeAssigned;

        weaponIcon.sprite = weaponItem.itemIcon;
        weaponIcon.enabled = true;

        gameObject.SetActive(true);
    }

    public void ClearInventorySlot() {
        weaponItem = null;
        
        weaponIcon.sprite = null;
        weaponIcon.enabled = false;

        gameObject.SetActive(false);
    }

    public void EquipItem()
    {
        playerInventory.weaponsInventory.Add((WeaponItem)playerInventory.GetItemFromEquipSlot(uiManager.currentSelectedSlotType));
        playerInventory.SetItemFromEquipSlot(uiManager.currentSelectedSlotType, weaponItem);
        playerInventory.weaponsInventory.Remove(weaponItem);

        weaponSlotManager.LoadWeaponOnSlot(playerInventory.RightHandWeapon, false);
        weaponSlotManager.LoadWeaponOnSlot(playerInventory.LeftHandWeapon, true);

        equipmentWindowUI.LoadWeaponOnEquipementScreen(playerInventory);
        uiManager.currentSelectedSlotType = 0;
    }
}
