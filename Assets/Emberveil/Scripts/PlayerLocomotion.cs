using UnityEngine;

public class PlayerLocomotion : MonoBehaviour
{
    private Transform cameraObject;
    private InputHandler inputHandler;
    private Vector3 moveDirection;

    [HideInInspector]
    public Transform myTransform;
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
        myTransform = transform;

        animatorHandler.Initialize();
    }

    private void Update()
    {
        var delta = Time.deltaTime;

        inputHandler.TickInput(delta);

        moveDirection = cameraObject.forward * inputHandler.vertical;

        moveDirection += cameraObject.right * inputHandler.horizontal;
        moveDirection.Normalize();

        float speed = movementSpeed;

        moveDirection *= speed;

        Vector3 projectedVelocity = Vector3.ProjectOnPlane(moveDirection, normalVector);
        rigidbody.velocity = projectedVelocity;

        animatorHandler.UpdateAnimatorValues(inputHandler.moveAmount, 0);

        if (animatorHandler.canRotate)
        {
            HandleRotation(delta);
        }
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
            targetDir = myTransform.forward;
        }

        var tr = Quaternion.LookRotation(targetDir);
        var targetRotation = Quaternion.Slerp(myTransform.rotation, tr, rotationSpeed * deltaTime);

        myTransform.rotation = targetRotation;
    }

    #endregion
}
