using UnityEngine;

namespace WizardArena.Enemies
{
    // The skeleton warrior: walks to the wizard and swings a telegraphed melee attack.
    // Numbers and sprites come from its EnemyConfig (Assets/Config/Enemies/SkeletonWarrior.asset).
    [RequireComponent(typeof(Enemy))]
    public sealed class SkeletonWarriorController : MonoBehaviour, IEnemyBehaviour
    {
        public IEnemyState CreateMainState(Enemy enemy)
        {
            return new SkeletonState(enemy);
        }
    }
}
