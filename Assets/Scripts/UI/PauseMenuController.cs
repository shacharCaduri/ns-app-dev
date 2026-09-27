using UnityEngine;
using UnityEngine.UIElements;
using WizardArena.Player;
using WizardArena.Stage;

namespace WizardArena.UI
{
    // Esc toggles a pause overlay: freezes the game (Time.timeScale = 0) and swaps the wizard's
    // input for a no-op one, so held movement/cast keys have no effect while paused.
    [RequireComponent(typeof(UIDocument))]
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private WizardController player;

        private VisualElement overlay;
        private IPlayerInput liveInput;

        public bool IsPaused { get; private set; }

        private void Awake()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            overlay = root.Q<VisualElement>("pause-overlay");
            root.Q<Button>("resume-button").clicked += Resume;
            root.Q<Button>("retry-button-pause").clicked += session.Retry;
            root.Q<Button>("quit-button-pause").clicked += MainMenuCommand.ReturnToMenu;
            liveInput = player.GetComponent<IPlayerInput>();
            SetVisible(false);
        }

        private void Update()
        {
            // Can't open the pause menu from an end screen; always allow closing it.
            if (!IsPaused && session.State != GameState.Playing) return;
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
        }

        public void TogglePause() => SetPaused(!IsPaused);

        public void Resume() => SetPaused(false);

        private void SetPaused(bool paused)
        {
            if (paused == IsPaused) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            player.UseInput(paused ? NullInput.Instance : liveInput);
            SetVisible(paused);
        }

        private void SetVisible(bool visible) => overlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        // A do-nothing input source so held keys have no effect while paused.
        private sealed class NullInput : IPlayerInput
        {
            public static readonly NullInput Instance = new NullInput();
            public float Horizontal => 0f;
            public bool JumpPressed => false;
            public bool CastPressed => false;
        }
    }
}
