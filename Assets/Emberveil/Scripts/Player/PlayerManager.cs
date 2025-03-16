using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    InputHandler inputHandler;
    Animator animator;
    PlayerLocomotion playerLocomotion;

    [Header("Player Flags")]
    public bool isInteracting;
    public bool isSprinting;
    public bool isInAir;
    public bool isGrounded;

    private void Start()
    {
        inputHandler = GetComponent<InputHandler>();
        animator = GetComponentInChildren<Animator>();
        playerLocomotion = GetComponent<PlayerLocomotion>();
    }

    private void Update()
    {
        var deltaTime = Time.deltaTime;

        isInteracting = animator.GetBool("isInteracting");
        
        inputHandler.TickInput(deltaTime);

        playerLocomotion.HandleMovement(deltaTime);
        playerLocomotion.HandleRollingAndSprinting(deltaTime);
        playerLocomotion.HandleFalling(deltaTime, playerLocomotion.moveDirection);
    }

    private void FixedUpdate()
    {
        float delta = Time.fixedDeltaTime;

        if (CameraHandler.Instance != null)
        {
            CameraHandler.Instance.FollowTarget(delta);
            CameraHandler.Instance.HandleCameraRotation(delta, inputHandler.mouseX, inputHandler.mouseY);
        }
    }

    private void LateUpdate()
    {
        inputHandler.rollFlag = false;
        inputHandler.sprintFlag = false;
        inputHandler.rightBumperInput = false;
        inputHandler.rightTriggerInput = false;

        if (isInAir)
        {
            playerLocomotion.inAirTimer = playerLocomotion.inAirTimer + Time.deltaTime;
        }
    }
}
