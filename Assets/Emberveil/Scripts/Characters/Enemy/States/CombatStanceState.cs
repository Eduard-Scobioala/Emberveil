using UnityEngine;

public class CombatStanceState : IEnemyState
{
    private EnemyManager manager;
    private float _timeSpentInStance = 0f;
    private const float MaxWaitTimeInStanceOnCooldown = 2.0f;

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

        // Reset decision flags
        _wantsToBackstab = false;
        _selectedAttackAction = null;
        _timeSpentInStance = 0f;

        Debug.Log($"{manager.name} entered CombatStanceState against {manager.CurrentTarget?.name}.");
    }

    public void Tick()
    {
        manager.Senses.TickSenses();
        if (manager.CurrentTarget == null) return;

        manager.Locomotion.RotateTowards(manager.CurrentTarget.transform.position);

        _selectedAttackAction = null; // Reset decision each tick
        _wantsToBackstab = false;

        if (manager.Combat.IsAttackOnCooldown)
        {
            _timeSpentInStance += Time.deltaTime;
        }
        else // Not on cooldown, try to select an action
        {
            _timeSpentInStance = 0f;
            // Priority 1: Backstab
            if (manager.CurrentTarget is PlayerManager playerTarget &&
                manager.Combat.CanAttemptBackstab(playerTarget) &&
                Random.value < manager.Combat.chanceToAttemptBackstab)
            {
                _wantsToBackstab = true;
            }

            // Priority 2: Regular Attack
            if (!_wantsToBackstab)
            {
                _selectedAttackAction = manager.Combat.GetAvailableAttack(manager.CurrentTarget);
            }
        }
    }

    public void FixedTick()
    {
        // If NavMeshAgent is enabled for minor adjustments, ensure it's not causing unwanted sliding.
        // manager.Locomotion.StopMovement(); // Or implement slight strafing logic here if desired for stance
    }

    public IEnemyState Transition()
    {
        if (manager.CurrentTarget == null)
        {
            return manager.returnToPostState;
        }

        float distanceToTarget = Vector3.Distance(manager.transform.position, manager.CurrentTarget.transform.position);
        if (distanceToTarget > manager.defaultStoppingDistance * 1.2f) // Target moved too far
        {
            return manager.chaseState;
        }

        // If an attack action was chosen (and not on cooldown)
        if (_selectedAttackAction != null)
        {
            manager.attackingState.SetAttackAction(_selectedAttackAction);
            return manager.attackingState;
        }
        if (_wantsToBackstab && manager.CurrentTarget is PlayerManager playerForBackstab)
        {
            manager.Combat.InitiateBackstabSequence(playerForBackstab);
            return manager.performingBackstabState;
        }

        // If attack is on cooldown AND repositioning is enabled AND waited long enough in stance
        if (manager.Combat.IsAttackOnCooldown && manager.canRepositionWhileOnCooldown && _timeSpentInStance >= MaxWaitTimeInStanceOnCooldown)
        {
            return manager.repositionState;
        }

        return null; // Stay in CombatStanceState
    }

    public void Exit()
    {
        var agent = manager.Locomotion.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.enabled)
        {
            agent.updateRotation = true; // Restore agent rotation control
        }
        Debug.Log($"{manager.name} exited CombatStanceState.");
    }
}
