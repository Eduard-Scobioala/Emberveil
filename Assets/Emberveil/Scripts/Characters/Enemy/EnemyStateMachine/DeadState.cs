using UnityEngine;

public class DeadState : EnemyState
{
    public override void EnterState(EnemyManager enemy)
    {
        base.EnterState(enemy);
        enemyAnimator.PlayTargetAnimation("Death_01", true);

        // Disable components
        enemyLocomotion.DisableNavMeshAgent();
        enemyLocomotion.enabled = false;

        enemyLocomotion.currentTarget = null;

        // Disable lock on for dead enemies
        enemy.lockOnTransform = null;

        Debug.Log($"{enemy.gameObject.name} entered DEAD state");

        Object.Destroy(enemy.gameObject, 5f);
    }

    public override void UpdateState() { }

    public override void FixedUpdateState() { }

    public override void ExitState() { }
}
