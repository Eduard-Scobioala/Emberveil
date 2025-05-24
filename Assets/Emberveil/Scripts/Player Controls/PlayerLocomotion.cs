using UnityEngine;

public class PlayerLocomotion : MonoBehaviour
{
    [Header("Camera Reference")]
    [SerializeField] private CameraController cameraController;

    private Transform playerTransform;
    private PlayerManager playerManager;
    public Vector3 moveDirection;

    [HideInInspector]
    public AnimatorHandler animatorHandler;

    public new Rigidbody rigidbody;

    [Header("Movement Stats")]
    [SerializeField] private float movementSpeed = 5;
    [SerializeField] private float walkingSpeed = 1;
    [SerializeField] private float sprintSpeed = 8;
    [SerializeField] private float rotationSpeed = 18;
    [SerializeField] private float fallingSpeed = 80;
    [SerializeField] private float lockOnMovementForwardMultiplier = 0.125f;
    [SerializeField] private float lockOnDodgeForwardMultiplier = 0.9f;

    [Header("Ground & Air Detection Stats")]
    [SerializeField] private float groundDetectionRayStartPoint = 0.5f;
    [SerializeField] private float minimumDistanceNeededToBeginFall = 1f;
    [SerializeField] private float groundDirectionRayDistance = 0.2f;
    [SerializeField] private LayerMask ignoreForGroundCheck;
    public float inAirTimer;


    private bool isDodgeButtonPressed = false;
    private bool isSprinting = false;
    private bool isDodging = false;

    private float dodgeInputTimer;
    private float horizontalInput;
    private float verticalInput;
    private float moveAmount;

    private Vector3 _rigidbodyVelocityRef = Vector3.zero;
    [SerializeField] private float movementSmoothTime = 0.05f;

    private void Awake()
    {
        playerTransform = transform;
        rigidbody = GetComponent<Rigidbody>();
        playerManager = GetComponent<PlayerManager>();
        animatorHandler = GetComponentInChildren<AnimatorHandler>();

        if (cameraController == null)
        {
            Debug.LogError("CameraController not assigned on PlayerLocomotion!", this);
        }

        if (animatorHandler != null) animatorHandler.Initialize();
        else Debug.LogError("AnimatorHandler not found on children!", this);

        if (playerManager != null) playerManager.isGrounded = true;
        else Debug.LogError("PlayerManager not found on this GameObject!", this);
    }

    private void OnEnable()
    {
        InputHandler.PlayerMovementPerformed += HandleMovementInput;
    }

    private void OnDisable()
    {
        InputHandler.PlayerMovementPerformed -= HandleMovementInput;
    }

    private void FixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;
        if (playerManager == null || cameraController == null) return;


        playerManager.isGrounded = CheckGrounded(); // More robust ground check
        HandleFallingAndLanding(deltaTime); // Consolidate falling logic

        if (playerManager.isInMidAction || isDodging) // If dodging, movement is handled by animation/root motion potentially
        {
            if (!isDodging) // If just in mid-action (not dodge), ensure velocity is managed if needed
                ApplyZeroMovement(); // Or some other logic to stop unwanted sliding
            return;
        }

        HandleMovement(deltaTime);
        HandleRotation(deltaTime); // Rotation should generally happen after movement calculation for facing
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        if (playerManager == null || cameraController == null) return;

