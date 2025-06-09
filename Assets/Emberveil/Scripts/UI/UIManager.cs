using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private EquipmentWindowUI equipmentWindow;

    [Header("UI Windows")]
    [SerializeField] private GameObject HUDInterface;
    [SerializeField] private GameObject optionsWindow;
    [SerializeField] private GameObject weaponInventoryWindow;
    [SerializeField] private GameObject equipementInventoryWindow;

    [Header("Weapon Inventory")]
    [SerializeField] private Transform weaponInventorySlotsParent;
    [SerializeField] private GameObject weaponInventorySlotPrefab;

    private WeaponInventorySlot[] weaponInventorySlots;

    //public EquipSlotType currentSelectedSlotType;

    private void Start()
    {
        weaponInventorySlots = weaponInventorySlotsParent.GetComponentsInChildren<WeaponInventorySlot>();
        equipmentWindow.LoadWeaponOnEquipementScreen(playerInventory);
    }

    private void OnEnable()
    {
        InputHandler.OptionsButtonPressed += HandleOptionsButtonPressed;
    }

    private void OnDisable()
    {
        InputHandler.OptionsButtonPressed -= HandleOptionsButtonPressed;
    }

    private void HandleOptionsButtonPressed()
    {
        bool isHUDActive = HUDInterface.activeSelf;

        ToggleHUDInterface(!isHUDActive);
        ToggleOptionsWindow(isHUDActive);

        // ESQ pressed while HUD active - prepare the UI for the Inventory
        if (isHUDActive)
        {
            UpdateUI();
        }
        // ESQ pressed while in inventory - close all windows
        else
        {
            CloseAllInventoryWindows();
        }
    }

    public void ToggleOptionsWindow(bool windowStatus)
    {
        optionsWindow.SetActive(windowStatus);
    }

    public void ToggleHUDInterface(bool windowStatus)
    {
        HUDInterface.SetActive(windowStatus);
    }

    private void UpdateUI()
    {
        #region Weapon Inventory Slots
        for (int i = 0; i < weaponInventorySlots.Length; i++)
        {
            //if (i < playerInventory.weaponsInventory.Count)
            //{
            //    if (weaponInventorySlots.Length < playerInventory.weaponsInventory.Count)
            //    {
            //        Instantiate(weaponInventorySlotPrefab, weaponInventorySlotsParent);
            //        weaponInventorySlots = weaponInventorySlotsParent.GetComponentsInChildren<WeaponInventorySlot>();
            //    }

            //    weaponInventorySlots[i].AddItem(playerInventory.weaponsInventory[i]);
            //}
            //else
            //{
            //    weaponInventorySlots[i].ClearInventorySlot();
            //}
        }
        #endregion
    }

    private void CloseAllInventoryWindows()
    {
        //currentSelectedSlotType = 0;

        weaponInventoryWindow.SetActive(false);
        equipementInventoryWindow.SetActive(false);
    }
}
