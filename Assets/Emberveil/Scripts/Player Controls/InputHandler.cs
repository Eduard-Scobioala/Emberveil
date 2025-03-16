using UnityEngine;

public class InputHandler : MonoBehaviour
{
    public float horizontal;
    public float vertical;
    public float moveAmount;
    public float mouseX;
    public float mouseY;

    public bool bInput;
    public bool rightBumperInput;
    public bool rightTriggerInput;

    public bool sprintFlag;
    public bool rollFlag;
    public float rollInputTimer;

    PlayerControls inputActions;
    PlayerAttacker playerAttacker;
    PlayerInventory playerInventory;

    Vector2 movementInput;
    Vector2 cameraInput;

    private void Awake()
    {
        playerAttacker = GetComponent<PlayerAttacker>();
        playerInventory = GetComponent<PlayerInventory>();
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
        bInput = inputActions.PlayerActions.Roll.IsPressed();

        if (bInput)
        {
            rollInputTimer += deltaTime;
            sprintFlag = true;
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
        inputActions.PlayerActions.RB.performed += i => rightBumperInput = true;
        inputActions.PlayerActions.RT.performed += i => rightTriggerInput = true;

        if (rightBumperInput)
        {
            playerAttacker.HandleLightAttack(playerInventory.rightHandWeapon);
        }

        if (rightTriggerInput)
        {
            playerAttacker.HandleHeavyAttack(playerInventory.rightHandWeapon);
        }
    }
}
