using UnityEngine;
using UnityEngine.UI;

public class ItemInteractionFooterUI : MonoBehaviour
{
    [SerializeField] private Button useButton;
    [SerializeField] private Button dropButton;

    private InventoryWindowUI inventoryWindow;
    private Item currentSelectedItem;

    public void Initialize(InventoryWindowUI parentWindow)
    {
        inventoryWindow = parentWindow;
        useButton.onClick.AddListener(OnUseClicked);
        dropButton.onClick.AddListener(OnDropClicked);
    }

    public void ShowOptionsForItem(Item item)
    {
        currentSelectedItem = item;
        if (item == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        // Show/hide USE button
        useButton.gameObject.SetActive(item is ConsumableItem);

        // Show/hide DROP button
        dropButton.gameObject.SetActive(item.isDroppable);

        // Set the first active button as the selected object for controller navigation
        if (useButton.gameObject.activeInHierarchy)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(useButton.gameObject);
        }
        else if (dropButton.gameObject.activeInHierarchy)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(dropButton.gameObject);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        currentSelectedItem = null;
    }

    private void OnUseClicked()
    {
        inventoryWindow?.OnUseItem(currentSelectedItem as ConsumableItem);
        Hide();
    }

    private void OnDropClicked()
    {
        inventoryWindow?.OnDropItem(currentSelectedItem);
        Hide();
    }

    private void OnDestroy()
    {
        useButton.onClick.RemoveAllListeners();
        dropButton.onClick.RemoveAllListeners();
    }
}
