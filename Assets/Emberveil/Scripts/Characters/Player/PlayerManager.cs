using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : CharacterManager
{
    private Animator animator;
    private PlayerLocomotion playerLocomotion;
    private InteractableUI interactableUI;
    private PlayerAttacker playerAttacker;

    [Header("Player Flags")]
    public bool isInMidAction;
    public bool isSprinting;
    public bool isInAir;
    public bool isGrounded;
    public bool canDoCombo;
    public bool isUsingRightHand;
    public bool isUsingLeftHand;
    public bool isInvulnerable;

    private bool pickedUpItem = false;
    private bool isInteracting = false;

    private List<Interactable> nearbyInteractables = new ();

    // Command buffering
    private ICommand pendingCommand;
    private bool wasInMidAction = false;

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
        playerLocomotion = GetComponent<PlayerLocomotion>();
        playerAttacker = GetComponent<PlayerAttacker>();
        interactableUI = FindObjectOfType<InteractableUI>();
    }

    private void OnEnable()
    {
        InputHandler.InteractButtonPressed += HandleInteractButtonPressed;
        InputHandler.LightAttackButtonPressed += HandleLightAttackInput;
        InputHandler.HeavyAttackButtonPressed += HandleHeavyAttackInput;
        InputHandler.JumpButtonPressed += HandleJumpInput;
        InputHandler.DodgeButtonPressed += HandleDodgeButtonPressed;
        InputHandler.DodgeButtonReleased += HandleDodgeButtonReleased;
    }

    private void OnDisable()
    {
        InputHandler.InteractButtonPressed -= HandleInteractButtonPressed;
        InputHandler.LightAttackButtonPressed -= HandleLightAttackInput;
        InputHandler.HeavyAttackButtonPressed -= HandleHeavyAttackInput;
        InputHandler.JumpButtonPressed -= HandleJumpInput;
        InputHandler.DodgeButtonPressed -= HandleDodgeButtonPressed;
        InputHandler.DodgeButtonReleased -= HandleDodgeButtonReleased;
    }

    private void Update()
    {
        isInMidAction = animator.GetBool("isInMidAction");
        canDoCombo = animator.GetBool("canDoCombo");
        isUsingRightHand = animator.GetBool("isUsingRightHand");
        isUsingLeftHand = animator.GetBool("isUsingLeftHand");
        isInvulnerable = animator.GetBool("isInvulnerable");
        animator.SetBool("isInAir", isInAir);

        // Execute pending command when action ends
        if (pendingCommand != null && pendingCommand.CanExecute())
        {
            pendingCommand.Execute();
            pendingCommand = null;
        }
        
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

    #region Handle Commands
    private void HandleCommand(ICommand command)
    {
        if (command.CanExecute())
        {
            command.Execute();
        }
        else
        {
            pendingCommand = command;
        }
    }

    private void HandleLightAttackInput()
    {
        ICommand command = new LightAttackCommand(playerAttacker, () => (isInMidAction, canDoCombo));
        HandleCommand(command);
    }

    private void HandleHeavyAttackInput()
    {
        ICommand command = new HeavyAttackCommand(playerAttacker, () => (isInMidAction, canDoCombo));
        HandleCommand(command);
    }

    private void HandleJumpInput()
    {
        ICommand command = new JumpCommand(playerLocomotion, () => isInMidAction);
        HandleCommand(command);
    }

    private void HandleDodgeButtonPressed()
    {
        ICommand command = new DodgeCommand(playerLocomotion, true, () => isInMidAction);
        HandleCommand(command);
    }

    private void HandleDodgeButtonReleased()
    {
        ICommand command = new DodgeCommand(playerLocomotion, false, () => isInMidAction);
        command.Execute();
    }
    
    private void HandleInteractButtonPressed()
    {
        isInteracting = true;
    }
    #endregion

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
