using System.Collections.Generic;
using UnityEngine;

public enum InitialStateType { Idle, Patrol }

public class EnemyManager2 : CharacterManager
{
    [Header("AI Settings")]
    public float detectionRadius = 20;
    public float minDetectionAngle = -50;
    public float maxDetectionAngle = 50;
    public bool isPerformingAction;
    public EnemyAttackAction[] enemyAttacks;
    public EnemyBackstabAction enemyBackstabAttack;

    [Header("Enemy Backstab Settings")]
    public float backstabCheckMaxDistance = 3f;
    public float backstabCheckMaxAngle = 45f;
    public float chanceToAttemptBackstab = 0.3f;
    public CharacterManager currentBackstabVictim { get; set; }

    [Header("Patrol Settings")]
    public List<Transform> patrolPoints;
    public float patrolWaitTime = 2f;
    
    [Header("State Machine")]
    public InitialStateType initialState = InitialStateType.Idle;
    [HideInInspector] public EnemyState defaultState;
    private EnemyState currentState;

    // Available States
    [HideInInspector] public IdleState idleState;
    [HideInInspector] public ChaseState chaseState;
    [HideInInspector] public CombatState combatState;
    [HideInInspector] public DeadState deadState;
    [HideInInspector] public StunnedState stunnedState;
    [HideInInspector] public PatrollingState patrolState;

    // Component references
    [HideInInspector] public EnemyLocomotion enemyLocomotion;
    [HideInInspector] public EnemyAnimator enemyAnimator;
    [HideInInspector] public EnemyStats enemyStats;

    protected override void Awake()
    {
        base.Awake();
        enemyLocomotion = GetComponent<EnemyLocomotion>();
        enemyAnimator = charAnimatorManager as EnemyAnimator;
        enemyStats = GetComponent<EnemyStats>();

        // Initialize states
        idleState = new IdleState();
        chaseState = new ChaseState();
        combatState = new CombatState();
        deadState = new DeadState();
        stunnedState = new StunnedState(() => isInMidAction || isBeingCriticallyHit, () => isInvulnerable);
        patrolState = new PatrollingState();
    }

    private void Start()
    {
        if (initialState == InitialStateType.Idle)
        {
            defaultState = idleState;
            SwitchState(idleState);
        }
        else if (initialState == InitialStateType.Patrol)
        {
            defaultState = patrolState;
            SwitchState(patrolState);
        }
    }

    private void OnEnable()
    {
        enemyStats.OnDamageReceived += HandleDamageReceived;
        enemyStats.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        enemyStats.OnDamageReceived -= HandleDamageReceived;
        enemyStats.OnDeath -= HandleDeath;
    }

    private void Update()
    {
        if (currentState == deadState)
            return;

        currentState?.UpdateState();
    }

    private void FixedUpdate()
    {
        if (currentState == deadState)
            return;

        currentState?.FixedUpdateState();
    }

    public void SwitchState(EnemyState newState)
    {
        currentState?.ExitState();

        currentState = newState;
        currentState.EnterState(this);
    }

    public EnemyAttackAction GetRandomAttack()
    {
        if (enemyLocomotion.currentTarget == null)
            return null;

        // Target information
        Transform targetTransform = enemyLocomotion.currentTarget.transform;
        Vector3 targetPosition = targetTransform.position;
        Vector3 directionToTarget = targetPosition - transform.position;

        // Calculate key metrics
        float angleToTarget = Vector3.Angle(directionToTarget, transform.forward);
        float distanceToTarget = Vector3.Distance(targetPosition, transform.position);

        // Find valid attacks and their total score
        List<(EnemyAttackAction, int)> validAttacks = new ();
        int totalScore = 0;

        foreach (EnemyAttackAction attack in enemyAttacks)
        {
            // Check if attack is valid based on distance and angle
            bool isInRangeDistance = distanceToTarget >= attack.minDistanceRequiredToAttack &&
                                    distanceToTarget <= attack.maxDistanceRequiredToAttack;

            bool isInRangeAngle = angleToTarget >= attack.minAttackAngle &&
                                 angleToTarget <= attack.maxAttackAngle;

            if (isInRangeDistance && isInRangeAngle)
            {
                totalScore += attack.attackScore;
                validAttacks.Add((attack, totalScore));
            }
        }

        // If no valid attacks found, return null
        if (validAttacks.Count == 0)
            return null;

        // Select a random attack based on weighted scores
        int randomValue = Random.Range(0, totalScore);

        foreach (var (attack, runningScore) in validAttacks)
        {
            if (randomValue < runningScore)
            {
                return attack;
            }
        }

        throw new System.Exception("Wtff? Your attacks score are screwed.");
    }

    private void HandleDamageReceived(int damage)
    {
        // Sometimes if the enemy is in action you should not be allowed to stun him
        // ex. a big swing which cannot be interuped in any way
        if (isInvulnerable || /*isInMidAction ||*/ isBeingCriticallyHit)
            return;

        if (currentState != deadState)
        {
            // Notify current state of damage
            currentState.OnDamageReceived();

            // Immediately switch to stunned state
            SwitchState(stunnedState);
        }
    }

    private void HandleDeath()
    {
        RaiseDeath();
        SwitchState(deadState);
    }

    public EnemyState GetCurrentState()
    {
        return currentState;
    }

    public void AnimEvent_ApplyBackstabDamageToVictim()
    {
        if (currentBackstabVictim != null && enemyBackstabAttack != null)
        {
            int damage = enemyBackstabAttack.backstabDamage;
            Debug.Log($"{gameObject.name} applying {damage} backstab damage to {currentBackstabVictim.name}");

            PlayerStats victimStats = currentBackstabVictim.GetComponent<PlayerStats>();
            if (victimStats != null)
            {
                victimStats.TakeDamange(damage);
            }
        }
    }

    public void AnimEvent_FinishPerformingBackstab()
    {
        Debug.Log($"{gameObject.name} finished performing backstab.");
        isInMidAction = false;
        isInvulnerable = false;
        isBeingCriticallyHit = false; // Enemy was the attacker, not being hit
        currentBackstabVictim = null;

        if (currentState is CombatState combatStateInstance)
        {
            combatStateInstance.ResetIsAttemptingBackstabFlag();
        }

        // Re-enable locomotion if it was disabled for the backstab
        if (enemyLocomotion != null) enemyLocomotion.enabled = true;

        // After backstab, enemy should re-evaluate.
        // The CombatState will handle the attackCooldownTimer for the backstab action.
    }

    #region Gizmos
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Vector3 fovLine1 = Quaternion.AngleAxis(maxDetectionAngle, transform.up) * transform.forward * detectionRadius;
        Vector3 fovLine2 = Quaternion.AngleAxis(minDetectionAngle, transform.up) * transform.forward * detectionRadius;
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, fovLine1);
        Gizmos.DrawRay(transform.position, fovLine2);
    }
    #endregion
}
