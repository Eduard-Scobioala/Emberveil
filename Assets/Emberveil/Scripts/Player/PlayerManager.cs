using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerManager : CharacterManager
{
    private InputHandler inputHandler;
    private Animator animator;
    private PlayerLocomotion playerLocomotion;
    private InteractableUI interactableUI;

    private List<Interactable> nearbyInteractables = new ();

    [Header("Player Flags")]
    public bool isInMidAction;
    public bool isSprinting;
    public bool isInAir;
    public bool isGrounded;
    public bool canDoCombo;

    private bool pickedUpItem = false;
    private bool isInteracting = false;

    private void Start()
    {
        inputHandler = GetComponent<InputHandler>();
        animator = GetComponentInChildren<Animator>();
        playerLocomotion = GetComponent<PlayerLocomotion>();
        interactableUI = FindObjectOfType<InteractableUI>();
    }

    private void OnEnable()
    {
        InputHandler.InteractButtonPressed += HandleInteractButtonPressed;
    }

    private void OnDisable()
    {
        InputHandler.InteractButtonPressed -= HandleInteractButtonPressed;
    }

    private void Update()
    {
        var deltaTime = Time.deltaTime;

        isInMidAction = animator.GetBool("isInMidAction");
        canDoCombo = animator.GetBool("canDoCombo");
        animator.SetBool("isInAir", isInAir);
        
        HandleInteractableUI();
    }

    private void FixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;

        playerLocomotion.HandleMovement(deltaTime);
        playerLocomotion.HandleFalling(deltaTime, playerLocomotion.moveDirection);
    }

    private void LateUpdate()
    {
        if (isInAir)
        {
            playerLocomotion.inAirTimer += Time.deltaTime;
        }
    }

    private void HandleInteractButtonPressed()
    {
        isInteracting = true;
    }

    #region Handle Interactables UI
    public void AddInteractable(Interactable interactable)
    {
        if (!nearbyInteractables.Contains(interactable))
        {
            nearbyInteractables.Add(interactable);
        }
    }

    public void RemoveInteractable(Interactable interactable)
    {
        nearbyInteractables.Remove(interactable);
    }

    private void HandleInteractableUI()
    {
        if (nearbyInteractables.Count > 0)
        {
            Interactable closest = GetClosestInteractable();
            if (closest != null)
            {
                // Don't update the UI, in case of multiple items, if the previus pick was not confirmed
                if (pickedUpItem)
                    return;

                interactableUI.interactableInfoText.text = closest.interactableInfoText;
                interactableUI.EnableInteractionPopUpGameObject(true);

                if (isInteracting)
                {
                    isInteracting = false;

                    interactableUI.itemInfoText.text = closest.GetItemName();
                    interactableUI.itemImage.sprite = closest.GetItemIcon();

                    closest.OnInteract(this);
                    nearbyInteractables.Remove(closest);

                    interactableUI.EnableItemPopUpGameObject(true);
                    pickedUpItem = true;
                }
            }
        }
        else
        {
            interactableUI.EnableInteractionPopUpGameObject(false);

            if (isInteracting)
            {
                isInteracting = false;

                interactableUI.EnableItemPopUpGameObject(false);
                pickedUpItem = false;
            }
        }
    }

    private Interactable GetClosestInteractable()
    {
        Interactable closest = null;
        float minDistance = float.MaxValue;
        Vector3 playerPos = transform.position;

        foreach (var interactable in nearbyInteractables)
        {
            if (interactable == null) continue;
            float distance = Vector3.Distance(playerPos, interactable.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = interactable;
            }
        }
        return closest;
    }
    #endregion
}
