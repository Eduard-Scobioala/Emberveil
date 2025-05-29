using UnityEngine;

public class CombatStanceState : IEnemyState
{
    private EnemyManager manager;
    private float maxRepositionTime = .1f; // Time to try repositioning before re-evaluating
    private float repositionTimer;

    // Internal flags to store decisions made in Tick()
    private bool _wantsToBackstab = false;
    private EnemyAttackActionSO _selectedAttackAction = null;

    public void Enter(EnemyManager manager)
    {
        this.manager = manager;
        manager.Locomotion.StopMovement();
        manager.Locomotion.EnableAgentNavigation();
        manager.Locomotion.GetComponent<UnityEngine.AI.NavMeshAgent>().updateRotation = false; // Manual rotation
        manager.Locomotion.SetAgentSpeed(manager.Locomotion.baseSpeed * 0.5f);

        repositionTimer = maxRepositionTime;

        // Reset decision flags
        _wantsToBackstab = false;
        _selectedAttackAction = null;

        Debug.Log($"{manager.name} entered CombatStanceState against {manager.CurrentTarget?.name}.");
    }

    public void Tick()
    {
        manager.Senses.TickSenses();
        if (manager.CurrentTarget == null)
        {
            _wantsToBackstab = false;
            _selectedAttackAction = null;
            return;
        }

        manager.Locomotion.RotateTowards(manager.CurrentTarget.transform.position);

        // Reset decisions each tick before re-evaluating
        _wantsToBackstab = false;
        _selectedAttackAction = null;

        if (!manager.Combat.IsAttackOnCooldown) // Only consider actions if not on general cooldown
        {
            // Priority 1: Backstab
            if (manager.CurrentTarget is PlayerManager playerTarget &&
                manager.Combat.CanAttemptBackstab(playerTarget) &&
                Random.value < manager.Combat.chanceToAttemptBackstab)
            {
                _wantsToBackstab = true;
            }

            // Priority 2: Regular Attack
            if (!_wantsToBackstab) // Ensure we don't select a normal attack if backstab was chosen
            {
                _selectedAttackAction = manager.Combat.GetAvailableAttack(manager.CurrentTarget);
                if (_selectedAttackAction != null)
                {
                    // Decision made to attack. Transition() will see _selectedAttackAction.
                }
            }
        }

        // If no action was decided and we're not on cooldown, tick down reposition timer
        if (!_wantsToBackstab && _selectedAttackAction == null && !manager.Combat.IsAttackOnCooldown)
        {
            repositionTimer -= Time.deltaTime;
        }
        else if (_wantsToBackstab || _selectedAttackAction != null)
        {
            // If an action is chosen, reset reposition timer so we don't immediately
            // reposition if the action leads back to CombatStanceState quickly.
            repositionTimer = maxRepositionTime;
        }
    }

    public void FixedTick()
    {
        // If agent is enabled and we're not actively trying to move for an attack,
        // ensure it's not sliding due to residual velocity.
        if (manager.Locomotion.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled)
        {
            manager.Locomotion.StopMovement(); // Or implement actual strafing/repositioning logic here
        }
    }

    public IEnemyState Transition()
    {
        // Target lost
        if (manager.CurrentTarget == null)
        {
            return manager.returnToPostState;
        }

        // Target moved too far away
        float distanceToTarget = Vector3.Distance(manager.transform.position, manager.CurrentTarget.transform.position);
        if (distanceToTarget > manager.defaultStoppingDistance * 1.2f) // Check a slightly larger range than stopping distance
        {
            return manager.chaseState;
        }

        // Wants to Backstab
        if (_wantsToBackstab)
        {
            // Ensure CurrentTarget is still valid and a PlayerManager before committing
            if (manager.CurrentTarget is PlayerManager playerForBackstab)
            {
                manager.Combat.InitiateBackstabSequence(playerForBackstab);
                _wantsToBackstab = false; // Consume the intention
                return manager.performingBackstabState;
            }
            else
            {
                _wantsToBackstab = false; // Target changed or invalid, clear intention
            }
        }

        // Selected a Regular Attack
        if (_selectedAttackAction != null)
        {
            manager.attackingState.SetAttackAction(_selectedAttackAction);
            _selectedAttackAction = null; // Consume the intention
            return manager.attackingState;
        }

        // Reposition timer expired, and no action was chosen
        if (repositionTimer <= 0)
        {
            return manager.chaseState;
        }

        // No transition conditions met, remain in CombatStanceState
        return null;
    }

    public void Exit()
    {
        if (manager.Locomotion.GetComponent<UnityEngine.AI.NavMeshAgent>() != null)
        {
            manager.Locomotion.GetComponent<UnityEngine.AI.NavMeshAgent>().updateRotation = true;
        }
    }
}
