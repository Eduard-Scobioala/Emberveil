using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    private EnemyManager enemyManager;
    private EnemyAnimator enemyAnimator;
    private EnemyLocomotion enemyLocomotion;

    [Header("Actions")]
    public List<EnemyAttackActionSO> attackActions = new List<EnemyAttackActionSO>();
    public EnemyBackstabActionSO backstabAction;

    [Header("Combat Settings")]
    public float minAttackCooldown = 1.0f;
    public float maxAttackCooldown = 3.0f;
    private float currentAttackCooldownTimer;
    public bool isAttackOnCooldown => currentAttackCooldownTimer > 0;

    [Header("Backstab Settings")]
    public float backstabCheckMaxDistance = 1.5f; // How close enemy needs to be to player's back
    public float backstabCheckMaxAngle = 45f;   // Angle behind player
    public float chanceToAttemptBackstab = 0.3f; // If opportunity arises

    public CharacterManager CurrentBackstabVictim { get; private set; }

    // TODO: Weapon handling - for now, assume a default damage collider
    [SerializeField] private DamageCollider rightHandDamageCollider; // Assign in inspector

    public void Initialize(EnemyManager manager)
    {
        enemyManager = manager;
        enemyAnimator = manager.EnemyAnimator;
        enemyLocomotion = manager.Locomotion;
        // TODO: Find/Setup DamageCollider, possibly from a weapon script
        if (rightHandDamageCollider == null) Debug.LogWarning("RightHandDamageCollider not set on EnemyCombat");
    }

    public void TickCombat()
    {
        if (currentAttackCooldownTimer > 0)
        {
            currentAttackCooldownTimer -= Time.deltaTime;
        }
    }

    public EnemyAttackActionSO GetAvailableAttack(CharacterManager target)
    {
        if (target == null || attackActions.Count == 0) return null;

        Vector3 directionToTarget = target.transform.position - transform.position;
        float distanceToTarget = directionToTarget.magnitude;
        float angleToTarget = Vector3.Angle(transform.forward, directionToTarget.normalized);

        List<EnemyAttackActionSO> validAttacks = new List<EnemyAttackActionSO>();
        foreach (var attack in attackActions)
        {
            if (distanceToTarget >= attack.minAttackDistance && distanceToTarget <= attack.maxAttackDistance &&
                angleToTarget >= attack.minAttackAngle && angleToTarget <= attack.maxAttackAngle)
            {
                validAttacks.Add(attack);
            }
        }

        if (validAttacks.Count == 0) return null;

        // TODO: Improve with scoring
        return validAttacks[Random.Range(0, validAttacks.Count)];
    }

    public void PerformAttack(EnemyAttackActionSO attackAction)
    {
        if (attackAction == null || enemyManager.IsPerformingCriticalAction || enemyManager.IsReceivingCriticalHit) return;

        Debug.Log($"{enemyManager.name} performing attack: {attackAction.actionName}");
        enemyManager.isPerformingNonCriticalAction = true; // A flag for general actions
        enemyLocomotion.StopMovement(); // Ensure enemy stops before attacking
        enemyLocomotion.FaceTargetInstantly(enemyManager.CurrentTarget.transform); // Snap to target

        enemyAnimator.PlayTargetAnimation(attackAction.animationName, true, 0.1f);
        currentAttackCooldownTimer = attackAction.recoveryTime + Random.Range(minAttackCooldown, maxAttackCooldown);

        // Handle damage collider timing with anim events
    }

    public bool CanAttemptBackstab(CharacterManager target)
    {
        if (backstabAction == null || target == null || !(target is PlayerManager)) return false;

        PlayerManager playerTarget = target as PlayerManager;
        // TODO: Add PlayerManager.canBeBackstabbed similar to CharacterManager
        if (playerTarget.isInMidAction || playerTarget.isInvulnerable /*|| !playerTarget.canBeBackstabbed*/) return false;

        // Check position: Enemy behind player?
        Vector3 directionFromPlayerToEnemy = (transform.position - playerTarget.transform.position).normalized;
        directionFromPlayerToEnemy.y = 0; // Horizontal plane

        // Angle relative to player's back
        float angle = Vector3.Angle(-playerTarget.transform.forward, directionFromPlayerToEnemy);

        if (angle <= backstabCheckMaxAngle)
        {
            // Check distance to player's backstab receiver point
            if (playerTarget.backstabReceiverPoint == null)
            {
                Debug.LogWarning($"Player {playerTarget.name} missing backstabReceiverPoint for {enemyManager.name} to target.");
                return false;
            }
            float distance = Vector3.Distance(transform.position, playerTarget.backstabReceiverPoint.position);
            if (distance <= backstabCheckMaxDistance)
            {
                return true;
            }
        }
        return false;
    }

    public void AttemptBackstab(PlayerManager playerTarget)
    {
        if (backstabAction == null || playerTarget == null) return;

        Debug.Log($"{enemyManager.name} attempting BACKSTAB on {playerTarget.name}");
        enemyManager.SetPerformingCriticalAction(true, true); // isPerforming, isAttacker
        CurrentBackstabVictim = playerTarget;

        // Snap enemy to player's backstab receiver point
        // The receiver point on the player is where an *attacker* would stand.
        Transform victimReceiverPoint = playerTarget.backstabReceiverPoint;
        if (victimReceiverPoint == null)
        {
            Debug.LogError($"Player {playerTarget.name} has no backstabReceiverPoint! Aborting backstab.");
            enemyManager.SetPerformingCriticalAction(false, false);
            CurrentBackstabVictim = null;
            return;
        }
        transform.position = victimReceiverPoint.position;

        // Enemy should look towards the player's core from this snap position.
        Vector3 lookAtTargetPos = playerTarget.lockOnTransform != null ? playerTarget.lockOnTransform.position : playerTarget.transform.position + playerTarget.transform.up * 1f;
        Vector3 directionToLook = lookAtTargetPos - transform.position;
        directionToLook.y = 0;
        if (directionToLook != Vector3.zero) transform.rotation = Quaternion.LookRotation(directionToLook);

        // Notify player they are being backstabbed
        playerTarget.GetBackstabbed(enemyManager.transform);

        enemyAnimator.PlayTargetAnimation(backstabAction.animationName, true, 0.05f);
        // Recovery/cooldown will be handled when the critical action state finishes
        // currentAttackCooldownTimer = backstabAction.recoveryTime; // Set by state exit or anim event
    }

    public void ApplyBackstabDamageOnVictim() // Called by EnemyAnimator AnimEvent
    {
        if (CurrentBackstabVictim != null && backstabAction != null)
        {
            Debug.Log($"{enemyManager.name} applying {backstabAction.backstabDamage} backstab damage to {CurrentBackstabVictim.name}");
            PlayerStats victimStats = CurrentBackstabVictim.GetComponent<PlayerStats>();
            victimStats?.TakeDamange(backstabAction.backstabDamage);
            // For new EnemyStats: victimStats?.TakeDamage(backstabAction.backstabDamage, DamageType.BackstabCritical, enemyManager.transform);
        }
    }

    public void EnableWeaponCollider(WeaponHand hand)
    {
        // TODO: Handle left/right hand
        if (rightHandDamageCollider != null)
        {
            // Pass damage from current attack action
            // rightHandDamageCollider.SetDamage(currentAttackAction.damage);
            rightHandDamageCollider.EnableDamageCollider();
            Debug.Log($"{enemyManager.name} enabled damage collider.");
        }
    }

    public void DisableWeaponCollider(WeaponHand hand)
    {
        if (rightHandDamageCollider != null)
        {
            rightHandDamageCollider.DisableDamageCollider();
            Debug.Log($"{enemyManager.name} disabled damage collider.");
        }
    }

    public void ResetCooldown()
    {
        currentAttackCooldownTimer = Random.Range(minAttackCooldown, maxAttackCooldown);
    }

    public void SetSpecificCooldown(float duration)
    {
        currentAttackCooldownTimer = duration;
    }

    public void ClearBackstabVictim()
    {
        CurrentBackstabVictim = null;
    }
}
