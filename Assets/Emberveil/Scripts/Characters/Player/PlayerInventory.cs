using System.Collections.Generic;
using UnityEngine;

public enum EquipSlotType
{
    RightHandSlot01,
    RightHandSlot02,
    RightHandSlot03,
    RightHandSlot04,
    LeftHandSlot01,
    LeftHandSlot02,
    LeftHandSlot03,
    LeftHandSlot04,
}

public class PlayerInventory : MonoBehaviour
{
    WeaponSlotManager weaponSlotManager;

    public WeaponItem RightHandWeapon { get; private set; }
    public WeaponItem LeftHandWeapon { get; private set; }

    public WeaponItem unarmedWeapon;

    public WeaponItem[] weaponsInRightHandSlots = new WeaponItem[1];
    public WeaponItem[] weaponsInLeftHandSlots = new WeaponItem[1];

    private int currentRightWeaponIndex = 0;
    private int currentLeftWeaponIndex = 0;

    public List<WeaponItem> weaponsInventory;

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
        EquipWeapon(TryGetWeapon(weaponsInRightHandSlots), false);
        EquipWeapon(TryGetWeapon(weaponsInLeftHandSlots), true);
    }

    private void OnEnable()
    {
        InputHandler.DPadLeftButtonPressed += HandleDPadLeftButtonPressed;
        InputHandler.DPadRightButtonPressed += HandleDPadRightButtonPressed;
    }

    private void OnDisable()
    {
        InputHandler.DPadLeftButtonPressed -= HandleDPadLeftButtonPressed;
        InputHandler.DPadRightButtonPressed -= HandleDPadRightButtonPressed;
    }

    private void HandleDPadLeftButtonPressed()
    {
        ChangeLeftWeapon();
    }

    private void HandleDPadRightButtonPressed()
    {
        ChangeRightWeapon();
    }

    private WeaponItem TryGetWeapon(WeaponItem[] weaponItems)
    {
        if (weaponItems !=  null && weaponItems.Length > 0)
        {
            return weaponItems[0];
        }
        else
        {
            return unarmedWeapon;
        }
    }

    public void ChangeLeftWeapon()
    {
        ChangeWeapon(ref currentLeftWeaponIndex, weaponsInLeftHandSlots, true);
    }

    public void ChangeRightWeapon()
    {
        ChangeWeapon(ref currentRightWeaponIndex, weaponsInRightHandSlots, false);
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

    public Item GetItemFromEquipSlot(EquipSlotType slotType)
    {
        return slotType switch
        {
            EquipSlotType.RightHandSlot01 => weaponsInRightHandSlots[0],
            EquipSlotType.RightHandSlot02 => weaponsInRightHandSlots[1],
            EquipSlotType.RightHandSlot03 => weaponsInRightHandSlots[2],
            EquipSlotType.RightHandSlot04 => weaponsInRightHandSlots[3],

            EquipSlotType.LeftHandSlot01 => weaponsInLeftHandSlots[0],
            EquipSlotType.LeftHandSlot02 => weaponsInLeftHandSlots[1],
            EquipSlotType.LeftHandSlot03 => weaponsInLeftHandSlots[2],
            EquipSlotType.LeftHandSlot04 => weaponsInLeftHandSlots[3],

            _ => throw new System.NotImplementedException(),
        };
    }

    public void SetItemFromEquipSlot(EquipSlotType slotType, WeaponItem item)
    {
        switch (slotType)
        {
            case >= EquipSlotType.RightHandSlot01 and <= EquipSlotType.RightHandSlot04:
                weaponsInRightHandSlots[(int)slotType - (int)EquipSlotType.RightHandSlot01] = item;
                RightHandWeapon = item;
                break;

            case >= EquipSlotType.LeftHandSlot01 and <= EquipSlotType.LeftHandSlot04:
                weaponsInLeftHandSlots[(int)slotType - (int)EquipSlotType.LeftHandSlot01] = item;
                LeftHandWeapon = item;
                break;
        }
    }
}
