using UnityEngine;

namespace WizardArena.Enemies
{
    // The blue slime: a slow ground patroller that hops toward the wizard once close.
    // Numbers and sprites come from its EnemyConfig (Assets/Config/Enemies/BlueSlime.asset).
    [RequireComponent(typeof(Enemy))]
    public sealed class BlueSlimeController : MonoBehaviour, IEnemyBehaviour
    {
        public IEnemyState CreateMainState(Enemy enemy)
        {
            return new SlimeState(enemy);
        }
    }
}
