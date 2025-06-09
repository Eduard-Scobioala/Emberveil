using System;
using UnityEngine;

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
    //public static event Action DPadUpButtonPressed;
    //public static event Action DPadDownButtonPressed;

    PlayerControls inputActions;

    private void OnEnable()
    {
        inputActions ??= new PlayerControls();
        inputActions.Enable();
        SubscribeInputEventsToHandlers();
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
    }
}
