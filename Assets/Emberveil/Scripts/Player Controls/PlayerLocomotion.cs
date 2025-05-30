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

    [Header("Jumping Stats")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float airControlFactor = 0.2f;

    [Header("Falling Stats")]
    [SerializeField] private float customGravity = 25f; // Stronger custom gravity for a snappier feel
    [SerializeField] private float maxFallSpeed = 50f;

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

    private Vector3 currentNormalVector = Vector3.up;
    private Vector3 rigidbodyVelocityRef = Vector3.zero;
    [SerializeField] private float movementSmoothTime = 0.08f;

    private bool applyJumpForceNextFixedUpdate = false;
    private bool _isJumpInitiatedThisFrame = false;

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

        rigidbody.useGravity = false; // We use custom gravity
    }

    private void Update()
    {
        if (playerManager == null || cameraController == null) return;
        moveAmount = Mathf.Clamp01(Mathf.Abs(horizontalInput) + Mathf.Abs(verticalInput));

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

        if (playerManager.isInAir)
        {
            inAirTimer += Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;
        if (playerManager == null || cameraController == null) return;

        bool previousGroundedState = playerManager.isGrounded;
        playerManager.isGrounded = CheckGrounded();

        if (_isJumpInitiatedThisFrame)
        {
            playerManager.isInAir = true;
        }

        HandleFallingAndLanding(deltaTime, previousGroundedState);

        if (applyJumpForceNextFixedUpdate)
        {
            ApplyActualJumpForce();
            applyJumpForceNextFixedUpdate = false;
        }

        if (playerManager.isInMidAction && !playerManager.isInAir) // If in a grounded action (not jumping/falling)
        {
            // If the action uses root motion, OnAnimatorMove handles it.
            // If not, and we want to prevent sliding:
            if (animatorHandler.anim != null && !animatorHandler.anim.applyRootMotion)
            {
                ApplyZeroHorizontalMovement();
            }
            return; // Let root motion or animation events control
        }

        if (playerManager.isInAir)
        {
            HandleAirborneMovement(deltaTime);
            // Rotation in air is often restricted or different
            HandleAirborneRotation(deltaTime);
        }
        else // Grounded
        {
            HandleGroundedMovement(deltaTime);
            HandleRotation(deltaTime);
        }

        _isJumpInitiatedThisFrame = false;
    }

    private void OnEnable()
    {
        InputHandler.PlayerMovementPerformed += HandleMovementInput;
    }

    private void OnDisable()
    {
        InputHandler.PlayerMovementPerformed -= HandleMovementInput;
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
        finalVelocity = Vector3.ProjectOnPlane(finalVelocity, currentNormalVector);
        //finalVelocity.y = rigidbody.velocity.y; // Preserve existing Y velocity for jump/fall continuity
        finalVelocity.y = -1f;

        rigidbody.velocity = Vector3.SmoothDamp(rigidbody.velocity, finalVelocity, ref rigidbodyVelocityRef, movementSmoothTime);
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
        if (playerManager.isCrouching) playerManager.ToggleCrouchState(); // Stand up to dodge
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
        float rayLength = groundDetectionRayStartPoint + groundDirectionRayDistance; // Total length from player pivot + startPoint offset
        bool hitGround = Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayLength, ~ignoreForGroundCheck, QueryTriggerInteraction.Ignore);

        Debug.DrawRay(rayStart, Vector3.down * rayLength, hitGround ? Color.green : Color.red);

        if (hitGround)
        {
            currentNormalVector = hit.normal;
            return true;
        }
        else
        {
            currentNormalVector = Vector3.up;
            return false;
        }
    }

    private void HandleFallingAndLanding(float deltaTime, bool wasGroundedLastFrame)
    {
        // Becoming airborne (walked off ledge OR jump force has taken effect)
        if (!playerManager.isGrounded && !playerManager.isInAir)
        {
            playerManager.isInAir = true;
            inAirTimer = 0;
            if (!IsCurrentlyInJumpAnimation()) // Only play falling if not in a specific jump anim
            {
                animatorHandler.PlayTargetAnimation("Jump_Idle", false);
            }
            Debug.Log("Became airborne (not from immediate jump press this frame).");
        }

        if (playerManager.isInAir)
        {
            rigidbody.AddForce(Vector3.down * customGravity, ForceMode.Acceleration);
            if (rigidbody.velocity.y < -maxFallSpeed)
            {
                rigidbody.velocity = new Vector3(rigidbody.velocity.x, -maxFallSpeed, rigidbody.velocity.z);
            }
        }

        // The critical part is to distinguish a real landing from the initial grounded state when jump is pressed.
        // wasGroundedLastFrame is used to detect a transition from air to ground.
        if (playerManager.isGrounded && wasGroundedLastFrame == false && playerManager.isInAir) // JUST LANDED (was in air, now grounded)
        {
            playerManager.isInAir = false;
            inAirTimer = 0;
            //animatorHandler.PlayTargetAnimation("Jump_End", true);
            playerManager.isInMidAction = false;
            Debug.Log("Landed from air. Playing Jump_End animation.");
        }
    }

    private bool IsCurrentlyInJumpAnimation()
    {
        if (animatorHandler == null || animatorHandler.anim == null) return false;
        AnimatorStateInfo stateInfo = animatorHandler.anim.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName("Jump_Start") || stateInfo.IsName("Jump_Lift");
    }

    public void FinishJumpAction() // Called by animation event from Jump_End animation
    {
        playerManager.isInMidAction = false;
        Debug.Log("Jump/Land Action Finished.");
    }

    public void HandleJumpButtonPressed()
    {
        if (playerManager.isInMidAction || !playerManager.isGrounded || playerManager.isCrouching)
        {
            // If crouching, first stand up
            if (playerManager.isCrouching)
            {
                playerManager.ToggleCrouchState();
            }
            return;
        }

        playerManager.isInAir = true;
        _isJumpInitiatedThisFrame = true;
        inAirTimer = 0;

        // An animation event on Jump_Start or Jump_Lift will call animatorHandler.AnimEvent_ApplyJumpForce()
        animatorHandler.PlayTargetAnimation("Jump_Start", true);
        Debug.Log("Jump Initiated, playing Jump_Start. Force will be applied via AnimEvent.");
    }

    public void TriggerApplyJumpForce() // Called by AnimatorHandler via Animation Event
    {
        applyJumpForceNextFixedUpdate = true;
    }

    private void ApplyActualJumpForce()
    {
        // Reset vertical velocity before applying new jump force to ensure consistent jump height
        rigidbody.velocity = new Vector3(rigidbody.velocity.x, 0, rigidbody.velocity.z);
        rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        // Add a bit of forward momentum based on current movement input
        Vector3 cameraForward = cameraController.transform.forward;
        Vector3 cameraRight = cameraController.transform.right;
        cameraForward.y = 0; cameraRight.y = 0;
        cameraForward.Normalize(); cameraRight.Normalize();
        Vector3 moveInputDirection = (cameraForward * verticalInput + cameraRight * horizontalInput).normalized;
        if (moveAmount > 0.1f) // Only if there's movement input
        {
            rigidbody.AddForce(moveInputDirection * moveAmount * (jumpForce * 0.2f), ForceMode.Impulse); // Smaller force forward
        }

        // Animator should transition from Jump_Start/Jump_Lift to Jump_Idle.
        //animatorHandler.PlayTargetAnimation("Jump_Idle", false);
        Debug.Log("Jump Force Applied. Should be in Jump_Idle animation.");
    }

    private void HandleAirborneMovement(float deltaTime)
    {
        Vector3 cameraForward = cameraController.transform.forward;
        Vector3 cameraRight = cameraController.transform.right;
        cameraForward.y = 0; cameraRight.y = 0;
        cameraForward.Normalize(); cameraRight.Normalize();

        Vector3 desiredMoveDirection = (cameraForward * verticalInput + cameraRight * horizontalInput).normalized;
        float speed = playerManager.isSprinting ? sprintSpeed : movementSpeed; // Can still "sprint" in air for more control

        Vector3 airMoveForce = desiredMoveDirection * speed * airControlFactor;

        // Apply force, but don't let it overcome existing momentum too quickly or exceed max air speed
        // This is a simple way; more complex air physics might be desired for some games.
        Vector3 horizontalVelocity = new Vector3(rigidbody.velocity.x, 0, rigidbody.velocity.z);
        Vector3 targetHorizontalVelocity = new Vector3(airMoveForce.x, 0, airMoveForce.z);

        // Lerp or AddForce for air control
        // rigidbody.AddForce(airMoveForce * deltaTime, ForceMode.VelocityChange); // Might feel too slidy
        // More direct control with clamping:
        Vector3 newHorizontalVelocity = Vector3.Lerp(horizontalVelocity, targetHorizontalVelocity, deltaTime * 5f); // Adjust lerp speed
        rigidbody.velocity = new Vector3(newHorizontalVelocity.x, rigidbody.velocity.y, newHorizontalVelocity.z);
    }

    private void HandleAirborneRotation(float deltaTime)
    {
        // Less rotation control in air, or only allow minor adjustments
        if (moveAmount > 0.1f) // Only rotate if there's input
        {
            Vector3 camForward = cameraController.transform.forward;
            Vector3 camRight = cameraController.transform.right;
            camForward.y = 0; camRight.y = 0;
            camForward.Normalize(); camRight.Normalize();
            Vector3 targetDir = camForward * verticalInput + camRight * horizontalInput;
            targetDir.y = 0;
            if (targetDir.sqrMagnitude > 0.01f)
            {
                targetDir.Normalize();
                Quaternion tr = Quaternion.LookRotation(targetDir);
                playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, tr, rotationSpeed * deltaTime * 0.5f); // Slower air rotation
            }
        }
    }

    private void ApplyZeroHorizontalMovement()
    {
        Vector3 targetVelocity = new Vector3(0, rigidbody.velocity.y, 0);
        rigidbody.velocity = Vector3.SmoothDamp(rigidbody.velocity, targetVelocity, ref rigidbodyVelocityRef, movementSmoothTime * 0.2f); // Very quick stop
    }

    public void ResetInputAndMovementState()
    {
        horizontalInput = 0f;
        verticalInput = 0f;
        moveAmount = 0f;
        rigidbody.velocity = new Vector3(0, rigidbody.velocity.y, 0); // Stop horizontal movement, preserve fall
        rigidbodyVelocityRef = Vector3.zero;

        // Ensure animator values are also reflecting "no input"
        if (animatorHandler != null)
        {
            animatorHandler.UpdateAnimatorValues(0f, 0f, false, playerManager.isCrouching, cameraController.IsLockedOn);
        }
    }
}
