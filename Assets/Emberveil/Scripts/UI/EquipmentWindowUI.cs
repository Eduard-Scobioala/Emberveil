using UnityEngine;

public class EquipmentWindowUI : MonoBehaviour
{
    public bool rightHandSlot01Selected;
    public bool rightHandSlot02Selected;
    public bool leftHandSlot01Selected;
    public bool leftHandSlot02Selected;

    private HandEquipmentSlotUI[] handEquipmentSlotUIs;

    private void Start()
    {
        handEquipmentSlotUIs = GetComponentsInChildren<HandEquipmentSlotUI>();
    }

    public void LoadWeaponOnEquipementScreen(PlayerInventory playerInventory)
    {
        for (int i = 0; i < handEquipmentSlotUIs.Length; i++)
        {
            if (handEquipmentSlotUIs[i].rightHandSlot01)
            {
                handEquipmentSlotUIs[i].AddItem(playerInventory.weaponsInRightHandSlots[0]);
            }
            else if (handEquipmentSlotUIs[i].rightHandSlot02)
            {
                handEquipmentSlotUIs[i].AddItem(playerInventory.weaponsInRightHandSlots[1]);
            }
            else if (handEquipmentSlotUIs[i].leftHandSlot01)
            {
                handEquipmentSlotUIs[i].AddItem(playerInventory.weaponsInLeftHandSlots[0]);
            }
            else if (handEquipmentSlotUIs[i].leftHandSlot02)
            {
                handEquipmentSlotUIs[i].AddItem(playerInventory.weaponsInLeftHandSlots[1]);
            }
        }
    }

    public void SelectedRightHandSlot01()
    {
        rightHandSlot01Selected = true;
    }

    public void SelectedRightHandSlot02()
    {
        rightHandSlot02Selected = true;
    }

    public void SelectedLeftHandSlot01()
    {
        leftHandSlot01Selected = true;
    }

    public void SelectedLeftHandSlot02()
    {
        leftHandSlot02Selected = true;
    }
}
