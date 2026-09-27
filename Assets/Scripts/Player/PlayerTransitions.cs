using System;
using UnityEngine;
using WizardArena.Combat;

namespace WizardArena.Player
{
    // The shimmer-in on spawn and the shrink-away into a point (portal or death).
    // Health ignores damage unless the wizard is fully in play.
    [RequireComponent(typeof(Health), typeof(SpriteRenderer))]
    public sealed class PlayerTransitions : MonoBehaviour
    {
        private enum Phase { Appearing, InPlay, Vanishing, Vanished }

        [SerializeField] private PlayerConfig config;

        private Health health;
        private SpriteRenderer spriteRenderer;
        private Vector3 normalScale;
        private Phase phase = Phase.Appearing;
        private float time;
        private Vector3 vanishFrom;
        private Vector3 vanishTo;

        public event Action Appeared;
        public event Action Vanished;

        public bool IsInPlay => phase == Phase.InPlay;
        public bool HasVanished => phase == Phase.Vanished;

        private void Awake()
        {
            health = GetComponent<Health>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            normalScale = transform.localScale;
            health.enabled = false;
            spriteRenderer.color = new Color(0.4f, 0.9f, 1f, 0f);
        }

        // Starts shrinking into the given point; ignored if already on the way out.
        internal void VanishInto(Vector3 destination)
        {
            if (phase == Phase.Vanishing || phase == Phase.Vanished) return;
            phase = Phase.Vanishing;
            time = 0f;
            vanishFrom = transform.position;
            vanishTo = destination;
            health.enabled = false;
            Collider2D body = GetComponent<Collider2D>();
            if (body != null) body.enabled = false;
        }

        internal void Tick(float deltaTime)
        {
            if (phase == Phase.InPlay || phase == Phase.Vanished) return;
            bool appearing = phase == Phase.Appearing;
            time += deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / (appearing ? config.AppearSeconds : config.VanishSeconds)));
            float visibility = appearing ? t : 1f - t;
            // Cyan-tinted and stretched thin while partly visible.
            spriteRenderer.color = new Color(Mathf.Lerp(0.35f, 1f, visibility), 0.9f + 0.1f * visibility, 1f, visibility);
            transform.localScale = Vector3.Scale(normalScale, new Vector3(Mathf.Lerp(0.15f, 1f, visibility), Mathf.Lerp(1.25f, 1f, visibility), 1f));
            if (!appearing) transform.position = Vector3.Lerp(vanishFrom, vanishTo, t);
            if (t < 1f) return;

            if (appearing)
            {
                phase = Phase.InPlay;
                health.enabled = true;
                spriteRenderer.color = Color.white;
                transform.localScale = normalScale;
                Appeared?.Invoke();
            }
            else
            {
                phase = Phase.Vanished;
                Vanished?.Invoke();
            }
        }
    }
}
