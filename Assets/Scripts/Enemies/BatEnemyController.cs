using UnityEngine;

namespace WizardArena.Enemies
{
    // The cave bat: an Enemy whose normal behaviour is wandering flight.
    // Numbers and sprites come from its EnemyConfig (Assets/Config/Enemies/Bat.asset).
    [RequireComponent(typeof(Enemy))]
    public sealed class BatEnemyController : MonoBehaviour, IEnemyBehaviour
    {
        public IEnemyState CreateMainState(Enemy enemy)
        {
            return new FlyingWanderState(enemy);
        }
    }
}
