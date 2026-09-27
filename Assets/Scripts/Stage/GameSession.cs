using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WizardArena.Stage
{
    // The state of the current run, one per scene. Anyone may read it and call the commands;
    // only stage logic (StageManager, StagePortal) changes the state.
    public sealed class GameSession : MonoBehaviour
    {
        // Raised with the new state. Not raised for the initial Playing: read State when you subscribe.
        public event Action<GameState> StateChanged;

        public GameState State { get; private set; } = GameState.Playing;
        public bool IsPlaying => State == GameState.Playing;

        // What Retry/Continue actually do, supplied by StageRunner (same assembly) in its
        // Awake. Left null -- e.g. a scene with no StageRunner -- both fall back to reloading
        // the scene from scratch.
        internal IStageProgression Progression { get; set; }

        // Restarts the current stage, in any state.
        public void Retry()
        {
            Time.timeScale = 1f;
            if (Progression != null)
            {
                Progression.RestartCurrentStage();
                return;
            }
            SceneManager.LoadScene(gameObject.scene.path, LoadSceneMode.Single);
        }

        // Goes on to the next stage. Ignored unless the stage was cleared.
        public void Continue()
        {
            if (State != GameState.StageCleared) return;
            if (Progression != null)
            {
                Progression.AdvanceToNextStage();
                return;
            }
            Retry();
        }

        // The first outcome wins: once the stage has ended, later outcomes are ignored.
        internal bool End(GameState outcome)
        {
            if (!IsPlaying || outcome == GameState.Playing) return false;
            State = outcome;
            StateChanged?.Invoke(outcome);
            return true;
        }

        // Called by StageRunner right before it (re)spawns a stage, so any end screen hides
        // and the run continues. Ignored if already Playing (e.g. the very first stage).
        internal void BeginStage()
        {
            if (State == GameState.Playing) return;
            State = GameState.Playing;
            StateChanged?.Invoke(GameState.Playing);
        }
    }
}
