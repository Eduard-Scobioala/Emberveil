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

        // Make sure agent is disabled and RB is non-kinematic on entering Idle
        enemyLocomotion.DisableNavMeshAgent();

        // Capture start position ONLY if it hasn't been set yet
        if (startPosition == Vector3.zero) // Use a better check? Maybe a bool flag?
        {
            startPosition = enemy.transform.position;
            Debug.Log($"{enemyManager.gameObject.name} setting start position: {startPosition}");
        }
        returningToStart = false;

        // Check immediately if we need to return
        HandleReturnToStartLogic();
    }

    public override void UpdateState()
    {
        if (returningToStart)
        {
            // Rely on agent's path completion or proximity check
            if (enemyLocomotion.IsAgentEnabled() && enemyLocomotion.navMeshAgent.isOnNavMesh)
            {
                if (enemyLocomotion.navMeshAgent.remainingDistance <= enemyLocomotion.navMeshAgent.stoppingDistance && !enemyLocomotion.navMeshAgent.pathPending)
                {
                    Debug.Log($"{enemyManager.gameObject.name} reached start position via agent.");
                    returningToStart = false;
                    enemyLocomotion.DisableNavMeshAgent(); // Stop agent, make RB non-kinematic
                    enemyAnimator.anim.SetFloat("Vertical", 0);
                }
                else
                {
                    // Still moving - update animation based on agent velocity
                    enemyAnimator.anim.SetFloat("Vertical", enemyLocomotion.navMeshAgent.desiredVelocity.magnitude > 0.1f ? 1 : 0, 0.1f, Time.deltaTime);
                }
            }
            else if (!enemyLocomotion.IsAgentEnabled()) // Agent got disabled somehow? Try re-enabling.
            {
                Debug.LogWarning($"{enemyManager.gameObject.name} agent disabled while returning to start. Re-enabling.");
                HandleReturnToStartLogic(); // Re-initiate return
            }
            // Maybe add a fallback distance check just in case agent gets stuck?
            float distToStartFallback = Vector3.Distance(enemyManager.transform.position, startPosition);
            if (distToStartFallback <= returnStoppingDistance)
            {
                Debug.Log($"{enemyManager.gameObject.name} reached start position via fallback distance check.");
                returningToStart = false;
                enemyLocomotion.DisableNavMeshAgent();
                enemyAnimator.anim.SetFloat("Vertical", 0);
            }

            return; // Skip detection while returning
        }

        // Default Idle Behavior: Look for Target
        detectionCheckCounter += Time.deltaTime;
        if (detectionCheckCounter >= detectionCheckInterval)
        {
            enemyLocomotion.FindAndVerifyTarget();
            detectionCheckCounter = 0f;
            CheckStateTransitions(); // Check transitions AFTER finding target
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
            returningToStart = false;
            enemyLocomotion.DisableNavMeshAgent();
            enemyAnimator.anim.SetFloat("Vertical", 0);
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
        // Check distance before initiating return
        float distCheck = Vector3.Distance(enemyManager.transform.position, startPosition);
        if (!returningToStart && distCheck > returnStoppingDistance)
        {
            Debug.Log($"{enemyManager.gameObject.name} initiating return to start. Distance: {distCheck}");
            returningToStart = true;
            enemyLocomotion.EnableNavMeshAgent(); // Enable agent, make RB kinematic
            enemyLocomotion.SetAgentDestination(startPosition);
            // Animation set in Update based on agent velocity
            // enemyAnimator.anim.SetFloat("Vertical", 1, 0.1f, Time.deltaTime); // Less ideal than using velocity
        }
        else if (!returningToStart)
        {
            // Already at start or close enough, ensure agent is disabled
            enemyLocomotion.DisableNavMeshAgent();
        }
    }
}
