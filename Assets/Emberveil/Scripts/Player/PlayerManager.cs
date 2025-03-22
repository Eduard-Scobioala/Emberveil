using UnityEngine;
using static UnityEngine.UI.Image;

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
    public bool canDoCombo;

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
        canDoCombo = animator.GetBool("canDoCombo");

        inputHandler.TickInput(deltaTime);

        playerLocomotion.HandleMovement(deltaTime);
        playerLocomotion.HandleRollingAndSprinting(deltaTime);
        playerLocomotion.HandleFalling(deltaTime, playerLocomotion.moveDirection);

        CheckForInteractableObject();
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
        inputHandler.dPadUp = false;
        inputHandler.dPadDown = false;
        inputHandler.dPadLeft = false;
        inputHandler.dPadRight = false;
        inputHandler.interactInput = false;

        if (isInAir)
        {
            playerLocomotion.inAirTimer = playerLocomotion.inAirTimer + Time.deltaTime;
        }
    }

    public void CheckForInteractableObject()
    {
        if (!inputHandler.interactInput)
            return;

        Collider[] initialOverlaps = Physics.OverlapSphere(transform.position, .5f);
        foreach (var collider in initialOverlaps)
        {
            if (collider.CompareTag("Interactable"))
            {
                if (collider.TryGetComponent<Interactable>(out var interactableObject))
                {
                    string interactableText = interactableObject.interactableText;

                    interactableObject.OnInteract(this);
                    return; // Return after first Interact - otherwise you will pick up everything in range
                }
            }
        }
    }
}
