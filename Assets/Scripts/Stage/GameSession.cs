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

        // Restarts the stage from scratch (reloads the scene), in any state.
        public void Retry()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameObject.scene.path, LoadSceneMode.Single);
        }

        // Goes on to the next stage. There is only one stage until [10] adds real progression,
        // so for now this just replays it, same as Retry. Ignored unless the stage was cleared.
        public void Continue()
        {
            if (State != GameState.StageCleared) return;
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
    }
}
