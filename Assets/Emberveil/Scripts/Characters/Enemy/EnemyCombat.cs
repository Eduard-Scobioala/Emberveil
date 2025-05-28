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
    public bool IsAttackOnCooldown => currentAttackCooldownTimer > 0;

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
        if (target == null || attackActions.Count == 0 || IsAttackOnCooldown) return null;

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

    public bool CanInitiateBackstabSequence(PlayerManager playerTarget)
    {
        if (backstabAction == null || playerTarget == null) return false;
        if (playerTarget.backstabReceiverPoint == null)
        {
            Debug.LogWarning($"Player {playerTarget.name} has no backstabReceiverPoint. Cannot initiate backstab.");
            return false;
        }
        return true;
    }

    public void InitiateBackstabSequence(PlayerManager playerTarget)
    {
        if (!CanInitiateBackstabSequence(playerTarget))
        {
            CurrentBackstabVictim = null; // Ensure it's cleared if pre-check fails
            return;
        }

        CurrentBackstabVictim = playerTarget;
        Debug.Log($"{enemyManager.name} is initiating backstab sequence on {playerTarget.name}. PerformingBackstabState will execute.");
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
