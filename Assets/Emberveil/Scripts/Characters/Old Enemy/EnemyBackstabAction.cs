using UnityEngine;

[CreateAssetMenu(menuName = "AI/Enemy Actions/Backstab Action")]
public class EnemyBackstabAction : EnemyAction
{
    public float recoveryTime = 3f;
    public int backstabDamage = 150;
}
