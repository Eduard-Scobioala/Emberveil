using UnityEngine;

public class PlayerLocomotion : MonoBehaviour
{
    [Header("Camera Reference")]
    [SerializeField] private CameraController cameraController;

    private Transform playerTransform;
    private PlayerManager playerManager;
    private AnimatorHandler animatorHandler;

    public new Rigidbody rigidbody;
    //public Vector3 moveDirection;

    [Header("Movement Stats")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float walkingSpeed = 2f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private float fallingSpeed = 300f;
    [SerializeField] private float movementSmoothTime = 0.05f;
    [SerializeField] private float maxFallSpeed = 350f;
    [SerializeField] private float airControlFactor = 1f;
    //[SerializeField] private float lockOnMovementForwardMultiplier = 0.125f;
    //[SerializeField] private float lockOnDodgeForwardMultiplier = 0.9f;

    [Header("Lock-On Movement Multipliers")]
    [SerializeField] private float lockOnStrafeSpeedMultiplier = 0.8f;
    [SerializeField] private float lockOnBackwardSpeedMultiplier = 0.6f;

    [Header("Ground & Air Detection Stats")]
    [SerializeField] private float groundDetectionRayStartPoint = 0.5f;
    //[SerializeField] private float minimumDistanceNeededToBeginFall = 1f;
    [SerializeField] private float groundDirectionRayDistance = 0.2f;
    [SerializeField] private LayerMask ignoreForGroundCheck;
    public float inAirTimer;

    // Input values
    private float horizontalInput;
    private float verticalInput;
    private float moveAmount;

    private Vector3 _currentNormalVector = Vector3.up;
    private Vector3 _rigidbodyVelocityRef = Vector3.zero;

    private void Awake()
    {
        playerTransform = transform;
        rigidbody = GetComponent<Rigidbody>();
        playerManager = GetComponent<PlayerManager>();
        animatorHandler = GetComponentInChildren<AnimatorHandler>();

        if (cameraController == null) Debug.LogError("CameraController not assigned on PlayerLocomotion!", this);
        
        if (animatorHandler == null) Debug.LogError("AnimatorHandler not found on children!", this);
        else animatorHandler.Initialize();

        if (playerManager == null) Debug.LogError("PlayerManager not found on this GameObject!", this);
        else playerManager.isGrounded = true;
    }

    private void Update()
    {
        if (playerManager == null || cameraController == null) return;

        // Calculate moveAmount based on raw input for animation blending
        moveAmount = Mathf.Clamp01(Mathf.Abs(horizontalInput) + Mathf.Abs(verticalInput));
        // PlayerManager.isSprinting is set by sprint input
        // PlayerManager.isCrouching is set by crouch input

        if (animatorHandler != null)
        {
            animatorHandler.UpdateAnimatorValues(
                verticalInput,
                horizontalInput,
                playerManager.isSprinting,
                playerManager.isCrouching,
                cameraController.IsLockedOn
            );
        }
    }

    private void FixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;
        if (playerManager == null || cameraController == null) return;

        playerManager.isGrounded = CheckGrounded();
        HandleAllMovement(deltaTime);
    }

    private void OnEnable()
    {
        InputHandler.PlayerMovementPerformed += HandleMovementInput;
    }

    private void OnDisable()
    {
        InputHandler.PlayerMovementPerformed -= HandleMovementInput;
    }


    public void HandleAllMovement(float deltaTime)
    {
        if (playerManager == null) return;

        HandleFallingAndLanding(deltaTime);

        if (playerManager.isInMidAction) return;

        HandleGroundedMovement(deltaTime);
        HandleRotation(deltaTime);
    }

    private void HandleMovementInput(Vector2 movementInput)
    {
        horizontalInput = movementInput.x;
        verticalInput = movementInput.y;
    }

