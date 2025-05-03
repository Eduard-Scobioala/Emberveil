using UnityEngine;

public class PatrollingState : EnemyState
{
    private int currentPatrolIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private float detectionCheckCounter = 0f;
    private float detectionCheckInterval = 0.2f;

    public override void EnterState(EnemyManager enemy)
    {
        base.EnterState(enemy);
        currentPatrolIndex = 0;
        isWaiting = false;
        waitTimer = 0f;
        detectionCheckCounter = 0f;
        enemyLocomotion.EnableNavMeshAgent();

        if (enemyManager.patrolPoints.Count > 0)
        {
            enemyLocomotion.SetAgentDestination(enemyManager.patrolPoints[currentPatrolIndex].position);
        }
        else
        {
            Debug.LogWarning($"{enemyManager.gameObject.name} has no patrol points. Switching to IdleState.");
            enemyManager.SwitchState(enemyManager.idleState);
        }
    }

    public override void UpdateState()
    {
        // Detection check
        detectionCheckCounter += Time.deltaTime;
        if (detectionCheckCounter >= detectionCheckInterval)
        {
            enemyLocomotion.FindAndVerifyTarget();
            detectionCheckCounter = 0f;
            CheckStateTransitions();
        }

        // Patrol logic
        if (enemyManager.patrolPoints.Count == 0) return;

        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isWaiting = false;
                currentPatrolIndex = (currentPatrolIndex + 1) % enemyManager.patrolPoints.Count;
                enemyLocomotion.SetAgentDestination(enemyManager.patrolPoints[currentPatrolIndex].position);
            }
        }
        else
        {
            if (enemyLocomotion.navMeshAgent.remainingDistance <= enemyLocomotion.navMeshAgent.stoppingDistance && !enemyLocomotion.navMeshAgent.pathPending)
            {
                isWaiting = true;
                waitTimer = enemyManager.patrolWaitTime;
            }
        }

        // Animation
        if (enemyLocomotion.IsAgentEnabled())
        {
            float moveAmount = enemyLocomotion.navMeshAgent.desiredVelocity.magnitude > 0.1f ? 1 : 0;
            enemyAnimator.anim.SetFloat("Vertical", moveAmount, 0.1f, Time.deltaTime);
        }
        else
        {
            enemyAnimator.anim.SetFloat("Vertical", 0, 0.1f, Time.deltaTime);
        }
    }

    public override void FixedUpdateState()
    {
        // No specific fixed update logic needed
    }

    public override void ExitState()
    {
        // No specific cleanup needed
    }

    public override void CheckStateTransitions()
    {
        if (enemyLocomotion.currentTarget != null)
        {
            enemyManager.SwitchState(enemyManager.chaseState);
        }
    }
}