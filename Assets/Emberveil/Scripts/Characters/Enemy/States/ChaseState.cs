using UnityEngine;

public class ChaseState : IEnemyState
{
    private EnemyManager manager;

    public void Enter(EnemyManager manager)
    {
        this.manager = manager;
        manager.Locomotion.EnableAgentNavigation();
        manager.Locomotion.SetAgentSpeed(manager.Locomotion.chaseSpeed);
        Debug.Log($"{manager.name} entered ChaseState, chasing {manager.CurrentTarget?.name}.");
        manager.isPerformingNonCriticalAction = false;
    }

    public void Tick()
    {
        manager.Senses.TickSenses(); // Continuously update target status
    }

    public void FixedTick()
    {
        if (manager.CurrentTarget != null)
        {
            manager.Locomotion.FollowTarget(manager.CurrentTarget.transform);
            manager.Locomotion.RotateTowards(manager.CurrentTarget.transform.position, manager.Locomotion.rotationSpeed);
        }
    }

    public IEnemyState Transition()
    {
        if (manager.CurrentTarget == null)
        {
            return manager.returnToPostState;
        }

        float distanceToTarget = Vector3.Distance(manager.transform.position, manager.CurrentTarget.transform.position);
        if (distanceToTarget <= manager.defaultStoppingDistance)
        {
            return manager.combatStanceState;
        }
        return null;
    }

    public void Exit()
    {
        manager.Locomotion.StopMovement(); // Stop if transitioning out abruptly
    }
}
