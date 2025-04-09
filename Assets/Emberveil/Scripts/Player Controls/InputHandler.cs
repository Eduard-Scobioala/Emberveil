using System;
using UnityEngine;

public class InputHandler : MonoBehaviour
{
    public float horizontal;
    public float vertical;
    public float moveAmount;
    public float mouseX;
    public float mouseY;

    public bool bInput;
    public bool interactInput;
    public bool optionsInput;
    public bool jumpInput;
    public bool rightBumperInput;
    public bool rightTriggerInput;
    public bool dPadUp;
    public bool dPadDown;
    public bool dPadLeft;
    public bool dPadRight;

    public bool sprintFlag;
    public bool rollFlag;
    public bool comboFlag;
    public float rollInputTimer;

    public static event Action OptionsButtonPressed;
    public static event Action LockOnButtonPressed;
    public static event Action LeftLockOnTargetButtonPressed;
    public static event Action RightLockOnTargetButtonPressed;
    public static event Action TwoHandingButtonPressed;

    PlayerControls inputActions;
    PlayerAttacker playerAttacker;
    PlayerInventory playerInventory;
    PlayerManager playerManager;
    CameraHandler cameraHandler;

    Vector2 movementInput;
    Vector2 cameraInput;

    private void Awake()
    {
        playerAttacker = GetComponent<PlayerAttacker>();
        playerInventory = GetComponent<PlayerInventory>();
        playerManager = GetComponent<PlayerManager>();
        cameraHandler = FindObjectOfType<CameraHandler>();
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

    private void SubscribeInputEventsToHandlers()
    {
        inputActions.PlayerActions.Roll.started += _ => bInput = true;
        inputActions.PlayerActions.Roll.canceled += _ => bInput = false;

        inputActions.PlayerActions.RB.performed += _ => rightBumperInput = true;
        inputActions.PlayerActions.RT.performed += _ => rightTriggerInput = true;

        inputActions.PlayerQuickSlots.DPadRight.performed += _ => dPadRight = true;
        inputActions.PlayerQuickSlots.DPadLeft.performed += _ => dPadLeft = true;

        inputActions.PlayerActions.Jump.performed += _ => jumpInput = true;
        inputActions.PlayerActions.Interact.performed += _ => interactInput = true;

        inputActions.PlayerActions.Options.performed += _ => OptionsButtonPressed?.Invoke();
        inputActions.PlayerActions.LockOn.performed += _ => LockOnButtonPressed?.Invoke();
        inputActions.PlayerActions.TwoHanding.performed += _ => TwoHandingButtonPressed?.Invoke();

        inputActions.PlayerMovement.LockOnTargetLeft.performed += _ => LeftLockOnTargetButtonPressed?.Invoke();
        inputActions.PlayerMovement.LockOnTargetRight.performed += _ => RightLockOnTargetButtonPressed?.Invoke();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    public void TickInput(float deltaTime)
    {
        HandleMoveInput(deltaTime);
        HandleRollInput(deltaTime);
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

    private void HandleRollInput(float deltaTime)
    {
        sprintFlag = bInput;

        if (bInput)
        {
            rollInputTimer += deltaTime;
        }
        else
        {
            if (rollInputTimer > 0 && rollInputTimer < 0.5f)
            {
                sprintFlag = false;
                rollFlag = true;
            }

            rollInputTimer = 0;
        }
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
