using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : CharacterManager
{
    [Header("AI Settings")]
    public float detectionRadius = 20;
    public float minDetectionAngle = -50;
    public float maxDetectionAngle = 50;
    public bool isPerformingAction;
    public EnemyAttackAction[] enemyAttacks;

    [Header("State Machine")]
    private EnemyState currentState;

    // Available States
    [HideInInspector] public IdleState idleState;
    [HideInInspector] public ChaseState chaseState;
    [HideInInspector] public CombatState combatState;
    [HideInInspector] public DeadState deadState;
    [HideInInspector] public StunnedState stunnedState;

    // Component references
    [HideInInspector] public EnemyLocomotion enemyLocomotion;
    [HideInInspector] public EnemyAnimator enemyAnimator;
    [HideInInspector] public EnemyStats enemyStats;

    private void Awake()
    {
        enemyLocomotion = GetComponent<EnemyLocomotion>();
        enemyAnimator = GetComponentInChildren<EnemyAnimator>();
        enemyStats = GetComponent<EnemyStats>();

        // Initialize states
        idleState = new IdleState();
        chaseState = new ChaseState();
        combatState = new CombatState();
        deadState = new DeadState();
        stunnedState = new StunnedState();
    }

    private void Start()
    {
        SwitchState(idleState);
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
        if (currentState != deadState) // Only react if not already dead
        {
            // Notify current state of damage
            currentState.OnDamageReceived();

            // Immediately switch to stunned state
            SwitchState(stunnedState);
        }
    }

    private void HandleDeath()
    {
        SwitchState(deadState);
    }

    public EnemyState GetCurrentState()
    {
        return currentState;
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
