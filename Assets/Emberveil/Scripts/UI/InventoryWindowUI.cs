using System.Collections.Generic;
using UnityEngine;

public class InventoryWindowUI : MonoBehaviour
{
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private Transform itemGridParent;
    [SerializeField] private GameObject inventorySlotPrefab;
    [SerializeField] private ItemInfoPanelUI itemInfoPanel;
    // [SerializeField] private ItemContextMenuUI contextMenu;

    private List<InventorySlotUI> currentSlots = new List<InventorySlotUI>();
    private int currentTabIndex = 0; // 0=Weapons, 1=Armor, 2=Talismans, 3=Consumables

    private void OnEnable()
    {
        PlayerInventory.OnInventoryUpdated += RefreshUI; // Refresh if inventory changes
        InputHandler.UITabLeftPressed += SwitchToPreviousTab;
        InputHandler.UITabRightPressed += SwitchToNextTab;
        RefreshUI(); // Initial population
    }

    private void OnDisable()
    {
        PlayerInventory.OnInventoryUpdated -= RefreshUI;
        InputHandler.UITabLeftPressed -= SwitchToPreviousTab;
        InputHandler.UITabRightPressed -= SwitchToNextTab;
    }

    private void SwitchToNextTab() => SwitchTab(1);
    private void SwitchToPreviousTab() => SwitchTab(-1);

    private void SwitchTab(int direction)
    {
        currentTabIndex += direction;
        if (currentTabIndex > 3) currentTabIndex = 0;
        if (currentTabIndex < 0) currentTabIndex = 3;
        RefreshUI();
        // TODO: Update tab button visuals to show active tab
    }

    public void RefreshUI()
    {
        // Clear existing slots
        foreach (Transform child in itemGridParent)
        {
            Destroy(child.gameObject);
        }
        currentSlots.Clear();

        // Populate based on current tab
        switch (currentTabIndex)
        {
            case 0: PopulateGrid(new List<Item>(playerInventory.weaponsInventory)); break;
            case 1: PopulateGrid(new List<Item>(playerInventory.armorInventory)); break;
            case 2: PopulateGrid(new List<Item>(playerInventory.talismanInventory)); break;
            case 3: PopulateGrid(new List<Item>(playerInventory.consumableInventory)); break;
        }
    }

    private void PopulateGrid(List<Item> items)
    {
        foreach (var item in items)
        {
            GameObject slotGO = Instantiate(inventorySlotPrefab, itemGridParent);
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            if (slotUI != null)
            {
                slotUI.Initialize(item, this);
                currentSlots.Add(slotUI);
                // TODO: Check if item is equipped and call slotUI.SetEquipped(true)
            }
        }
    }

    public void OnSlotSelected(InventorySlotUI slot)
    {
        // Update the info panel
        if (itemInfoPanel != null)
        {
            itemInfoPanel.DisplayItemInfo(slot.GetItem());
        }
    }

    public void OnSlotInteracted(InventorySlotUI slot)
    {
        // Show context menu (Use/Discard)
        // if (contextMenu != null)
        // {
        //     contextMenu.Show(slot.GetItem(), slot.transform.position);
        // }
    }
}
