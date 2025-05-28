using UnityEngine;

public class ChaseState : IEnemyState
{
    private EnemyManager manager;

    public void Enter(EnemyManager manager)
    {
        this.manager = manager;
        manager.Locomotion.EnableAgentNavigation();
        manager.Locomotion.SetAgentSpeed(manager.Locomotion.baseSpeed * 1.5f); // Chase speed
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
        }
    }

    public IEnemyState Transition()
    {
        if (manager.CurrentTarget == null)
        {
            // TODO: Could go to an "InvestigateLastKnownPosition" state
            return manager.idleState; // Or patrolState if applicable
        }

        float distanceToTarget = Vector3.Distance(manager.transform.position, manager.CurrentTarget.transform.position);
        if (distanceToTarget <= manager.defaultStoppingDistance) // Check against NavMeshAgent's stopping distance
        {
            // Check if backstab is possible before committing to general combat
            if (manager.CurrentTarget is PlayerManager playerTarget &&
                manager.Combat.CanAttemptBackstab(playerTarget) &&
                Random.value < manager.Combat.chanceToAttemptBackstab)
            {
                return manager.performingBackstabState;
            }
            return manager.combatStanceState;
        }
        return null;
    }

    public void Exit()
    {
        manager.Locomotion.StopMovement(); // Stop if transitioning out abruptly
    }
}
