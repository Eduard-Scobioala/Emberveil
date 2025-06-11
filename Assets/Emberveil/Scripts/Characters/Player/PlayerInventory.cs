using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static event Action OnInventoryUpdated; // Fire when any item is added/removed
    public static event Action OnEquipmentUpdated; // Fire when any equipped item changes

    [Header("Core References")]
    [SerializeField] private WeaponSlotManager weaponSlotManager;
    [SerializeField] public WeaponItem unarmedWeaponData;

    // --- Equipped Items ---
    [Header("Current Loadout")]
    public WeaponItem[] rightHandWeaponSlots = new WeaponItem[3];
    public Item leftHandItem; // Could be a shield or other item later
    public ArmorItem headArmor, bodyArmor, handArmor, legArmor;
    public TalismanItem[] talismanSlots = new TalismanItem[4];
    public ConsumableItem[] consumableSlots = new ConsumableItem[2]; // For D-Pad Up/Down

    public int currentRightWeaponIndex { get; private set; } = 0; // Index for rightHandWeaponSlots
    public WeaponItem EquippedRightWeapon => rightHandWeaponSlots[currentRightWeaponIndex];

    // --- Master Item Lists ---
    [Header("Collected Items")]
    public List<WeaponItem> weaponsInventory = new();
    public List<ArmorItem> armorInventory = new();
    public List<TalismanItem> talismanInventory = new();
    public List<ConsumableItem> consumableInventory = new();

    private void Awake()
    {
        if (weaponSlotManager == null) weaponSlotManager = GetComponentInChildren<WeaponSlotManager>();
        if (unarmedWeaponData == null) Debug.LogError("PlayerInventory: Unarmed Weapon Data not assigned!");
    }

    private void Start()
    {
        // Set initial state
        EquipWeapon(currentRightWeaponIndex);
    }

    private void OnEnable()
    {
        InputHandler.DPadRightButtonPressed += CycleNextWeapon;
        InputHandler.DPadUpButtonPressed += UseConsumableSlot1;
        InputHandler.DPadDownButtonPressed += UseConsumableSlot2;
    }

    private void OnDisable()
    {
        InputHandler.DPadRightButtonPressed -= CycleNextWeapon;
        InputHandler.DPadUpButtonPressed -= UseConsumableSlot1;
        InputHandler.DPadDownButtonPressed -= UseConsumableSlot2;
    }

    public void AddItem(Item item)
    {
        if (item is WeaponItem weapon) weaponsInventory.Add(weapon);
        else if (item is ArmorItem armor) armorInventory.Add(armor);
        else if (item is TalismanItem talisman) talismanInventory.Add(talisman);
        else if (item is ConsumableItem consumable) consumableInventory.Add(consumable);

        OnInventoryUpdated?.Invoke();
    }

    public void RemoveItem(Item item)
    {
        if (item is WeaponItem weapon) weaponsInventory.Remove(weapon);
        else if (item is ArmorItem armor) armorInventory.Remove(armor);
        else if (item is TalismanItem talisman) talismanInventory.Remove(talisman);
        else if (item is ConsumableItem consumable) consumableInventory.Remove(consumable);

        OnInventoryUpdated?.Invoke();
    }

    // --- Equipment Management ---

    public void EquipWeapon(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= rightHandWeaponSlots.Length) return;

        currentRightWeaponIndex = slotIndex;
        weaponSlotManager.LoadWeaponOnSlot(EquippedRightWeapon ?? unarmedWeaponData, true);
        OnEquipmentUpdated?.Invoke();
    }

    public void EquipArmor(ArmorItem armor)
    {
        if (armor == null) return;
        switch (armor.armorType)
        {
            case ArmorType.Head: headArmor = armor; break;
            case ArmorType.Body: bodyArmor = armor; break;
            case ArmorType.Hands: handArmor = armor; break;
            case ArmorType.Legs: legArmor = armor; break;
        }
        // TODO: Logic to visually change player model
        OnEquipmentUpdated?.Invoke();
    }

    // Add similar EquipTalisman, EquipConsumable methods...

    // --- Action Handlers ---

    private void CycleNextWeapon()
    {
        if (GetComponent<PlayerManager>().playerAnimator.IsInMidAction) return;

        int nextIndex = currentRightWeaponIndex + 1;
        if (nextIndex >= rightHandWeaponSlots.Length)
        {
            nextIndex = 0;
        }
        EquipWeapon(nextIndex);
    }

    private void UseConsumableSlot1() => UseConsumable(consumableSlots[0]);
    private void UseConsumableSlot2() => UseConsumable(consumableSlots[1]);

    private void UseConsumable(ConsumableItem consumable)
    {
        if (consumable != null && !GetComponent<PlayerManager>().playerAnimator.IsInMidAction)
        {
            // TODO: Play use animation, then call consumable.Use() via an animation event
            consumable.Use(GetComponent<PlayerManager>());
            // Optional: Remove from inventory if it's a one-time use item
            // RemoveItem(consumable);
        }
    }
}