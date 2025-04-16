using System;
using UnityEngine;

public class InputHandler : MonoBehaviour
{
    public float horizontal;
    public float vertical;
    public float moveAmount;
    public float mouseX;
    public float mouseY;

    public bool interactInput;
    public bool optionsInput;
    public bool rightBumperInput;
    public bool rightTriggerInput;
    public bool dPadUp;
    public bool dPadDown;
    public bool dPadLeft;
    public bool dPadRight;

    public bool comboFlag;
    public float rollInputTimer;

    public static event Action DodgeButtonPressed;
    public static event Action DodgeButtonReleased;
    public static event Action JumpButtonPressed;
    public static event Action OptionsButtonPressed;
    public static event Action LockOnButtonPressed;
    public static event Action LeftLockOnTargetButtonPressed;
    public static event Action RightLockOnTargetButtonPressed;
    public static event Action TwoHandingButtonPressed;

    PlayerControls inputActions;
    PlayerAttacker playerAttacker;
    PlayerInventory playerInventory;
    PlayerManager playerManager;

    Vector2 movementInput;
    Vector2 cameraInput;

    private void Awake()
    {
        playerAttacker = GetComponent<PlayerAttacker>();
        playerInventory = GetComponent<PlayerInventory>();
        playerManager = GetComponent<PlayerManager>();
    }

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

        inputActions.PlayerActions.RB.performed += _ => rightBumperInput = true;
        inputActions.PlayerActions.RT.performed += _ => rightTriggerInput = true;

        inputActions.PlayerQuickSlots.DPadRight.performed += _ => dPadRight = true;
        inputActions.PlayerQuickSlots.DPadLeft.performed += _ => dPadLeft = true;

        inputActions.PlayerActions.Jump.performed += _ => JumpButtonPressed?.Invoke();
        inputActions.PlayerActions.Interact.performed += _ => interactInput = true;

        inputActions.PlayerActions.Options.performed += _ => OptionsButtonPressed?.Invoke();
        inputActions.PlayerActions.LockOn.performed += _ => LockOnButtonPressed?.Invoke();
        inputActions.PlayerActions.TwoHanding.performed += _ => TwoHandingButtonPressed?.Invoke();

        inputActions.PlayerMovement.LockOnTargetLeft.performed += _ => LeftLockOnTargetButtonPressed?.Invoke();
        inputActions.PlayerMovement.LockOnTargetRight.performed += _ => RightLockOnTargetButtonPressed?.Invoke();
    }

    public void TickInput(float deltaTime)
    {
        HandleMoveInput(deltaTime);
        HandleAttackInput(deltaTime);
        HandleQuickSlotsInput();
    }

    private void HandleMoveInput(float deltaTime)
    {
        horizontal = movementInput.x;
        vertical = movementInput.y;
        moveAmount = Mathf.Clamp01(Mathf.Abs(horizontal) + Mathf.Abs(vertical));

        mouseX = cameraInput.x;
        mouseY = cameraInput.y;
    }

    private void HandleAttackInput(float deltaTime)
    {
        if (rightBumperInput)
        {
            if (playerManager.canDoCombo)
            {
                comboFlag = true;
                playerAttacker.HandleWeaponCombo(playerInventory.RightHandWeapon);
                comboFlag = false;
            }
            else
            {
                if (playerManager.canDoCombo || playerManager.isInteracting)
                    return;
                playerAttacker.HandleLightAttack(playerInventory.RightHandWeapon);
            }
        }

        if (rightTriggerInput)
        {
            playerAttacker.HandleHeavyAttack(playerInventory.RightHandWeapon);
        }
    }

    private void HandleQuickSlotsInput()
    {
        if (dPadRight)
        {
            playerInventory.ChangeRightWeapon();
        }
        else if (dPadLeft)
        {
            playerInventory.ChangeLeftWeapon();
        }
    }
}
