using UnityEngine;

public class PerformingBackstabState : IEnemyState
{
    private EnemyManager manager;
    private bool animationFinishedNotified = false; // Flag set by animation event

    public void Enter(EnemyManager manager)
    {
        this.manager = manager;
        animationFinishedNotified = false;

        PlayerManager victim = manager.Combat.CurrentBackstabVictim as PlayerManager;

        if (victim == null)
        {
            Debug.LogError($"{manager.name} entered PerformingBackstabState but CurrentBackstabVictim is null or not PlayerManager. Exiting to CombatStance.");
            manager.SwitchState(manager.combatStanceState); // Failsafe, go back
            return;
        }

        manager.isInvulnerable = true;
        manager.Locomotion.DisableAgentAndPhysicsControl();

        // Snap enemy to player's backstab receiver point
        Transform victimReceiverPoint = victim.backstabReceiverPoint;
        manager.transform.position = victimReceiverPoint.position;

        // Enemy should look towards the player's core
        Vector3 lookAtTargetPos = victim.lockOnTransform != null ? victim.lockOnTransform.position : victim.transform.position + victim.transform.up * 1f;
        Vector3 directionToLook = lookAtTargetPos - manager.transform.position;
        directionToLook.y = 0;
        if (directionToLook != Vector3.zero) manager.transform.rotation = Quaternion.LookRotation(directionToLook);

        // Notify player they are being backstabbed
        victim.GetBackstabbed(manager.transform);

        // Play enemy's backstab animation
        manager.EnemyAnimator.PlayTargetAnimation(manager.Combat.backstabAction.animationName, true, 0.05f);

        Debug.Log($"{manager.name} entered PerformingBackstabState, executing backstab on {victim.name}.");
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
        manager.Combat.SetGeneralAttackCooldown(manager.Combat.backstabAction.recoveryTime + Random.Range(manager.Combat.minAttackCooldown, manager.Combat.maxAttackCooldown));
        Debug.Log($"{manager.name} exited PerformingBackstabState.");
    }
}
