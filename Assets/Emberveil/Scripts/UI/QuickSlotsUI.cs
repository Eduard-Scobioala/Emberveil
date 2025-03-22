using UnityEngine;
using UnityEngine.UI;

public class QuickSlotsUI : MonoBehaviour
{
    [SerializeField] Image leftWeaponIcon;
    [SerializeField] Image rightWeaponIcon;

    public void UpdateWeaponQuickSlotsUI(WeaponItem weapon, bool isLeft)
    {
        var weaponIcon = isLeft ? leftWeaponIcon : rightWeaponIcon;

        if (weapon.itemIcon != null)
        {
            weaponIcon.sprite = weapon.itemIcon;
            weaponIcon.enabled = true;
        }
        else
        {
            weaponIcon.sprite = null;
            weaponIcon.enabled = false;
        }
    }
}
