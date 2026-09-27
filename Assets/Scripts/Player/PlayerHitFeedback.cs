using UnityEngine;
using WizardArena.Combat;

namespace WizardArena.Player
{
    // The red flash and squash pulse after the wizard takes damage.
    [RequireComponent(typeof(Health), typeof(SpriteRenderer))]
    public sealed class PlayerHitFeedback : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;

        private Health health;
        private SpriteRenderer spriteRenderer;
        private Vector3 normalScale;
        private float remaining;
        private bool needsReset;

        public bool IsPlaying => remaining > 0f;

        private void Awake()
        {
            health = GetComponent<Health>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            normalScale = transform.localScale;
        }

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
        }

        // Stops the effect without restoring the look (a transition takes over).
        internal void Cancel()
        {
            remaining = 0f;
            needsReset = false;
        }

        internal void Tick(float deltaTime)
        {
            if (remaining <= 0f)
            {
                if (needsReset)
                {
                    transform.localScale = normalScale;
                    spriteRenderer.color = Color.white;
                    needsReset = false;
                }
                return;
            }

            remaining -= deltaTime;
            needsReset = true;
            float progress = 1f - remaining / config.HitFeedbackSeconds;
            float pulse = Mathf.Sin(progress * Mathf.PI * 4f) * (1f - progress);
            Vector2 squash = config.HitSquash;
            transform.localScale = Vector3.Scale(normalScale, new Vector3(1f + pulse * squash.x, 1f - pulse * squash.y, 1f));
            spriteRenderer.color = Color.Lerp(config.HitFlashColor, Color.white, progress);
        }

        private void OnDamaged(DamageInfo damage)
        {
            remaining = config.HitFeedbackSeconds;
        }
    }
}
