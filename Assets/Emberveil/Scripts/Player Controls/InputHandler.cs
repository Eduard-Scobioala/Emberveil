using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public static event Action<Vector2> PlayerMovementPerformed;
    public static event Action<Vector2> CameraMovementPerformed;

    // Dodge and Sprint related events
    public static event Action DodgeTapped;
    public static event Action SprintHolding;
    public static event Action SprintReleased;

    // Action Buttons
    public static event Action JumpButtonPressed;
    public static event Action AttackButtonPressed;
    public static event Action InteractButtonPressed;
    public static event Action OptionsButtonPressed;
    public static event Action LockOnButtonPressed;
    public static event Action TwoHandingButtonPressed;
    public static event Action CrouchButtonPressed;

    // Lock-On Target Switching
    public static event Action LeftLockOnTargetButtonPressed;
    public static event Action RightLockOnTargetButtonPressed;

    // D-Pad for Quick Slots
    public static event Action DPadLeftButtonPressed;
    public static event Action DPadRightButtonPressed;
    public static event Action DPadUpButtonPressed;
    public static event Action DPadDownButtonPressed;

    // UI Events
    public static event Action<Vector2> UINavigatePerformed;
    public static event Action UISubmitPressed;
    public static event Action UICancelPressed;
    public static event Action UIContextMenuPressed;
    public static event Action UITabLeftPressed;
    public static event Action UITabRightPressed;


    PlayerControls inputActions;

    private void OnEnable()
    {
        inputActions ??= new PlayerControls();
        inputActions.Enable();
        SubscribeInputEventsToHandlers();

        // Start with gameplay map enabled by default
        SwitchToActionMap(inputActions.PlayerActions);
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void SubscribeInputEventsToHandlers()
    {
        // Player Movement
        inputActions.PlayerMovement.Movement.performed += ctx => PlayerMovementPerformed?.Invoke(ctx.ReadValue<Vector2>());
        inputActions.PlayerMovement.Camera.performed += ctx => CameraMovementPerformed?.Invoke(ctx.ReadValue<Vector2>());
        inputActions.PlayerMovement.LockOnTargetLeft.performed += _ => LeftLockOnTargetButtonPressed?.Invoke();
        inputActions.PlayerMovement.LockOnTargetRight.performed += _ => RightLockOnTargetButtonPressed?.Invoke();

        // Player Actions
        inputActions.PlayerActions.Dodge.performed += _ => DodgeTapped?.Invoke();
        inputActions.PlayerActions.Sprint.performed += _ => SprintHolding?.Invoke();
        inputActions.PlayerActions.Sprint.canceled += _ => SprintReleased?.Invoke();
        inputActions.PlayerActions.Jump.performed += _ => JumpButtonPressed?.Invoke();
        inputActions.PlayerActions.Attack.performed += _ => AttackButtonPressed?.Invoke();
        inputActions.PlayerActions.Interact.performed += _ => InteractButtonPressed?.Invoke();
        inputActions.PlayerActions.LockOn.performed += _ => LockOnButtonPressed?.Invoke();
        inputActions.PlayerActions.TwoHanding.performed += _ => TwoHandingButtonPressed?.Invoke();
        inputActions.PlayerActions.Crouch.performed += _ => CrouchButtonPressed?.Invoke();
        inputActions.PlayerActions.Options.performed += _ => OptionsButtonPressed?.Invoke();

        // Player Quick Slots
        inputActions.PlayerQuickSlots.DPadLeft.performed += _ => DPadLeftButtonPressed?.Invoke();
        inputActions.PlayerQuickSlots.DPadRight.performed += _ => DPadRightButtonPressed?.Invoke();
        inputActions.PlayerQuickSlots.DPadUp.performed += _ => DPadUpButtonPressed?.Invoke();
        inputActions.PlayerQuickSlots.DPadDown.performed += _ => DPadDownButtonPressed?.Invoke();

        // UI Map
        inputActions.UI.Navigate.performed += ctx => UINavigatePerformed?.Invoke(ctx.ReadValue<Vector2>());
        inputActions.UI.Submit.performed += _ => UISubmitPressed?.Invoke();
        inputActions.UI.Cancel.performed += _ => UICancelPressed?.Invoke();
        inputActions.UI.ContextMenu.performed += _ => UIContextMenuPressed?.Invoke();
        inputActions.UI.TabLeft.performed += _ => UITabLeftPressed?.Invoke();
        inputActions.UI.TabRight.performed += _ => UITabRightPressed?.Invoke();
    }

    public void SwitchToActionMap(InputActionMap actionMap)
    {
        if (!actionMap.enabled)
        {
            inputActions.PlayerMovement.Disable();
            inputActions.PlayerActions.Disable();
            inputActions.PlayerQuickSlots.Disable();
            inputActions.UI.Disable();

            actionMap.Enable();
            Debug.Log($"Switched to Action Map: {actionMap.name}");
        }
    }

    // Public methods for UIManager to call
    public void EnableGameplayInput()
    {
        inputActions.UI.Disable();
        inputActions.PlayerMovement.Enable();
        inputActions.PlayerActions.Enable();
        inputActions.PlayerQuickSlots.Enable();
    }

    public void EnableUIInput()
    {
        inputActions.PlayerMovement.Disable();
        inputActions.PlayerActions.Disable();
        inputActions.PlayerQuickSlots.Disable();
        inputActions.UI.Enable();
    }
}
