using UnityEngine;

public class HitReactionState : IEnemyState
{
    private EnemyManager manager;
    private Transform attacker;
    private bool isInvincibleDuringStun;
    private float hitStunDuration; // Or get from attack data / animation length

    public HitReactionState(bool isInvincibleDuringStun = false, float hitStunDuration = 0.1f)
    {
        this.isInvincibleDuringStun = isInvincibleDuringStun;
        this.hitStunDuration = hitStunDuration;
    }

    public void SetAttacker(Transform attackerTransform)
    {
        attacker = attackerTransform;
    }

    public void Enter(EnemyManager manager)
    {
        this.manager = manager;
        manager.Locomotion.StopMovement();
        manager.Locomotion.DisableAgentNavigation(true); // Allow root motion or physics pushes

        if (isInvincibleDuringStun) manager.EnemyAnimator.IsInvulnerable = true;

        if (attacker != null) manager.Locomotion.FaceTargetInstantly(attacker);

        // TODO: Choose hit animation based on damage type, direction, etc.
        manager.EnemyAnimator.PlayTargetAnimation("Damage_01", true); // Generic damage animation
        Debug.Log($"{manager.name} entered HitReactionState.");

        // Reset attacker after use
        attacker = null;
    }

    public void Tick()
    {
        hitStunDuration = isInvincibleDuringStun ? (hitStunDuration - Time.deltaTime) : 0;
    }

    public void FixedTick() { }

    public IEnemyState Transition()
    {
        if (hitStunDuration <= 0)
        {
            if (manager.Stats.isDead) return manager.deadState; // Check for death from this hit

            if (manager.CurrentTarget != null)
            {
                float distanceToTarget = Vector3.Distance(manager.transform.position, manager.CurrentTarget.transform.position);
                if (distanceToTarget <= manager.defaultStoppingDistance * 1.2f)
                {
                    return manager.combatStanceState;
                }
                return manager.chaseState;
            }
            return manager.returnToPostState;
        }
        return null;
    }

    public void Exit()
    {
        if (isInvincibleDuringStun) manager.EnemyAnimator.IsInvulnerable = false;
    }
}
