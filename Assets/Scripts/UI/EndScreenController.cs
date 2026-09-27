using UnityEngine;
using UnityEngine.UIElements;
using WizardArena.Stage;

namespace WizardArena.UI
{
    // Game Over / Stage Cleared / Victory, driven purely by GameSession.StateChanged. The
    // primary button calls whichever GameSession command fits the state it is showing.
    [RequireComponent(typeof(UIDocument))]
    public sealed class EndScreenController : MonoBehaviour
    {
        private static readonly Color DefeatColor = new Color(1f, 0.4f, 0.42f);
        private static readonly Color TriumphColor = new Color(0.4f, 1f, 0.75f);

        [SerializeField] private GameSession session;

        private VisualElement overlay;
        private Label heading;
        private Button primaryButton;
        private GameState state;

        private void Awake()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            overlay = root.Q<VisualElement>("end-overlay");
            heading = root.Q<Label>("end-heading");
            primaryButton = root.Q<Button>("end-primary-button");
            root.Q<Button>("end-quit-button").clicked += MainMenuCommand.ReturnToMenu;
            SetVisible(false);
        }

        private void OnEnable() => session.StateChanged += OnStateChanged;

        private void OnDisable()
        {
            session.StateChanged -= OnStateChanged;
            primaryButton.clicked -= OnPrimaryClicked;
        }

        private void OnStateChanged(GameState newState)
        {
            state = newState;
            if (newState == GameState.Playing)
            {
                SetVisible(false);
                return;
            }

            primaryButton.clicked -= OnPrimaryClicked;
            primaryButton.clicked += OnPrimaryClicked;
            switch (newState)
            {
                case GameState.Defeated:
                    heading.text = "GAME OVER";
                    heading.style.color = DefeatColor;
                    primaryButton.text = "Retry";
                    break;
                case GameState.Victory:
                    heading.text = "VICTORY!";
                    heading.style.color = TriumphColor;
                    primaryButton.text = "Play Again";
                    break;
                default:
                    heading.text = "STAGE CLEARED";
                    heading.style.color = TriumphColor;
                    primaryButton.text = "Continue";
                    break;
            }
            SetVisible(true);
        }

        private void OnPrimaryClicked()
        {
            switch (state)
            {
                case GameState.StageCleared:
                    session.Continue();
                    break;
                case GameState.Victory:
                    // "Play Again" means the whole run, not just the last stage.
                    session.RestartRun();
                    break;
                default:
                    session.Retry();
                    break;
            }
        }

        private void SetVisible(bool visible) => overlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
