public interface IEnemyState
{
    void Enter(EnemyManager manager);
    void Tick();              // For Update logic
    void FixedTick();         // For FixedUpdate logic
    IEnemyState Transition(); // Returns new state if transition occurs, else null
    void Exit();
}
