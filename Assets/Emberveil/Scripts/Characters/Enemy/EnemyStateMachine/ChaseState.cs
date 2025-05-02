using UnityEngine;

public class ChaseState : EnemyState
{
    [SerializeField] private float timeUntilTargetLoss = 5f;
    private float targetLossTimer = 0f;

    public override void EnterState(EnemyManager enemy)
    {
        base.EnterState(enemy);
        Debug.Log($"{enemy.gameObject.name} entered CHASE state");

        enemyAnimator.anim.SetFloat("Vertical", 1, 0.1f, Time.deltaTime);
        enemyLocomotion.EnableNavMeshAgent();
        targetLossTimer = 0f;
    }

    public override void UpdateState()
    {
        // Continuously verify the target (checks distance, LoS indirectly via timer)
        if (enemyLocomotion.currentTarget != null)
        {
            // Check if Line of Sight is maintained
            if (enemyLocomotion.HasLineOfSightToTarget())
            {
                targetLossTimer = 0f; // Reset timer if target is visible
            }
            else
            {
                // Increment timer if LoS is broken
                targetLossTimer += Time.deltaTime;
            }

            // Also check max distance contingency
            float dist = Vector3.Distance(enemyLocomotion.currentTarget.transform.position, enemyManager.transform.position);
            if (dist > enemyLocomotion.maxChaseDistance)
            {
                Debug.Log($"{enemyManager.gameObject.name} lost target during chase: Exceeded max distance.");
                enemyLocomotion.currentTarget = null; // Lose target immediately if too far
            }
        }

        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        if (enemyLocomotion.currentTarget != null && !enemyManager.isPerformingAction)
        {
            enemyLocomotion.HandleMoveToTarget();
        }
        else
        {
            // If target became null (e.g., from UpdateState checks), switch to idle
            if (enemyLocomotion.currentTarget == null)
            {
                enemyManager.SwitchState(enemyManager.idleState);
            }
        }
    }

    public override void ExitState()
    {
        enemyAnimator.anim.SetFloat("Vertical", 0, 0.1f, Time.deltaTime);
        enemyLocomotion.DisableNavMeshAgent();
        targetLossTimer = 0f;
    }

    public override void CheckStateTransitions()
    {
        // Condition 1: Target Lost (Timer expired or became null)
        if (enemyLocomotion.currentTarget == null || targetLossTimer >= timeUntilTargetLoss)
        {
            if (enemyLocomotion.currentTarget != null)
            {
                Debug.Log($"{enemyManager.gameObject.name} lost target: Timer expired ({targetLossTimer}s >= {timeUntilTargetLoss}s). Returning to Idle.");
            }
            enemyLocomotion.currentTarget = null;
            enemyManager.SwitchState(enemyManager.idleState);
            return;
        }

        // Condition 2: Target still valid, check for combat range
        if (enemyLocomotion.currentTarget != null)
        {
            enemyLocomotion.distanceFromTarget = Vector3.Distance(
                enemyLocomotion.currentTarget.transform.position,
                enemyManager.transform.position);

            // If close enough, switch to combat
            if (enemyLocomotion.distanceFromTarget <= enemyLocomotion.stoppingDistance)
            {
                enemyManager.SwitchState(enemyManager.combatState);
            }
            // Else: Continue chasing (handled by FixedUpdateState)
        }
    }
}
