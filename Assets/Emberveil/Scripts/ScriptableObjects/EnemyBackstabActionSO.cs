using UnityEngine;

[CreateAssetMenu(menuName = "SO/AI/Enemy Actions/Backstab Action")]
public class EnemyBackstabActionSO : EnemyActionSO
{
    [Header("Backstab Specifics")]
    public int backstabDamage = 150;
}
