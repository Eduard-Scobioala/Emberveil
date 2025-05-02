using UnityEngine;

namespace EnemyStateMachine
{
    public abstract class EnemyState
    {
        protected EnemyManager enemyManager;
        protected EnemyLocomotion enemyLocomotion;
        protected EnemyAnimator enemyAnimator;

        public virtual void EnterState(EnemyManager enemy)
        {
            enemyManager = enemy;
            enemyLocomotion = enemy.GetComponent<EnemyLocomotion>();
            enemyAnimator = enemy.GetComponentInChildren<EnemyAnimator>();
        }

        public abstract void UpdateState();
        public abstract void FixedUpdateState();
        public abstract void ExitState();

        public virtual void CheckStateTransitions() { }

        public virtual void OnDamageReceived() { }
    }

    // Idle/Patrol State
    public class IdleState : EnemyState
    {
        private float detectionCheckCounter = 0f;
        private float detectionCheckInterval = 0.2f;

        public override void EnterState(EnemyManager enemy)
        {
            base.EnterState(enemy);
            enemyAnimator.PlayTargetAnimation("Locomotion", false);
            Debug.Log($"{enemy.gameObject.name} entered IDLE state");
        }

        public override void UpdateState()
        {
            detectionCheckCounter += Time.deltaTime;

            if (detectionCheckCounter >= detectionCheckInterval)
            {
                // Check for player detection
                enemyLocomotion.HandleDetection();
                detectionCheckCounter = 0f;

                CheckStateTransitions();
            }
        }

        public override void FixedUpdateState()
        {
            // Nothing to do in fixed update for idle
        }

        public override void ExitState()
        {
            // Clean up any idle specific behaviors
        }

        public override void CheckStateTransitions()
        {
            if (enemyLocomotion.currentTarget != null)
            {
                enemyManager.SwitchState(enemyManager.chaseState);
            }
        }
    }

    // Chase State
    public class ChaseState : EnemyState
    {
        public override void EnterState(EnemyManager enemy)
        {
            base.EnterState(enemy);
            enemyAnimator.anim.SetFloat("Vertical", 1, 0.1f, Time.deltaTime);
            enemyLocomotion.EnableNavMeshAgent();
            Debug.Log($"{enemy.gameObject.name} entered CHASE state");
        }

        public override void UpdateState()
        {
            CheckStateTransitions();
        }

        public override void FixedUpdateState()
        {
            if (enemyLocomotion.currentTarget != null)
            {
                enemyLocomotion.HandleMoveToTarget();
            }
            else
            {
                // Lost target, go back to idle
                enemyManager.SwitchState(enemyManager.idleState);
            }
        }

        public override void ExitState()
        {
            enemyAnimator.anim.SetFloat("Vertical", 0, 0.1f, Time.deltaTime);
        }

        public override void CheckStateTransitions()
        {
            // If we're close enough to the target, switch to combat
            if (enemyLocomotion.currentTarget != null)
            {
                enemyLocomotion.distanceFromTarget = Vector3.Distance(
                    enemyLocomotion.currentTarget.transform.position,
                    enemyManager.transform.position);

                if (enemyLocomotion.distanceFromTarget <= enemyLocomotion.stoppingDistance)
                {
                    enemyManager.SwitchState(enemyManager.combatState);
                }

                // If we've lost sight of target for too long, return to idle
                // This logic could be implemented with a timer
            }
        }
    }

    // Combat State
    public class CombatState : EnemyState
    {
        private float attackCooldownTimer = 0f;

        public override void EnterState(EnemyManager enemy)
        {
            base.EnterState(enemy);
            enemyAnimator.anim.SetFloat("Vertical", 0, 0.1f, Time.deltaTime);
            Debug.Log($"{enemy.gameObject.name} entered COMBAT state");
        }

        public override void UpdateState()
        {
            attackCooldownTimer -= Time.deltaTime;

            // Handle rotation to face target
            if (enemyLocomotion.currentTarget != null)
            {
                enemyLocomotion.HandleRotateTowardsTarget();
            }

            CheckStateTransitions();
        }

        public override void FixedUpdateState()
        {
            // If we can attack, do so
            if (attackCooldownTimer <= 0 && !enemyManager.isPerformingAction)
            {
                AttackTarget();
            }
        }

        public override void ExitState()
        {
            // Clean up combat specific behaviors
        }

        public override void CheckStateTransitions()
        {
            if (enemyLocomotion.currentTarget == null)
            {
                enemyManager.SwitchState(enemyManager.idleState);
                return;
            }

            enemyLocomotion.distanceFromTarget = Vector3.Distance(
                enemyLocomotion.currentTarget.transform.position,
                enemyManager.transform.position);

            // If target is too far, chase them
            if (enemyLocomotion.distanceFromTarget > enemyLocomotion.stoppingDistance * 1.2f)
            {
                enemyManager.SwitchState(enemyManager.chaseState);
            }
        }

        private void AttackTarget()
        {
            // Select an attack to perform
            EnemyAttackAction selectedAttack = enemyManager.GetRandomAttack();

            if (selectedAttack != null)
            {
                enemyManager.isPerformingAction = true;
                attackCooldownTimer = selectedAttack.recoveryTime;
                enemyAnimator.PlayTargetAnimation(selectedAttack.actionAnimation, true);
            }
        }

        public override void OnDamageReceived()
        {
            // Could implement special behavior when hit during combat
        }
    }

    // Dead State
    public class DeadState : EnemyState
    {
        public override void EnterState(EnemyManager enemy)
        {
            base.EnterState(enemy);
            // Play death animation
            enemyAnimator.PlayTargetAnimation("Death_01", true);

            // Disable components
            enemyLocomotion.DisableNavMeshAgent();

            // Make sure the enemy doesn't interact with other entities
            Collider[] colliders = enemy.GetComponentsInChildren<Collider>();
            foreach (Collider c in colliders)
            {
                c.enabled = false;
            }

            Debug.Log($"{enemy.gameObject.name} entered DEAD state");

            // Optional: Add a timer to destroy/disable the enemy after death animation
            Object.Destroy(enemy.gameObject, 3f); // Destroy after 3 seconds
        }

        public override void UpdateState()
        {
            // Nothing to do for dead enemies
        }

        public override void FixedUpdateState()
        {
            // Nothing to do for dead enemies
        }

        public override void ExitState()
        {
            // Dead is final state, no exit needed
        }
    }

    // Stunned/Hit Reaction State (optional)
    public class StunnedState : EnemyState
    {
        private float stunTimer;
        private float stunDuration;

        public override void EnterState(EnemyManager enemy)
        {
            base.EnterState(enemy);
            stunDuration = 1.0f; // Default stun duration
            stunTimer = stunDuration;
            enemyAnimator.PlayTargetAnimation("Damage_01", true);
            Debug.Log($"{enemy.gameObject.name} entered STUNNED state");
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
}