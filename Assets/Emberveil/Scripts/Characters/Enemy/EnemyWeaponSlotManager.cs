using UnityEngine;

public class EnemyWeaponSlotManager : MonoBehaviour
{
    public EnemyManager enemyManager;
    public WeaponItem rightHandWeapon;

    private WeaponHolderSlot rightHandSlot;
    private DamageCollider rightHandDamageCollider;

    private void Awake()
    {
        if (enemyManager == null)
            enemyManager = GetComponentInParent<EnemyManager>();
        if (enemyManager == null)
            Debug.LogError("EnemyWeaponSlotManager: EnemyManager reference not set!", this);

        rightHandSlot = GetComponentInChildren<WeaponHolderSlot>();
        if (rightHandSlot == null)
            Debug.LogError("EnemyWeaponSlotManager: No WeaponHolderSlot found in children!", this);
    }

    private void Start()
    {
        if (rightHandWeapon != null && rightHandSlot != null)
        {
            LoadWeaponOnSlot(rightHandWeapon, WeaponHand.Right);
        }
    }

    public void LoadWeaponOnSlot(WeaponItem weapon, WeaponHand hand)
    {
        if (hand == WeaponHand.Right && rightHandSlot != null)
        {
            if (rightHandDamageCollider != null)
            {
                rightHandDamageCollider.OnDamageableHit -= HandleRightHandHit;
            }

            rightHandSlot.LoadWeaponModel(weapon);
            rightHandDamageCollider = null;

            if (rightHandSlot.currentWeaponModel != null)
            {
                rightHandDamageCollider = rightHandSlot.currentWeaponModel.GetComponentInChildren<DamageCollider>();
                if (rightHandDamageCollider != null)
                {
                    rightHandDamageCollider.Wielder = enemyManager; // Set wielder
                    rightHandDamageCollider.OnDamageableHit += HandleRightHandHit; // Subscribe
                }
            }
            else if (weapon != null && weapon.isUnarmed) // Handle unarmed "fist" collider for enemy
            {

            }
        }
    }

    private void HandleRightHandHit(Collider victimCollider)
    {
        if (enemyManager != null && enemyManager.Combat != null)
        {
            enemyManager.Combat.ProcessHit(victimCollider, rightHandWeapon); 
        }
    }

    public void OpenDamageCollider(WeaponHand hand)
    {
        if (hand == WeaponHand.Right && rightHandDamageCollider != null)
        {
            rightHandDamageCollider.EnableDamageCollider();
        }
    }

    public void CloseDamageCollider(WeaponHand hand)
    {
        if (hand == WeaponHand.Right && rightHandDamageCollider != null)
        {
            rightHandDamageCollider.DisableDamageCollider();
        }
    }

    #region Handle Weapon's Stamina Consumption
    public void DrainStaminaLightAttack()
    {

    }

    public void DrainStaminaHeavyAttack()
    {

    }
    #endregion

    public void EnableCombo()
    {
        //anim.SetBool("canDoCombo", true);
    }

    public void DisableCombo()
    {
        //anim.SetBool("canDoCombo", false);
    }
}