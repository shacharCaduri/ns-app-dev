using UnityEngine;
using WizardArena.Combat;
using WizardArena.Enemies;

namespace WizardArena.Stage
{
    // Runs the stage's win/lose conditions: opens the portal once every required enemy is
    // dead, and ends the run in Defeated when the wizard's Health dies. StagePortal ends the
    // run in StageCleared itself, once the wizard has fully entered it.
    public sealed class StageManager : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private StagePortal portal;
        [SerializeField] private Health playerHealth;

        private void OnEnable()
        {
            EnemyRegistry.Died += OnEnemyDied;
            playerHealth.Died += OnPlayerDied;
        }

        private void OnDisable()
        {
            EnemyRegistry.Died -= OnEnemyDied;
            playerHealth.Died -= OnPlayerDied;
        }

        private void Start()
        {
            // Zero enemies at the start (an already-cleared arena, or a minimal test rig)
            // opens the portal right away instead of waiting for a Died event that never comes.
            CheckPortal();
        }

        private void OnEnemyDied(Enemy enemy)
        {
            CheckPortal();
        }

        private void CheckPortal()
        {
            if (EnemyRegistry.AliveCount == 0) portal.Open();
        }

        private void OnPlayerDied()
        {
            session.End(GameState.Defeated);
        }
    }
}
