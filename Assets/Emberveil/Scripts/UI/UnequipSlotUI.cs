using UnityEngine;
using UnityEngine.EventSystems;

public class UnequipSlotUI : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    private InventoryWindowUI parentWindow;

    public void Initialize(InventoryWindowUI parent)
    {
        parentWindow = parent;
    }

    // Called by a Button component's OnClick() event
    public void OnUnequipClicked()
    {
        parentWindow?.OnUnequipSlotInteracted();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        parentWindow?.OnUnequipSlotSelected();
    }

    public void OnSelect(BaseEventData eventData)
    {
        parentWindow?.OnUnequipSlotSelected();
    }
}
