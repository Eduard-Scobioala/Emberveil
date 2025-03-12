using UnityEngine;

public class PlayerLocomotion : MonoBehaviour
{
    private Transform cameraObject;
    private InputHandler inputHandler;
    private Vector3 moveDirection;

    [HideInInspector]
    public AnimatorHandler animatorHandler;

    public new Rigidbody rigidbody;
    public GameObject normalCamera;

    [Header("Stats")]
    [SerializeField]
    private float movementSpeed = 5;
    [SerializeField]
    private float rotationSpeed = 18;

    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        inputHandler = GetComponent<InputHandler>();
        animatorHandler = GetComponentInChildren<AnimatorHandler>();

        cameraObject = Camera.main.transform;

        animatorHandler.Initialize();
    }

    private void Update()
    {
        var deltaTime = Time.deltaTime;

        inputHandler.TickInput(deltaTime);

        HandleMovement(deltaTime);
        HandleRollingAndSprinting(deltaTime);
    }

    #region Movement

    private Vector3 normalVector;
    private Vector3 targetPosition;

    private void HandleRotation(float deltaTime)
    {
        float moveOverride = inputHandler.moveAmount;

        var targetDir =
            cameraObject.forward * inputHandler.vertical
            + cameraObject.right * inputHandler.horizontal;

        targetDir.Normalize();
        targetDir.y = 0;

        if (targetDir == Vector3.zero)
        {
            targetDir = transform.forward;
        }

        var tr = Quaternion.LookRotation(targetDir);
        var targetRotation = Quaternion.Slerp(transform.rotation, tr, rotationSpeed * deltaTime);

        transform.rotation = targetRotation;
    }

    private void HandleMovement(float deltaTime)
    {
        moveDirection = cameraObject.forward * inputHandler.vertical;

        moveDirection += cameraObject.right * inputHandler.horizontal;
        moveDirection.Normalize();
        moveDirection.y = 0;

        float speed = movementSpeed;

        moveDirection *= speed;

        Vector3 projectedVelocity = Vector3.ProjectOnPlane(moveDirection, normalVector);
        rigidbody.velocity = projectedVelocity;

        animatorHandler.UpdateAnimatorValues(inputHandler.moveAmount, 0);

        if (animatorHandler.canRotate)
        {
            HandleRotation(deltaTime);
        }
    }

    public void HandleRollingAndSprinting(float deltaTime)
    {
        var isInt = animatorHandler.anim.GetBool("isInteracting");
        if (animatorHandler.anim.GetBool("isInteracting"))
            return;

        if (inputHandler.rollFlag)
        {
            moveDirection = cameraObject.forward * inputHandler.vertical;
            moveDirection += cameraObject.right * inputHandler.horizontal;

            if (inputHandler.moveAmount > 0)
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

    #endregion
}
