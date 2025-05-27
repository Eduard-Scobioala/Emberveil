using System;
using UnityEngine;

public class StunnedState : EnemyState
{
    private float stunTimer;
    private float stunDuration;
    private readonly Func<bool> isInMidAction;
    private readonly Func<bool> isInvulnerable;

    public StunnedState(Func<bool> isInMidAction, Func<bool> isInvulnerable)
    {
        this.isInMidAction = isInMidAction;
        this.isInvulnerable = isInvulnerable;
    }

    public override void EnterState(EnemyManager enemy)
    {
        base.EnterState(enemy);
        stunDuration = 1.0f; // Default stun duration
        stunTimer = stunDuration;

        if (!isInMidAction() && !isInvulnerable())
        {
            enemyAnimator.PlayTargetAnimation("Damage_01", true);
            Debug.Log($"{enemy.gameObject.name} entered STUNNED state");
        }
    }

    public override void UpdateState()
    {
        stunTimer -= Time.deltaTime;
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        // Nothing specific here
    }

    public override void ExitState()
    {
        // Clean up stun specific behaviors
    }

    public override void CheckStateTransitions()
    {
        if (stunTimer <= 0)
        {
            // Return to previous state or determine next state
            // Could keep track of previous state or just go to combat if target exists
            if (enemyLocomotion.currentTarget != null)
            {
                enemyLocomotion.distanceFromTarget = Vector3.Distance(
                    enemyLocomotion.currentTarget.transform.position,
                    enemyManager.transform.position);

                if (enemyLocomotion.distanceFromTarget <= enemyLocomotion.stoppingDistance)
                {
                    enemyManager.SwitchState(enemyManager.combatState);
                }
                else
                {
                    enemyManager.SwitchState(enemyManager.chaseState);
                }
            }
            else
            {
                enemyManager.SwitchState(enemyManager.idleState);
            }
        }
    }
}
