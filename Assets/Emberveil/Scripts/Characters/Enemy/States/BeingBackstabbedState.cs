using UnityEngine;

public class BeingBackstabbedState : IEnemyState
{
    private EnemyManager manager;
    private Transform attacker;
    private bool animationFinishedNotified = false; // Flag set by animation event

    public void SetAttacker(Transform attackerTransform)
    {
        attacker = attackerTransform;
    }

    public void Enter(EnemyManager manager)
    {
        this.manager = manager;
        animationFinishedNotified = false;
        manager.isBeingCriticallyHit = true;
        manager.isInvulnerable = true;  // Invulnerable while being backstabbed

        manager.Locomotion.DisableAgentAndPhysicsControl(); // Animation takes full control

        // Orient self relative to attacker
        if (attacker != null)
        {
            Vector3 directionFromAttacker = manager.transform.position - attacker.position;
            directionFromAttacker.y = 0;
            directionFromAttacker.Normalize();
            if (directionFromAttacker != Vector3.zero)
            {
                manager.transform.rotation = Quaternion.LookRotation(directionFromAttacker);
            }
        }

        manager.EnemyAnimator.PlayTargetAnimation(manager.beingBackstabbedAnimation, true, 0.05f);
        Debug.Log($"{manager.name} entered BeingBackstabbedState.");
    }

    public void Tick()
    {
        // Logic is mostly driven by animation events.
    }

    public void FixedTick() { }

    // Called from EnemyManager via AnimEvent_FinishBeingCriticallyHit
    public void OnCriticalHitAnimationEnd()
    {
        animationFinishedNotified = true;
    }

    public IEnemyState Transition()
    {
        if (animationFinishedNotified)
        {
            if (manager.Stats.isDead)
            {
                return manager.deadState;
            }
            // If survived, decide next state (e.g., briefly stunned or back to combat/idle)
            // For now, just go to idle/chase based on target presence
            if (manager.CurrentTarget != null) return manager.chaseState;
            return manager.idleState;
        }
        return null;
    }

    public void Exit()
    {
        manager.isBeingCriticallyHit = false;
        manager.isInvulnerable = false; // Reset invulnerability
        // Re-enable appropriate locomotion, handled by the next state's Enter usually
        // manager.Locomotion.EnableAgentNavigation(); // Example
        attacker = null;
        Debug.Log($"{manager.name} exited BeingBackstabbedState.");
    }
}
