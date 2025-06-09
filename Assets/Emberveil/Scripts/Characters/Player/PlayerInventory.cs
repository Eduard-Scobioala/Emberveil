using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerInventory : MonoBehaviour
{
    public static event Action<WeaponItem> OnEquippedWeaponChanged;

    [Header("References")]
    [SerializeField] private WeaponSlotManager weaponSlotManager;
    [SerializeField] public WeaponItem unarmedWeaponData;

    public WeaponItem EquippedWeapon { get; private set; }

    // Quick Slots for Right Hand Weapons
    [Tooltip("Maximum 3 weapons for quick slots.")]
    public WeaponItem[] quickSlotWeapons = new WeaponItem[3];
    private int currentQuickSlotIndex = -1; // -1 means unarmed, 0-2 for quickSlotWeapons

    [Header("Inventory")]
    public List<WeaponItem> collectedWeapons = new List<WeaponItem>(); // All weapons player has found

    private void Awake()
    {
        if (weaponSlotManager == null)
            weaponSlotManager = GetComponentInChildren<WeaponSlotManager>();
        if (weaponSlotManager == null)
            Debug.LogError("PlayerInventory: WeaponSlotManager not found!");
        if (unarmedWeaponData == null)
            Debug.LogError("PlayerInventory: Unarmed Weapon Data (SO_UnarmedWeapon) not assigned!");
    }

    private void Start()
    {
        EquipWeaponFromQuickSlot(0, true); // Force equip initial, even if null
    }

    private void OnEnable()
    {
        InputHandler.DPadRightButtonPressed += CycleNextWeapon;
    }

    private void OnDisable()
    {
        InputHandler.DPadRightButtonPressed -= CycleNextWeapon;
    }

    public void AddWeaponToInventory(WeaponItem weapon)
    {
        if (weapon != null && !weapon.isUnarmed && !collectedWeapons.Contains(weapon))
        {
            collectedWeapons.Add(weapon);
            Debug.Log($"Added {weapon.name} to inventory.");
        }
    }

    public void AssignWeaponToQuickSlot(WeaponItem weapon, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= quickSlotWeapons.Length)
        {
            Debug.LogError($"Invalid quick slot index: {slotIndex}");
            return;
        }
        if (weapon != null && weapon.isUnarmed)
        {
            Debug.LogWarning("Cannot assign Unarmed as a quick slot weapon. It's the default.");
            return;
        }

        quickSlotWeapons[slotIndex] = weapon;
        Debug.Log($"Assigned {weapon?.name ?? "nothing"} to quick slot {slotIndex}.");

        // If this was the currently equipped weapon's slot, re-evaluate equipped weapon
        if (currentQuickSlotIndex == slotIndex)
        {
            EquipWeaponFromQuickSlot(slotIndex); // This will update EquippedWeapon and visuals
        }
        OnEquippedWeaponChanged?.Invoke(EquippedWeapon); // Notify UI
    }

    public void CycleNextWeapon()
    {
        if (playerManager != null && playerManager.playerAnimator.IsInMidAction) return; // Don't switch mid-action

        int initialIndex = currentQuickSlotIndex;
        int nextIndex = currentQuickSlotIndex;
        int attempts = 0;

        do
        {
            nextIndex++;
            if (nextIndex >= quickSlotWeapons.Length)
            {
                nextIndex = -1; // Cycle to unarmed
            }
            attempts++;
            // If next slot is not null OR we've cycled back to unarmed (-1) OR we've tried all slots
            if (nextIndex == -1 || (quickSlotWeapons[nextIndex] != null && quickSlotWeapons[nextIndex] != EquippedWeapon) || attempts > quickSlotWeapons.Length + 1)
            {
                break;
            }
        }
        while (nextIndex != initialIndex && attempts <= quickSlotWeapons.Length + 1); // attempts check prevents infinite loop if all slots are same weapon

        EquipWeaponFromQuickSlot(nextIndex);
    }

    // Call this to equip a weapon. Index -1 for unarmed.
    public void EquipWeaponFromQuickSlot(int slotIndex, bool forceEquip = false)
    {
        if (!forceEquip && playerManager != null && playerManager.playerAnimator.IsInMidAction) return;

        currentQuickSlotIndex = slotIndex;

        if (slotIndex >= 0 && slotIndex < quickSlotWeapons.Length && quickSlotWeapons[slotIndex] != null)
        {
            EquippedWeapon = quickSlotWeapons[slotIndex];
        }
        else
        {
            EquippedWeapon = unarmedWeaponData; // Default to unarmed
            currentQuickSlotIndex = -1; // Explicitly set index for unarmed
        }

        // Debug.Log($"Equipping: {EquippedWeapon.name} (Slot: {currentQuickSlotIndex})");
        weaponSlotManager.LoadWeaponOnSlot(EquippedWeapon, true); // false for right hand
        OnEquippedWeaponChanged?.Invoke(EquippedWeapon);
    }


    // For UI to get weapon in a slot
    public WeaponItem GetWeaponInQuickSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < quickSlotWeapons.Length)
        {
            return quickSlotWeapons[slotIndex];
        }
        return null;
    }

    // Lazy reference for PlayerManager if needed
    private PlayerManager _playerManager;
    private PlayerManager playerManager => _playerManager ??= GetComponent<PlayerManager>();
}