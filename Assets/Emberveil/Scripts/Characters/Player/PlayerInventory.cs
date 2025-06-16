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

    [Tooltip("The list of consumables available for quick cycling.")]
    public List<InventorySlot> consumableQuickSlots = new ();
    [SerializeField] private int maxConsumableQuickSlots = 8;

    public int CurrentRightWeaponIndex { get; private set; } = 0;
    public int CurrentConsumableIndex { get; private set; } = 0;

    public WeaponItem EquippedRightWeapon => (CurrentRightWeaponIndex >= 0 && CurrentRightWeaponIndex < rightHandWeaponSlots.Length) ? rightHandWeaponSlots[CurrentRightWeaponIndex] : null;
    public InventorySlot CurrentConsumableSlot => GetSlotAtIndex(CurrentConsumableIndex);
    public InventorySlot NextConsumableSlot => GetNextValidSlot(CurrentConsumableIndex);

    // --- Master Item Lists ---
    [Header("Collected Items")]
    public List<InventorySlot> weaponsInventory = new();
    public List<InventorySlot> armorInventory = new();
    public List<InventorySlot> talismanInventory = new();
    public List<InventorySlot> consumableInventory = new();

    [Header("Flasks")]
    public HealthConsumable healingFlaskSO;
    public int maxFlaskCharges = 3;
    private InventorySlot _healingFlaskSlot;

    private PlayerManager playerManager;

    private void Awake()
    {
        if (weaponSlotManager == null) weaponSlotManager = GetComponentInChildren<WeaponSlotManager>();
        if (unarmedWeaponData == null) Debug.LogError("PlayerInventory: Unarmed Weapon Data not assigned!");

        playerManager = GetComponent<PlayerManager>();

        for (int i = 0; i < maxConsumableQuickSlots; i++)
        {
            consumableQuickSlots.Add(null);
        }
    }

    private void Start()
    {
        InitializeFlasks();
        EquipWeapon(CurrentRightWeaponIndex);
        OnEquipmentUpdated?.Invoke();
    }

    private void OnEnable()
    {
        InputHandler.DPadRightButtonPressed += CycleNextWeapon;
        InputHandler.DPadUpButtonPressed += UseCurrentConsumable;
        InputHandler.DPadDownButtonPressed += CycleNextConsumable;
    }

    private void OnDisable()
    {
        InputHandler.DPadRightButtonPressed -= CycleNextWeapon;
        InputHandler.DPadUpButtonPressed -= UseCurrentConsumable;
        InputHandler.DPadDownButtonPressed -= CycleNextConsumable;
    }

    private void InitializeFlasks()
    {
        _healingFlaskSlot = consumableInventory.FirstOrDefault(slot => slot?.item == healingFlaskSO);
        if (_healingFlaskSlot == null)
        {
            _healingFlaskSlot = new InventorySlot(healingFlaskSO, maxFlaskCharges);
            consumableInventory.Add(_healingFlaskSlot);
        }
        else
        {
            _healingFlaskSlot.quantity = maxFlaskCharges;
        }

        // Equip flask to the first empty quick slot, or slot 0 if it's empty
        int firstEmptySlot = consumableQuickSlots.FindIndex(s => s == null);
        if (firstEmptySlot != -1)
        {
            consumableQuickSlots[firstEmptySlot] = _healingFlaskSlot;
        }
    }

    public void RefillFlasks()
    {
        if (_healingFlaskSlot != null)
        {
            _healingFlaskSlot.quantity = maxFlaskCharges;
            OnInventoryUpdated?.Invoke(); // Notify UI to update flask count
        }
    }

    public void AddItem(Item item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return;

        List<InventorySlot> targetInventory = GetInventoryListForItemType(item);
        if (targetInventory == null) return;

        // Find if the item already exists in the inventory
        InventorySlot existingSlot = targetInventory.FirstOrDefault(slot => slot.item == item);

        if (existingSlot != null)
        {
            // Add to existing stack
            existingSlot.AddQuantity(quantity);
        }
        else
        {
            // Create a new stack
            targetInventory.Add(new InventorySlot(item, quantity));
        }

        OnInventoryUpdated?.Invoke();
    }

    public void RemoveItem(Item item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return;

        List<InventorySlot> targetInventory = GetInventoryListForItemType(item);
        if (targetInventory == null) return;

        InventorySlot existingSlot = targetInventory.FirstOrDefault(slot => slot.item == item);
        if (existingSlot != null)
        {
            existingSlot.RemoveQuantity(quantity);
            if (existingSlot.quantity <= 0)
            {
                // Also check if this item is in any quick slots and clear them
                for (int i = 0; i < consumableQuickSlots.Count; i++)
                {
                    if (consumableQuickSlots[i]?.item == item)
                    {
                        consumableQuickSlots[i] = null;
                    }
                }
                targetInventory.Remove(existingSlot);
                OnEquipmentUpdated?.Invoke(); // Fire this too, in case it was a quick-slotted item
            }
        }

        OnInventoryUpdated?.Invoke();
    }

    private List<InventorySlot> GetInventoryListForItemType(Item item)
    {
        return item switch
        {
            WeaponItem => weaponsInventory,
            ArmorItem => armorInventory,
            TalismanItem => talismanInventory,
            ConsumableItem => consumableInventory,
            _ => null
        };
    }

    // --- Equipment Management ---

    public void EquipWeapon(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= rightHandWeaponSlots.Length) return;

        CurrentRightWeaponIndex = slotIndex;
        WeaponItem weaponToEquip = rightHandWeaponSlots[CurrentRightWeaponIndex] ?? unarmedWeaponData;
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

    public Item GetItemInSlot(EquipmentSlotCategory category, int slotIndex)
    {
        switch (category)
        {
            case EquipmentSlotCategory.RightHand:
                return slotIndex >= 0 && slotIndex < rightHandWeaponSlots.Length ? rightHandWeaponSlots[slotIndex] : null;
            case EquipmentSlotCategory.Head: return headArmor;
            case EquipmentSlotCategory.Body: return bodyArmor;
            case EquipmentSlotCategory.Hands: return handArmor;
            case EquipmentSlotCategory.Legs: return legArmor;
            case EquipmentSlotCategory.Talisman:
                return slotIndex >= 0 && slotIndex < talismanSlots.Length ? talismanSlots[slotIndex] : null;

            case EquipmentSlotCategory.Consumable:
                return slotIndex >= 0 && slotIndex < consumableQuickSlots.Count && consumableQuickSlots[slotIndex] != null
                    ? consumableQuickSlots[slotIndex].item
                    : null;

            default:
                return null;
        }
    }

    // --- Action Handlers ---

    private void CycleNextWeapon()
    {
        if (playerManager.playerAnimator.IsInMidAction) return;

        ChangeWeapon(1);
    }

    private void ChangeWeapon(int direction)
    {
        if (playerManager.playerAnimator.IsInMidAction) return;
        if (rightHandWeaponSlots.All(slot => slot == null)) { EquipWeapon(CurrentRightWeaponIndex); return; }

        int nextIndex = CurrentRightWeaponIndex;
        for (int i = 0; i < rightHandWeaponSlots.Length; i++)
        {
            nextIndex = (nextIndex + direction + rightHandWeaponSlots.Length) % rightHandWeaponSlots.Length;
            if (rightHandWeaponSlots[nextIndex] != null)
            {
                EquipWeapon(nextIndex);
                return;
            }
        }
    }

    public void UseConsumable(InventorySlot consumableSlot)
    {
        if (consumableSlot?.item == null || consumableSlot.quantity <= 0 || playerManager.playerAnimator.IsInMidAction) return;
        var consumable = consumableSlot.item as ConsumableItem;
        if (consumable == null) return;

        consumable.Use(playerManager);

        if (consumableSlot.item != healingFlaskSO) RemoveItem(consumableSlot.item, 1);
        else _healingFlaskSlot.RemoveQuantity(1);

        OnInventoryUpdated?.Invoke();
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
            if (consumableQuickSlots.Select(slot => slot?.item).Contains(item)) return true;
        }
        // Check left hand item
        if (leftHandItem == item) return true;

        return false;
    }

    public void EquipItem(Item item, EquipmentSlotCategory category, int slotIndex)
    {
        UnequipItem(item);
        switch (category)
        {
            case EquipmentSlotCategory.RightHand:
                if (item is WeaponItem weapon && slotIndex < rightHandWeaponSlots.Length)
                {
                    rightHandWeaponSlots[slotIndex] = weapon;
                    if (CurrentRightWeaponIndex == slotIndex) EquipWeapon(slotIndex);
                }
                break;
            case EquipmentSlotCategory.Head: if (item is ArmorItem h) headArmor = h; break;
            case EquipmentSlotCategory.Body: if (item is ArmorItem b) bodyArmor = b; break;
            case EquipmentSlotCategory.Hands: if (item is ArmorItem ha) handArmor = ha; break;
            case EquipmentSlotCategory.Legs: if (item is ArmorItem l) legArmor = l; break;
            case EquipmentSlotCategory.Talisman:
                if (item is TalismanItem t && slotIndex < talismanSlots.Length) talismanSlots[slotIndex] = t;
                break;
            case EquipmentSlotCategory.Consumable:
                if (item is ConsumableItem c && slotIndex < consumableQuickSlots.Count)
                {
                    InventorySlot sourceSlot = consumableInventory.FirstOrDefault(s => s?.item == c);
                    consumableQuickSlots[slotIndex] = sourceSlot;
                }
                break;
        }
        OnEquipmentUpdated?.Invoke();
    }

    public void UnequipItem(Item item)
    {
        if (item == null) return;

        // Check weapon slots
        for (int i = 0; i < rightHandWeaponSlots.Length; i++)
        {
            if (rightHandWeaponSlots[i] == item)
            {
                rightHandWeaponSlots[i] = null;
                // If it was the active weapon, re-equip to trigger unarmed state
                if (CurrentRightWeaponIndex == i) EquipWeapon(i);
            }
        }

        // Check armor slots
        if (headArmor == item) headArmor = null;
        if (bodyArmor == item) bodyArmor = null;
        if (handArmor == item) handArmor = null;
        if (legArmor == item) legArmor = null;

        // Check talisman slots
        for (int i = 0; i < talismanSlots.Length; i++)
        {
            if (talismanSlots[i] == item) talismanSlots[i] = null;
        }

        // Check consumable quick slots
        for (int i = 0; i < consumableQuickSlots.Count; i++)
        {
            if (consumableQuickSlots[i]?.item == item)
            {
                consumableQuickSlots[i] = null;
            }
        }
    }

    public void UnequipItemFromSlot(EquipmentSlotCategory category, int slotIndex)
    {
        bool changed = false;
        switch (category)
        {
            case EquipmentSlotCategory.RightHand:
                if (slotIndex < rightHandWeaponSlots.Length && rightHandWeaponSlots[slotIndex] != null)
                {
                    rightHandWeaponSlots[slotIndex] = null;
                    if (CurrentRightWeaponIndex == slotIndex) // If we unequipped the active weapon
                    {
                        EquipWeapon(slotIndex); // This will re-evaluate and load unarmed
                    }
                    changed = true;
                }
                break;
            case EquipmentSlotCategory.Armor:
                Debug.LogWarning("UnequipItemFromSlot called with generic 'Armor' category. Please specify Head, Body, etc.");
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
                if (slotIndex < consumableQuickSlots.Count && consumableQuickSlots[slotIndex] != null)
                {
                    consumableQuickSlots[slotIndex] = null;
                    changed = true;
                }
                break;
        }

        if (changed)
        {
            OnEquipmentUpdated?.Invoke();
        }
    }

    public void AssignConsumableToQuickSlot(InventorySlot consumableSlot)
    {
        if (consumableSlot == null || consumableSlot.item as ConsumableItem == null) return;
        if (!consumableQuickSlots.Contains(consumableSlot))
        {
            consumableQuickSlots.Add(consumableSlot);
            OnEquipmentUpdated?.Invoke();
        }
    }

    public void RemoveConsumableFromQuickSlot(InventorySlot consumableSlot)
    {
        if (consumableSlot == null) return;
        if (consumableQuickSlots.Contains(consumableSlot))
        {
            consumableQuickSlots.Remove(consumableSlot);
            // Adjust index if we removed the current or a previous slot
            if (CurrentConsumableIndex >= consumableQuickSlots.Count && consumableQuickSlots.Count > 0)
            {
                CurrentConsumableIndex = consumableQuickSlots.Count - 1;
            }
            OnEquipmentUpdated?.Invoke();
        }
    }

    public void CycleNextConsumable()
    {
        if (playerManager.playerAnimator.IsInMidAction) return;
        int nextValidIndex = FindNextValidConsumableSlot(1);
        if (nextValidIndex != -1)
        {
            CurrentConsumableIndex = nextValidIndex;
            OnEquipmentUpdated?.Invoke();
        }
    }

    private int FindNextValidConsumableSlot(int direction)
    {
        if (consumableQuickSlots.All(slot => slot == null)) return -1;
        int nextIndex = CurrentConsumableIndex;
        for (int i = 0; i < consumableQuickSlots.Count; i++)
        {
            nextIndex = (nextIndex + direction + consumableQuickSlots.Count) % consumableQuickSlots.Count;
            if (consumableQuickSlots[nextIndex] != null) return nextIndex;
        }
        return CurrentConsumableIndex; // Fallback to current if it's the only one
    }

    private void UseCurrentConsumable()
    {
        InventorySlot slotToUse = CurrentConsumableSlot;
        if (slotToUse == null) return;
        UseConsumable(slotToUse);
    }

    private InventorySlot GetSlotAtIndex(int index)
    {
        if (consumableQuickSlots == null || consumableQuickSlots.Count == 0 || index < 0 || index >= consumableQuickSlots.Count) return null;
        return consumableQuickSlots[index];
    }

    private InventorySlot GetNextValidSlot(int startIndex)
    {
        if (consumableQuickSlots.Count(s => s != null) <= 1) return null; // No "next" if 0 or 1 items
        int nextValidIndex = FindNextValidConsumableSlot(1);
        return consumableQuickSlots[nextValidIndex];
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
        RemoveItem(item, 1);
    }
}