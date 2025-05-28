using UnityEngine;

public class AttackingState : IEnemyState
{
    private EnemyManager manager;
    private EnemyAttackActionSO currentAttack;
    private float animationTimer;

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
            // Force transition back to combat stance to re-evaluate
            // This is a failsafe, should ideally not happen.
            manager.SwitchState(manager.combatStanceState);
            return;
        }

        Debug.Log($"{manager.name} entered AttackingState with {currentAttack.actionName}.");
        manager.isPerformingNonCriticalAction = true;
        manager.Locomotion.StopMovement();
        manager.Locomotion.DisableAgentNavigation(true); // Non-kinematic for root motion
        if (manager.CurrentTarget != null) manager.Locomotion.FaceTargetInstantly(manager.CurrentTarget.transform);

        manager.EnemyAnimator.PlayTargetAnimation(currentAttack.animationName, true);
        animationTimer = 0f; // Timer to track animation progress for manual collider control if needed

        // Set cooldown now, so even if interrupted, it's on cooldown
        manager.Combat.SetSpecificCooldown(currentAttack.recoveryTime);
    }

    public void Tick()
    {
        animationTimer += Time.deltaTime;

        // The state finishes when isPerformingNonCriticalAction is false,
        // which should be set by an animation event or after recoveryTime.
        // For now, we'll use a simple timer based on recovery time as a fallback.
        // Ideally, an animation event "AnimEvent_FinishAction" would set isPerformingNonCriticalAction to false.
        if (animationTimer >= currentAttack.recoveryTime) // Fallback if no anim event
        {
            manager.isPerformingNonCriticalAction = false;
        }
    }

    public void FixedTick() { }

    public IEnemyState Transition()
    {
        if (!manager.isPerformingNonCriticalAction) // Action is considered finished
        {
            if (manager.CurrentTarget == null) return manager.idleState;

            // Check distance to decide next state
            float distanceToTarget = Vector3.Distance(manager.transform.position, manager.CurrentTarget.transform.position);
            if (distanceToTarget <= manager.defaultStoppingDistance * 1.1f) // Close enough for another attack/stance
            {
                return manager.combatStanceState;
            }
            else // Target moved away
            {
                return manager.chaseState;
            }
        }
        return null;
    }

    public void Exit()
    {
        manager.isPerformingNonCriticalAction = false;
        // Ensure collider is closed if it wasn't by timer/event
        // if (damageColliderOpened && !damageColliderClosed) manager.Combat.DisableWeaponCollider(WeaponHand.Right);
        currentAttack = null; // Clear the attack
    }
}
