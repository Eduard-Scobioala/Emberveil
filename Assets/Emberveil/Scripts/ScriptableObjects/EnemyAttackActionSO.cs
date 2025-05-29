using UnityEngine;

[CreateAssetMenu(menuName = "SO/AI/Enemy Actions/Attack Action")]
public class EnemyAttackActionSO : EnemyActionSO
{
    [Header("Attack Specifics")]
    public float minAttackDistance = 0f;
    public float maxAttackDistance = 3f;
    public float minAttackAngle = -35f;
    public float maxAttackAngle = 35f;

    public int damage = 20;
    public DamageType damageType = DamageType.Standard;
    public float poiseDamage = 10f;

    public bool canBeParried = true;
    public bool isHeavyAttack = false;

    [Header("Scoring Properties")]
    public int baseScore = 10; // Default desirability
    [Tooltip("Multiplier if target is close (e.g., within 0.5 * maxAttackDistance)")]
    public float closeRangeScoreMultiplier = 1.0f;
    [Tooltip("Multiplier if target is at medium range")]
    public float midRangeScoreMultiplier = 1.0f;
    [Tooltip("Multiplier if target is at far range (e.g., beyond 0.75 * maxAttackDistance but still in range)")]
    public float farRangeScoreMultiplier = 1.0f;

    [Tooltip("Multiplier if target's health is low (e.g., below 30%)")]
    public float targetLowHealthScoreMultiplier = 1.0f;
    [Tooltip("Multiplier if this enemy's health is low")]
    public float selfLowHealthScoreMultiplier = 1.0f;

    [Tooltip("Cooldown specifically for this attack after it's used (0 means use general combat cooldown)")]
    public float specificAttackCooldown = 0f;

    public virtual int CalculateScore(EnemyManager self, CharacterManager target, float distanceToTarget, float angleToTarget)
    {
        if (target == null) return 0;

        int currentScore = baseScore;

        // Range-based scoring
        if (distanceToTarget <= minAttackDistance + (maxAttackDistance - minAttackDistance) * 0.33f) // Close
            currentScore = Mathf.RoundToInt(currentScore * closeRangeScoreMultiplier);
        else if (distanceToTarget <= minAttackDistance + (maxAttackDistance - minAttackDistance) * 0.66f) // Mid
            currentScore = Mathf.RoundToInt(currentScore * midRangeScoreMultiplier);
        else // Far
            currentScore = Mathf.RoundToInt(currentScore * farRangeScoreMultiplier);

        // Target health-based scoring
        CharacterStats targetStats = target.GetComponent<CharacterStats>();
        if (targetStats != null && targetStats.currentHealth / (float)targetStats.maxHealth < 0.3f) // Target health < 30%
        {
            currentScore = Mathf.RoundToInt(currentScore * targetLowHealthScoreMultiplier);
        }

        // Self health-based scoring
        if (self.Stats != null && self.Stats.currentHealth / (float)self.Stats.maxHealth < 0.3f) // Self health < 30%
        {
            currentScore = Mathf.RoundToInt(currentScore * selfLowHealthScoreMultiplier);
        }

        // TODO: More complex scoring logic
        // e.g., if player is casting, bonus to fast attacks
        // e.g., if player just whiffed an attack, bonus to counter-attacks
        // e.g., if this attack is on its specific cooldown, return 0 or very low score

        return Mathf.Max(0, currentScore);
    }

    public bool IsBasicConditionsMet(float distanceToTarget, float angleToTarget)
    {
        return distanceToTarget >= minAttackDistance && distanceToTarget <= maxAttackDistance &&
               angleToTarget >= minAttackAngle && angleToTarget <= maxAttackAngle;
    }
}