using System.Collections;
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
    [SerializeField] private GameObject itemInfoWindow;
    [SerializeField] private InventoryWindowUI inventoryWindowUI;
    [SerializeField] private GameObject statusWindow;
    [SerializeField] private GameObject levelUpWindow;

    [Header("Death Screen")]
    [SerializeField] private CanvasGroup youDiedScreenCanvasGroup;

    [Header("Audio")]
    [SerializeField] private SoundSO clickSound;
    private SoundEmitter soundEmitter;

    public bool IsMenuOpen { get; private set; }

    private void Awake()
    {
        if (inputHandler == null) inputHandler = FindObjectOfType<InputHandler>();
        if (inventoryWindowUI == null) inventoryWindowUI = inventoryWindow.GetComponent<InventoryWindowUI>();

        if (youDiedScreenCanvasGroup != null)
        {
            youDiedScreenCanvasGroup.alpha = 0;
            youDiedScreenCanvasGroup.gameObject.SetActive(false);
        }

        if (!TryGetComponent<SoundEmitter>(out soundEmitter))
        {
            soundEmitter = gameObject.AddComponent<SoundEmitter>();
        }
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
        PlaySoundOnClick();
        hudWindow.SetActive(false);
        mainMenuWindow.SetActive(true);

        equipmentWindow.SetActive(false);
        inventoryWindow.SetActive(false);
        optionsWindow.SetActive(false);
        statusWindow.SetActive(false);
        levelUpWindow.SetActive(false);

        //Time.timeScale = 0f; // Pause game
        inputHandler.EnableUIInput();
        Cursor.lockState = CursorLockMode.None; // Re-lock cursor
        Cursor.visible = true;
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
        itemInfoWindow.SetActive(false);
        statusWindow.SetActive(false);
        levelUpWindow.SetActive(false);

        inventoryWindowUI?.ExitSelectionMode();

        //Time.timeScale = 1f; // Resume game
        inputHandler.EnableGameplayInput();
        Cursor.lockState = CursorLockMode.Locked; // Re-lock cursor
        Cursor.visible = false;
    }

    private void HandleCancel()
    {
        PlaySoundOnClick();
        if (levelUpWindow != null && levelUpWindow.activeSelf)
        {
            CloseAllMenus();
            return;
        }

        if (IsMenuOpen)
        {
            // Add more complex logic here if you have deeper menus
            CloseAllMenus();
        }
    }

    public void ShowInventoryForSelection(EquipmentSlotUI clickedSlot)
    {
        if (inventoryWindowUI == null) return;

        // Hide other windows
        equipmentWindow.SetActive(false);

        // Tell the inventory window to open in selection mode
        inventoryWindowUI.OpenInSelectionMode(clickedSlot.slotCategory, clickedSlot.slotIndex);

        // Show the inventory window
        inventoryWindow.SetActive(true);
    }

    // --- Methods to be called by main menu buttons ---
    public void OpenEquipmentWindow()
    {
        PlaySoundOnClick();
        mainMenuWindow.SetActive(false);
        equipmentWindow.SetActive(true);
    }

    public void OpenInventoryWindow()
    {
        PlaySoundOnClick();
        mainMenuWindow.SetActive(false);
        inventoryWindow.SetActive(true);
    }

    public void OpenOptionsWindow()
    {
        PlaySoundOnClick();
        mainMenuWindow.SetActive(false);
        optionsWindow.SetActive(true);
    }

    public void OpenStatusWindow()
    {
        PlaySoundOnClick();
        mainMenuWindow.SetActive(false);
        statusWindow.SetActive(true);
    }

    public void OpenLevelUpWindow()
    {
        PlaySoundOnClick();
        mainMenuWindow.SetActive(false);
        hudWindow.SetActive(false);
        equipmentWindow.SetActive(false);
        inventoryWindow.SetActive(false);
        optionsWindow.SetActive(false);
        statusWindow.SetActive(false);

        InputHandler.UICancelPressed += SaveGame;
        levelUpWindow.SetActive(true);

        IsMenuOpen = true;
        inputHandler.EnableUIInput();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public IEnumerator ShowYouDiedScreen()
    {
        IsMenuOpen = true;
        hudWindow.SetActive(false);
        youDiedScreenCanvasGroup.gameObject.SetActive(true);

        float fadeDuration = 2f;
        float timer = 0;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            youDiedScreenCanvasGroup.alpha = Mathf.Lerp(0, 1, timer / fadeDuration);
            yield return null;
        }
        youDiedScreenCanvasGroup.alpha = 1;
    }

    public void HideYouDiedScreen()
    {
        youDiedScreenCanvasGroup.alpha = 0;
        youDiedScreenCanvasGroup.gameObject.SetActive(false);
        hudWindow.SetActive(true);
        IsMenuOpen = false;
    }

    private void SaveGame()
    {
        InputHandler.UICancelPressed -= SaveGame;
        SaveLoadManager.Instance.SaveGame();
    }

    private void PlaySoundOnClick()
    {
        if (soundEmitter != null)
        {
            soundEmitter.PlaySFX(clickSound);
        }
    }
}
