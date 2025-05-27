using UnityEngine;
using System.Collections;

public class CombatState : EnemyState
{
    private float attackCooldownTimer = 0f;
    private bool isAttacking = false; // General attacking flag
    private bool isAttemptingBackstab = false; // Specific for backstab sequence

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
        isAttemptingBackstab = false;
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
        if (attackCooldownTimer <= 0 && !enemyManager.isPerformingAction && !isAttacking && !isAttemptingBackstab)
        {
            if (enemyLocomotion.currentTarget != null)
            {
                // Try backstab first if available
                if (enemyManager.enemyBackstabAttack != null && CanAttemptBackstab())
                {
                    if (Random.value < enemyManager.chanceToAttemptBackstab)
                    {
                        PerformBackstabAttack();
                    }
                    else
                    {
                        AttackTarget();
                    }
                }
                else
                {
                    AttackTarget();
                }
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

    private bool CanAttemptBackstab()
    {
        if (enemyLocomotion.currentTarget == null || enemyManager.enemyBackstabAttack == null)
            return false;

        PlayerManager playerTarget = enemyLocomotion.currentTarget.GetComponent<PlayerManager>();
        if (playerTarget == null || !playerTarget.canBeBackstabbed || playerTarget.isInMidAction || playerTarget.isInvulnerable)
        {
            // Player is not a PlayerManager, or is immune/busy
            return false;
        }

        // Check position: Enemy behind player?
        Vector3 directionToEnemy = (enemyManager.transform.position - playerTarget.transform.position).normalized;
        directionToEnemy.y = 0;

        float angle = Vector3.Angle(-playerTarget.transform.forward, directionToEnemy);

        if (angle <= enemyManager.backstabCheckMaxAngle) // Enemy is within the "back" cone of the player
        {
            // Check distance: Enemy close enough to player's (assumed) backstab receiver point
            // For simplicity, we'll check distance to player's main transform.
            // A more precise check would involve a 'player.backstabReceiverPoint' if player had one for enemies.
            float distance = Vector3.Distance(enemyManager.transform.position, playerTarget.transform.position);
            if (distance <= enemyManager.backstabCheckMaxDistance)
            {
                return true;
            }
        }
        return false;
    }

    private void PerformBackstabAttack()
    {
        Debug.Log($"{enemyManager.gameObject.name} attempting BACKSTAB on {enemyLocomotion.currentTarget.name}");
        enemyManager.isPerformingAction = true;
        isAttemptingBackstab = true;
        attackCooldownTimer = enemyManager.enemyBackstabAttack.recoveryTime;
        enemyManager.currentBackstabVictim = enemyLocomotion.currentTarget.GetComponent<CharacterManager>();

        enemyManager.isInMidAction = true;
        enemyManager.isInvulnerable = true; // Enemy is invulnerable during their backstab

        PlayerManager playerVictim = enemyLocomotion.currentTarget.GetComponent<PlayerManager>();
        if (playerVictim != null)
        {
            // The enemy should snap to the player's backstabReceiverPoint.
            // This point on the player is where an *attacker* would stand.
            Vector3 snapPosition = playerVictim.backstabReceiverPoint.position;

            // The enemy should look towards the player's core/center from this snap position.
            // Player's lockOnTransform is a good approximation for their center.
            Vector3 lookAtTargetPos = playerVictim.lockOnTransform != null ? playerVictim.lockOnTransform.position : playerVictim.transform.position + playerVictim.transform.up * 1f;
            Vector3 directionToLook = lookAtTargetPos - snapPosition;
            directionToLook.y = 0; // Keep enemy rotation horizontal

            Quaternion snapRotation = Quaternion.LookRotation(directionToLook.normalized);

            enemyManager.transform.position = snapPosition;
            enemyManager.transform.rotation = snapRotation;

            playerVictim.GetBackstabbed(enemyManager.transform);

            Debug.Log($"{enemyManager.name} snapped to {playerVictim.name}'s backstabReceiverPoint. Pos: {snapPosition}, Rot: {snapRotation.eulerAngles}");
        }
        else
        {
            Debug.LogError($"{enemyManager.name} initiated backstab but target {enemyLocomotion.currentTarget.name} is not a PlayerManager or is null! Aborting backstab.");
            enemyManager.isPerformingAction = false;
            isAttemptingBackstab = false;
            attackCooldownTimer = 0.1f;
            enemyManager.isInMidAction = false;
            enemyManager.isInvulnerable = false;
            enemyManager.currentBackstabVictim = null;
            return;
        }

        enemyAnimator.PlayTargetAnimation(enemyManager.enemyBackstabAttack.actionAnimation, true);

        // Reset isAttemptingBackstab via anim event (FinishPerformingBackstab)
        // AnimEvent_FinishPerformingBackstab on EnemyManager will reset flags.
    }

    public override void ExitState()
    {
        enemyManager.isPerformingAction = false;
        isAttacking = false;
        isAttemptingBackstab = false;

        enemyManager.StopCoroutine(ResetAttackAnimationFlag());
    }

    public override void CheckStateTransitions()
    {
        if (isAttacking || isAttemptingBackstab || attackCooldownTimer > 1f) return;

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

    public void ResetIsAttemptingBackstabFlag()
    {
        isAttemptingBackstab = false;
    }

    private IEnumerator ResetAttackAnimationFlag(float animationDelay = 0.8f)
    {
        yield return new WaitForSeconds(animationDelay);

        isAttacking = false;
    }
}