using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class InventoryWindowUI : MonoBehaviour
{
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private Transform itemGridParent;
    [SerializeField] private GameObject inventorySlotPrefab;
    [SerializeField] private GameObject unequipSlotPrefab;
    [SerializeField] private ItemInfoPanelUI itemInfoPanel;
    [SerializeField] private ItemInteractionFooterUI interactionFooter;
    // [SerializeField] private ItemContextMenuUI contextMenu;

    [Header("Tab Management")]
    [SerializeField] private GameObject tabsContainer; // The parent GameObject of your tab buttons
    [SerializeField] private List<TabButtonUI> tabButtons = new List<TabButtonUI>();

    // --- Selection Mode State ---
    private bool isInSelectionMode = false;
    private EquipmentSlotCategory selectionCategory;
    private int selectionSlotIndex;

    private int currentTabIndex = 0; // 0=Weapons, 1=Armor, 2=Talismans, 3=Consumables
    private EquipmentSlotCategory equipmentTypeFilter;

    private void Awake()
    {
        interactionFooter?.Initialize(this);
    }

    private void OnEnable()
    {
        PlayerInventory.OnInventoryUpdated += RefreshUI; // Refresh if inventory changes
        PlayerInventory.OnEquipmentUpdated += RefreshUI; // Also refresh on equip changes to update dots
        InputHandler.UITabLeftPressed += SwitchToPreviousTab;
        InputHandler.UITabRightPressed += SwitchToNextTab;

        // Immediately hide the info panel when this window is shown
        if (itemInfoPanel != null)
        {
            itemInfoPanel.ClearPanel();
        }
        interactionFooter?.Hide();

        RefreshUI(); // Initial population
    }

    private void OnDisable()
    {
        PlayerInventory.OnInventoryUpdated -= RefreshUI;
        PlayerInventory.OnEquipmentUpdated += RefreshUI;
        InputHandler.UITabLeftPressed -= SwitchToPreviousTab;
        InputHandler.UITabRightPressed -= SwitchToNextTab;
    }

    public void SwitchToNextTab() => SwitchTab(1);
    private void SwitchToPreviousTab() => SwitchTab(-1);

    private void SwitchTab(int direction)
    {
        currentTabIndex += direction;
        if (currentTabIndex > 3) currentTabIndex = 0;
        if (currentTabIndex < 0) currentTabIndex = 3;

        SetCurrentTab(currentTabIndex);
    }

    public void SetCurrentTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= tabButtons.Count) return;

        currentTabIndex = tabIndex;
        RefreshUI();
    }

    private void UpdateTabButtonVisuals()
    {
        for (int i = 0; i < tabButtons.Count; i++)
        {
            if (tabButtons[i] != null)
            {
                tabButtons[i].SetActiveState(i == currentTabIndex);
            }
        }
    }

    public void OpenInSelectionMode(EquipmentSlotCategory category, int slotIndex)
    {
        isInSelectionMode = true;
        selectionCategory = category;
        selectionSlotIndex = slotIndex;

        // Disable tab switching
        if (tabsContainer != null) tabsContainer.SetActive(false);
        InputHandler.UITabLeftPressed -= SwitchToPreviousTab;
        InputHandler.UITabRightPressed -= SwitchToNextTab;

        // Force to the correct tab for the item type
        currentTabIndex = GetTabIndexForCategory(category);
        equipmentTypeFilter = category;

        // Refresh the UI to show only the filtered items
        RefreshUI();
    }

    public void ExitSelectionMode()
    {
        isInSelectionMode = false;

        // Re-enable tab switching
        if (tabsContainer != null) tabsContainer.SetActive(true);
        InputHandler.UITabLeftPressed += SwitchToPreviousTab;
        InputHandler.UITabRightPressed += SwitchToNextTab;
    }

    public void RefreshUI()
    {
        if (!gameObject.activeInHierarchy) return;

        UpdateTabButtonVisuals();

        // Clear existing slots
        foreach (Transform child in itemGridParent)
        {
            Destroy(child.gameObject);
        }

        if (isInSelectionMode)
        {
            Item itemInTargetSlot = playerInventory.GetItemInSlot(selectionCategory, selectionSlotIndex);
            if (itemInTargetSlot != null) // Only show unequip if the slot isn't already empty
            {
                // Instantiate the dedicated unequip prefab
                GameObject unequipGO = Instantiate(unequipSlotPrefab, itemGridParent);
                UnequipSlotUI unequipSlotUI = unequipGO.GetComponent<UnequipSlotUI>();
                if (unequipSlotUI != null)
                {
                    unequipSlotUI.Initialize(this);
                }
            }
        }

        // Populate based on current tab
        IEnumerable<InventorySlot> slotsToDisplay = currentTabIndex switch
        {
            0 => playerInventory.weaponsInventory,
            1 => playerInventory.armorInventory,
            2 => playerInventory.talismanInventory,
            3 => playerInventory.consumableInventory,
            _ => null
        };

        if (currentTabIndex == 1 && isInSelectionMode)
        {
            ArmorType typeToFilter = SlotCategoryToArmorType(selectionCategory);
            slotsToDisplay = playerInventory.armorInventory.Where(slot => (slot.item as ArmorItem)?.armorType == typeToFilter).ToList();
        }

        PopulateGrid(slotsToDisplay);
    }

    private Func<InventorySlot, bool> ItemIsOfArmorType(ArmorType armorType)
    {
        return slot => (slot.item as ArmorItem).armorType == armorType;
    }

    private ArmorType SlotCategoryToArmorType(EquipmentSlotCategory slotCategory)
    {
        return slotCategory switch
        {
            EquipmentSlotCategory.Head => ArmorType.Head,
            EquipmentSlotCategory.Body => ArmorType.Body,
            EquipmentSlotCategory.Legs => ArmorType.Legs,
            EquipmentSlotCategory.Hands => ArmorType.Hands,
            _ => (ArmorType)(-1)
        };
    }

    private void PopulateGrid(IEnumerable<InventorySlot> slots)
    {
        foreach (var slotData in slots)
        {
            GameObject slotGO = Instantiate(inventorySlotPrefab, itemGridParent);
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            if (slotUI != null)
            {
                slotUI.Initialize(slotData, this); // Pass the whole InventorySlot
                slotUI.SetEquipped(playerInventory.IsItemEquipped(slotData.item));
            }
        }
    }

    public void OnSlotSelected(InventorySlotUI slot)
    {
        if (itemInfoPanel != null)
        {
            itemInfoPanel.DisplayItemInfo(slot.GetItem());
        }
    }

    public void OnUnequipSlotSelected()
    {
        if (itemInfoPanel != null)
        {
            itemInfoPanel.DisplayUnequipInfo();
        }
    }

    public void OnSlotInteracted(InventorySlotUI slot)
    {
        if (isInSelectionMode)
        {
            playerInventory.EquipItem(slot.GetItem(), selectionCategory, selectionSlotIndex);
            CloseWindowAndReturnToEquipment();
        }
        else
        {
            interactionFooter?.ShowOptionsForItem(slot.GetItem());
        }
    }

    public void OnUseItem(ConsumableItem item)
    {
        if (item != null)
        {
            // Find the corresponding inventory slot to pass to UseConsumable
            InventorySlot slotToUse = playerInventory.consumableInventory.FirstOrDefault(s => s.item == item);
            if (slotToUse != null)
            {
                playerInventory.UseConsumable(slotToUse);
            }
        }
    }

    public void OnDropItem(Item item)
    {
        if (item != null)
        {
            playerInventory.DropItem(item);
        }
    }

    public void OnUnequipSlotInteracted()
    {
        if (isInSelectionMode)
        {
            playerInventory.UnequipItemFromSlot(selectionCategory, selectionSlotIndex);
            CloseWindowAndReturnToEquipment();
        }
    }

    private void CloseWindowAndReturnToEquipment()
    {
        gameObject.SetActive(false);
        FindObjectOfType<UIManager>().OpenEquipmentWindow();
        ExitSelectionMode();
    }

    private int GetTabIndexForCategory(EquipmentSlotCategory category)
    {
        return category switch
        {
            EquipmentSlotCategory.RightHand => 0,  // Weapons
            EquipmentSlotCategory.Armor
                or EquipmentSlotCategory.Head
                or EquipmentSlotCategory.Body
                or EquipmentSlotCategory.Hands
                or EquipmentSlotCategory.Legs => 1,// Armor
            EquipmentSlotCategory.Talisman => 2,   // Talismans
            EquipmentSlotCategory.Consumable => 3, // Consumables
            // Add LeftHand case later
            _ => 0,
        };
    }
}
