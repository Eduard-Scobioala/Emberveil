using UnityEngine;

[CreateAssetMenu(menuName = "Items/Weapon Item")]
public class WeaponItem : Item
{
    public GameObject modelPrefab;
    public bool isUnarmed;

    [Header("Idle Animations")]
    public string Right_Arm_Idle;
    public string Left_Arm_Idle;
    public string Two_Handed_Idle;

    [Header("One Handed Attack Animations")]
    public string OH_Light_Attack_01;
    public string OH_Light_Attack_02;
    public string TH_Light_Attack_01;
    public string TH_Light_Attack_02;
    public string OH_Heavy_Attack_01;

    [Header("Stamina Costs")]
    public int baseStamina;
    public float lightAttackStaminaMultiplier;
    public float heavyAttackStaminaMultiplier;

    [Header("Damage Stats")]
    public int lightAttackDmg;
    public int heavyAttackDmg;
    public int critDmgMultiplier;

    public int GetBackstabDmg()
    {
        return lightAttackDmg * critDmgMultiplier;
    }
}
