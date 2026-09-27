namespace WizardArena.Enemies
{
    // Runs exactly one state at a time. Changing to the current state re-enters it
    // (a second hit while hurt restarts the hurt reaction).
    public sealed class EnemyStateMachine
    {
        public IEnemyState Current { get; private set; }

        public void ChangeTo(IEnemyState next)
        {
            Current?.Exit();
            Current = next;
            Current?.Enter();
        }

        public void Tick(float deltaTime)
        {
            Current?.Tick(deltaTime);
        }
    }
}
