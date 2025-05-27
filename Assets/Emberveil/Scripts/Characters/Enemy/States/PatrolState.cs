using UnityEngine;

public class PatrolState : IEnemyState
{
    private EnemyManager manager;
    private int currentPatrolPointIndex = 0;
    private float waitTimer;
    private bool isWaitingAtPoint = false;

    [Header("Patrol Settings (Example - better to get from manager/patrolPath)")]
    private float patrolWaitTime = 3f; // Time to wait at each patrol point

    public void Enter(EnemyManager manager)
    {
        this.manager = manager;
        manager.isPerformingNonCriticalAction = false;

        if (manager.patrolRoute == null || manager.patrolRoute.patrolPoints.Count == 0)
        {
            Debug.LogWarning($"{manager.name} tried to enter PatrolState but has no patrol path. Switching to Idle.");
            // No immediate switch here, let Transition handle it to avoid recursion if Idle also tries to go to Patrol.
            return;
        }

        manager.Locomotion.EnableAgentNavigation();
        manager.Locomotion.SetAgentSpeed(manager.Locomotion.baseSpeed); // Normal patrol speed
        currentPatrolPointIndex = GetClosestPatrolPointIndex(); // Start at the closest point
        SetNextDestination();
        isWaitingAtPoint = false;
        waitTimer = 0f;
        Debug.Log($"{manager.name} entered PatrolState.");
    }

    public void Tick()
    {
        manager.Senses.TickSenses(); // Keep an eye out for targets

        if (isWaitingAtPoint)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isWaitingAtPoint = false;
                AdvancePatrolPoint();
                SetNextDestination();
            }
        }
        else
        {
            // Check if reached destination
            if (manager.Locomotion.HasReachedDestination)
            {
                isWaitingAtPoint = true;
                waitTimer = manager.patrolRoute.patrolWaitTime;
                manager.EnemyAnimator.SetMovementValues(0, 0); // Stop animation
                // Optional: Play a specific "waiting" or "looking around" animation
            }
        }
    }

    public void FixedTick()
    {
        // Locomotion is handled by NavMeshAgent, animator params updated in EnemyManager.Update()
    }

    private void SetNextDestination()
    {
        if (manager.patrolRoute != null && manager.patrolRoute.patrolPoints.Count > 0)
        {
            Transform targetPoint = manager.patrolRoute.patrolPoints[currentPatrolPointIndex];
            if (targetPoint != null)
            {
                manager.Locomotion.MoveToPoint(targetPoint.position);
                // Debug.Log($"{manager.name} patrolling to point {currentPatrolPointIndex}: {targetPoint.name}");
            }
            else
            {
                Debug.LogWarning($"{manager.name} patrol point {currentPatrolPointIndex} is null. Advancing.");
                AdvancePatrolPoint(); // Skip null point
                SetNextDestination();
            }
        }
    }

    private void AdvancePatrolPoint()
    {
        if (manager.patrolRoute == null || manager.patrolRoute.patrolPoints.Count == 0) return;

        currentPatrolPointIndex = (currentPatrolPointIndex + 1) % manager.patrolRoute.patrolPoints.Count;
    }

    private int GetClosestPatrolPointIndex()
    {
        if (manager.patrolRoute == null || manager.patrolRoute.patrolPoints.Count == 0) return 0;

        int closestIndex = 0;
        float minDistance = float.MaxValue;

        for (int i = 0; i < manager.patrolRoute.patrolPoints.Count; i++)
        {
            if (manager.patrolRoute.patrolPoints[i] == null) continue;

            float distance = Vector3.Distance(manager.transform.position, manager.patrolRoute.patrolPoints[i].position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }
        return closestIndex;
    }

    public IEnemyState Transition()
    {
        if (manager.patrolRoute == null || manager.patrolRoute.patrolPoints.Count == 0)
        {
            return manager.idleState; // No path, go idle
        }

        if (manager.CurrentTarget != null)
        {
            return manager.chaseState;
        }
        return null;
    }

    public void Exit()
    {
        // Stop movement if transitioning out abruptly
        if (!isWaitingAtPoint) manager.Locomotion.StopMovement();
    }
}
