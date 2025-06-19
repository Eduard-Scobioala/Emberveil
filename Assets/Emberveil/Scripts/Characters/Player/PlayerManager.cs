using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Commands
{
    public ICommand AttackCommand;
    public ICommand JumpCommand;
    public ICommand DodgeCommand;
    public ICommand SprintHoldCommand;
    public ICommand SprintReleaseCommand;
    public ICommand CrouchCommand;
};

public class PlayerManager : CharacterManager
{
    public InteractableUI interactableUI;
    private PlayerStats playerStats;

    public PlayerAttacker playerAttacker;
    public PlayerLocomotion playerLocomotion;
    public PlayerInventory playerInventory;
    public PlayerAnimator playerAnimator;
    [SerializeField] private InputHandler inputHandler;
    [SerializeField] private CameraController cameraController;

    [Header("Player Flags")]
    public bool isSprinting;

    private bool isPickingUp = false;
    private bool pickedUpItem = false;

    private readonly List<Interactable> nearbyInteractables = new ();

    // Command buffering
    private ICommand pendingCommand;
    private Commands commands;
    [SerializeField] private float pendingCommandInterval = 0.2f;
    private float pendingCommandTimer;

    public CharacterManager currentBackstabTarget;
    public override bool IsDead => playerStats.isDead;

    protected override void Awake()
    {
        base.Awake();
        playerStats = GetComponent<PlayerStats>();
        playerInventory = GetComponent<PlayerInventory>();
        charAnimator = GetComponentInChildren<Animator>();
        playerAnimator = GetComponentInChildren<PlayerAnimator>();
        playerLocomotion = GetComponent<PlayerLocomotion>();
        playerAttacker = GetComponent<PlayerAttacker>();
        interactableUI = FindObjectOfType<InteractableUI>();
    }

    private void Start()
    {
        InitCommands();
    }

    private void Update()
    {
        // Execute pending command when action ends
        if (pendingCommand != null)
        {
            if (pendingCommand.CanExecute())
            {
                pendingCommand.Execute();
                pendingCommand = null;
            }
            
            pendingCommandTimer -= Time.deltaTime;
            if (pendingCommandTimer < 0)
            {
                pendingCommand = null;
            }
        }

        HandleInteractableUI();
    }

    private void LateUpdate()
    {
        if (playerAnimator.IsInAir)
        {
            playerLocomotion.inAirTimer += Time.deltaTime;
        }
    }

    private void OnEnable()
    {
        InputHandler.InteractButtonPressed += HandleInteractButtonPressed;
        InputHandler.JumpButtonPressed += HandleJumpInput;
        InputHandler.DodgeTapped += HandleDodgeButton;
        InputHandler.SprintHolding += HandleSprintHolding;
        InputHandler.SprintReleased += HandleSprintReleased;
        InputHandler.CrouchButtonPressed += HandleCrouchInput;
        InputHandler.AttackButtonPressed += HandleAttackInput;
    }

    private void OnDisable()
    {
        InputHandler.InteractButtonPressed -= HandleInteractButtonPressed;
        InputHandler.JumpButtonPressed -= HandleJumpInput;
        InputHandler.DodgeTapped -= HandleDodgeButton;
        InputHandler.SprintHolding -= HandleSprintHolding;
        InputHandler.SprintReleased -= HandleSprintReleased;
        InputHandler.CrouchButtonPressed -= HandleCrouchInput;
        InputHandler.AttackButtonPressed -= HandleAttackInput;
    }

    private void InitCommands()
    {
        commands = new Commands
        {
            AttackCommand = new RelayCommand(
                () => playerStats.CanPerformStaminaConsumingAction() &&
                    (!playerAnimator.IsInMidAction || playerAnimator.CanDoCombo),
                playerAttacker.HandleAttackButton),

            JumpCommand = new RelayCommand(
                () => playerStats.CanPerformStaminaConsumingAction() &&
                    !playerAnimator.IsInMidAction && playerAnimator.IsGrounded,
                playerLocomotion.HandleJumpButtonPressed),

            DodgeCommand = new RelayCommand(
                () => playerStats.CanPerformStaminaConsumingAction() && !playerAnimator.IsInMidAction,
                playerLocomotion.HandleDodgeTapped),

            SprintHoldCommand = new RelayCommand(
                () => playerStats.CanPerformStaminaConsumingAction() && 
                    !playerAnimator.IsInMidAction && !playerAnimator.IsCrouching && playerAnimator.IsGrounded,
                playerLocomotion.HandleSprintHolding),

            SprintReleaseCommand = new RelayCommand(
                () => true,
                playerLocomotion.HandleSprintReleased),

            CrouchCommand = new RelayCommand(
                () => !playerAnimator.IsInMidAction && playerAnimator.IsGrounded,
                ToggleCrouchState)
        };
    }

    public void ToggleCrouchState()
    {
        if (playerAnimator.IsInAir) return;

        playerAnimator.IsCrouching = !playerAnimator.IsCrouching;

        if (playerAnimator.IsCrouching)
        {
            isSprinting = false; // Cannot sprint while crouching
        }
    }

