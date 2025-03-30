using UnityEngine;

public class EquipmentWindowUI : MonoBehaviour
{
    [SerializeField] private HandEquipmentSlotUI[] handEquipmentSlotsUI;

    public void LoadWeaponOnEquipementScreen(PlayerInventory playerInventory)
    {
        foreach (var slot in handEquipmentSlotsUI)
        {
            slot.AddItem(playerInventory.GetItemFromEquipSlot(slot.equipSlotType));
        }
    }
}
