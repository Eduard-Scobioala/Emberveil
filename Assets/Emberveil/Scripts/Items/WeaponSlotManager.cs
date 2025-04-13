using UnityEngine;

public class WeaponSlotManager : MonoBehaviour
{
    private WeaponHolderSlot leftHandSlot;
    private WeaponHolderSlot rightHandSlot;
    private WeaponHolderSlot backSlot;

    private DamageCollider leftHandDamageCollider;
    private DamageCollider rightHandDamageCollider;

    private Animator animator;
    private QuickSlotsUI quickSlotsUI;
    private PlayerStats playerStats;

    [SerializeField] private PlayerInventory playerInventory;

    public WeaponItem attackingWeapon = null;

    private bool isTwoHanding = false;
    

    private void Awake()
    {
        WeaponHolderSlot[] weaponHolderSlots = GetComponentsInChildren<WeaponHolderSlot>();
        foreach (WeaponHolderSlot weaponSlot in weaponHolderSlots)
        {
            if (weaponSlot.isLeftHandSlot)
            {
                leftHandSlot = weaponSlot;
            }
            else if (weaponSlot.isRightHandSlot)
            {
                rightHandSlot = weaponSlot;
            }
            else if (weaponSlot.isBackSlot)
            {
                backSlot = weaponSlot;
            }
        }

        animator = GetComponent<Animator>();
        quickSlotsUI = FindObjectOfType<QuickSlotsUI>();
        playerStats = GetComponentInParent<PlayerStats>();
    }

    private void OnEnable()
    {
        InputHandler.TwoHandingButtonPressed += HandleTwoHandingButtonPressed;
    }

    private void OnDisable()
    {
        InputHandler.TwoHandingButtonPressed -= HandleTwoHandingButtonPressed;
    }

    public void LoadWeaponOnSlot(WeaponItem weaponItem, bool isLeft)
    {
        if (isLeft)
        {
            leftHandSlot.currentWeapon = weaponItem;
            leftHandSlot.LoadWeaponModel(weaponItem);
            LoadLeftWeaponDamageCollider();
            quickSlotsUI.UpdateWeaponQuickSlotsUI(weaponItem, true);
            #region Handle Left Weapon Idle Animations
            if (weaponItem !=  null)
            {
                animator.CrossFade(weaponItem.Left_Arm_Idle, 0.2f);
            }
            else
            {
                animator.CrossFade("Left Arm Empty", 0.2f);
            }
            #endregion
        }
        else
        {
            if (isTwoHanding)
            {
                backSlot.LoadWeaponModel(leftHandSlot.currentWeapon);
                leftHandSlot.UnloadWeaponAndDestroy();
                animator.CrossFade(weaponItem.Two_Handed_Idle, 0.2f);
            }
            else
            {
                #region Handle Right Weapon Idle Animations

                animator.CrossFade("Both Arms Empty", .2f);

                backSlot.UnloadWeaponAndDestroy();

                if (weaponItem != null)
                {
                    animator.CrossFade(weaponItem.Right_Arm_Idle, 0.2f);
                }
                else
                {
                    animator.CrossFade("Right Arm Empty", 0.2f);
                }
                #endregion
            }

            rightHandSlot.currentWeapon = weaponItem;
            rightHandSlot.LoadWeaponModel(weaponItem);
            LoadRightWeaponDamageCollider();
            quickSlotsUI.UpdateWeaponQuickSlotsUI(weaponItem, false);
        }
    }

    private void HandleTwoHandingButtonPressed()
    {
        isTwoHanding = !isTwoHanding;

        if (isTwoHanding)
        {
            LoadWeaponOnSlot(playerInventory.RightHandWeapon, false);
        }
        else
        {
            LoadWeaponOnSlot(playerInventory.RightHandWeapon, false);
            LoadWeaponOnSlot(playerInventory.LeftHandWeapon, true);
        }
    }

    #region Handle Weapon's Damage Collider

    private void LoadLeftWeaponDamageCollider()
    {
        leftHandDamageCollider = leftHandSlot.currentWeaponModel.GetComponentInChildren<DamageCollider>();
    }

    private void LoadRightWeaponDamageCollider()
    {
        rightHandDamageCollider = rightHandSlot.currentWeaponModel.GetComponentInChildren<DamageCollider>();
    }

    public void OpenLeftDamageCollider()
    {
        leftHandDamageCollider.EnableDamageCollider();
    }

    public void OpenRightDamageCollider()
    {
        rightHandDamageCollider.EnableDamageCollider();
    }

    public void CloseLeftDamageCollider()
    {
        leftHandDamageCollider.DisableDamageCollider();
    }

    public void CloseRightDamageCollider()
    {
        rightHandDamageCollider.DisableDamageCollider();
    }

    #endregion

    #region Handle Weapon's Stamina Consumption
    public void DrainStaminaLightAttack()
    {
        if (attackingWeapon != null)
        {
            playerStats.ConsumeStamina(Mathf.RoundToInt(attackingWeapon.baseStamina * attackingWeapon.lightAttackMultiplier));
        }
    }

    public void DrainStaminaHeavyAttack()
    {
        if (attackingWeapon != null)
        {
            playerStats.ConsumeStamina(Mathf.RoundToInt(attackingWeapon.baseStamina * attackingWeapon.heavyAttackMultiplier));
        }
    }
    #endregion
}
