using UnityEngine;

public class WeaponSlotManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Animator animator;

    [Header("Weapon Slots")]
    [SerializeField] private WeaponHolderSlot rightHandSlot;
    // [SerializeField] private WeaponHolderSlot leftHandSlot; // For shield later
    // [SerializeField] private WeaponHolderSlot backSlot; // If you have visible sheathing

    private DamageCollider rightHandDamageCollider;
    // private DamageCollider leftHandDamageCollider; // For shield bash later

    public WeaponItem attackingWeapon { get; set; } // Set by PlayerAttacker

    // isTwoHanding concept might be simplified or removed if only one weapon
    // public bool isTwoHanding = false;

    private void Awake()
    {
        if (playerManager == null) playerManager = GetComponentInParent<PlayerManager>();
        if (playerInventory == null) playerInventory = GetComponentInParent<PlayerInventory>();
        if (playerStats == null) playerStats = GetComponentInParent<PlayerManager>().GetComponent<PlayerStats>();
        if (animator == null && playerManager != null) animator = playerManager.GetComponentInChildren<Animator>(); // Get player animator

        if (rightHandSlot == null) Debug.LogError("WeaponSlotManager: Right Hand Slot not assigned!");
    }

    public void LoadWeaponOnSlot(WeaponItem weaponItem, bool isRightHand)
    {
        if (!isRightHand)
        {
            // Handle left hand (shield) later
            return;
        }

        if (rightHandSlot == null) return;

        // Unsubscribe from old collider's event if it exists
        if (rightHandDamageCollider != null)
        {
            rightHandDamageCollider.OnDamageableHit -= HandleRightHandHit;
        }

        rightHandSlot.LoadWeaponModel(weaponItem);
        attackingWeapon = weaponItem ?? playerInventory.unarmedWeaponData;

        LoadRightWeaponDamageCollider(); // This finds/initializes the new collider
        //UpdateAnimatorOverrides(attackingWeapon); // Use the potentially unarmed weapon data
    }

    private void LoadRightWeaponDamageCollider()
    {
        DamageCollider newCollider;
        if (rightHandSlot.currentWeaponModel != null)
        {
            newCollider = rightHandSlot.currentWeaponModel.GetComponentInChildren<DamageCollider>();
        }
        else // Unarmed: Try to find a persistent DamageCollider on the player's hand model
        {
            newCollider = playerManager.GetComponentInChildren<DamageCollider>(true); // Find inactive ones too
            if (newCollider != null && newCollider.transform.parent != rightHandSlot.transform) // Ensure it's not a weapon's collider
            {
                // This is likely the fist collider.
            }
            else
            {
                newCollider = null; // Didn't find a dedicated fist collider
            }
        }

        rightHandDamageCollider = newCollider;

        if (rightHandDamageCollider != null)
        {
            rightHandDamageCollider.Wielder = playerManager; // Set the wielder
            rightHandDamageCollider.OnDamageableHit += HandleRightHandHit; // Subscribe to the event
        }
        else
        {
            Debug.LogWarning("No DamageCollider found for right hand/unarmed.");
        }
    }

    private void HandleRightHandHit(Collider victimCollider)
    {
        playerManager.playerAttacker.ProcessHit(victimCollider, attackingWeapon);
    }

    // Left hand would be similar if/when shields are added
    // private void LoadLeftWeaponDamageCollider() { ... }

    public void OpenRightHandDamageCollider()
    {
        if (rightHandDamageCollider != null)
        {
            rightHandDamageCollider.EnableDamageCollider();
        }
        else
        {
            Debug.LogWarning("Attempted to open Right Hand Damage Collider, but it's null.");
        }
    }

    public void CloseRightHandDamageCollider()
    {
        if (rightHandDamageCollider != null)
        {
            rightHandDamageCollider.DisableDamageCollider();
        }
    }

    // Open/CloseLeftHandDamageCollider for shields later

    // Stamina consumption will be triggered by Animation Events on PlayerAnimator
    public void DrainStaminaForAttack(PlayerAttackType attackType)
    {
        if (attackingWeapon == null || playerStats == null) return;

        float multiplier = attackType switch
        {
            PlayerAttackType.LightAttack => attackingWeapon.lightAttackStaminaMultiplier,
            PlayerAttackType.RollAttack => attackingWeapon.rollAttackStaminaMultiplier,
            PlayerAttackType.BackstepAttack => attackingWeapon.backstepAttackStaminaMultiplier,
            PlayerAttackType.JumpAttack => attackingWeapon.jumpAttackStaminaMultiplier,
            _ => 1f
        };

        playerStats.ConsumeStamina(attackingWeapon.baseStamina * multiplier);
    }
}
