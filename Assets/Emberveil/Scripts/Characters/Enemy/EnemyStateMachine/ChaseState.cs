using UnityEngine;

public class ChaseState : EnemyState
{
    [SerializeField] private float timeUntilTargetLoss = 5f;
    private float targetLossTimer = 0f;

    public override void EnterState(EnemyManager enemy)
    {
        base.EnterState(enemy);
        Debug.Log($"{enemy.gameObject.name} entered CHASE state");

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
    }

    public override void ExitState()
    {
        enemyLocomotion.DisableNavMeshAgent();
        enemyAnimator.anim.SetFloat("Vertical", 0, 0.1f, Time.deltaTime);
        targetLossTimer = 0f;
    }

    public override void CheckStateTransitions() // Called from Update
    {
        // Condition 1: Target Lost
        if (enemyLocomotion.currentTarget == null || targetLossTimer >= timeUntilTargetLoss)
        {
            if (enemyLocomotion.currentTarget != null && targetLossTimer >= timeUntilTargetLoss)
            {
                Debug.Log($"{enemyManager.gameObject.name} lost target: Timer expired. Returning to default state.");
            }
            enemyLocomotion.currentTarget = null;
            enemyManager.SwitchState(enemyManager.defaultState);
            return;
        }

        // Condition 2: Target valid, check combat range
        // distanceFromTarget is updated within HandleMoveToTarget, but we might need it here too
        enemyLocomotion.distanceFromTarget = Vector3.Distance(
               enemyLocomotion.currentTarget.transform.position,
               enemyManager.transform.position);

        // Use the NavMeshAgent's stopping distance for consistency
        if (enemyLocomotion.distanceFromTarget <= enemyLocomotion.navMeshAgent.stoppingDistance)
        {
            enemyManager.SwitchState(enemyManager.combatState);
        }
        // Else: Continue chasing (handled by FixedUpdateState calling HandleMoveToTarget)
    }
}
