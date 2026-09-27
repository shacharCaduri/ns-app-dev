using UnityEngine;
using UnityEngine.UIElements;
using WizardArena.Combat;

namespace WizardArena.UI
{
    // The always-on HUD: wizard HP bar and numbers, stage name, controls hint. Reacts only to
    // Health events -- it never polls Health every frame.
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private string stageName = "Ruins Antechamber";

        private VisualElement hpFill;
        private Label hpText;

        private void Awake()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            hpFill = root.Q<VisualElement>("hp-bar-fill");
            hpText = root.Q<Label>("hp-text");
            root.Q<Label>("stage-name").text = stageName;
        }

        private void OnEnable()
        {
            playerHealth.Damaged += OnDamaged;
            playerHealth.Healed += OnHealed;
            playerHealth.Died += Refresh;
        }

        private void OnDisable()
        {
            playerHealth.Damaged -= OnDamaged;
            playerHealth.Healed -= OnHealed;
            playerHealth.Died -= Refresh;
        }

        // Not OnEnable: Health.Awake (which sets Current = Max) is not guaranteed to have run
        // yet for another GameObject at that point. Start runs after every Awake in the scene.
        private void Start() => Refresh();

        private void OnDamaged(DamageInfo damage) => Refresh();

        private void OnHealed(int amountHealed) => Refresh();

        private void Refresh()
        {
            int max = Mathf.Max(1, playerHealth.Max);
            float fraction = Mathf.Clamp01((float)playerHealth.Current / max);
            hpFill.style.width = Length.Percent(fraction * 100f);
            hpText.text = $"{playerHealth.Current} / {max}";
        }
    }
}
