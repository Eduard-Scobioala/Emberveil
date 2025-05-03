using UnityEngine;
using System.Collections;

public class CombatState : EnemyState
{
    private float attackCooldownTimer = 0f;
    private bool isAttacking = false; // Tracks if currently IN the attack animation

    public override void EnterState(EnemyManager enemy)
    {
        base.EnterState(enemy);
        Debug.Log($"{enemy.gameObject.name} entered COMBAT state");

        // === Ensure Stopping on Entry ===
        enemyAnimator.anim.SetFloat("Vertical", 0, 0.05f, Time.deltaTime);

        // CRITICAL: Disable agent and make RB non-kinematic for combat physics/stability
        enemyLocomotion.DisableNavMeshAgent();

        // Explicitly stop any remaining Rigidbody movement from Chase state
        if (enemyLocomotion.enemyRigidbody != null)
        {
            enemyLocomotion.enemyRigidbody.velocity = Vector3.zero;
            enemyLocomotion.enemyRigidbody.angularVelocity = Vector3.zero;
        }

        isAttacking = false;
        enemyManager.isPerformingAction = false;
        attackCooldownTimer = 0f; // Start ready to attack potentially

        // Rotate towards target immediately on entering combat if not attacking
        if (enemyLocomotion.currentTarget != null)
        {
            enemyLocomotion.HandleRotateTowardsTarget(); // Use manual rotation here as agent is disabled
        }
    }

    public override void UpdateState()
    {
        if (attackCooldownTimer > 0)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        // If we were performing an action and the cooldown has now expired
        if (enemyManager.isPerformingAction && attackCooldownTimer <= 0)
        {
            if (!isAttacking)
            {
                Debug.Log($"{enemyManager.gameObject.name} finished recovery/cooldown.");
                enemyManager.isPerformingAction = false;
            }
        }

        // Handle rotation ONLY if we have a target AND we are NOT in the middle of an attack animation swing
        if (enemyLocomotion.currentTarget != null && !isAttacking)
        {
            enemyLocomotion.HandleRotateTowardsTarget();
        }

        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        if (attackCooldownTimer <= 0 && !enemyManager.isPerformingAction && !isAttacking)
        {
            if (enemyLocomotion.currentTarget != null)
            {
                AttackTarget();
            }
            else
            {
                CheckStateTransitions();
            }
        }

        if (!isAttacking)
        {
            enemyAnimator.anim.SetFloat("Vertical", 0);
        }
    }

    public override void ExitState()
    {
        enemyManager.isPerformingAction = false;
        isAttacking = false;

        enemyManager.StopCoroutine(ResetAttackAnimationFlag());
    }

    public override void CheckStateTransitions()
    {
        if (isAttacking) return;

        // --- Target Lost ---
        if (enemyLocomotion.currentTarget == null)
        {
            enemyManager.SwitchState(enemyManager.defaultState);
            return;
        }

        // --- Target Too Far ---
        enemyLocomotion.distanceFromTarget = Vector3.Distance(
            enemyLocomotion.currentTarget.transform.position,
            enemyManager.transform.position);

        // If target is too far, chase them (allow this during cooldown phase)
        if (enemyLocomotion.distanceFromTarget > enemyLocomotion.stoppingDistance * 1.5f) // Buffer distance
        {
            enemyManager.SwitchState(enemyManager.chaseState);
            return;
        }

        // Otherwise, remain in CombatState. Attack readiness is handled in FixedUpdate.
    }

    private void AttackTarget()
    {
        EnemyAttackAction selectedAttack = enemyManager.GetRandomAttack();

        if (selectedAttack != null)
        {
            // Check distance/angle one last time before committing
            enemyLocomotion.distanceFromTarget = Vector3.Distance(
               enemyLocomotion.currentTarget.transform.position,
               enemyManager.transform.position);

            Vector3 directionToTarget = enemyLocomotion.currentTarget.transform.position - enemyManager.transform.position;
            float angleToTarget = Vector3.Angle(directionToTarget, enemyManager.transform.forward);

            bool isInRangeDistance = enemyLocomotion.distanceFromTarget >= selectedAttack.minDistanceRequiredToAttack &&
                                     enemyLocomotion.distanceFromTarget <= selectedAttack.maxDistanceRequiredToAttack;
            bool isInRangeAngle = angleToTarget >= selectedAttack.minAttackAngle &&
                                  angleToTarget <= selectedAttack.maxAttackAngle;


            if (isInRangeDistance && isInRangeAngle)
            {
                Debug.Log($"{enemyManager.gameObject.name} attacking with {selectedAttack.actionAnimation}. Setting flags.");

                enemyManager.isPerformingAction = true;
                isAttacking = true;
                attackCooldownTimer = selectedAttack.recoveryTime;

                // Ensure agent is disabled before attack animation
                enemyLocomotion.DisableNavMeshAgent(); // Redundant maybe, but safe

                enemyAnimator.anim.SetFloat("Vertical", 0);
                if (enemyLocomotion.enemyRigidbody != null)
                {
                    enemyLocomotion.enemyRigidbody.velocity = Vector3.zero;
                }

                enemyAnimator.PlayTargetAnimation(selectedAttack.actionAnimation, true);

                float animationEndDelay = Mathf.Min(attackCooldownTimer * 0.9f, 1.0f);
                enemyManager.StartCoroutine(ResetAttackAnimationFlag(animationEndDelay));
            }
            else
            {
                // Target moved out of range/angle just before attack.
                // Do nothing this frame, rotation will happen in Update, next FixedUpdate will re-evaluate.
                Debug.Log($"{enemyManager.gameObject.name} attack aborted: Target not in range/angle.");
            }
        }
        else
        {
            // No valid attack found (e.g., player behind).
            // Do nothing this frame, rotation will happen in Update.
            Debug.Log($"{enemyManager.gameObject.name} no valid attack found.");
        }
    }

    private IEnumerator ResetAttackAnimationFlag(float animationDelay = 0.8f)
    {
        yield return new WaitForSeconds(animationDelay);

        isAttacking = false;
    }
}