using UnityEngine;

public class PlayerLocomotion : MonoBehaviour
{
    private Transform cameraObject;
    private Transform Orientation => cameraHandler.IsLockedOn ? transform : cameraObject;
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

    [Header("Ground & Air Detection Stats")]
    [SerializeField] private float groundDetectionRayStartPoint = 0.5f;
    [SerializeField] private float minimumDistanceNeededToBeginFall = 1f;
    [SerializeField] private float groundDirectionRayDistance = 0.2f;
    [SerializeField] private LayerMask ignoreForGroundCheck;
    public float inAirTimer;

    [Header("Camera Reference")]
    [SerializeField] private CameraController cameraHandler;

    private bool isDodgeButtonPressed = false;
    private bool isSprinting = false;
    private bool isDoging = false;

    private float dodgeInputTimer;
    private float horizontal;
    private float vertical;
    private float moveAmount;

    private void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        playerManager = GetComponent<PlayerManager>();
        animatorHandler = GetComponentInChildren<AnimatorHandler>();

        cameraObject = Camera.main.transform;

        animatorHandler.Initialize();

        playerManager.isGrounded = true;
    }

    private void OnEnable()
    {
        InputHandler.PlayerMovementPerformed += HandleMovementInput;
    }

    private void OnDisable()
    {
        InputHandler.PlayerMovementPerformed -= HandleMovementInput;
    }

    private void Update()
    {
        var deltaTime = Time.deltaTime;

        HandleRollingAndSprinting(deltaTime);
    }

    private void HandleMovementInput(Vector2 movementInput)
    {
        horizontal = movementInput.x;
        vertical = movementInput.y;
        moveAmount = Mathf.Clamp01(Mathf.Abs(horizontal) + Mathf.Abs(vertical));
    }

    public void HandleDodgeButtonPressed()
    {
        isDodgeButtonPressed = true;
        isSprinting = true;
    }

    public void HandleDodgeButtonReleased()
    {
        isDodgeButtonPressed = false;
        isSprinting = false;
    }

    #region Movement

    private Vector3 normalVector;
    private Vector3 targetPosition;

    private void HandleRotation(float deltaTime)
    {
        Vector3 targetDir;
        // If locked on and not sprinting or rolling - face the lock on target,
        // else - rotate based on input
        if (cameraHandler.IsLockedOn && !(isSprinting || isDoging))
        {
            targetDir = cameraHandler.CurrentLockOnTarget.transform.position - transform.position;
        }
        else
        {
            targetDir = cameraObject.forward * vertical + cameraObject.right * horizontal;

            if (targetDir == Vector3.zero)
            {
                targetDir = transform.forward;
            }
        }

        targetDir.y = 0;
        targetDir.Normalize();

        var tr = Quaternion.LookRotation(targetDir);
        var targetRotation = Quaternion.Slerp(transform.rotation, tr, rotationSpeed * deltaTime);

        transform.rotation = targetRotation;
    }

    public void HandleMovement(float deltaTime)
    {
        if (isDoging)
            return;

        if (playerManager.isInMidAction)
            return;

        moveDirection = Orientation.forward * vertical;

        moveDirection += Orientation.right * horizontal;
        moveDirection.Normalize();
        moveDirection.y = 0;

        float speed = movementSpeed;
        if (isSprinting && moveAmount > 0.5f)
        {
            speed = sprintSpeed;
            playerManager.isSprinting = true;
            moveDirection *= speed;
        }
        else
        {
            if (moveAmount <= 0.5f)
            {
                moveDirection *= walkingSpeed;
            }
            else
            {
                moveDirection *= speed;
            }

            playerManager.isSprinting = false;
        }

        Vector3 projectedVelocity = Vector3.ProjectOnPlane(moveDirection, normalVector);
        rigidbody.velocity = projectedVelocity;

        if (cameraHandler.IsLockedOn && !isSprinting)
        {
            animatorHandler.UpdateAnimatorValues(vertical, horizontal, playerManager.isSprinting);
        }
        else
        {
            animatorHandler.UpdateAnimatorValues(moveAmount, 0, playerManager.isSprinting);
        }

        if (animatorHandler.canRotate)
        {
            HandleRotation(deltaTime);
        }
    }

    private void HandleRollingAndSprinting(float deltaTime)
    {
        if (animatorHandler.IsInMidAction())
            return;

        HandleDodgeFlags(deltaTime);

        if (isDoging)
        {
            isDoging = false;

            moveDirection = Orientation.forward * vertical;
            moveDirection += Orientation.right * horizontal;

            if (moveAmount > 0)
            {
                animatorHandler.PlayTargetAnimation("Roll", true);
                moveDirection.y = 0;
                Quaternion rollRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = rollRotation;
            }
            else
            {
                animatorHandler.PlayTargetAnimation("Backstep", true);
            }
        }
    }

    private void HandleDodgeFlags(float deltaTime)
    {
        if (isDodgeButtonPressed)
        {
            dodgeInputTimer += deltaTime;
        }
        else
        {
            if (dodgeInputTimer > 0 && dodgeInputTimer < 0.5f)
            {
                isSprinting = false;
                isDoging = true;
            }

            dodgeInputTimer = 0;
        }
    }

    public void HandleFalling(float deltaTime, Vector3 moveDirection)
    {
        playerManager.isGrounded = false;
        RaycastHit hit;
        Vector3 origin = transform.position;
        origin.y += groundDetectionRayStartPoint;

        if (Physics.Raycast(origin, transform.forward, out hit, 0.4f))
        {
            moveDirection = Vector3.zero;
        }

        if (playerManager.isInAir)
        {
            rigidbody.AddForce(-Vector3.up * fallingSpeed);
            rigidbody.AddForce(moveDirection * fallingSpeed / 10f); // Adds an effect of hoping from the edge
        }

        Vector3 dir = moveDirection;
        dir.Normalize();
        origin += dir * groundDirectionRayDistance;

        targetPosition = transform.position;

        Debug.DrawRay(origin, -Vector3.up * minimumDistanceNeededToBeginFall, Color.red, 0.1f, false);
        if (Physics.Raycast(origin, -Vector3.up, out hit, minimumDistanceNeededToBeginFall, ignoreForGroundCheck))
        {
            normalVector = hit.normal;
            Vector3 tp = hit.point;
            playerManager.isGrounded = true;
            targetPosition.y = tp.y;

            if (playerManager.isInAir)
            {
                if(inAirTimer > 0.5f)
                {
                    Debug.Log("You were in air for " + inAirTimer);
                    animatorHandler.PlayTargetAnimation("Land", true);
                    inAirTimer = 0;
                }
                else
                {
                    animatorHandler.PlayTargetAnimation("Empty", false);
                    inAirTimer = 0;
                }

                playerManager.isInAir = false;
            }
        }
        else
        {
            if (playerManager.isGrounded)
            {
                playerManager.isGrounded = false;
            }

            if (playerManager.isInAir == false)
            {
                if (playerManager.isInMidAction == false)
                {
                    animatorHandler.PlayTargetAnimation("Falling", true);
                }

                Vector3 vel = rigidbody.velocity;
                vel.Normalize();
                rigidbody.velocity = vel * (movementSpeed / 2);
                playerManager.isInAir = true;
            }
        }

        // Make sure the player keeps its position while in animations
        if (playerManager.isGrounded)
        {
            if (playerManager.isInMidAction || moveAmount > 0)
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime / 0.1f);
            }
            else
            {
                transform.position = targetPosition;
            }
        }
    }
    
    public void HandleJumpButtonPressed() {
        if (playerManager.isInMidAction)
            return;

        if (moveAmount > 0)
        {
            moveDirection = Orientation.forward * vertical
                + Orientation.right * horizontal;
            moveDirection.y = 0;
            
            animatorHandler.PlayTargetAnimation("Jump", true);

            Quaternion jumpRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = jumpRotation;
        }
    }

    #endregion
}
