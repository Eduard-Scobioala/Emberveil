using System;
using UnityEngine;

public class InputHandler : MonoBehaviour
{
    public static event Action<Vector2> PlayerMovementPerformed;
    public static event Action<Vector2> CameraMovementPerformed;

    public static event Action DodgeButtonPressed;
    public static event Action DodgeButtonReleased;

    public static event Action DodgeTapped;
    public static event Action SprintHolding;
    public static event Action SprintReleased;

    public static event Action JumpButtonPressed;
    public static event Action LightAttackButtonPressed;
    public static event Action HeavyAttackButtonPressed;
    public static event Action InteractButtonPressed;
    public static event Action OptionsButtonPressed;
    public static event Action LockOnButtonPressed;
    public static event Action LeftLockOnTargetButtonPressed;
    public static event Action RightLockOnTargetButtonPressed;
    public static event Action TwoHandingButtonPressed;
    //public static event Action DPadUpButtonPressed;
    //public static event Action DPadDownButtonPressed;
    public static event Action DPadLeftButtonPressed;
    public static event Action DPadRightButtonPressed;

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
        inputActions.PlayerMovement.Movement.performed +=
            context => PlayerMovementPerformed?.Invoke(context.ReadValue<Vector2>());

        inputActions.PlayerMovement.Camera.performed +=
            context => CameraMovementPerformed?.Invoke(context.ReadValue<Vector2>());

        //inputActions.PlayerActions.Dodge.started += _ => DodgeButtonPressed?.Invoke();
        //inputActions.PlayerActions.Dodge.canceled += _ => DodgeButtonReleased?.Invoke();

        inputActions.PlayerActions.Dodge.performed += context => DodgeTapped?.Invoke();
        inputActions.PlayerActions.Sprint.performed += context => SprintHolding?.Invoke();
        inputActions.PlayerActions.Sprint.canceled += context => SprintReleased?.Invoke();

        inputActions.PlayerActions.RB.performed += _ => LightAttackButtonPressed?.Invoke();
        inputActions.PlayerActions.RT.performed += _ => HeavyAttackButtonPressed?.Invoke();
        inputActions.PlayerActions.TwoHanding.performed += _ => TwoHandingButtonPressed?.Invoke();

        inputActions.PlayerQuickSlots.DPadRight.performed += _ => DPadRightButtonPressed?.Invoke();
        inputActions.PlayerQuickSlots.DPadLeft.performed += _ => DPadLeftButtonPressed?.Invoke();

        inputActions.PlayerActions.Jump.performed += _ => JumpButtonPressed?.Invoke();
        inputActions.PlayerActions.Interact.performed += _ => InteractButtonPressed?.Invoke();

        inputActions.PlayerActions.LockOn.performed += _ => LockOnButtonPressed?.Invoke();
        inputActions.PlayerMovement.LockOnTargetLeft.performed += _ => LeftLockOnTargetButtonPressed?.Invoke();
        inputActions.PlayerMovement.LockOnTargetRight.performed += _ => RightLockOnTargetButtonPressed?.Invoke();

        inputActions.PlayerActions.Options.performed += _ => OptionsButtonPressed?.Invoke();
    }
}
