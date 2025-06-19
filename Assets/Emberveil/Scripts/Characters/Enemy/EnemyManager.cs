using UnityEngine;

[RequireComponent(typeof(SavableEntity))]
public class EnemyManager : CharacterManager, ISavable
{
    // AI Core Components
    public EnemyStats Stats { get; private set; }
    public EnemyLocomotion Locomotion { get; private set; }
    public EnemyAnimator EnemyAnimator { get; private set; }
    public EnemyCombat Combat { get; private set; }
    public EnemySenses Senses { get; private set; }

    // State Machine
    public IEnemyState CurrentState { get; private set; }
    public IdleState idleState;
    public PatrolState patrolState;
    public ChaseState chaseState;
    public CombatStanceState combatStanceState;
    public AttackingState attackingState;
    public PerformingBackstabState performingBackstabState; // Attacker
    public BeingBackstabbedState beingBackstabbedState;   // Victim
    public HitReactionState hitReactionState;
    public ReturnToPostState returnToPostState;
    public RepositionState repositionState;
    public DeadState deadState;
    // TODO: Add more states: InvestigateState, PoiseBreakState

    public EnemyWeaponSlotManager EnemyWeaponSlotManager { get; private set; }

    public CharacterManager CurrentTarget { get; set; } // Who the AI is focused on
    public PatrolRoute patrolRoute;
    [HideInInspector] public Vector3 initialPosition;
    [HideInInspector] public Quaternion initialRotation;

    [Header("AI Behavior Settings")]
    public bool canRepositionWhileOnCooldown = true;
    public float defaultStoppingDistance = 1.5f;
    public bool isInvincibleDuringStun = false;
    public float hitStunDuration = 0.1f;

    [Header("UI")]
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private Transform healthBarAttachPoint;
    private EnemyHealthBarUI healthBarUI;

    private SavableEntity savableEntity;
    private Transform lastAttacker;

    public bool IsPerformingCriticalAction => CurrentState is PerformingBackstabState;
    public bool IsReceivingCriticalHit => CurrentState is BeingBackstabbedState;

    public bool HasAttackActionConcluded { get; set; }

    public override bool IsDead => Stats.isDead;


    protected override void Awake()
    {
        base.Awake(); // From your CharacterManager
        savableEntity = GetComponent<SavableEntity>();

        Stats = GetComponent<EnemyStats>();
        Locomotion = GetComponent<EnemyLocomotion>();
        EnemyAnimator = GetComponentInChildren<EnemyAnimator>();
        Combat = GetComponent<EnemyCombat>();
        Senses = GetComponent<EnemySenses>();

        if (Stats == null) Debug.LogError("EnemyStats not found!", this);
        if (Locomotion == null) Debug.LogError("EnemyLocomotion not found!", this);
        if (EnemyAnimator == null) Debug.LogError("EnemyAnimator not found or not assigned!", this);
        if (Combat == null) Debug.LogError("EnemyCombat not found!", this);
        if (Senses == null) Debug.LogError("EnemySenses not found!", this);

        EnemyWeaponSlotManager = GetComponentInChildren<EnemyWeaponSlotManager>();
        if (EnemyWeaponSlotManager == null) Debug.LogWarning($"{name} is missing an EnemyWeaponSlotManager component in children.");

        // Initialize components
        //Stats.Initialize(this);
        Locomotion.Initialize(this);
        EnemyAnimator.Initialize(this);
        Combat.Initialize(this);
        Senses.Initialize(this);

        // Create state instances
        idleState = new IdleState();
        patrolState = new PatrolState();
        chaseState = new ChaseState();
        combatStanceState = new CombatStanceState();
        attackingState = new AttackingState();
        performingBackstabState = new PerformingBackstabState();
        beingBackstabbedState = new BeingBackstabbedState();
        hitReactionState = new HitReactionState(isInvincibleDuringStun, hitStunDuration);
        returnToPostState = new ReturnToPostState();
        repositionState = new RepositionState();
        deadState = new DeadState();
    }

