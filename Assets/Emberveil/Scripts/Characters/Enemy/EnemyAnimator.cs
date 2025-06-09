using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR;

public class EnemyAnimator : AnimatorManager // Assuming AnimatorManager exists
{
    private EnemyManager enemyManager;
    private EnemyLocomotion enemyLocomotion;
    private EnemyCombat enemyCombat;

    // Animator Hashes
    public readonly int hashVertical = Animator.StringToHash("Vertical");
    public readonly int hashHorizontal = Animator.StringToHash("Horizontal");
    // Add other common hashes like "IsInteracting", "IsDead"

    public void Initialize(EnemyManager manager)
    {
        enemyManager = manager;
        enemyLocomotion = manager.Locomotion;
        enemyCombat = manager.Combat;
        anim = GetComponent<Animator>(); // Make sure this is set
        if (anim == null) Debug.LogError("Animator not found on EnemyAnimator's GameObject or children.", this);
    }

    public void PlayTargetAnimation(string animationName, bool isInMidAction, float transitionDuration = 0.1f)
    {
        if (anim == null) return;

        IsInMidAction = isInMidAction;
        anim.applyRootMotion = isInMidAction; // Typically true for actions, false for locomotion blends
        anim.CrossFade(animationName, transitionDuration);
    }

    public void SetBool(string paramName, bool value)
    {
        if (anim == null) return;
        anim.SetBool(paramName, value);
    }

    public void SetFloat(string paramName, float value, float dampTime = 0.1f, float deltaTime = -1f)
    {
        if (anim == null) return;
        if (deltaTime < 0) deltaTime = Time.deltaTime;
        anim.SetFloat(paramName, value, dampTime, deltaTime);
    }

    public void SetFloat(int paramID, float value, float dampTime = 0.1f, float deltaTime = -1f) // Overload for hash IDs
    {
        if (anim == null) return;
        if (deltaTime < 0) deltaTime = Time.deltaTime;
        anim.SetFloat(paramID, value, dampTime, deltaTime);
    }

    public void SetMovementValues(float verticalSpeed, float horizontalSpeed)
    {
        if (anim == null) return;
        anim.SetFloat(hashVertical, verticalSpeed, 0.1f, Time.deltaTime);
        anim.SetFloat(hashHorizontal, horizontalSpeed, 0.1f, Time.deltaTime);
    }

    public void SetTrigger(string paramName)
    {
        if (anim == null) return;
        anim.SetTrigger(paramName);
    }

    // OnAnimatorMove is called on the GameObject with the Animator component
    // This script should be on that same GameObject or a child that can get it.
    private void OnAnimatorMove()
    {
        if (enemyLocomotion == null || !anim.applyRootMotion || Time.deltaTime <= 0) return;

        // Only apply root motion if the locomotion system is prepared for it
        // (e.g., RB is non-kinematic, NavAgent is off or overridden by current state)
        // Or if in a critical action where animation fully dictates movement
        if ((AgentIsNull() || !enemyLocomotion.GetComponent<NavMeshAgent>().enabled && !enemyLocomotion.GetComponent<Rigidbody>().isKinematic) ||
            enemyManager.IsPerformingCriticalAction || enemyManager.IsReceivingCriticalHit)
        {
            enemyLocomotion.ApplyRootMotion(anim.deltaPosition, anim.deltaRotation);
        }
    }

    private bool AgentIsNull() => enemyLocomotion.GetComponent<NavMeshAgent>() == null;

    // --- Animation Event Handlers ---
    // These are called by name from Animation Events in animation clips

    public void AnimEvent_EnableDamageCollider()
    {
        enemyManager.EnemyWeaponSlotManager?.OpenDamageCollider(WeaponHand.Right);
    }

    public void AnimEvent_DisableDamageCollider()
    {
        enemyManager.EnemyWeaponSlotManager?.CloseDamageCollider(WeaponHand.Right);
    }

    public void AnimEvent_AttackLanded()
    {
        // TODO: Play SFX, VFX for attack impact
    }

    public void AnimEvent_ApplyBackstabDamageToVictim() // When enemy is ATTACKER
    {
        enemyCombat?.ApplyBackstabDamageOnVictim();
    }

    public void AnimEvent_FinishPerformingCriticalAction() // When enemy is ATTACKER
    {
        // This signals the end of the enemy's attacking critical animation (e.g., backstab)
        enemyManager?.Notify_FinishedPerformingCriticalAction();
    }

    public void AnimEvent_FinishBeingCriticallyHit() // When enemy is VICTIM
    {
        // This signals the end of the enemy's victim critical animation (e.g., being backstabbed)
        enemyManager?.Notify_FinishedBeingCriticallyHit();
    }

    public void AnimEvent_EnableInvulnerability()
    {
        if (enemyManager != null) IsInvulnerable = true;
    }

    public void AnimEvent_DisableInvulnerability()
    {
        if (enemyManager != null) IsInvulnerable = false;
    }

    public void AnimEvent_CanCombo() // If enemies have combos
    {
        // enemyCombat?.SetCanCombo(true);
    }

    public void AnimEvent_CannotCombo()
    {
        // enemyCombat?.SetCanCombo(false);
    }

    public void AnimEvent_Footstep()
    {
        // TODO: Play footstep sound
    }

    public void AnimEvent_AttackActionConcluded() // NEW Animation Event Handler
    {
        enemyManager?.Notify_AttackActionConcluded();
    }

    public void AnimEvent_FinishAction()
    {
        IsInMidAction = false;
    }
}
