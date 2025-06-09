using UnityEngine;
using UnityEngine.UI;

public class HandEquipmentSlotUI : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private Image icon;
    private Item item;

    //public EquipSlotType equipSlotType;


    public void AddItem(Item newItem)
    {
        item = newItem;
        icon.sprite = item.itemIcon;
        icon.enabled = true;
        gameObject.SetActive(true);
    }

    public void ClearItem()
    {
        item = null;
        icon.sprite = null;
        icon.enabled = false;
        gameObject.SetActive(false);
    }

    public void SelectSlot()
    {
        //uiManager.currentSelectedSlotType = equipSlotType;
    }
}
