using UnityEngine;
using WizardArena.Combat;

namespace WizardArena.UI
{
    // A small "current / max HP" bar that floats above any Health, built from two flat
    // SpriteRenderers (background + fill) instead of OnGUI. Hidden once the target dies.
    public sealed class WorldHealthBar : MonoBehaviour
    {
        [Tooltip("The Health to show. Defaults to the one on this object.")]
        [SerializeField] private Health health;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.9f, 0f);
        [SerializeField] private Vector2 size = new Vector2(0.7f, 0.1f);
        [SerializeField] private Color backgroundColor = new Color(0.086f, 0.133f, 0.22f);
        [SerializeField] private Color fillColor = new Color(0.263f, 0.855f, 0.643f);
        [SerializeField] private int sortingOrder = 20;

        // Shared 1x1-unit sprites: Center for the background, Left (pivot on its left edge)
        // for the fill, so shrinking it keeps the left edge in place.
        private static Sprite pixelCenter;
        private static Sprite pixelLeft;

        private SpriteRenderer background;
        private SpriteRenderer fill;

        private void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            BuildBar();
        }

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Healed += OnHealed;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Healed -= OnHealed;
            health.Died -= OnDied;
        }

        // Not OnEnable: Health.Awake (which sets Current = Max) is not guaranteed to have run
        // yet at that point. Start runs after every Awake in the scene.
        private void Start() => Refresh();

        private void OnDamaged(DamageInfo damage) => Refresh();

        private void OnHealed(int amountHealed) => Refresh();

        private void OnDied() => SetVisible(false);

        private void Refresh()
        {
            if (health.IsDead)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            float fraction = health.Max > 0 ? Mathf.Clamp01((float)health.Current / health.Max) : 0f;
            fill.transform.localScale = new Vector3(size.x * fraction, size.y, 1f);
        }

        private void SetVisible(bool visible)
        {
            background.enabled = visible;
            fill.enabled = visible;
        }

        private void BuildBar()
        {
            EnsureSprites();

            GameObject barRoot = new GameObject("HealthBar");
            barRoot.transform.SetParent(transform, false);
            barRoot.transform.localPosition = worldOffset;

            background = CreateBar(barRoot.transform, "Background", pixelCenter, backgroundColor, sortingOrder);
            background.transform.localScale = new Vector3(size.x, size.y, 1f);

            fill = CreateBar(barRoot.transform, "Fill", pixelLeft, fillColor, sortingOrder + 1);
            fill.transform.localPosition = new Vector3(-size.x / 2f, 0f, 0f);
        }

        private static SpriteRenderer CreateBar(Transform parent, string name, Sprite sprite, Color color, int order)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static void EnsureSprites()
        {
            if (pixelCenter != null) return;
            Texture2D texture = Texture2D.whiteTexture;
            Rect rect = new Rect(0f, 0f, texture.width, texture.height);
            pixelCenter = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), texture.width);
            pixelLeft = Sprite.Create(texture, rect, new Vector2(0f, 0.5f), texture.width);
        }
    }
}
