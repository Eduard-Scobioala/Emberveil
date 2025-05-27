using UnityEngine;

public class EnemyManager : CharacterManager
{
    //"AI Core Components"
    public EnemyStats Stats { get; private set; }
    public EnemyLocomotion Locomotion { get; private set; }
    public EnemyAnimator EnemyAnimator { get; private set; }
    public EnemyCombat Combat { get; private set; }
    public EnemySenses Senses { get; private set; }

    //"State Machine"
    public IEnemyState CurrentState { get; private set; }
    public IdleState idleState;
    public PatrolState patrolState;
    public ChaseState chaseState;
    public CombatStanceState combatStanceState;
    public AttackingState attackingState;
    public PerformingBackstabState performingBackstabState; // Attacker
    public BeingBackstabbedState beingBackstabbedState;   // Victim
    public HitReactionState hitReactionState;
    public DeadState deadState;
    // Add more states like FleeState, InvestigateState, PoiseBreakState

    //"AI Behavior"
    public CharacterManager CurrentTarget { get; set; } // Who the AI is focused on
    public PatrolRoute patrolRoute;
    public float defaultStoppingDistance = 1.5f;


    public bool IsPerformingCriticalAction => CurrentState is PerformingBackstabState;
    public bool IsReceivingCriticalHit => CurrentState is BeingBackstabbedState;
    public bool isPerformingNonCriticalAction; // For regular attacks

    protected override void Awake()
    {
        base.Awake(); // From your CharacterManager

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
        hitReactionState = new HitReactionState();
        deadState = new DeadState();

        // Default flags from CharacterManager
        // isInMidAction (use isPerformingNonCriticalAction or state checks)
        // isInvulnerable
        // isBeingCriticallyHit (use beingBackstabbedState)
        // canBeBackstabbed
    }

    private void Start()
    {
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

    public void SwitchState(IEnemyState newState)
    {
        if (CurrentState == deadState && newState != deadState) return; // Cannot leave dead state except for cleanup

        CurrentState?.Exit();
        CurrentState = newState;
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
        SwitchState(deadState);
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
            isInvulnerable = true; // Usually invulnerable during criticals
            Locomotion.DisableAgentAndPhysicsControl();
        }
        else
        {
            isInvulnerable = false; // Reset on exit from critical state
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
}
