using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public enum EquipmentSlotCategory { RightHand, LeftHand, Armor, Head, Body, Hands, Legs, Talisman, Consumable }

public class EquipmentSlotUI : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    [Header("Slot Info")]
    public EquipmentSlotCategory slotCategory;
    public int slotIndex; // 0-2 for RightHand, 0-3 for Talisman, etc.

    [Header("UI References")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private ItemInfoPanelUI itemInfoPanel;

    [Header("Audio")]
    [SerializeField] private SoundSO clickSound;
    [SerializeField] private SoundSO selectedSound;
    private SoundEmitter soundEmitter;

    private Item currentItem;

    private void Awake()
    {
        if (!TryGetComponent<SoundEmitter>(out soundEmitter))
        {
            soundEmitter = gameObject.AddComponent<SoundEmitter>();
        }
    }

    public void UpdateSlot(Item item)
    {
        currentItem = item;
        if (currentItem != null && currentItem.itemIcon != null)
        {
            icon.sprite = currentItem.itemIcon;
            icon.enabled = true;
            if (itemNameText != null) itemNameText.text = currentItem.name;
        }
        else
        {
            icon.sprite = null;
            icon.enabled = false;
            if (itemNameText != null) itemNameText.text = "";
        }
    }

    public void OnSlotClicked()
    {
        PlaySoundOnClick();
        uiManager.ShowInventoryForSelection(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (itemInfoPanel != null)
        {
            itemInfoPanel.DisplayItemInfo(currentItem);
        }
    }

    // For controller navigation
    public void OnSelect(BaseEventData eventData)
    {
        PlaySoundOnSelect();
        if (itemInfoPanel != null)
        {
            itemInfoPanel.DisplayItemInfo(currentItem);
        }
    }

    private void PlaySoundOnClick()
    {
        if (soundEmitter != null)
        {
            soundEmitter.PlaySFX(clickSound);
        }
    }

    private void PlaySoundOnSelect()
    {
        if (soundEmitter != null)
        {
            soundEmitter.PlaySFX(selectedSound);
        }
    }
}
