using System;
using UnityEngine;

public class InputHandler : MonoBehaviour
{
    public float horizontal;
    public float vertical;
    public float moveAmount;
    public float mouseX;
    public float mouseY;

    public static event Action DodgeButtonPressed;
    public static event Action DodgeButtonReleased;
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

    Vector2 movementInput;
    Vector2 cameraInput;

    private void OnEnable()
    {
        if (inputActions == null)
        {
            inputActions = new PlayerControls();
            inputActions.PlayerMovement.Movement.performed +=
                context => movementInput = context.ReadValue<Vector2>();
            inputActions.PlayerMovement.Camera.performed +=
                context => cameraInput = context.ReadValue<Vector2>();
        }

        inputActions.Enable();
        SubscribeInputEventsToHandlers();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void SubscribeInputEventsToHandlers()
    {
        inputActions.PlayerActions.Dodge.started += _ => DodgeButtonPressed?.Invoke();
        inputActions.PlayerActions.Dodge.canceled += _ => DodgeButtonReleased?.Invoke();

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

    public void TickInput(float deltaTime)
    {
        HandleMoveInput(deltaTime);
    }

    private void HandleMoveInput(float deltaTime)
    {
        horizontal = movementInput.x;
        vertical = movementInput.y;
        moveAmount = Mathf.Clamp01(Mathf.Abs(horizontal) + Mathf.Abs(vertical));

        mouseX = cameraInput.x;
        mouseY = cameraInput.y;
    }
}