    private void HandleGroundedMovement(float deltaTime)
    {
        Vector3 targetMoveDirection;

        // Determine base move direction from input and camera/player orientation
        if (cameraController.IsLockedOn && !playerManager.isSprinting) // Lock-on movement (not sprinting)
        {
            // Player's local forward/right based on current facing (towards target)
            targetMoveDirection = playerTransform.forward * verticalInput + playerTransform.right * horizontalInput;
        }
        else // Free movement or sprinting
        {
            Vector3 cameraForward = cameraController.transform.forward;
            Vector3 cameraRight = cameraController.transform.right;
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();
            targetMoveDirection = cameraForward * verticalInput + cameraRight * horizontalInput;
        }

        targetMoveDirection.Normalize(); // Ensure consistent speed for diagonal movement

        // Determine current speed based on state
        float currentSpeed;
        if (playerManager.isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else if (playerManager.isSprinting)
        {
            currentSpeed = sprintSpeed;
        }
        else // Walking or normal running based on input magnitude
        {
            // Used moveAmount (0-1) to blend between walk and run speed.
            // PlayerAnimatorHandler will determine if it's walk (0.5) or run (1.0) for animation.
            currentSpeed = (moveAmount > 0.5f) ? movementSpeed : walkingSpeed;
            if (moveAmount <= 0.01f) currentSpeed = 0; // No input, no speed
        }

        // Apply speed multipliers for lock-on if applicable
        if (cameraController.IsLockedOn && !playerManager.isCrouching && !playerManager.isSprinting)
        {
            if (verticalInput < -0.1f) // Moving backward
            {
                currentSpeed *= lockOnBackwardSpeedMultiplier;
            }
            else if (Mathf.Abs(horizontalInput) > 0.1f && verticalInput >= -0.1f) // Strafing (and not primarily moving backward)
            {
                currentSpeed *= lockOnStrafeSpeedMultiplier;
            }
        }

        Vector3 finalVelocity = targetMoveDirection * currentSpeed;

        // Project onto ground normal
        finalVelocity = Vector3.ProjectOnPlane(finalVelocity, _currentNormalVector);
        finalVelocity.y = rigidbody.velocity.y; // Preserve existing Y velocity for jump/fall continuity

        rigidbody.velocity = Vector3.SmoothDamp(rigidbody.velocity, finalVelocity, ref _rigidbodyVelocityRef, movementSmoothTime);
    }

    private void HandleRotation(float deltaTime)
    {
        if (playerManager.isCrouching && cameraController.IsLockedOn)
        {
            // Minimal rotation when crouched and locked on, primarily face target
            if (cameraController.CurrentLockOnTarget != null)
            {
                Vector3 directionToTarget = cameraController.CurrentLockOnTarget.transform.position - playerTransform.position;
                directionToTarget.y = 0;
                if (directionToTarget.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget.normalized);
                    playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, targetRotation, rotationSpeed * deltaTime * 0.5f); // Slower rotation
                }
            }
            return;
        }


        if (animatorHandler.canRotate == false && playerManager.isInMidAction) // Animator controls rotation during some actions
            return;

        Vector3 targetDir;
        if (cameraController.IsLockedOn && !playerManager.isSprinting) // Prioritize facing lock-on target
        {
            if (cameraController.CurrentLockOnTarget != null)
            {
                targetDir = cameraController.CurrentLockOnTarget.transform.position - playerTransform.position;
            }
            else targetDir = playerTransform.forward; // Fallback
        }
        else // Free look rotation based on movement input relative to camera
        {
            Vector3 camForward = cameraController.transform.forward;
            Vector3 camRight = cameraController.transform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            targetDir = camForward * verticalInput + camRight * horizontalInput;
        }

        targetDir.y = 0;
        if (targetDir.sqrMagnitude < 0.01f && !cameraController.IsLockedOn) // If no input and not locked on, don't force rotation
        {
            // TODO: If not locked on and no input, maintain current forward or gradually align with camera.
            // For now, just don't rotate if no input and not locked on.
            return;
        }
        if (targetDir.sqrMagnitude < 0.01f && cameraController.IsLockedOn) // If locked on but no move input, still face target
        {
            if (cameraController.CurrentLockOnTarget != null)
                targetDir = cameraController.CurrentLockOnTarget.transform.position - playerTransform.position;
            else targetDir = playerTransform.forward;
            targetDir.y = 0;
        }


