using System;
using System.Collections.Generic;
using System.Linq;
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
    public WeaponItem[] rightHandWeaponSlots = new WeaponItem[4];
    public Item leftHandItem; // Could be a shield or other item later
    public ArmorItem headArmor, bodyArmor, handArmor, legArmor;
    public TalismanItem[] talismanSlots = new TalismanItem[4];
    public ConsumableItem[] consumableSlots = new ConsumableItem[8]; // For D-Pad Up/Down

    public int currentRightWeaponIndex { get; private set; } = 0; // Index for rightHandWeaponSlots
    public WeaponItem EquippedRightWeapon => rightHandWeaponSlots[currentRightWeaponIndex];

    // --- Master Item Lists ---
    [Header("Collected Items")]
    public List<WeaponItem> weaponsInventory = new();
    public List<ArmorItem> armorInventory = new();
    public List<TalismanItem> talismanInventory = new();
    public List<ConsumableItem> consumableInventory = new();

    [Header("Flasks")]
    public HealthConsumable healingFlask;
    public int maxFlaskCharges = 3;
    public int currentFlaskCharges;

    private PlayerManager playerManager;

    private void Awake()
    {
        if (weaponSlotManager == null) weaponSlotManager = GetComponentInChildren<WeaponSlotManager>();
        if (unarmedWeaponData == null) Debug.LogError("PlayerInventory: Unarmed Weapon Data not assigned!");

        playerManager = GetComponent<PlayerManager>();
    }

    private void Start()
    {
        currentFlaskCharges = maxFlaskCharges;
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
        WeaponItem weaponToEquip = rightHandWeaponSlots[currentRightWeaponIndex] ?? unarmedWeaponData;
        weaponSlotManager.LoadWeaponOnSlot(weaponToEquip, true);
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

    // Similar EquipTalisman, EquipConsumable methods...

    public void UnequipItemFromSlot(EquipmentSlotCategory category, int slotIndex)
    {
        bool changed = false;
        switch (category)
        {
            case EquipmentSlotCategory.RightHand:
                if (slotIndex < rightHandWeaponSlots.Length && rightHandWeaponSlots[slotIndex] != null)
                {
                    rightHandWeaponSlots[slotIndex] = null;
                    if (currentRightWeaponIndex == slotIndex) // If we unequipped the active weapon
                    {
                        EquipWeapon(slotIndex); // This will re-evaluate and load unarmed
                    }
                    changed = true;
                }
                break;
            case EquipmentSlotCategory.Armor:
                // Armor category is determined by the item's ArmorType, not the slot's
                if (headArmor != null) { headArmor = null; changed = true; }
                break;
            case EquipmentSlotCategory.Head: if (headArmor != null) { headArmor = null; changed = true; } break;
            case EquipmentSlotCategory.Body: if (bodyArmor != null) { bodyArmor = null; changed = true; } break;
            case EquipmentSlotCategory.Hands: if (handArmor != null) { handArmor = null; changed = true; } break;
            case EquipmentSlotCategory.Legs: if (legArmor != null) { legArmor = null; changed = true; } break;

            case EquipmentSlotCategory.Talisman:
                if (slotIndex >= 0 && slotIndex < talismanSlots.Length && talismanSlots[slotIndex] != null)
                {
                    talismanSlots[slotIndex] = null;
                    changed = true;
                }
                break;
            case EquipmentSlotCategory.Consumable:
                if (slotIndex < consumableSlots.Length && consumableSlots[slotIndex] != null)
                {
                    consumableSlots[slotIndex] = null;
                    changed = true;
                }
                break;
        }

        if (changed)
        {
            OnEquipmentUpdated?.Invoke();
        }
    }

    public Item GetItemInSlot(EquipmentSlotCategory category, int slotIndex)
    {
        return category switch
        {
            EquipmentSlotCategory.RightHand => slotIndex < rightHandWeaponSlots.Length ? rightHandWeaponSlots[slotIndex] : null,
            EquipmentSlotCategory.Head => headArmor,
            EquipmentSlotCategory.Body => bodyArmor,
            EquipmentSlotCategory.Hands => handArmor,
            EquipmentSlotCategory.Legs => legArmor,
            EquipmentSlotCategory.Talisman => slotIndex < talismanSlots.Length ? talismanSlots[slotIndex] : null,
            EquipmentSlotCategory.Consumable => slotIndex < consumableSlots.Length ? consumableSlots[slotIndex] : null,
            _ => null,
        };
    }

    // --- Action Handlers ---

    private void CycleNextWeapon()
    {
        if (GetComponent<PlayerManager>().playerAnimator.IsInMidAction) return;

        int nextIndex = currentRightWeaponIndex + 1;
        // Loop through available weapons, skipping empty slots
        int attempts = 0;
        while (attempts < rightHandWeaponSlots.Length)
        {
            if (nextIndex >= rightHandWeaponSlots.Length)
            {
                nextIndex = 0; // Wrap around
            }
            if (rightHandWeaponSlots[nextIndex] != null) // Found a non-empty slot
            {
                EquipWeapon(nextIndex);
                return;
            }
            nextIndex++;
            attempts++;
        }
        // If all slots are empty, it will not change.
    }

    private void UseConsumableSlot1() => UseConsumable(consumableSlots[0]);
    private void UseConsumableSlot2() => UseConsumable(consumableSlots[1]);

    public void UseConsumable(ConsumableItem consumable)
    {
        if (consumable == null || playerManager.playerAnimator.IsInMidAction) return;

        // Special handling for the regenerating flask
        if (consumable is HealthConsumable healthPotion && healthPotion.isFlask)
        {
            if (currentFlaskCharges > 0)
            {
                currentFlaskCharges--;
                consumable.Use(playerManager); // Use it
                OnEquipmentUpdated?.Invoke(); // Update UI
            }
            else
            {
                Debug.Log("Out of flask charges!");
                // Play "empty flask" sound
            }
        }
        else // For regular, one-off consumables
        {
            // The consumable's own Use() method should remove it from inventory
            consumable.Use(playerManager);
        }
    }

    public bool IsItemEquipped(Item item)
    {
        if (item == null) return false;

        // Check weapon slots
        if (item is WeaponItem)
        {
            if (rightHandWeaponSlots.Contains(item as WeaponItem)) return true;
        }
        // Check armor slots
        if (item is ArmorItem armor)
        {
            if (headArmor == armor || bodyArmor == armor || handArmor == armor || legArmor == armor) return true;
        }
        // Check talisman slots
        if (item is TalismanItem)
        {
            if (talismanSlots.Contains(item as TalismanItem)) return true;
        }
        // Check consumable slots
        if (item is ConsumableItem)
        {
            if (consumableSlots.Contains(item as ConsumableItem)) return true;
        }
        // Check left hand item
        if (leftHandItem == item) return true;

        return false;
    }

    public void EquipItem(Item item, EquipmentSlotCategory category, int slotIndex)
    {
        // Unequip the item if it's already in another slot of the same category
        // e.g., unequipping a weapon from slot 1 if you try to equip it to slot 0.
        UnequipItem(item);

        switch (category)
        {
            case EquipmentSlotCategory.RightHand:
                if (item is WeaponItem weapon && slotIndex < rightHandWeaponSlots.Length)
                {
                    rightHandWeaponSlots[slotIndex] = weapon;
                    // If this is the active weapon slot, update the model
                    if (currentRightWeaponIndex == slotIndex)
                    {
                        weaponSlotManager.LoadWeaponOnSlot(EquippedRightWeapon ?? unarmedWeaponData, true);
                    }
                }
                break;
            case EquipmentSlotCategory.Armor:
            case EquipmentSlotCategory.Head:
            case EquipmentSlotCategory.Body:
            case EquipmentSlotCategory.Legs:
            case EquipmentSlotCategory.Hands:
                if (item is ArmorItem armor)
                {
                    switch (armor.armorType)
                    {
                        case ArmorType.Head: headArmor = armor; break;
                        case ArmorType.Body: bodyArmor = armor; break;
                        case ArmorType.Hands: handArmor = armor; break;
                        case ArmorType.Legs: legArmor = armor; break;
                    }
                    // TODO: Update visual model
                }
                break;
            case EquipmentSlotCategory.Talisman:
                if (item is TalismanItem talisman && slotIndex >= 0 && slotIndex < talismanSlots.Length)
                {
                    talismanSlots[slotIndex] = talisman;
                    // The update of player stats/buffs will happend on the Invoke
                }
                break;
            case EquipmentSlotCategory.Consumable:
                if (item is ConsumableItem consumable && slotIndex < consumableSlots.Length)
                {
                    consumableSlots[slotIndex] = consumable;
                }
                break;
                // Add LeftHand case when implemented
        }

        OnEquipmentUpdated?.Invoke();
    }

    public void UnequipItem(Item item)
    {
        if (item == null) return;
        bool changed = false;

        for (int i = 0; i < rightHandWeaponSlots.Length; i++)
        {
            if (rightHandWeaponSlots[i] == item) { rightHandWeaponSlots[i] = null; changed = true; }
        }
        if (headArmor == item) { headArmor = null; changed = true; }
        if (bodyArmor == item) { bodyArmor = null; changed = true; }
        if (handArmor == item) { handArmor = null; changed = true; }
        if (legArmor == item) { legArmor = null; changed = true; }
        for (int i = 0; i < talismanSlots.Length; i++)
        {
            if (talismanSlots[i] == item) { talismanSlots[i] = null; changed = true; }
        }
        for (int i = 0; i < consumableSlots.Length; i++)
        {
            if (consumableSlots[i] == item) { consumableSlots[i] = null; changed = true; }
        }
        // etc.

        if (changed) OnEquipmentUpdated?.Invoke();
    }

    public void RefillFlasks()
    {
        currentFlaskCharges = maxFlaskCharges;
        OnEquipmentUpdated?.Invoke();
    }

    public void DropItem(Item item)
    {
        if (item == null || !item.isDroppable || item.itemPickupPrefab == null)
        {
            Debug.LogWarning($"Cannot drop item: {item?.itemName ?? "NULL"}");
            return;
        }

        // Instantiate the pickup prefab in front of the player
        Vector3 dropPosition = transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;
        GameObject droppedItemGO = Instantiate(item.itemPickupPrefab, dropPosition, Quaternion.identity);

        // Ensure the pickup script on the prefab knows which item it contains
        ItemPickUp pickupScript = droppedItemGO.GetComponent<ItemPickUp>();
        if (pickupScript != null)
        {
            pickupScript.SetItem(item);
        }

        // Remove from inventory
        RemoveItem(item);
    }
}