using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    InputHandler inputHandler;
    Animator animator;
    CameraHandler cameraHandler;
    PlayerLocomotion playerLocomotion;

    [Header("Player Flags")]
    public bool isInteracting;
    public bool isSprinting;
    public bool isInAir;
    public bool isGrounded;

    private void Awake()
    {
        cameraHandler = CameraHandler.Instance;
    }

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

        if (cameraHandler != null)
        {
            cameraHandler.FollowTarget(delta);
            cameraHandler.HandleCameraRotation(delta, inputHandler.mouseX, inputHandler.mouseY);
        }
    }

    private void LateUpdate()
    {
        isSprinting = inputHandler.bInput;

        inputHandler.rollFlag = false;
        inputHandler.sprintFlag = false;

        if (isInAir)
        {
            playerLocomotion.inAirTimer = playerLocomotion.inAirTimer + Time.deltaTime;
        }
    }
}
