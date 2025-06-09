using UnityEngine;

[CreateAssetMenu(menuName = "Items/Weapon Item")]
public class WeaponItem : Item
{
    public GameObject modelPrefab;
    public bool isUnarmed;

    [Header("Idle Animations")]
    public string Right_Arm_Idle;
    public string Two_Handed_Idle;

    [Header("Attack Animations")]
    public string OH_Light_Attack_01;
    public string OH_Light_Attack_02;
    public string TH_Light_Attack_01;
    public string TH_Light_Attack_02;

    public string OH_Roll_Attack_01;
    public string OH_Backstep_Attack_01;
    public string OH_Jump_Attack_Start;
    public string OH_Jump_Attack_Idle;
    public string OH_Jump_Attack_End;

    [Header("Stamina Costs")]
    public int baseStamina;
    public float lightAttackStaminaMultiplier;
    public float rollAttackStaminaMultiplier = 1.2f;
    public float backstepAttackStaminaMultiplier = 1.0f;
    public float jumpAttackStaminaMultiplier = 1.1f;

    [Header("Damage Stats")]
    public int lightAttackDmg = 10;
    public int rollAttackDmg = 12;
    public int backstepAttackDmg = 10;
    public int jumpAttackDmg = 15;
    public int critDmgMultiplier = 2;

    public int GetBackstabDmg()
    {
        return (lightAttackDmg > 0 ? lightAttackDmg : 5) * critDmgMultiplier;
    }
}
