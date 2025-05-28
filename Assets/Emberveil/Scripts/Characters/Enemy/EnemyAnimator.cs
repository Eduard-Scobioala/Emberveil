using UnityEngine;
using UnityEngine.AI;

public class EnemyAnimator : AnimatorManager // Assuming AnimatorManager exists
{
    private EnemyManager enemyManager;
    private EnemyLocomotion enemyLocomotion;
    private EnemyCombat enemyCombat;

    // Animator Hashes
    private readonly int hashForwardSpeed = Animator.StringToHash("ForwardSpeed");
    private readonly int hashStrafeSpeed = Animator.StringToHash("StrafeSpeed");
    // Add other common hashes like "IsInteracting", "IsDead"

    public void Initialize(EnemyManager manager)
    {
        enemyManager = manager;
        enemyLocomotion = manager.Locomotion;
        enemyCombat = manager.Combat;
        anim = GetComponent<Animator>(); // Make sure this is set
        if (anim == null) Debug.LogError("Animator not found on EnemyAnimator's GameObject or children.", this);
    }

    public void SetMovementValues(float forwardAmount, float strafeAmount)
    {
        if (anim == null) return;
        anim.SetFloat(hashForwardSpeed, forwardAmount, 0.1f, Time.deltaTime);
        anim.SetFloat(hashStrafeSpeed, strafeAmount, 0.1f, Time.deltaTime);
    }

    public void PlayTargetAnimation(string animationName, bool isInMidAction, float transitionDuration = 0.1f)
    {
        if (anim == null) return;

        SetBool("isInMidAction", isInMidAction);
        anim.applyRootMotion = isInMidAction; // Typically true for actions, false for locomotion blends
        anim.CrossFade(animationName, transitionDuration);
    }

    public void SetBool(string paramName, bool value)
    {
        if (anim == null) return;
        anim.SetBool(paramName, value);
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
        if ((!enemyLocomotion.GetComponent<NavMeshAgent>().enabled && !enemyLocomotion.GetComponent<Rigidbody>().isKinematic) ||
            enemyManager.IsPerformingCriticalAction || enemyManager.IsReceivingCriticalHit)
        {
            enemyLocomotion.ApplyRootMotion(anim.deltaPosition, anim.deltaRotation);
        }
    }

    // --- Animation Event Handlers ---
    // These are called by name from Animation Events in animation clips

    public void AnimEvent_EnableDamageCollider()
    {
        enemyCombat?.EnableWeaponCollider(WeaponHand.Right); // Assuming right hand for now
    }

    public void AnimEvent_DisableDamageCollider()
    {
        enemyCombat?.DisableWeaponCollider(WeaponHand.Right);
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
        if (enemyManager != null) enemyManager.isInvulnerable = true;
    }

    public void AnimEvent_DisableInvulnerability()
    {
        if (enemyManager != null) enemyManager.isInvulnerable = false;
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
}
