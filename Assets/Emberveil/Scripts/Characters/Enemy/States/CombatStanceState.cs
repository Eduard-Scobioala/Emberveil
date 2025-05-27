using UnityEngine;

public class CombatStanceState : IEnemyState
{
    private EnemyManager manager;
    private float maxRepositionTime = 2f; // Time to try repositioning before re-evaluating
    private float repositionTimer;

    public void Enter(EnemyManager manager)
    {
        this.manager = manager;
        manager.Locomotion.StopMovement();
        // Keep agent enabled for potential strafing/repositioning, but don't let it auto-rotate
        manager.Locomotion.EnableAgentNavigation();
        manager.Locomotion.GetComponent<UnityEngine.AI.NavMeshAgent>().updateRotation = false;
        manager.Locomotion.SetAgentSpeed(manager.Locomotion.baseSpeed * 0.5f); // Slower speed for combat stance
        repositionTimer = maxRepositionTime;
        manager.isPerformingNonCriticalAction = false;
        Debug.Log($"{manager.name} entered CombatStanceState against {manager.CurrentTarget?.name}.");
    }

    public void Tick()
    {
        manager.Senses.TickSenses();
        if (manager.CurrentTarget == null) return;

        manager.Locomotion.RotateTowards(manager.CurrentTarget.transform.position);

        if (!manager.Combat.isAttackOnCooldown)
        {
            // Try backstab first if conditions are met
            if (manager.CurrentTarget is PlayerManager playerTarget &&
                manager.Combat.CanAttemptBackstab(playerTarget) &&
                Random.value < manager.Combat.chanceToAttemptBackstab)
            {
                // Transition will be handled by returning performingBackstabState
                return;
            }

            // Then try a regular attack
            EnemyAttackActionSO attack = manager.Combat.GetAvailableAttack(manager.CurrentTarget);
            if (attack != null)
            {
                manager.attackingState.SetAttackAction(attack);
                // Transition will be handled by returning attackingState
                return;
            }
        }

        repositionTimer -= Time.deltaTime;
    }

    public void FixedTick()
    {
        // TODO: Implement subtle strafing or repositioning logic if needed
        // manager.Locomotion.StrafeAroundTarget(manager.CurrentTarget);
        // For now, just stand and rotate.
        manager.Locomotion.StopMovement(); // Ensure no sliding if agent velocity was non-zero
    }

    public IEnemyState Transition()
    {
        if (manager.CurrentTarget == null)
        {
            return manager.idleState;
        }

        float distanceToTarget = Vector3.Distance(manager.transform.position, manager.CurrentTarget.transform.position);
        if (distanceToTarget > manager.defaultStoppingDistance * 1.2f) // Target moved away
        {
            return manager.chaseState;
        }

        // If conditions for backstab are met (checked in Tick)
        if (!manager.Combat.isAttackOnCooldown &&
            manager.CurrentTarget is PlayerManager playerTarget &&
            manager.Combat.CanAttemptBackstab(playerTarget) &&
            Random.value < manager.Combat.chanceToAttemptBackstab) // Re-check random for transition
        {
            manager.Combat.AttemptBackstab(playerTarget); // This will set up the manager
            return manager.performingBackstabState;
        }

        // If an attack is ready (checked in Tick)
        if (!manager.Combat.isAttackOnCooldown)
        {
            EnemyAttackActionSO attack = manager.Combat.GetAvailableAttack(manager.CurrentTarget);
            if (attack != null)
            {
                manager.attackingState.SetAttackAction(attack);
                return manager.attackingState;
            }
        }

        if (repositionTimer <= 0) // Failed to find an action, try chasing again briefly
        {
            return manager.chaseState;
        }

        return null;
    }

    public void Exit()
    {
        manager.Locomotion.GetComponent<UnityEngine.AI.NavMeshAgent>().updateRotation = true; // Restore agent rotation
    }
}
