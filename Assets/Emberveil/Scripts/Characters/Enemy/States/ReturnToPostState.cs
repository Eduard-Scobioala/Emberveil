using UnityEngine;

public class ReturnToPostState : IEnemyState
{
    private EnemyManager manager;
    private float repathTimer;
    private const float repathInterval = 1.0f; // How often to re-check path if stuck

    public void Enter(EnemyManager manager)
    {
        this.manager = manager;
        Debug.Log($"{manager.name} entered ReturnToPostState, returning to {manager.initialPosition}.");

        manager.isPerformingNonCriticalAction = false;
        manager.CurrentTarget = null; // Ensure no target is being tracked
        manager.Senses.ForceLoseTarget(); // Explicitly clear senses

        manager.Locomotion.EnableAgentNavigation();
        manager.Locomotion.SetAgentSpeed(manager.Locomotion.baseSpeed * 0.6f); // Slightly slower than normal walk
        manager.Locomotion.MoveToPoint(manager.initialPosition);
        repathTimer = repathInterval;
    }

    public void Tick()
    {
        // If senses pick up a target again while returning, transition immediately
        manager.Senses.TickSenses();

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0)
        {
            // Periodically re-issue the move command in case the agent got stuck or path invalidated
            if (!manager.Locomotion.HasReachedDestination)
            {
                manager.Locomotion.MoveToPoint(manager.initialPosition);
            }
            repathTimer = repathInterval;
        }
    }

    public void FixedTick()
    {
        // Rotation is handled by NavMeshAgent as it moves back
        // Or, if agent.updateRotation is false, you might want to rotate towards initialRotation when very close
        if (manager.Locomotion.HasReachedDestination)
        {
            // Smoothly orient to initial rotation
            manager.transform.rotation = Quaternion.Slerp(
                manager.transform.rotation,
                manager.initialRotation,
                manager.Locomotion.rotationSpeed * Time.fixedDeltaTime * 0.5f // Slower final orientation
            );
        }
    }

    public IEnemyState Transition()
    {
        // Priority 1: If a target is spotted while returning, go back to chasing
        if (manager.CurrentTarget != null)
        {
            return manager.chaseState;
        }

        // Priority 2: Reached destination
        if (manager.Locomotion.HasReachedDestination)
        {
            // Check if rotation is close enough to initialRotation
            if (Quaternion.Angle(manager.transform.rotation, manager.initialRotation) < 5.0f) // Threshold for orientation
            {
                Debug.Log($"{manager.name} has returned to post and oriented.");
                // Decide what to do next: Idle or Patrol
                if (manager.patrolRoute != null && manager.patrolRoute.patrolPoints.Count > 0)
                {
                    return manager.patrolState;
                }
                return manager.idleState;
            }
        }
        return null;
    }

    public void Exit()
    {
        // Ensure NavMeshAgent is stopped if transitioning out for another reason (like spotting target)
        if (manager.CurrentTarget != null) // Exiting because we spotted a target
        {
            manager.Locomotion.StopMovement();
        }
        // Agent speed and updateRotation will be set by the next state.
    }
}
