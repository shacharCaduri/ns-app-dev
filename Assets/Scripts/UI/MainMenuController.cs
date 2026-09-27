using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace WizardArena.UI
{
    // The title screen (Assets/Scenes/MainMenu.unity, built by MenuSetup). Play loads the
    // arena; StageRunner always starts a freshly loaded scene at stage 1 (currentIndex 0 in its
    // own Awake), so there is nothing stage-specific to do here. Quit uses the same QuitCommand
    // every other Quit button shares -- this is the only Quit that should actually exit the app.
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuController : MonoBehaviour
    {
        private void Awake()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            root.Q<Button>("menu-play-button").clicked += Play;
            root.Q<Button>("menu-quit-button").clicked += QuitCommand.Execute;
        }

        // Public so the main menu PlayMode test can drive it without simulating a pointer click.
        public void Play() => SceneManager.LoadScene(SceneNames.Arena, LoadSceneMode.Single);
    }
}
