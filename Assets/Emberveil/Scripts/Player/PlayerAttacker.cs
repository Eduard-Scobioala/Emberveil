using System;
using UnityEngine;

public class PlayerAttacker : MonoBehaviour
{
    private AnimatorHandler animatorHandler;
    private WeaponSlotManager weaponSlotManager;
    private InputHandler inputHandler;
    private PlayerManager playerManager;
    private PlayerInventory playerInventory;

    private string lastAttack;
    private bool comboFlag;

    private void Awake()
    {
        animatorHandler = GetComponentInChildren<AnimatorHandler>();
        weaponSlotManager = GetComponentInChildren<WeaponSlotManager>();
        inputHandler = GetComponent<InputHandler>();
        playerManager = GetComponent<PlayerManager>();
        playerInventory = GetComponent<PlayerInventory>();
    }

    public void HandleLightAttackButtonPressed()
    {
        if (playerManager.canDoCombo)
        {
            comboFlag = true;
            HandleWeaponCombo(playerInventory.RightHandWeapon);
            comboFlag = false;
        }
        else
        {
            if (playerManager.canDoCombo || playerManager.isInMidAction)
                return;
            LightAttack(playerInventory.RightHandWeapon);
        }
    }

    public void HandleHeavyAttackButtonPressed()
    {
        HeavyAttack(playerInventory.RightHandWeapon);
    }

    public void HandleWeaponCombo(WeaponItem weaponItem)
    {
        if (comboFlag)
        {
            animatorHandler.anim.SetBool("canDoCombo", false);

            if (lastAttack == weaponItem.OH_Light_Attack_01)
            {
                animatorHandler.PlayTargetAnimation(weaponItem.OH_Light_Attack_02, true);
            }
        }
    }

    public void LightAttack(WeaponItem weapon)
    {
        weaponSlotManager.attackingWeapon = weapon;
        animatorHandler.PlayTargetAnimation(weapon.OH_Light_Attack_01, true);
        lastAttack = weapon.OH_Light_Attack_01;
    }

    public void HeavyAttack(WeaponItem weapon)
    {
        weaponSlotManager.attackingWeapon = weapon;
        animatorHandler.PlayTargetAnimation(weapon.OH_Heavy_Attack_01, true);
        lastAttack = weapon.OH_Heavy_Attack_01;
    }
}
