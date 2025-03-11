using UnityEngine;

public class InputHandler : MonoBehaviour
{
    public float horizontal;
    public float vertical;
    public float moveAmount;
    public float mouseX;

    public float mouseY;
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
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    public void TickInput(float deltaTime)
    {
        MoveInput(deltaTime);
    }

    private void MoveInput(float deltaTime)
    {
        horizontal = movementInput.x;
        vertical = movementInput.y;
        moveAmount = Mathf.Clamp01(Mathf.Abs(horizontal) + Mathf.Abs(vertical));

        mouseX = cameraInput.x;
        mouseY = cameraInput.y;
    }
}
