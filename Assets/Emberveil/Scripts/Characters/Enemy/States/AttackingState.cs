using UnityEngine;

public class AttackingState : IEnemyState
{
    private EnemyManager manager;
    private EnemyAttackActionSO currentAttack;
    //private float animationTimer;

    public void SetAttackAction(EnemyAttackActionSO attack)
    {
        currentAttack = attack;
    }

    public void Enter(EnemyManager manager)
    {
        this.manager = manager;
        if (currentAttack == null)
        {
            Debug.LogError("AttackingState entered without an attack action set!");
            manager.SwitchState(manager.combatStanceState);
            return;
        }

        // Debug.Log($"{manager.name} entered AttackingState with {currentAttack.actionName}.");
        manager.EnemyAnimator.IsInMidAction = true; // Animation starts, enemy is busy
        manager.HasAttackActionConcluded = false;   // Reset for this new attack

        manager.Locomotion.StopMovement();
        manager.Locomotion.DisableAgentNavigation(true);
        if (manager.CurrentTarget != null) manager.Locomotion.FaceTargetInstantly(manager.CurrentTarget.transform);

        manager.Combat.NotifyAttackActionStarted(currentAttack); // This sets the gameplay cooldown
        manager.EnemyAnimator.PlayTargetAnimation(currentAttack.animationName, true);
        // Animation should have AnimEvent_AttackActionConcluded and AnimEvent_FinishAction
    }

    public void Tick() { } // Transitions driven by IsInMidAction and hasAttackActionConcluded

    public void FixedTick() { }

    public IEnemyState Transition()
    {
        if (manager.HasAttackActionConcluded)
        {
            if (manager.canRepositionWhileOnCooldown && manager.Combat.IsAttackOnCooldown && manager.CurrentTarget != null)
            {
                return manager.repositionState;
            }
        }

        if (!manager.EnemyAnimator.IsInMidAction) // Entire animation sequence is complete
        {
            if (manager.CurrentTarget == null)
            {
                return manager.idleState;
            }
            return manager.combatStanceState; // Default back to combat stance to re-evaluate
        }
        return null;
    }

    public void Exit()
    {
        // Ensure IsInMidAction is false if exiting for any reason other than normal animation end.
        // If AnimEvent_FinishAction fired, IsInMidAction is already false.
        // If transitioning to RepositionState while IsInMidAction is still true (visual recovery),
        // then RepositionState.Enter doesn't mess with IsInMidAction, and AttackingState.Exit doesn't need to force it false.
        // The enemy is still "in an action sequence" until the original attack animation's FinishAction.
        // However, for safety if interrupted:
        if(manager.EnemyAnimator.IsInMidAction) manager.EnemyAnimator.IsInMidAction = false; // Or let the new state manage.

        manager.HasAttackActionConcluded = false; // Reset for next time
        currentAttack = null;
        Debug.Log($"{manager.name} exited AttackingState.");
    }
}
