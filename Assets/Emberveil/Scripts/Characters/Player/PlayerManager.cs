using System.Collections.Generic;
using UnityEngine;

public struct Commands
{
    public ICommand LightAttackCommand;
    public ICommand HeavyAttackCommand;
    public ICommand JumpCommand;
    public ICommand DodgeCommand;
    public ICommand SprintHoldCommand;
    public ICommand SprintReleaseCommand;
};

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
    private Commands commands;

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
        playerLocomotion = GetComponent<PlayerLocomotion>();
        playerAttacker = GetComponent<PlayerAttacker>();
        interactableUI = FindObjectOfType<InteractableUI>();

        InitCommands();
    }

    private void OnEnable()
    {
        InputHandler.InteractButtonPressed += HandleInteractButtonPressed;
        InputHandler.LightAttackButtonPressed += HandleLightAttackInput;
        InputHandler.HeavyAttackButtonPressed += HandleHeavyAttackInput;
        InputHandler.JumpButtonPressed += HandleJumpInput;
        InputHandler.DodgeTapped += HandleDodgeButton;
        InputHandler.SprintHolding += HandleSprintHolding;
        InputHandler.SprintReleased += HandleSprintReleased;
    }

    private void OnDisable()
    {
        InputHandler.InteractButtonPressed -= HandleInteractButtonPressed;
        InputHandler.LightAttackButtonPressed -= HandleLightAttackInput;
        InputHandler.HeavyAttackButtonPressed -= HandleHeavyAttackInput;
        InputHandler.JumpButtonPressed -= HandleJumpInput;
        InputHandler.DodgeTapped -= HandleDodgeButton;
        InputHandler.SprintHolding -= HandleSprintHolding;
        InputHandler.SprintReleased -= HandleSprintReleased;
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

    private void LateUpdate()
    {
        if (isInAir)
        {
            playerLocomotion.inAirTimer += Time.deltaTime;
        }
    }

    private void InitCommands()
    {
        commands = new Commands
        {
            LightAttackCommand = new RelayCommand(
                () => !isInMidAction || canDoCombo,
                playerAttacker.HandleLightAttackButtonPressed),

            HeavyAttackCommand = new RelayCommand(
                () => !isInMidAction || canDoCombo,
                playerAttacker.HandleHeavyAttackButtonPressed),

            JumpCommand = new RelayCommand(
                () => !isInMidAction,
                playerLocomotion.HandleJumpButtonPressed),

            DodgeCommand = new RelayCommand(
                () => !isInMidAction,
                playerLocomotion.HandleDodgeTapped),

            SprintHoldCommand = new RelayCommand(
                () => !isInMidAction,
                playerLocomotion.HandleSprintHolding),

            SprintReleaseCommand = new RelayCommand(
                () => true,
                playerLocomotion.HandleSprintReleased)
        };
    }

    #region Handle Commands
    private void HandleCommand(ICommand command)
    {
        if (command.CanExecute())
        {
            // If a command could be exectuted,there is
            // no reason to hold for the pending command
            pendingCommand = null;
            command.Execute();
        }
        else
        {
            pendingCommand = command;
        }
    }

    private void HandleLightAttackInput()
    {
        HandleCommand(commands.LightAttackCommand);
    }

    private void HandleHeavyAttackInput()
    {
        HandleCommand(commands.HeavyAttackCommand);
    }

    private void HandleJumpInput()
    {
        HandleCommand(commands.JumpCommand);
    }

    private void HandleDodgeButton()
    {
        HandleCommand(commands.DodgeCommand);
    }

    private void HandleSprintHolding()
    {
        HandleCommand(commands.SprintHoldCommand);
    }

    private void HandleSprintReleased()
    {
        HandleCommand(commands.SprintReleaseCommand);
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
