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

    private EnemyAttackAction currentEnemyAttack;
    private EnemyLocomotion enemyLocomotion;
    private EnemyAnimator enemyAnimator;

    private float currentRecoveryTime = 0;

    private void Awake()
    {
        enemyLocomotion = GetComponent<EnemyLocomotion>();
        enemyAnimator = GetComponentInChildren<EnemyAnimator>();
    }

    private void Update()
    {
        HandleRecoveryTime();
    }

    private void FixedUpdate()
    {
        HandleCurrentAction();
    }

    private void HandleCurrentAction()
    {
        if (enemyLocomotion.currentTarget != null)
        {
            enemyLocomotion.distanceFromTarget = Vector3.Distance(enemyLocomotion.currentTarget.transform.position, transform.position);
        }

        if (enemyLocomotion.currentTarget == null)
        {
            enemyLocomotion.HandleDetection();
        }
        else if (enemyLocomotion.distanceFromTarget > enemyLocomotion.stoppingDistance)
        {
            enemyLocomotion.HandleMoveToTarget();
        }
        else if (enemyLocomotion.distanceFromTarget <= enemyLocomotion.stoppingDistance)
        {
            AttackTarget();
        }
    }

    private void HandleRecoveryTime()
    {
        if (currentRecoveryTime > 0)
        {
            currentRecoveryTime -= Time.deltaTime;
        }

        if (isPerformingAction)
        {
            if (currentRecoveryTime <= 0)
            {
                isPerformingAction = false;
            }
        }
    }

    #region Attacks
    private void AttackTarget()
    {
        if (isPerformingAction)
            return;

        GetNewAttack();

        isPerformingAction = true;
        currentRecoveryTime = currentEnemyAttack.recoveryTime;
        enemyAnimator.PlayTargetAnimation(currentEnemyAttack.actionAnimation, true);
        
        currentEnemyAttack = null;
    }

    private void GetNewAttack()
    {
        if (currentEnemyAttack != null)
            return;

        // Target information
        Transform targetTransform = enemyLocomotion.currentTarget.transform;
        Vector3 targetPosition = targetTransform.position;
        Vector3 directionToTarget = targetPosition - transform.position;

        // Calculate key metrics
        float angleToTarget = Vector3.Angle(directionToTarget, transform.forward);
        float distanceToTarget = Vector3.Distance(targetPosition, transform.position);

        // Update locomotion data
        enemyLocomotion.distanceFromTarget = distanceToTarget;

        // Find valid attacks and their total score
        List<(EnemyAttackAction, int)> validAttacks = new();
        int currentAttackScore = 0;

        foreach (EnemyAttackAction attack in enemyAttacks)
        {
            // Check if attack is valid based on distance and angle
            bool isInRangeDistance = distanceToTarget >= attack.minDistanceRequiredToAttack &&
                                    distanceToTarget <= attack.maxDistanceRequiredToAttack;

            bool isInRangeAngle = angleToTarget >= attack.minAttackAngle &&
                                 angleToTarget <= attack.maxAttackAngle;

            if (isInRangeDistance && isInRangeAngle)
            {
                currentAttackScore += attack.attackScore;
                validAttacks.Add((attack, currentAttackScore));
            }
        }

        // If no valid attacks found, return
        if (validAttacks.Count == 0)
            return;

        // Select a random attack based on weighted scores
        int randomValue = Random.Range(0, currentAttackScore);

        foreach (var (attack, runningScore) in validAttacks)
        {
            if (runningScore > randomValue)
            {
                currentEnemyAttack = attack;
                break;
            }
        }
    }
    #endregion

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
