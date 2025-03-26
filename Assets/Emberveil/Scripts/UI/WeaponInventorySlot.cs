using UnityEngine;
using UnityEngine.UI;

public class WeaponInventorySlot : MonoBehaviour
{
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
}
