using UnityEngine;

public class WeaponHolderSlot : MonoBehaviour
{
    public Transform parentOverride;
    public GameObject currentWeaponModel { get; private set; }

    // Shield specific logic might be added later if this slot is also for shields
    // public bool isShieldSlot;

    public void UnloadWeaponAndDestroy()
    {
        if (currentWeaponModel != null)
        {
            Destroy(currentWeaponModel);
            currentWeaponModel = null;
        }
    }

    public void LoadWeaponModel(WeaponItem weaponItem)
    {
        UnloadWeaponAndDestroy();

        if (weaponItem == null || weaponItem.modelPrefab == null)
        {
            return; // Nothing to load for unarmed or if no prefab
        }

        currentWeaponModel = Instantiate(weaponItem.modelPrefab);
        if (currentWeaponModel != null)
        {
            Transform actualParent = parentOverride != null ? parentOverride : transform;
            currentWeaponModel.transform.SetParent(actualParent, false); // Set parent and reset local transform
        }
    }
}
