using UnityEngine;

public class PlayerLocomotion : MonoBehaviour
{
    [Header("Camera Reference")]
    [SerializeField] private CameraController cameraController;

    private Transform playerTransform;
    private PlayerManager playerManager;
    private PlayerAnimator playerAnimator;
    private PlayerStats playerStats;

    public new Rigidbody rigidbody;

    [Header("Movement Stats")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float walkingSpeed = 2f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private float actionRotationSpeed = 7f;

    [Header("Jumping Stats")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float airControlFactor = 0.2f;
    [SerializeField] private float maxAirSpeed = 4f; // Maximum horizontal speed while airborne

    [Header("Falling Stats")]
    [SerializeField] private float customGravity = 25f;
    [SerializeField] private float maxFallSpeed = 50f;

    [Header("Stamina Costs")]
    [SerializeField] private float dodgeStaminaCost = 10f;
    [SerializeField] private float jumpStaminaCost = 8f;
    [SerializeField] private float sprintTickStaminaCost = 8f;

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
        playerStats = GetComponent<PlayerStats>();
        playerAnimator = GetComponentInChildren<PlayerAnimator>();

        if (cameraController == null) Debug.LogError("CameraController not assigned on PlayerLocomotion!", this);
        
        if (playerAnimator == null) Debug.LogError("AnimatorHandler not found on children!", this);
        else playerAnimator.Initialize();

        if (playerManager == null) Debug.LogError("PlayerManager not found on this GameObject!", this);
        else playerAnimator.IsGrounded = true;

        if (playerStats == null) Debug.LogError("PlayerStats not found on this GameObject!", this);

        rigidbody.useGravity = false; // We use custom gravity
    }

    private void Update()
    {
        if (playerManager == null || cameraController == null) return;
        moveAmount = Mathf.Clamp01(Mathf.Abs(horizontalInput) + Mathf.Abs(verticalInput));

        if (playerAnimator != null)
        {
            playerAnimator.UpdateAnimatorValues(
                verticalInput,
                horizontalInput,
                playerManager.isSprinting,
                playerAnimator.IsCrouching,
                cameraController.IsLockedOn
            );
        }

        if (playerAnimator.IsInAir)
        {
            inAirTimer += Time.deltaTime;
        }

        if (playerManager.isSprinting && playerAnimator.IsGrounded)
        {
            if (playerStats.currentStamina > 0)
            {
                float staminaToConsumeThisFrame = sprintTickStaminaCost * Time.deltaTime;
                // ConsumeStamina will handle setting it to 0 if it overdrafts
                playerStats.ConsumeStamina(staminaToConsumeThisFrame);

                // If after consumption, stamina is 0, stop sprinting.
                if (playerStats.currentStamina <= 0)
                {
                    playerManager.isSprinting = false;
                }
            }
            else
            {
                playerManager.isSprinting = false;
            }
        }
    }

    private void FixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;
        if (playerManager == null || cameraController == null) return;

        bool previousGroundedState = playerAnimator.IsGrounded;
        playerAnimator.IsGrounded = CheckGrounded();

        if (_isJumpInitiatedThisFrame)
        {
            playerAnimator.IsInAir = true;
        }

        HandleFallingAndLanding(deltaTime, previousGroundedState);

        if (applyJumpForceNextFixedUpdate)
        {
            ApplyActualJumpForce();
            applyJumpForceNextFixedUpdate = false;
        }

        if (playerAnimator.CanRotate)
        {
            HandleRotation(deltaTime);
        }

        if (playerAnimator.IsInMidAction && !playerAnimator.IsInAir) // If in a grounded action (not jumping/falling)
        {
            // If the action uses root motion, OnAnimatorMove handles it.
            // If not, and we want to prevent sliding:
            if (playerAnimator.anim != null && !playerAnimator.anim.applyRootMotion)
            {
                ApplyZeroHorizontalMovement();
            }
            return; // Let root motion or animation events control
        }

        if (playerAnimator.IsInAir)
        {
            HandleAirborneMovement(deltaTime);
            HandleAirborneRotation(deltaTime);
        }
        else // Grounded
        {
            HandleGroundedMovement(deltaTime);
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
        if (playerAnimator.IsCrouching)
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
        if (cameraController.IsLockedOn && !playerAnimator.IsCrouching && !playerManager.isSprinting)
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
        finalVelocity.y = rigidbody.velocity.y; // Preserve existing Y velocity for jump/fall continuity
        //finalVelocity.y = -1f;

        //rigidbody.velocity = Vector3.SmoothDamp(rigidbody.velocity, finalVelocity, ref rigidbodyVelocityRef, movementSmoothTime);
        rigidbody.velocity = finalVelocity;
    }

    private void HandleRotation(float deltaTime)
    {
        float currentRotationSpeedToUse = rotationSpeed;

        if (playerAnimator.IsInMidAction)
        {
            // If we're in an action, use the slower attack rotation speed.
            currentRotationSpeedToUse = actionRotationSpeed;
        }

        if (playerAnimator.IsCrouching && cameraController.IsLockedOn)
        {
            // Minimal rotation when crouched and locked on, primarily face target
            if (cameraController.CurrentLockOnTarget != null)
            {
                Vector3 directionToTarget = cameraController.CurrentLockOnTarget.transform.position - playerTransform.position;
                directionToTarget.y = 0;
                if (directionToTarget.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget.normalized);
                    //playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, targetRotation, rotationSpeed * deltaTime * 0.5f); // Slower rotation
                    playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, targetRotation, currentRotationSpeedToUse * deltaTime * 0.5f);
                }
            }
            return;
        }

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

