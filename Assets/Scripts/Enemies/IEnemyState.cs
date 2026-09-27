namespace WizardArena.Enemies
{
    // One behaviour of an enemy (wander, hurt, dead...). Driven by an EnemyStateMachine.
    public interface IEnemyState
    {
        void Enter();
        void Tick(float deltaTime);
        void Exit();
    }
}