    private void Start()
    {
        // Capture initial position and rotation
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        if (healthBarPrefab != null)
        {
            GameObject healthBarInstance = Instantiate(healthBarPrefab, healthBarAttachPoint);
            healthBarUI = healthBarInstance.GetComponent<EnemyHealthBarUI>();
            // Initialize the UI with the stats component, passing the reference
            healthBarUI.Initialize(Stats);
        }
        else Debug.LogWarning($"Enemy {name} is missing a Health Bar Prefab.");

        // Subscribe to events
        Stats.OnDamagedEvent += HandleDamageTaken;
        Stats.OnDeathEvent += HandleDeath;
        Senses.OnTargetSpotted += HandleTargetSpotted;
        Senses.OnTargetLost += HandleTargetLost;

        // Set initial state
        if (patrolRoute != null && patrolRoute.patrolPoints.Count > 0)
        {
            SwitchState(patrolState);
        }
        else
        {
            SwitchState(idleState);
        }
    }

    private void Update()
    {
        CurrentState?.Tick();
        Combat.TickCombat();
        HandleStateTransitions();
        Locomotion.UpdateAnimatorMovementParameters(); // Update animator based on locomotion state
    }

    private void FixedUpdate()
    {
        CurrentState?.FixedTick();
    }

    private void OnDestroy()
    {
        // Unsubscribe
        if (Stats != null)
        {
            Stats.OnDamagedEvent -= HandleDamageTaken;
            Stats.OnDeathEvent -= HandleDeath;
        }
        if (Senses != null)
        {
            Senses.OnTargetSpotted -= HandleTargetSpotted;
            Senses.OnTargetLost -= HandleTargetLost;
        }
    }

    private void FindAndAssignAttachPoint()
    {
        healthBarAttachPoint = transform.Find("HealthBarAttachPoint");
        if (healthBarAttachPoint == null)
        {
            // Create one dynamically if it doesn't exist
            GameObject attachPointGO = new GameObject("HealthBarAttachPoint");
            attachPointGO.transform.SetParent(transform);
            // Position it above the character's head.
            float height = GetComponent<CapsuleCollider>()?.height ?? 2.0f;
            attachPointGO.transform.localPosition = new Vector3(0, height + 0.5f, 0);
            healthBarAttachPoint = attachPointGO.transform;
        }
    }

    public void SwitchState(IEnemyState newState)
    {
        CurrentState?.Exit();
        CurrentState = newState;

        HasAttackActionConcluded = false;

        if (healthBarUI != null)
        {
            // Show health bar if in combat-related states, hide otherwise
            bool shouldBeVisible = (newState is ChaseState || newState is CombatStanceState ||
                                    newState is AttackingState || newState is RepositionState);
            healthBarUI.SetVisibility(shouldBeVisible);
        }

        Debug.Log($"{name} transitioning to {newState.GetType().Name}");
        CurrentState.Enter(this);
    }

    private void HandleStateTransitions()
    {
        if (CurrentState == null) return;
        IEnemyState nextState = CurrentState.Transition();
        if (nextState != null)
        {
            SwitchState(nextState);
        }
    }

    // --- Event Handlers ---
    private void HandleTargetSpotted(CharacterManager target)
    {
        CurrentTarget = target;
        // Current state might react to this by transitioning (e.g., Idle to Chase)
    }

    private void HandleTargetLost()
    {
        CurrentTarget = null;
        // Current state might react (e.g., Chase to Idle/ReturnToPost)
    }

    private void HandleDamageTaken(int damageAmount, DamageType type, Transform attacker)
    {
        if (attacker != null)
        {
            lastAttacker = attacker;
        }

        if (CurrentState == deadState || CurrentState == beingBackstabbedState) return; // Already dead or in critical hit anim

        // Specific handling for backstabs
        if (type == DamageType.BackstabCritical)
        {
            // The GetBackstabbed method should have already been called by the attacker
            // This event is more for logging or if additional logic is needed after damage application
            // SwitchState(beingBackstabbedState); // This is usually initiated by GetBackstabbed
            return;
        }

        // For other damage, transition to HitReactionState
        // unless in a state that shouldn't be interrupted (e.g. performing critical action)
        if (!(CurrentState is PerformingBackstabState)) // Don't interrupt own backstab
        {
            hitReactionState.SetAttacker(attacker); // Pass attacker for facing
            SwitchState(hitReactionState);
        }
    }

