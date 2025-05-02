using UnityEngine;

public class IdleState : EnemyState
{
    private float detectionCheckCounter = 0f;
    private float detectionCheckInterval = 0.2f;

    // Settings returning to start position
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private bool returningToStart = false;
    [SerializeField] private float returnStoppingDistance = 0.5f;

    public override void EnterState(EnemyManager enemy)
    {
        base.EnterState(enemy);
        enemyAnimator.PlayTargetAnimation("Idle", true);
        Debug.Log($"{enemy.gameObject.name} entered IDLE state");

        base.EnterState(enemy);
        Debug.Log($"{enemy.gameObject.name} entered IDLE state");
        enemyAnimator.PlayTargetAnimation("Idle", true);
        enemyLocomotion.DisableNavMeshAgent();

        if (startPosition == Vector3.zero)
        {
            startPosition = enemy.transform.position;
        }
        returningToStart = false;

        // Check if far from start and need to return
        HandleReturnToStartLogic();
    }

    public override void UpdateState()
    {
        if (returningToStart)
        {
            float distToStart = Vector3.Distance(enemyManager.transform.position, startPosition);
            if (distToStart <= returnStoppingDistance)
            {
                Debug.Log($"{enemyManager.gameObject.name} returned to start position.");
                returningToStart = false;
                enemyLocomotion.DisableNavMeshAgent();
                enemyAnimator.anim.SetFloat("Vertical", 0); // Stop moving anim
            }
            else
            {
                // Continue moving towards start
                if (enemyLocomotion.IsAgentEnabled())
                {
                    enemyLocomotion.SetAgentDestination(startPosition);
                }
                else // Agent got disabled, re-enable
                {
                    HandleReturnToStartLogic();
                }

            }
            return; // Skip detection while returning
        }


        // Default Idle Behavior: Look for Target
        detectionCheckCounter += Time.deltaTime;

        if (detectionCheckCounter >= detectionCheckInterval)
        {
            enemyLocomotion.FindAndVerifyTarget();
            detectionCheckCounter = 0f;

            CheckStateTransitions();
        }
    }

    public override void FixedUpdateState()
    {
        if (returningToStart && enemyLocomotion.IsAgentEnabled())
        {
            enemyLocomotion.HandleRotateTowardsPosition(startPosition);
        }
    }

    public override void ExitState()
    {
        if (returningToStart) // If exiting while returning, stop the agent
        {
            enemyLocomotion.DisableNavMeshAgent();
            enemyAnimator.anim.SetFloat("Vertical", 0);
            returningToStart = false;
        }
    }

    public override void CheckStateTransitions()
    {
        if (returningToStart) return; // Don't transition if returning

        if (enemyLocomotion.currentTarget != null)
        {
            enemyManager.SwitchState(enemyManager.chaseState);
        }
        else if (Vector3.Distance(enemyManager.transform.position, startPosition) > returnStoppingDistance * 2f) // If idle and far from start
        {
            HandleReturnToStartLogic();
        }
    }

    void HandleReturnToStartLogic()
    {
        if (!returningToStart && Vector3.Distance(enemyManager.transform.position, startPosition) > returnStoppingDistance)
        {
            Debug.Log($"{enemyManager.gameObject.name} is returning to start position.");
            returningToStart = true;
            enemyLocomotion.EnableNavMeshAgent();
            enemyLocomotion.SetAgentDestination(startPosition);
            enemyAnimator.anim.SetFloat("Vertical", 1, 0.1f, Time.deltaTime);
        }
    }
}
