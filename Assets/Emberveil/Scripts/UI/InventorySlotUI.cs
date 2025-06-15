using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private GameObject equippedIndicator; // A small dot/image

    private Item heldItem;
    private InventoryWindowUI parentWindow;

    public void Initialize(Item item, InventoryWindowUI parent)
    {
        heldItem = item;
        parentWindow = parent;

        icon.sprite = item.itemIcon;
        icon.enabled = true;
        if (equippedIndicator != null) equippedIndicator.SetActive(false);
    }

    public Item GetItem() => heldItem;

    public void SetEquipped(bool isEquipped)
    {
        if (equippedIndicator != null) equippedIndicator.SetActive(isEquipped);
    }

    // For mouse hover
    public void OnPointerEnter(PointerEventData eventData)
    {
        parentWindow?.OnSlotSelected(this);
    }

    // For controller navigation
    public void OnSelect(BaseEventData eventData)
    {
        parentWindow?.OnSlotSelected(this);
    }

    // Called by an EventTrigger component for Right-Click or a Button component for Submit
    public void OnInteract()
    {
        parentWindow?.OnSlotInteracted(this);
    }
}