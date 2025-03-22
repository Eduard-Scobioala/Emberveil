using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    WeaponSlotManager weaponSlotManager;

    public WeaponItem RightHandWeapon { get; private set; }
    public WeaponItem LeftHandWeapon { get; private set; }

    public WeaponItem unarmedWeapon;

    public WeaponItem[] weaponsInRightHandSlots = new WeaponItem[1];
    public WeaponItem[] weaponsInLeftHandSlots = new WeaponItem[1];

    private int currentRightWeaponIndex = -1;
    private int currentLeftWeaponIndex = -1;

    public List<WeaponItem> weaponsInventary;

    private void Awake()
    {
        weaponSlotManager = GetComponentInChildren<WeaponSlotManager>();
        if (weaponSlotManager == null)
        {
            Debug.LogError("WeaponSlotManager not found in children.");
        }
    }

    private void Start()
    {
        EquipWeapon(unarmedWeapon, false);
        EquipWeapon(unarmedWeapon, true);
    }

    public void ChangeRightWeapon()
    {
        ChangeWeapon(ref currentRightWeaponIndex, weaponsInRightHandSlots, false);
    }

    public void ChangeLeftWeapon()
    {
        ChangeWeapon(ref currentLeftWeaponIndex, weaponsInLeftHandSlots, true);
    }

    private void ChangeWeapon(ref int currentWeaponIndex, WeaponItem[] weaponSlots, bool isLeftHand)
    {
        currentWeaponIndex++;

        if (currentWeaponIndex > weaponSlots.Length - 1 || weaponSlots[currentWeaponIndex] == null)
        {
            currentWeaponIndex = -1;
            EquipWeapon(unarmedWeapon, isLeftHand);
        }
        else
        {
            EquipWeapon(weaponSlots[currentWeaponIndex], isLeftHand);
        }
    }

    private void EquipWeapon(WeaponItem weapon, bool isLeftHand)
    {
        if (weaponSlotManager == null) return;

        if (isLeftHand)
        {
            LeftHandWeapon = weapon;
        }
        else
        {
            RightHandWeapon = weapon;
        }

        weaponSlotManager.LoadWeaponOnSlot(weapon, isLeftHand);
    }
}
