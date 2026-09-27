using UnityEngine;
using UnityEngine.SceneManagement;

namespace WizardArena.UI
{
    // Returns to the main menu from anywhere in a run (pause menu, end screens). Distinct from
    // QuitCommand, which only the main menu's own Quit button uses to leave the app entirely.
    internal static class MainMenuCommand
    {
        internal static void ReturnToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneNames.MainMenu, LoadSceneMode.Single);
        }
    }
}
