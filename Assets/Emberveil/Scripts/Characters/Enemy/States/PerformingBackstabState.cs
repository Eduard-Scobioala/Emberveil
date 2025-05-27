using UnityEngine;

public class PerformingBackstabState : IEnemyState
{
    private EnemyManager manager;
    private bool animationFinishedNotified = false; // Flag set by animation event

    public void Enter(EnemyManager manager)
    {
        this.manager = manager;
        animationFinishedNotified = false;
        manager.isInMidAction = true; // Use CharacterManager's flag
        manager.isInvulnerable = true;

        // Locomotion should already be disabled and enemy snapped by EnemyCombat.AttemptBackstab()
        // Animation should have been started by EnemyCombat.AttemptBackstab()
        Debug.Log($"{manager.name} entered PerformingBackstabState.");

        // Cooldown is set by EnemyCombat after the animation via Notify_FinishedPerformingCriticalAction
    }

    public void Tick()
    {
        // Logic is mostly driven by animation events.
        // If victim dies mid-animation, we might want to abort early.
        if (manager.Combat.CurrentBackstabVictim != null && manager.Combat.CurrentBackstabVictim.GetComponent<CharacterStats>().isDead)
        {
            // Victim died, perhaps prematurely end animation or ensure cleanup.
            // For now, let animation finish.
        }
    }

    public void FixedTick() { }

    // Called from EnemyManager via AnimEvent_FinishPerformingCriticalAction
    public void OnCriticalActionAnimationEnd()
    {
        animationFinishedNotified = true;
    }

    public IEnemyState Transition()
    {
        if (animationFinishedNotified)
        {
            if (manager.CurrentTarget == null || manager.Combat.CurrentBackstabVictim == null || manager.Combat.CurrentBackstabVictim.GetComponent<CharacterStats>().isDead)
            {
                return manager.idleState; // Target gone or dead
            }
            // After backstab, usually go back to combat stance
            return manager.combatStanceState;
        }
        return null;
    }

    public void Exit()
    {
        manager.isInMidAction = false;
        manager.isInvulnerable = false;
        manager.Locomotion.EnableAgentNavigation();
        manager.Combat.ClearBackstabVictim();
        // Set cooldown for the backstab action via Combat
        manager.Combat.SetSpecificCooldown(manager.Combat.backstabAction.recoveryTime + Random.Range(manager.Combat.minAttackCooldown, manager.Combat.maxAttackCooldown));
        Debug.Log($"{manager.name} exited PerformingBackstabState.");
    }
}