        targetDir.Normalize();
        Quaternion tr = Quaternion.LookRotation(targetDir);
        playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, tr, rotationSpeed * deltaTime);
    }

    public void HandleDodgeTapped()
    {
        if (playerManager.isInMidAction || !playerManager.isGrounded) return;

        if (playerManager.isCrouching) // Stand up to dodge
        {
            playerManager.isCrouching = false;
            // Animator should transition from crouch due to isCrouching = false
        }
        PerformDodge();
    }

    private void PerformDodge()
    {
        playerManager.isInMidAction = true; // Player is busy
        animatorHandler.SetBool("isDodging", true); // For animator state if needed

        string targetAnimation;
        Vector3 dodgeDir = playerTransform.forward; // Default dodge forward if no input

        if (moveAmount > 0.01f) // Directional Dodge / Roll
        {
            targetAnimation = playerManager.isCrouching ? "Crouch_Roll" : "Roll"; // Different roll if somehow still crouching

            if (cameraController.IsLockedOn)
            {
                dodgeDir = playerTransform.forward * verticalInput + playerTransform.right * horizontalInput;
            }
            else
            {
                Vector3 camForward = cameraController.transform.forward;
                Vector3 camRight = cameraController.transform.right;
                camForward.y = 0; camRight.y = 0; camForward.Normalize(); camRight.Normalize();
                dodgeDir = camForward * verticalInput + camRight * horizontalInput;
            }
            dodgeDir.Normalize();
            if (dodgeDir.sqrMagnitude > 0.01f) playerTransform.rotation = Quaternion.LookRotation(dodgeDir);
        }
        else // Neutral Dodge / Backstep
        {
            targetAnimation = playerManager.isCrouching ? "Crouch_Backstep" : "Backstep";
            dodgeDir = -playerTransform.forward; // Backstep directly backwards
            // No need to change rotation for backstep typically
        }
        animatorHandler.PlayTargetAnimation(targetAnimation, true); // isInMidAction = true
        // Root motion will handle movement. OnDodgeAnimationEnd resets flags.
    }

    public void OnDodgeAnimationEnd()
    {
        playerManager.isInMidAction = false;
        animatorHandler.SetBool("isDodging", false);
        ResetInputAndMovementState();
    }

    public void HandleSprintHolding()
    {
        if (playerManager.isCrouching || playerManager.isInMidAction || !playerManager.isGrounded)
        {
            playerManager.isSprinting = false; // Can't sprint if crouching or in action
            return;
        }
        playerManager.isSprinting = moveAmount > 0.5f;
    }

    public void HandleSprintReleased()
    {
        playerManager.isSprinting = false;
    }

    private bool CheckGrounded()
    {
        Vector3 rayStart = playerTransform.position + (Vector3.up * groundDetectionRayStartPoint);
        float rayLength = groundDetectionRayStartPoint + groundDirectionRayDistance;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayLength, ~ignoreForGroundCheck, QueryTriggerInteraction.Ignore))
        {
            _currentNormalVector = hit.normal;
            return true;
        }
        _currentNormalVector = Vector3.up;
        return false;
    }

    private void HandleFallingAndLanding(float deltaTime)
    {
        if (!playerManager.isGrounded && !playerManager.isInAir) // Just became airborne
        {
            playerManager.isInAir = true;
            inAirTimer = 0;
            // Do not play falling animation if already in an action like jump or dodge
            if (!playerManager.isInMidAction)
            {
                animatorHandler.PlayTargetAnimation("Falling", false); // isInMidAction = false for falling loop
            }
        }

        if (playerManager.isInAir)
        {
            // Apply gravity. Using AddForce might be better than directly setting velocity if you want more physics control.
            // However, for platformer-like feel, direct velocity or a custom gravity accumulation is common.
            rigidbody.AddForce(Vector3.down * fallingSpeed, ForceMode.Acceleration); // Consistent downward force
            // Cap fall speed:
            if (rigidbody.velocity.y < -maxFallSpeed) rigidbody.velocity = new Vector3(rigidbody.velocity.x, -maxFallSpeed, rigidbody.velocity.z);

            // Minimal air control
            Vector3 airControlDirection = (cameraController.transform.forward * verticalInput + cameraController.transform.right * horizontalInput).normalized;
            rigidbody.AddForce(airControlDirection * airControlFactor * deltaTime, ForceMode.VelocityChange);
        }

        if (playerManager.isGrounded && playerManager.isInAir) // Just landed
        {
            playerManager.isInAir = false;
            // Play land animation only if falling for a certain duration and not already in another action
            if (inAirTimer > 0.2f && !playerManager.isInMidAction) // Adjust threshold
            {
                animatorHandler.PlayTargetAnimation("Land", true); // Brief action
            }
            inAirTimer = 0;
        }
    }

    public void HandleJumpButtonPressed()
    {
        if (playerManager.isInMidAction || !playerManager.isGrounded) return;

        if (playerManager.isCrouching) // Stand up to jump
        {
            playerManager.isCrouching = false;
            // Animator should transition out of crouch
        }

        playerManager.isInMidAction = true;
        // TODO: Implement actual jump force application
        // For now, just playing an animation. Physics of jump needs to be added.
        // e.g., rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        // The "Jump" animation should be a startup, then transition to "Falling" or "Jump_Apex" then "Falling"
        animatorHandler.PlayTargetAnimation("Jump_Start", true); // This animation should ideally apply upward force via event or here
        // A common pattern is to apply jump force slightly after the animation starts
        // Or the jump animation itself has root motion for the upward movement.
        // For simplicity now, assume "Jump_Start" transitions to "Falling", and gravity handles the arc.
        // A proper jump would involve more physics. Example for adding force:
        // float jumpForce = 7f;
        // rigidbody.AddForce(Vector3.up * jumpForce + playerTransform.forward * moveAmount * 2f, ForceMode.Impulse); // Add some forward momentum
    }

    public void ResetInputAndMovementState()
    {
        horizontalInput = 0f;
        verticalInput = 0f;
        moveAmount = 0f; // Recalculate based on fresh input
        rigidbody.velocity = new Vector3(0, rigidbody.velocity.y, 0); // Stop horizontal movement, preserve fall
                                                                      // You might also want to reset _rigidbodyVelocityRef if it's causing issues with SmoothDamp
        _rigidbodyVelocityRef = Vector3.zero;

        // Ensure animator values are also reflecting "no input"
        if (animatorHandler != null)
        {
            animatorHandler.UpdateAnimatorValues(0f, 0f, false, playerManager.isCrouching, cameraController.IsLockedOn);
        }
    }
}
