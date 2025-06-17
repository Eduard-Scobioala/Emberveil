using UnityEngine;

public class DeadState : IEnemyState
{
    private EnemyManager manager;
    private float despawnTimer = 5f;

    public void Enter(EnemyManager manager)
    {
        this.manager = manager;
        Debug.Log($"{manager.name} entered DeadState.");

        manager.CurrentTarget = null; // No longer has a target
        manager.Senses.ForceLoseTarget(); // Ensure senses are cleared
        manager.Locomotion.DisableAgentAndPhysicsControl(); // Stop all movement, make kinematic

        // Disable all colliders on this GameObject and its children
        Collider[] colliders = manager.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }

        if (manager.lockOnTransform != null) manager.lockOnTransform.gameObject.SetActive(false); // Disable lock-on point

        // Play death animation. Ensure it's not interrupted by isPerformingCriticalHit checks
        if (!manager.isBeingCriticallyHit)
        {
            manager.EnemyAnimator.PlayTargetAnimation("Death_01", true); // "Death_01" or appropriate
        }
        manager.EnemyAnimator.SetBool("isDead", true); // For animator transitions if any

        // TODO: Handle dropping loot, notifying quest systems, etc.
    }

    public void Tick()
    {
        despawnTimer -= Time.deltaTime;
        if (despawnTimer <= 0)
        {
            // TODO: Maybe pool instead of destroy
            //Object.Destroy(manager.gameObject);
            manager.gameObject.SetActive(false);
        }
    }

    public void FixedTick() { }

    public IEnemyState Transition()
    {
        return null; // No transitions out of DeadState except destruction
    }

    public void Exit()
    {
        // Cleanup if needed before destroy, but usually handled by OnDestroy()
    }
}
