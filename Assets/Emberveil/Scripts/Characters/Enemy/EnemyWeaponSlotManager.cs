using UnityEngine;

public class EnemyWeaponSlotManager : MonoBehaviour
{
    public EnemyManager enemyManager;
    public WeaponItem rightHandWeapon;

    private WeaponHolderSlot rightHandSlot;
    private DamageCollider rightHandDamageCollider;

    private void Start()
    {
        LoadWeaponOnBothHands();
    }

    private void Awake()
    {
        WeaponHolderSlot[] weaponHolderSlots = GetComponentsInChildren<WeaponHolderSlot>();
        foreach (WeaponHolderSlot weaponSlot in weaponHolderSlots)
        {
            rightHandSlot = weaponSlot;
        }
    }

    public void LoadWeaponOnBothHands()
    {
        if (rightHandWeapon != null)
        {
            LoadWeaponOnSlot(rightHandWeapon, false);
        }
    }

    public void LoadWeaponOnSlot(WeaponItem weapon, bool isLeft)
    {
        rightHandSlot.LoadWeaponModel(weapon);
        LoadWeaponsDamageCollider(false);
    }

    public void LoadWeaponsDamageCollider(bool isLeft)
    {
        rightHandDamageCollider = rightHandSlot.currentWeaponModel.GetComponentInChildren<DamageCollider>();
        if (rightHandDamageCollider != null)
        {
            rightHandDamageCollider.Wielder = enemyManager;
            rightHandDamageCollider.OnDamageableHit += HandleRightHandHit;
        }
    }

    private void HandleRightHandHit(Collider victimCollider)
    {
        enemyManager.Combat.ProcessHit(victimCollider, rightHandWeapon);
    }

    public void OpenDamageCollider()
    {
        rightHandDamageCollider.EnableDamageCollider();
    }

    public void CloseDamageCollider()
    {
        rightHandDamageCollider.DisableDamageCollider();
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