using UnityEngine;

[CreateAssetMenu(menuName = "SO/AI/Enemy Actions/Attack Action")]
public class EnemyAttackActionSO : EnemyActionSO
{
    [Header("Attack Specifics")]
    public int attackScore = 10; // For AI decision weighting
    public float minAttackDistance = 0f;
    public float maxAttackDistance = 3f;
    public float minAttackAngle = -35f; // Relative to forward
    public float maxAttackAngle = 35f;  // Relative to forward

    public int damage = 20;
    public DamageType damageType = DamageType.Standard;
    public float poiseDamage = 10f;

    [Tooltip("Time after animation start to enable damage collider")]
    public float damageColliderStartTime = 0.2f;
    [Tooltip("Time after animation start to disable damage collider")]
    public float damageColliderEndTime = 0.5f;

    public bool canBeParried = true;
    public bool isHeavyAttack = false;
}
