using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private InputHandler inputHandler;
    [SerializeField] private PlayerInventory playerInventory;

    [Header("UI Windows")]
    [SerializeField] private GameObject hudWindow;
    [SerializeField] private GameObject mainMenuWindow; // Loadout, Inventory, Options buttons
    [SerializeField] private GameObject equipmentWindow;
    [SerializeField] private GameObject inventoryWindow;
    [SerializeField] private GameObject optionsWindow;

    public bool IsMenuOpen { get; private set; }

    private void Awake()
    {
        if (inputHandler == null) inputHandler = FindObjectOfType<InputHandler>();
    }

    private void OnEnable()
    {
        InputHandler.OptionsButtonPressed += ToggleMainMenu;
        InputHandler.UICancelPressed += HandleCancel; // Listen for UI cancel to close menu
    }

    private void OnDisable()
    {
        InputHandler.OptionsButtonPressed -= ToggleMainMenu;
        InputHandler.UICancelPressed -= HandleCancel;
    }

    public void ToggleMainMenu()
    {
        IsMenuOpen = !IsMenuOpen;

        if (IsMenuOpen)
        {
            OpenMainMenu();
        }
        else
        {
            CloseAllMenus();
        }
    }

    private void OpenMainMenu()
    {
        hudWindow.SetActive(false);
        mainMenuWindow.SetActive(true);

        equipmentWindow.SetActive(false);
        inventoryWindow.SetActive(false);
        optionsWindow.SetActive(false);

        Time.timeScale = 0f; // Pause game
        inputHandler.EnableUIInput();
        // TODO: Select the first button in mainMenuWindow for controller navigation
    }

    public void CloseAllMenus()
    {
        IsMenuOpen = false;
        hudWindow.SetActive(true);
        mainMenuWindow.SetActive(false);
        equipmentWindow.SetActive(false);
        inventoryWindow.SetActive(false);
        optionsWindow.SetActive(false);

        Time.timeScale = 1f; // Resume game
        inputHandler.EnableGameplayInput();
        Cursor.lockState = CursorLockMode.Locked; // Re-lock cursor
        Cursor.visible = false;
    }

    private void HandleCancel()
    {
        // If any sub-menu is open (like filtered inventory), close it first.
        // If only the main menu is open, then close everything.
        if (IsMenuOpen)
        {
            // Add more complex logic here if you have deeper menus
            CloseAllMenus();
        }
    }

    // --- Methods to be called by main menu buttons ---
    public void OpenEquipmentWindow()
    {
        mainMenuWindow.SetActive(false);
        equipmentWindow.SetActive(true);
    }

    public void OpenInventoryWindow()
    {
        mainMenuWindow.SetActive(false);
        inventoryWindow.SetActive(true);
    }

    public void OpenOptionsWindow()
    {
        mainMenuWindow.SetActive(false);
        optionsWindow.SetActive(true);
    }
}