        bool canRotateBasedOnInput = moveAmount > 0.01f || playerManager.charAnimManager.IsInMidAction;

        if (targetDir.sqrMagnitude < 0.01f && !cameraController.IsLockedOn)
        {
            return; // Don't rotate if no input, not locked on, and not in an action
        }
        if (targetDir.sqrMagnitude < 0.01f && cameraController.IsLockedOn) // If locked on but no move input, still face target
        {
            if (cameraController.CurrentLockOnTarget != null)
                targetDir = cameraController.CurrentLockOnTarget.transform.position - playerTransform.position;
            else targetDir = playerTransform.forward;
            targetDir.y = 0;
        }

        // Ensure targetDir is not zero before normalizing, important if we allow rotation during attacks with no movement input
        if (targetDir.sqrMagnitude > 0.001f)
        {
            targetDir.Normalize();
            var tr = Quaternion.LookRotation(targetDir);
            playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, tr, currentRotationSpeedToUse * deltaTime);
        }
    }

    public void HandleDodgeTapped()
    {
        if (playerAnimator.IsInMidAction || !playerAnimator.IsGrounded) return;
        if (playerAnimator.IsCrouching) playerManager.ToggleCrouchState(); // Stand up to dodge
        PerformDodge();
    }

    private void PerformDodge()
    {
        playerStats.ConsumeStamina(dodgeStaminaCost);

        playerManager.charAnimManager.IsInMidAction = true; // Player is busy
        playerAnimator.IsDodging = true;

        Vector3 dodgeDir;

        if (moveAmount > 0.01f) // Directional Dodge / Roll
        {
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

            var dodgeAngle = Vector3.SignedAngle(playerTransform.forward, dodgeDir, Vector3.up);
            playerAnimator.RollDirection = AngleToDirection8(dodgeAngle);

            if (dodgeDir.sqrMagnitude > 0.01f && !cameraController.IsLockedOn)
            {
                playerTransform.rotation = Quaternion.LookRotation(dodgeDir);
            }
        }
        else // Neutral Dodge / Backstep
        {
            playerAnimator.RollDirection = -1; // Backstep animation
            // No need to change rotation for backstep typically
        }
        playerAnimator.ApplyRootMotion(true);
        // Root motion will handle movement. OnDodgeAnimationEnd resets flags.
    }

    private int AngleToDirection8(float angle)
    {
        // Normalize the angle to 0-360 range first
        float normalizedAngle = (angle + 360) % 360;

        // Divide the circle into 8 sectors (45 degrees each)
        // Add 22.5 to shift the sectors so 0 is centered at North/Up
        int direction = (int)((normalizedAngle + 22.5f) / 45f) % 8;

        return direction;
    }

    public void OnDodgeAnimationEnd()
    {
        playerAnimator.IsInMidAction = false;
        playerAnimator.IsDodging = false;
    }

    public void HandleSprintHolding()
    {
        if (playerAnimator.IsCrouching || playerAnimator.IsInMidAction || !playerAnimator.IsGrounded)
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
        if (!playerAnimator.IsGrounded && !playerAnimator.IsInAir)
        {
            playerAnimator.IsInAir = true;
            inAirTimer = 0;
            if (!IsCurrentlyInJumpAnimation()) // Only play falling if not in a specific jump anim
            {
                playerAnimator.PlayTargetAnimation("Jump_Idle", true);
            }
            Debug.Log("Became airborne (not from immediate jump press this frame).");
        }

        if (playerAnimator.IsInAir)
        {
            rigidbody.AddForce(Vector3.down * customGravity, ForceMode.Acceleration);
            if (rigidbody.velocity.y < -maxFallSpeed)
            {
                rigidbody.velocity = new Vector3(rigidbody.velocity.x, -maxFallSpeed, rigidbody.velocity.z);
            }
        }

        // The critical part is to distinguish a real landing from the initial grounded state when jump is pressed.
        // wasGroundedLastFrame is used to detect a transition from air to ground.
        if (playerAnimator.IsGrounded && wasGroundedLastFrame == false && playerAnimator.IsInAir) // JUST LANDED (was in air, now grounded)
        {
            playerAnimator.IsInAir = false;
            inAirTimer = 0;
            //animatorHandler.PlayTargetAnimation("Jump_End", true);
            //playerManager.charAnimManager.IsInMidAction = false;
            Debug.Log("Landed from air. Playing Jump_End animation.");
        }
    }

    private bool IsCurrentlyInJumpAnimation()
    {
        if (playerAnimator == null || playerAnimator.anim == null) return false;
        AnimatorStateInfo stateInfo = playerAnimator.anim.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName("Jump_Start") || stateInfo.IsName("Jump_Lift");
    }

    public void FinishJumpAction() // Called by animation event from Jump_End animation
    {
        playerAnimator.IsInMidAction = false;
        Debug.Log("Jump/Land Action Finished.");
    }

    public void HandleJumpButtonPressed()
    {
        if (playerAnimator.IsInMidAction || !playerAnimator.IsGrounded)
        {
            return;
        }

        playerStats.ConsumeStamina(jumpStaminaCost);

        if (playerAnimator.IsCrouching)
        {
            playerManager.ToggleCrouchState();
            if (playerAnimator.IsCrouching) return;
        }

        playerAnimator.IsInAir = true;
        _isJumpInitiatedThisFrame = true;
        inAirTimer = 0;

        // An animation event on Jump_Start or Jump_Lift will call animatorHandler.AnimEvent_ApplyJumpForce()
        playerAnimator.PlayTargetAnimation("Jump_Start", true, rootMotion: false);
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
        if (moveAmount <= 0.01f) return; // No input, no air control

        Vector3 cameraForward = cameraController.transform.forward;
        Vector3 cameraRight = cameraController.transform.right;
        cameraForward.y = 0; cameraRight.y = 0;
        cameraForward.Normalize(); cameraRight.Normalize();

        Vector3 desiredMoveDirection = (cameraForward * verticalInput + cameraRight * horizontalInput).normalized;

        // Get current horizontal velocity
        Vector3 currentHorizontalVelocity = new Vector3(rigidbody.velocity.x, 0, rigidbody.velocity.z);

        // Calculate desired velocity based on input
        Vector3 targetVelocity = desiredMoveDirection * (movementSpeed * airControlFactor);

        // Calculate the difference between current and desired velocity
        Vector3 velocityDifference = targetVelocity - currentHorizontalVelocity;

        // Limit the acceleration to prevent sudden velocity changes (teleportation)
        float maxAcceleration = airControlFactor * movementSpeed * 2f; // Adjust multiplier as needed
        if (velocityDifference.magnitude > maxAcceleration * deltaTime)
        {
            velocityDifference = velocityDifference.normalized * maxAcceleration * deltaTime;
        }

        // Apply the limited velocity change
        Vector3 newVelocity = currentHorizontalVelocity + velocityDifference;

        // Clamp to maximum air speed
        if (newVelocity.magnitude > maxAirSpeed)
        {
            newVelocity = newVelocity.normalized * maxAirSpeed;
        }

        // Set the new velocity
        rigidbody.velocity = new Vector3(newVelocity.x, rigidbody.velocity.y, newVelocity.z);
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

    public void StopAllMovement()
    {
        rigidbody.velocity = Vector3.zero;
        if (playerAnimator != null)
        {
            playerAnimator.UpdateAnimatorValues(0, 0, false, false, false);
        }
    }
}
