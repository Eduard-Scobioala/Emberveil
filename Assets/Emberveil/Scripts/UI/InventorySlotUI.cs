using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private GameObject equippedIndicator; // A small dot/image

    private InventorySlot heldSlotData;
    private InventoryWindowUI parentWindow;

    [Header("Audio")]
    [SerializeField] private SoundSO clickSound;
    [SerializeField] private SoundSO selectedSound;
    private SoundEmitter soundEmitter;

    private void Awake()
    {
        if (!TryGetComponent<SoundEmitter>(out soundEmitter))
        {
            soundEmitter = gameObject.AddComponent<SoundEmitter>();
        }
    }

    public void Initialize(InventorySlot slotData, InventoryWindowUI parent)
    {
        heldSlotData = slotData;
        parentWindow = parent;

        icon.sprite = slotData.item.itemIcon;
        icon.enabled = true;

        // Show quantity text only if stackable (quantity > 1)
        if (quantityText != null)
        {
            if (slotData.item is ConsumableItem)
            {
                quantityText.text = slotData.quantity.ToString();
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }

        if (equippedIndicator != null) equippedIndicator.SetActive(false);
    }

    public Item GetItem() => heldSlotData?.item;
    public InventorySlot GetSlotData() => heldSlotData;

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
        PlaySoundOnSelect();
        parentWindow?.OnSlotSelected(this);
    }

    // Called by an EventTrigger component for Right-Click or a Button component for Submit
    public void OnInteract()
    {
        parentWindow?.OnSlotInteracted(this);
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