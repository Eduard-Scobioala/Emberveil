using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private PlayerInventory playerInventory;

    [SerializeField] private GameObject optionsWindow;

    [Header("Weapon Inventory")]
    [SerializeField] private Transform weaponInventorySlotsParent;
    [SerializeField] private GameObject weaponInventorySlotPrefab;
    private WeaponInventorySlot[] weaponInventorySlots;

    private void OnEnable()
    {
        InputHandler.OnToggleOptions += ToggleOptionsWindow;
    }

    private void OnDisable()
    {
        InputHandler.OnToggleOptions -= ToggleOptionsWindow;
    }

    public void UpdateUI()
    {
        #region Weapon Inventory Slots
        for (int i = 0; i < weaponInventorySlots.Lenght; i++)
        {
            if (i < playerInventory.weaponsInventory.Count)
            {
                if (weaponInventorySlots.Lenght < playerInventory.weaponsInventory.Count)
                {
                    Instantiate(weaponInventorySlotPrefab, weaponInventorySlotsParent);
                    weaponInventorySlots = weaponInventorySlotsParent.GetComponentInChildren<weaponInventorySlotPrefab>();
                }

                weaponInventorySlots[i].AddItem(playerInventory.weaponsInventory[i]);
            }
            else
            {
                weaponInventorySlots[i].ClearInventorySlot();
            }
        }
        #endregion
    }

    public void ToggleOptionsWindow()
    {
        optionsWindow.SetActive(!optionsWindow.activeSelf);
    }
}
