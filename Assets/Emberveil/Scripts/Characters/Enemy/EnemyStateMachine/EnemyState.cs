
public abstract class EnemyState
{
    protected EnemyManager enemyManager;
    protected EnemyLocomotion enemyLocomotion;
    protected EnemyAnimator enemyAnimator;

    public virtual void EnterState(EnemyManager enemy)
    {
        enemyManager = enemy;
        enemyLocomotion = enemy.GetComponent<EnemyLocomotion>();
        enemyAnimator = enemy.GetComponentInChildren<EnemyAnimator>();
    }

    public abstract void UpdateState();
    public abstract void FixedUpdateState();
    public abstract void ExitState();

    public virtual void CheckStateTransitions() { }

    public virtual void OnDamageReceived() { }
}