    public override void GetBackstabbed(Transform attacker)
    {
        if (playerAnimator.IsCrouching) // Force stand up if backstabbed while crouching
        {
            playerAnimator.IsCrouching = false;
            // Animator should transition out of crouch automatically due to isCrouching=false
        }
        base.GetBackstabbed(attacker);
    }

    #region Death Handling

    public void HandleDeath()
    {
        if (charAnimManager.IsInvulnerable) return;

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        Debug.Log("Player has died. Starting death sequence.");

        // Lockdown the Player
        inputHandler.EnableUIInput();
        playerLocomotion.StopAllMovement();
        cameraController.StopFollowing();

        // Prevent any further input or actions
        playerAnimator.IsInvulnerable = true;
        playerAnimator.IsDead = true;

        // 1. Play death animation
        if (!isBeingCriticallyHit)
        {
            playerAnimator.PlayTargetAnimation("Death_01", true);
        }

        // 2. Show the "YOU DIED" screen
        yield return StartCoroutine(interactableUI.GetComponentInParent<UIManager>().ShowYouDiedScreen());

        // 3. Wait for a few seconds
        yield return new WaitForSeconds(4.0f);

        // 4. Handle currency loss
        playerStats.HandleDeathPenalty();

        // 5. Respawn the player
        RespawnPlayer();

        // 6. Hide the "YOU DIED" screen
        interactableUI.GetComponentInParent<UIManager>().HideYouDiedScreen();

        // 7. Restore player state
        charAnimManager.IsInvulnerable = false;
        inputHandler.EnableGameplayInput();
        cameraController.StartFollowing();
    }

    public void RespawnPlayer()
    {
        WorldManager.Instance.ResetWorldState();

        playerStats.isDead = false;
        playerAnimator.IsDead = false;
        Debug.Log("Respawning player...");
        SaveLoadManager.Instance.LoadGame();

        playerAnimator.PlayTargetAnimation("Empty", false); // Clear any lingering animations
    }

    #endregion

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
            pendingCommandTimer = pendingCommandInterval;
            pendingCommand = command;
        }
    }

    private void HandleAttackInput() => HandleCommand(commands.AttackCommand);
    private void HandleJumpInput() => HandleCommand(commands.JumpCommand);
    private void HandleDodgeButton() => HandleCommand(commands.DodgeCommand);
    private void HandleSprintHolding() => HandleCommand(commands.SprintHoldCommand);
    private void HandleSprintReleased() => HandleCommand(commands.SprintReleaseCommand);
    private void HandleCrouchInput() => HandleCommand(commands.CrouchCommand);
    private void HandleInteractButtonPressed() => isPickingUp = true;
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
                // Allow re-showing interaction if item popup closed
                if (pickedUpItem && !interactableUI.itemPopUp.activeSelf)
                {
                    pickedUpItem = false;
                }

                if (pickedUpItem) return;

                interactableUI.interactableInfoText.text = closest.interactableInfoText;
                interactableUI.EnableInteractionPopUpGameObject(true);

                if (isPickingUp)
                {
                    isPickingUp = false;

                    closest.OnInteract(this);

                    interactableUI.EnableInteractionPopUpGameObject(false);
                    pickedUpItem = true;

                    if (closest.IsInteractablePickUp)
                    {
                        interactableUI.itemInfoText.text = closest.GetItemName();
                        interactableUI.itemImage.sprite = closest.GetItemIcon();
                        interactableUI.EnableItemPopUpGameObject(true);
                    }
                }
            }
            else // No closest, but list not empty (e.g. all became null)
            {
                interactableUI.EnableInteractionPopUpGameObject(false);
            }
        }
        else
        {
            interactableUI.EnableInteractionPopUpGameObject(false);

            if (isPickingUp || pickedUpItem)
            {
                isPickingUp = false;

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

        nearbyInteractables.RemoveAll(item => item == null || !item.isActiveAndEnabled);

        foreach (var interactable in nearbyInteractables)
        {
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

    #region Animation Events
    public void AnimEvent_ApplyBackstabDamage()
    {
        if (currentBackstabTarget != null && playerAttacker != null)
        {
            WeaponItem weaponItem = playerInventory.EquippedRightWeapon;
            int backstabDamage = weaponItem != null ? weaponItem.GetBackstabDmg() : 200;
            Debug.Log($"Player applying {backstabDamage} backstab damage to {currentBackstabTarget.name}");

            EnemyStats victimStats = currentBackstabTarget.GetComponent<EnemyStats>();
            if (victimStats != null)
            {
                victimStats.TakeDamage(backstabDamage, DamageType.BackstabCritical, transform);
            }
        }
        else
        {
            Debug.LogWarning("ApplyBackstabDamage called, but currentBackstabTarget or playerAttacker is null.");
        }
    }

    public void AnimEvent_FinishPerformingBackstab()
    {
        Debug.Log("Player finished performing backstab animation.");
        charAnimManager.IsInMidAction = false;
        charAnimManager.IsInvulnerable = false;
        isBeingCriticallyHit = false;
        currentBackstabTarget = null;
    }
    #endregion
}
