using UnityEngine;

public class PlayerAttacker : MonoBehaviour
{
    private PlayerAnimator playerAnimator;
    private WeaponSlotManager weaponSlotManager;
    private PlayerManager playerManager;
    private PlayerInventory playerInventory;
    private PlayerStats playerStats;

    [Header("Backstab Settings")]
    [SerializeField] private float backstabRaycastDistance = 1.5f;
    [SerializeField] private float backstabMaxInteractionDistance = .6f;
    [SerializeField] private float backstabMaxAngle = 45f;
    [SerializeField] private LayerMask backstabLayerMask;
    [SerializeField] private string playerBackstabAnimation = "Backstab_Main_01";
    [SerializeField] private float playerSnapSpeed = 15f;

    private void Awake()
    {
        playerAnimator = GetComponentInChildren<PlayerAnimator>();
        weaponSlotManager = GetComponentInChildren<WeaponSlotManager>();
        playerManager = GetComponent<PlayerManager>();
        playerInventory = GetComponent<PlayerInventory>();
        playerStats = GetComponent<PlayerStats>();

        if (backstabLayerMask == 0) // If not set in inspector
        {
            Debug.LogError("Backstab LayerMask not set on PlayerAttacker. Backstabs may not work.", this);
        }
    }

    public void HandleAttackButton()
    {
        if (playerAnimator.IsInMidAction && !playerAnimator.CanDoCombo)
            return;

        WeaponItem currentWeapon = playerInventory.RightHandWeapon;
        if (currentWeapon == null) currentWeapon = playerInventory.unarmedWeapon;

        weaponSlotManager.attackingWeapon = currentWeapon; // For stamina/damage later

        // Check for Backstab (highest priority)
        if (!playerAnimator.IsInMidAction && playerAnimator.IsGrounded && TryPerformBackstab())
        {
            playerAnimator.IsCrouching = false; // Stand up for backstab
            return;
        }

        // Attack / Combo
        PerformAttack(currentWeapon);
    }

    public void PerformAttack(WeaponItem weapon)
    {
        if (weapon == null) return;
        // playerStats.ConsumeStamina(Mathf.RoundToInt(weapon.baseStamina * weapon.lightAttackStaminaMultiplier)); // Stamina handled by Anim Event

        playerAnimator.IsInMidAction = true;
        playerAnimator.ApplyRootMotion(true);
        playerAnimator.TriggerAttack();
    }

    private bool TryPerformBackstab()
    {
        // Raycast forward from player's approximate chest height
        Vector3 rayOrigin = (playerManager.lockOnTransform != null ? playerManager.lockOnTransform.position : playerManager.transform.position + Vector3.up * 1f);

        RaycastHit[] hits = Physics.SphereCastAll(rayOrigin, 0.5f, playerManager.transform.forward, backstabRaycastDistance, backstabLayerMask, QueryTriggerInteraction.Ignore);

        if (hits.Length == 0) return false;

        // Find the closest valid CharacterManager that can be backstabbed
        CharacterManager potentialVictim = null;
        float closestVictimDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            // Ensure we're not hitting ourselves if player is on the backstabLayerMask for some reason
            if (hit.transform.root == playerManager.transform.root) continue;

            CharacterManager targetCharManager = hit.collider.GetComponentInParent<CharacterManager>();
            if (targetCharManager != null && targetCharManager.canBeBackstabbed && !targetCharManager.charAnimManager.IsInMidAction)
            {
                float distanceToHitPoint = Vector3.Distance(playerManager.transform.position, hit.point);
                if (distanceToHitPoint < closestVictimDistance) // Prioritize by actual hit point for spherecast
                {
                    closestVictimDistance = distanceToHitPoint;
                    potentialVictim = targetCharManager;
                }
            }
        }

        if (potentialVictim == null) return false;

        // Check angle and precise distance to the victim's backstab receiver point
        Vector3 directionToVictimReceiver = (potentialVictim.backstabReceiverPoint.position - playerManager.transform.position);
        float distanceToReceiver = directionToVictimReceiver.magnitude;

        if (distanceToReceiver > backstabMaxInteractionDistance)
        {
            return false;
        }

        // Angle check: Player needs to be behind the enemy.
        // Vector from enemy's forward to the player.
        Vector3 victimForward = potentialVictim.transform.forward;
        Vector3 playerRelativePos = playerManager.transform.position - potentialVictim.transform.position;
        playerRelativePos.y = 0; // Flatten for pure horizontal angle
        victimForward.y = 0;     // Flatten

        float angle = Vector3.Angle(victimForward, playerRelativePos.normalized * -1f); // * -1f because we want angle to enemy's back

        if (angle <= backstabMaxAngle)
        {
            ExecuteBackstab(potentialVictim);
            return true;
        }

        return false;
    }

    private void ExecuteBackstab(CharacterManager victim)
    {
        Debug.Log($"Executing backstab on {victim.name}");
        playerAnimator.IsInMidAction = true;
        playerAnimator.IsInvulnerable = true;
        //playerManager.isBeingCriticallyHit = true; // Player is also in a critical sequence
        playerManager.currentBackstabTarget = victim;

        // Snap player to victim's backstab receiver point.
        // The receiver point on the enemy should be positioned where the player *stands* to initiate the backstab.
        // The player should then look towards the enemy's core.
        Vector3 snapPosition = victim.backstabReceiverPoint.position;
        Quaternion snapRotation = Quaternion.LookRotation(victim.transform.position - snapPosition);

        // Coroutine for smooth snapping (optional, direct set is simpler to start)
        // StartCoroutine(SmoothSnap(snapPosition, snapRotation));
        playerManager.transform.position = snapPosition; // Direct snap for now
        playerManager.transform.rotation = snapRotation; // Direct snap


        // Tell victim they are being backstabbed. Victim handles its own animation and orientation.
        victim.GetBackstabbed(playerManager.transform); // Pass player's transform as attacker

        // Play player's backstab animation
        playerAnimator.PlayTargetAnimation(playerBackstabAnimation, true);

        // Animation events on playerBackstabAnimation will call:
        // 1. PlayerManager.AnimEvent_ApplyBackstabDamage()
        // 2. PlayerManager.AnimEvent_FinishPerformingBackstab()
    }
}