        moveAmount = Mathf.Clamp01(Mathf.Abs(horizontalInput) + Mathf.Abs(verticalInput));
        playerManager.isSprinting = isSprinting && moveAmount > 0.5f; // Update sprint status
    }

    private void HandleMovementInput(Vector2 movementInput)
    {
        horizontalInput = movementInput.x;
        verticalInput = movementInput.y;
    }

    public void HandleDodgeTapped()
    {
        if (playerManager.isGrounded && !playerManager.isInMidAction && !isDodging)
        {
            PerformDodge();
        }
    }

    public void HandleSprintHolding()
    {
        if (moveAmount > 0.1f && playerManager.isGrounded && !playerManager.isInMidAction && !isDodging)
        {
            isSprinting = true;
            if (playerManager != null) playerManager.isSprinting = true;
        }
    }

    public void HandleSprintReleased()
    {
        isSprinting = false;
        if (playerManager != null) playerManager.isSprinting = false;
    }

    #region Movement

    private Vector3 _currentNormalVector = Vector3.up; // Store the ground normal

    private void HandleRotation(float deltaTime)
    {
        if (playerManager.isInMidAction && !animatorHandler.canRotate) // Allow rotation during some actions if canRotate is true
            return;

        Vector3 targetDir;
        Quaternion targetRotation;

        if (cameraController.IsLockedOn && !(isSprinting || isDodging))
        {
            if (cameraController.CurrentLockOnTarget != null)
            {
                targetDir = cameraController.CurrentLockOnTarget.transform.position - playerTransform.position;
            }
            else // Target lost or null
            {
                targetDir = playerTransform.forward; // Maintain current facing if target is gone
            }
        }
        else
        {
            // Use camera's Y rotation for free look direction
            Vector3 cameraForwardFlattened = cameraController.transform.forward; // Rig's forward
            cameraForwardFlattened.y = 0;
            cameraForwardFlattened.Normalize();

            Vector3 cameraRightFlattened = cameraController.transform.right; // Rig's right
            cameraRightFlattened.y = 0;
            cameraRightFlattened.Normalize();

            targetDir = cameraForwardFlattened * verticalInput + cameraRightFlattened * horizontalInput;

            if (targetDir.sqrMagnitude < 0.01f) // Check sqrMagnitude to avoid sqrt
            {
                targetDir = playerTransform.forward; // Maintain current facing if no input
            }
        }

        targetDir.y = 0; // Ensure rotation is only on Y axis
        targetDir.Normalize();

        if (targetDir.sqrMagnitude > 0.01f) // Only rotate if there's a valid direction
        {
            targetRotation = Quaternion.LookRotation(targetDir);
            playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, targetRotation, rotationSpeed * deltaTime);
        }
    }

    public void HandleMovement(float deltaTime)
    {
        // This method is now called from FixedUpdate

        Vector3 rawMoveDirection;

        if (cameraController.IsLockedOn && !isSprinting) // During lock-on (and not sprinting)
        {
            // Movement is relative to the player's current orientation (which should be facing the target)
            rawMoveDirection = CalculateMoveDirection(playerTransform, verticalInput, horizontalInput, lockOnMovementForwardMultiplier);
        }
        else // Normal movement or sprinting (even if locked on, sprinting breaks from strict strafe)
        {
            Vector3 cameraForwardFlattened = cameraController.transform.forward;
            cameraForwardFlattened.y = 0;
            cameraForwardFlattened.Normalize();

            Vector3 cameraRightFlattened = cameraController.transform.right;
            cameraRightFlattened.y = 0;
            cameraRightFlattened.Normalize();

            rawMoveDirection = cameraForwardFlattened * verticalInput + cameraRightFlattened * horizontalInput;
        }

        rawMoveDirection.Normalize(); // Normalize to ensure consistent speed diagonally

        float currentSpeed = movementSpeed;
        if (playerManager.isSprinting) // Use the already updated playerManager.isSprinting
        {
            currentSpeed = sprintSpeed;
        }
        else
        {
            if (moveAmount <= 0.5f && moveAmount > 0) // Walking threshold
            {
                currentSpeed = walkingSpeed;
            }
        }

        if (moveAmount == 0) currentSpeed = 0; // No input, no speed

        moveDirection = rawMoveDirection * currentSpeed; // Final desired velocity vector

        // Project moveDirection onto the ground plane
        Vector3 projectedMoveDirection = Vector3.ProjectOnPlane(moveDirection, _currentNormalVector);

        // Smoothly change the rigidbody's velocity
        // rigidbody.velocity = projectedMoveDirection; // Old direct set
        rigidbody.velocity = Vector3.SmoothDamp(rigidbody.velocity, projectedMoveDirection, ref _rigidbodyVelocityRef, movementSmoothTime);


        // Animation
        if (animatorHandler != null)
        {
            if (cameraController.IsLockedOn && !playerManager.isSprinting)
            {
                // For lock-on, pass vertical and horizontal directly for strafing animations
                animatorHandler.UpdateAnimatorValues(verticalInput, horizontalInput, playerManager.isSprinting);
            }
            else
            {
                // For free movement, pass the combined moveAmount
                animatorHandler.UpdateAnimatorValues(moveAmount, 0, playerManager.isSprinting);
            }
        }
    }

    private void ApplyZeroMovement()
    {
        rigidbody.velocity = Vector3.SmoothDamp(rigidbody.velocity, Vector3.zero, ref _rigidbodyVelocityRef, movementSmoothTime);
    }

    private void PerformDodge()
    {
        if (animatorHandler == null || playerManager.isInMidAction) return;

        playerManager.isInMidAction = true; // Player is busy during dodge

        Vector3 dodgeDir;
        string targetAnimation;

        if (moveAmount > 0.01f) // Directional Dodge / Roll
        {
            isDodging = true; // Set dodging flag, clear it via animation event or timer
            targetAnimation = "Roll"; // Or your specific directional dodge animation name

            // Dodge direction:
            if (cameraController.IsLockedOn)
            {
                // Roll relative to player's orientation (which is facing target)
                dodgeDir = CalculateMoveDirection(playerTransform, verticalInput, horizontalInput, lockOnDodgeForwardMultiplier);
            }
            else
            {
                // Roll relative to camera
                Vector3 cameraForwardFlattened = cameraController.transform.forward;
                cameraForwardFlattened.y = 0;
                Vector3 cameraRightFlattened = cameraController.transform.right;
                cameraRightFlattened.y = 0;
                dodgeDir = (cameraForwardFlattened.normalized * verticalInput) + (cameraRightFlattened.normalized * horizontalInput);
            }
            dodgeDir.Normalize();
            if (dodgeDir.sqrMagnitude > 0.01f)
            {
                playerTransform.rotation = Quaternion.LookRotation(dodgeDir);
            }
        }
        else // Neutral Dodge / Backstep
        {
            targetAnimation = "Backstep";
        }

        animatorHandler.PlayTargetAnimation(targetAnimation, true);

        // For root motion dodges, the animation drives movement.
        // For non-root motion, you might apply an impulse or set velocity here:
        // Example: rigidbody.AddForce(dodgeDir * dodgeForce, ForceMode.Impulse);

        // IMPORTANT: You need a way to reset isDodging and playerManager.isInMidAction
        // Done via an Animation Event at the end of the dodge animation
        // Or might as well be done with simple coroutine timer.
        // StartCoroutine(ResetDodgeState(dodgeAnimationDuration));
    }

    // Called from an animation event
    public void OnDodgeAnimationEnd()
    {
        isDodging = false;
        playerManager.isInMidAction = false;
    }

    private bool CheckGrounded()
    {
        Vector3 rayStart = playerTransform.position + Vector3.up * groundDetectionRayStartPoint;
        float rayDistance = groundDetectionRayStartPoint + groundDirectionRayDistance - 0.01f; // Slight offset to ensure it hits
        RaycastHit hit;

        Debug.DrawRay(rayStart, Vector3.down * rayDistance, Color.blue);
        if (Physics.Raycast(rayStart, Vector3.down, out hit, rayDistance, ~ignoreForGroundCheck))
        {
            _currentNormalVector = hit.normal;
            return true;
        }
        else
        {
            _currentNormalVector = Vector3.up;
            return false;
        }
    }

    private void HandleFallingAndLanding(float deltaTime)
    {
        if (!playerManager.isGrounded && !playerManager.isInAir) // Just became airborne
        {
            playerManager.isInAir = true;
            inAirTimer = 0;
            if (!playerManager.isInMidAction) // Avoid interrupting actions like jump startup
            {
                animatorHandler.PlayTargetAnimation("Falling", true);
            }
        }

        if (playerManager.isInAir)
        {
            inAirTimer += deltaTime;
            rigidbody.AddForce(Vector3.down * fallingSpeed * deltaTime, ForceMode.VelocityChange); // Apply gravity smoothly

            // Air control (optional, very minimal)
            // Vector3 airControlVelocity = (moveDirection.normalized * movementSpeed * 0.1f);
            // rigidbody.AddForce(airControlVelocity * deltaTime, ForceMode.VelocityChange);
        }

        if (playerManager.isGrounded && playerManager.isInAir) // Just landed
        {
            playerManager.isInAir = false;
            if (inAirTimer > 0.3f) // Threshold for hard land animation
            {
                animatorHandler.PlayTargetAnimation("Land", true);
            }
            else
            {
                // animatorHandler.PlayTargetAnimation("Empty", false); // Or just let current anim finish
            }
            inAirTimer = 0;
        }
    }

    public void HandleJumpButtonPressed()
    { // Call from InputHandler
        if (playerManager.isInMidAction || !playerManager.isGrounded)
            return;

        playerManager.isInMidAction = true; // Player is busy
        // Determine jump direction
        Vector3 jumpDir;
        if (moveAmount > 0.01f)
        {
            // Jump in movement direction
            Vector3 cameraForwardFlattened = cameraController.transform.forward;
            cameraForwardFlattened.y = 0;
            Vector3 cameraRightFlattened = cameraController.transform.right;
            cameraRightFlattened.y = 0;
            jumpDir = (cameraForwardFlattened.normalized * verticalInput) + (cameraRightFlattened.normalized * horizontalInput);
            jumpDir.Normalize();
            if (jumpDir.sqrMagnitude > 0.01f)
            {
                playerTransform.rotation = Quaternion.LookRotation(jumpDir);
            }
        }
        // else neutral jump (straight up or slightly forward)

        animatorHandler.PlayTargetAnimation("Jump", true); // This animation should handle liftoff
        // Actual jump force applied by animation event or small delay.
        // Example: StartCoroutine(ApplyJumpForce(0.1f, jumpStrength));
        // Reset playerManager.isInMidAction after jump animation completes or can blend out.
    }

    private Vector3 CalculateMoveDirection(Transform playerTransform, float verticalInput, float horizontalInput, float forwardMultiplier)
    {
        return playerTransform.forward * verticalInput
             + playerTransform.right * horizontalInput
             + playerTransform.forward * (forwardMultiplier * Mathf.Abs(horizontalInput));
    }

    #endregion
}