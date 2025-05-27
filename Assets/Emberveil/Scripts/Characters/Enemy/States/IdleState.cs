using UnityEngine;

public class IdleState : IEnemyState
{
    private EnemyManager manager;
    private float idleTimer;
    private readonly float minIdleTime = 2f;
    private readonly float maxIdleTime = 5f;

    public void Enter(EnemyManager manager)
    {
        this.manager = manager;
        manager.Locomotion.StopMovement();
        manager.Locomotion.DisableAgentNavigation(true); // Ensure non-kinematic for idle physics if any
        manager.EnemyAnimator.SetMovementValues(0, 0);
        manager.EnemyAnimator.PlayTargetAnimation("Idle", false);
        idleTimer = Random.Range(minIdleTime, maxIdleTime);
        manager.isPerformingNonCriticalAction = false;
        Debug.Log($"{manager.name} entered IdleState.");
    }

    public void Tick()
    {
        manager.Senses.TickSenses(); // Keep looking for targets

        idleTimer -= Time.deltaTime;
    }

    public void FixedTick() { }

    public IEnemyState Transition()
    {
        if (manager.CurrentTarget != null)
        {
            return manager.chaseState;
        }
        if (idleTimer <= 0 && manager.patrolRoute != null && manager.patrolRoute.patrolPoints.Count > 0)
        {
            return manager.patrolState;
        }
        return null;
    }

    public void Exit() { }
}
