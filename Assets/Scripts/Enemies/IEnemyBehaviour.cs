namespace WizardArena.Enemies
{
    // Put one component implementing this next to Enemy to give an enemy type its
    // normal behaviour (e.g. the bat's wandering flight). Enemy runs the shared hurt
    // and death states itself and returns to this state after a non-lethal hit.
    public interface IEnemyBehaviour
    {
        IEnemyState CreateMainState(Enemy enemy);
    }
}
