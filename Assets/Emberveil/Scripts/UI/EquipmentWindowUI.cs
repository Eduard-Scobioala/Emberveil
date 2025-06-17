using System.Collections.Generic;
using UnityEngine;

public class EquipmentWindowUI : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private ItemInfoPanelUI itemInfoPanel;
    [SerializeField] private UIManager uiManager;

    [Header("Slot Containers")]
    [SerializeField] private List<EquipmentSlotUI> rightHandSlots = new ();
    // [SerializeField] private List<EquipmentSlotUI> leftHandSlots = new ();
    [SerializeField] private EquipmentSlotUI headArmorSlot;
    [SerializeField] private EquipmentSlotUI bodyArmorSlot;
    [SerializeField] private EquipmentSlotUI handArmorSlot;
    [SerializeField] private EquipmentSlotUI legArmorSlot;
    [SerializeField] private List<EquipmentSlotUI> talismanSlots = new ();
    [SerializeField] private List<EquipmentSlotUI> consumableSlots = new ();

    private void OnEnable()
    {
        // Subscribe to the event when the window becomes active
        PlayerInventory.OnEquipmentUpdated += RefreshAllSlots;

        // Immediately hide the info panel when this window is shown
        if (itemInfoPanel != null)
        {
            itemInfoPanel.ClearPanel();
        }

        // Immediately populate with current equipment state
        RefreshAllSlots();
    }

    private void OnDisable()
    {
        // Unsubscribe when the window is hidden to prevent unnecessary updates
        PlayerInventory.OnEquipmentUpdated -= RefreshAllSlots;
    }

    public void RefreshAllSlots()
    {
        if (playerInventory == null) return;

        // --- Right Hand Weapon Slots ---
        for (int i = 0; i < rightHandSlots.Count; i++)
        {
            if (i < playerInventory.rightHandWeaponSlots.Length)
            {
                rightHandSlots[i].UpdateSlot(playerInventory.rightHandWeaponSlots[i]);
            }
            else // UI has more slots than inventory logic supports, hide extra
            {
                rightHandSlots[i].UpdateSlot(null);
            }
        }

        // --- Armor Slots ---
        headArmorSlot?.UpdateSlot(playerInventory.headArmor);
        bodyArmorSlot?.UpdateSlot(playerInventory.bodyArmor);
        handArmorSlot?.UpdateSlot(playerInventory.handArmor);
        legArmorSlot?.UpdateSlot(playerInventory.legArmor);

        // --- Talisman Slots ---
        for (int i = 0; i < talismanSlots.Count; i++)
        {
            if (i < playerInventory.talismanSlots.Length)
            {
                talismanSlots[i].UpdateSlot(playerInventory.talismanSlots[i]);
            }
            else
            {
                talismanSlots[i].UpdateSlot(null);
            }
        }

        // --- Consumable Slots ---
        for (int i = 0; i < consumableSlots.Count; i++)
        {
            if (consumableSlots[i] != null) // Check the UI slot exists
            {
                if (i < playerInventory.consumableQuickSlots.Count)
                {
                    InventorySlot inventorySlot = playerInventory.consumableQuickSlots[i];
                    // Pass the item from the slot, which can be null if the slot is empty
                    consumableSlots[i].UpdateSlot(inventorySlot?.item);
                }
                else
                {
                    consumableSlots[i].UpdateSlot(null);
                }
            }
        }
    }
}
