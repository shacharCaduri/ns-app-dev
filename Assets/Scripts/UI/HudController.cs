using UnityEngine;
using UnityEngine.UIElements;
using WizardArena.Combat;
using WizardArena.Stage;

namespace WizardArena.UI
{
    // The always-on HUD: wizard HP bar and numbers, stage name, controls hint. Reacts only to
    // Health/StageRunner events -- it never polls either every frame.
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private string stageName = "Ruins Antechamber";
        [Tooltip("Optional: keeps the stage name in sync as stages change. Without one, the " +
                 "name above is shown as-is (e.g. a minimal scene with a single stage).")]
        [SerializeField] private StageRunner stageRunner;

        private VisualElement hpFill;
        private Label hpText;
        private Label stageNameLabel;

        private void Awake()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            hpFill = root.Q<VisualElement>("hp-bar-fill");
            hpText = root.Q<Label>("hp-text");
            stageNameLabel = root.Q<Label>("stage-name");
            stageNameLabel.text = stageName;
        }

        private void OnEnable()
        {
            playerHealth.Damaged += OnDamaged;
            playerHealth.Healed += OnHealed;
            playerHealth.Died += Refresh;
            if (stageRunner != null) stageRunner.StageStarted += OnStageStarted;
        }

        private void OnDisable()
        {
            playerHealth.Damaged -= OnDamaged;
            playerHealth.Healed -= OnHealed;
            playerHealth.Died -= Refresh;
            if (stageRunner != null) stageRunner.StageStarted -= OnStageStarted;
        }

        // Not OnEnable: Health.Awake (which sets Current = Max) and StageRunner.Awake (which
        // picks the first stage) are not guaranteed to have run yet for another GameObject at
        // that point. Start runs after every Awake in the scene.
        private void Start()
        {
            Refresh();
            if (stageRunner != null) SetStageName(stageRunner.CurrentStage.DisplayName);
        }

        private void OnDamaged(DamageInfo damage) => Refresh();

        private void OnHealed(int amountHealed) => Refresh();

        private void OnStageStarted(StageDefinition stage) => SetStageName(stage.DisplayName);

        // Called from Start, or whenever StageRunner starts a new stage.
        public void SetStageName(string name)
        {
            stageName = name;
            if (stageNameLabel != null) stageNameLabel.text = name;
        }

        private void Refresh()
        {
            int max = Mathf.Max(1, playerHealth.Max);
            float fraction = Mathf.Clamp01((float)playerHealth.Current / max);
            hpFill.style.width = Length.Percent(fraction * 100f);
            hpText.text = $"{playerHealth.Current} / {max}";
        }
    }
}