    private void HandleDeath()
    {
        if (lastAttacker != null && lastAttacker.CompareTag("Player"))
        {
            PlayerStats playerStats = lastAttacker.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                int currencyAmount = Stats.GetCurrencyReward();
                playerStats.AddCurrency(currencyAmount);
                Debug.Log($"{name} died and awarded {currencyAmount} currency to the player.");
            }
        }

        Stats.isDead = true;

        RaiseDeath();
        SwitchState(deadState);
    }

    public void RespawnEnemy()
    {
        Stats.isDead = false;
        gameObject.SetActive(true);
        Stats.currentHealth = Stats.maxHealth;
        transform.SetPositionAndRotation(initialPosition, initialRotation);

        // Re-enable components disabled on death
        GetComponent<Collider>().enabled = true;
        if (lockOnTransform != null) lockOnTransform.gameObject.SetActive(true);

        // Switch back to a starting state
        if (patrolRoute != null && patrolRoute.patrolPoints.Count > 0)
        {
            SwitchState(patrolState);
        }
        else
        {
            SwitchState(idleState);
        }

        Debug.Log($"{name} has been respawned.");
    }

    // --- Critical Action Control Methods ---
    public override void GetBackstabbed(Transform attacker) // Called by PlayerAttacker or other enemies
    {
        if (CurrentState == deadState || !canBeBackstabbed) return;

        Debug.Log($"{gameObject.name} is being backstabbed by {attacker.name}");
        base.GetBackstabbed(attacker); // Calls CharacterManager version if any shared logic needed

        beingBackstabbedState.SetAttacker(attacker);
        SwitchState(beingBackstabbedState);
    }

    public void SetPerformingCriticalAction(bool isPerforming, bool isAttackerPerspective)
    {
        // This method helps states like PerformingBackstabState to manage overall enemy behavior.
        // The flags `isPerformingCriticalAction` and `isReceivingCriticalHit` on CharacterManager
        // are now effectively controlled by being IN these specific states.
        if (isPerforming)
        {
            EnemyAnimator.IsInvulnerable = true; // Usually invulnerable during criticals
            Locomotion.DisableAgentAndPhysicsControl();
        }
        else
        {
            EnemyAnimator.IsInvulnerable = false; // Reset on exit from critical state
            // Locomotion re-enabled by the exiting state
        }
        // The boolean flags on EnemyManager are now less important than the state itself
    }

    // Called by EnemyAnimator's animation event when ENEMY'S ATTACKING critical animation finishes
    public void Notify_FinishedPerformingCriticalAction()
    {
        if (CurrentState is PerformingBackstabState performingState)
        {
            performingState.OnCriticalActionAnimationEnd();
        }
        Combat.ClearBackstabVictim();
    }

    // Called by EnemyAnimator's animation event when ENEMY'S VICTIM critical animation finishes
    public void Notify_FinishedBeingCriticallyHit()
    {
        if (CurrentState is BeingBackstabbedState victimState)
        {
            victimState.OnCriticalHitAnimationEnd();
        }
        // Check if health is <=0, if so, transition to DeadState if not already handled
        if (Stats.currentHealth <= 0 && CurrentState != deadState)
        {
            SwitchState(deadState);
        }
    }

    public void Notify_AttackActionConcluded()
    {
        Debug.Log($"{name}: AttackActionConcluded reported by animation.");
        HasAttackActionConcluded = true;
    }

    #region Saving and Loading

    public string GetUniqueIdentifier()
    {
        return savableEntity.GetUniqueIdentifier();
    }

    [System.Serializable]
    private struct EnemySaveData
    {
        public bool isDead;
    }

    public object CaptureState()
    {
        return new EnemySaveData { isDead = Stats.isDead };
    }

    public void RestoreState(object state)
    {
        if (state is EnemySaveData saveData)
        {
            Stats.isDead = saveData.isDead;
            //if (Stats.isDead)
            //{
            //    // If the loaded state is "dead", immediately put the enemy into the dead state.
            //    SwitchState(deadState);
            //}
            gameObject.SetActive(!saveData.isDead);
        }
    }
    #endregion
}
