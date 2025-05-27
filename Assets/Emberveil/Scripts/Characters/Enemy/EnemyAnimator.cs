using UnityEngine;

public class EnemyAnimator : AnimatorManager
{
    private EnemyLocomotion enemyLocomotion;
    private EnemyManager enemyManager;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        enemyLocomotion = GetComponentInParent<EnemyLocomotion>();
        enemyManager = GetComponentInParent<EnemyManager>();
        if (enemyManager == null)
        {
            Debug.LogError("EnemyAnimator could not find EnemyManager in parent.", this);
        }
    }

    public void AnimEvent_FinishBeingBackstabbed()
    {
        if (enemyManager != null)
        {
            enemyManager.FinishBeingBackstabbed();
        }
        else
        {
            Debug.LogError("AnimEvent_FinishBeingBackstabbed called, but EnemyManager is null!", this);
        }
    }

    public void AnimEvent_EnableInvulnerability()
    {
        if (enemyManager != null) enemyManager.isInvulnerable = true;
    }

    public void AnimEvent_DisableInvulnerability()
    {
        if (enemyManager != null) enemyManager.isInvulnerable = false;
    }

    public void AnimEvent_ApplyBackstabDamageToVictim()
    {
        if (enemyManager != null)
        {
            enemyManager.AnimEvent_ApplyBackstabDamageToVictim();
        }
    }

    public void AnimEvent_FinishPerformingBackstab()
    {
        if (enemyManager != null)
        {
            enemyManager.AnimEvent_FinishPerformingBackstab();
        }
    }

    private void OnAnimatorMove()
    {
        if (anim == null || enemyManager == null || Time.deltaTime <= 0)
        {
            // Not ready or no time has passed, do nothing.
            return;
        }

        // Get the root motion from the animator for this frame.
        Vector3 deltaPosition = anim.deltaPosition;
        Quaternion deltaRotation = anim.deltaRotation;

        // --- Apply to Rigidbody (Recommended for physics-based characters) ---
        if (enemyLocomotion != null && enemyLocomotion.enemyRigidbody != null)
        {
            // If the Rigidbody is kinematic, use MovePosition/MoveRotation.
            // If it's non-kinematic, applying velocity or using MovePosition/MoveRotation
            // can work, but MovePosition/Rotation is often better for direct animation control.

            // We need to be careful if the NavMeshAgent is active, as it also controls position.
            // Generally, during animations that use root motion (like attacks or being hit),
            // the NavMeshAgent should be disabled or its updating paused.

            if (enemyLocomotion.enemyRigidbody.isKinematic)
            {
                // For kinematic rigidbodies, directly move them.
                // This is often the case when NavMeshAgent is active and has taken over,
                // but during root motion victim animations, agent should be off.
                enemyLocomotion.enemyRigidbody.MovePosition(enemyLocomotion.enemyRigidbody.position + deltaPosition);
                enemyLocomotion.enemyRigidbody.MoveRotation(enemyLocomotion.enemyRigidbody.rotation * deltaRotation);
            }
            else // Non-kinematic Rigidbody
            {
                // For non-kinematic, you can apply it as a velocity or use MovePosition.
                // Using MovePosition is often more direct for root motion.
                // This is the state the Rigidbody should be in when NavMeshAgent is disabled
                // for victim animations.

                // Ensure drag is low so it doesn't fight the root motion too much
                // enemyLocomotion.enemyRigidbody.drag = 0; // You might already do this

                // Calculate desired velocity from deltaPosition
                Vector3 worldVelocity = deltaPosition / Time.deltaTime;

                // If you want to preserve existing Y velocity (e.g., for gravity if falling while being hit),
                // you can do this, but for simple pushback, directly applying is fine.
                // worldVelocity.y = enemyLocomotion.enemyRigidbody.velocity.y;

                enemyLocomotion.enemyRigidbody.velocity = worldVelocity;

                // Apply rotation (usually less problematic than position)
                enemyManager.transform.rotation *= deltaRotation; // Apply delta to current rotation

                // ALTERNATIVE FOR NON-KINEMATIC (often more stable for root motion):
                // enemyLocomotion.enemyRigidbody.MovePosition(enemyLocomotion.enemyRigidbody.position + deltaPosition);
                // enemyLocomotion.enemyRigidbody.MoveRotation(enemyLocomotion.enemyRigidbody.rotation * deltaRotation);
            }
        }
        else // --- Fallback: Apply directly to Transform (if no Rigidbody or it's not the primary mover) ---
        {
            enemyManager.transform.position += deltaPosition;
            enemyManager.transform.rotation *= deltaRotation;
        }
    }
}
