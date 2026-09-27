using UnityEngine;
using WizardArena.Combat;

namespace WizardArena.UI
{
    // A small "current / max HP" bar floating above any Health. Hidden once it dies.
    // Still immediate-mode OnGUI; ticket [09] replaces it with a proper world-space UI.
    public sealed class WorldHealthBar : MonoBehaviour
    {
        [Tooltip("The Health to show. Defaults to the one on this object.")]
        [SerializeField] private Health health;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.9f, 0f);
        [Tooltip("Resources path of a JSON style (width, height, border, background, fill).")]
        [SerializeField] private string styleResource = "UI/BatHealthBar";

        private Style style;
        private Color background;
        private Color fill;
        private GUIStyle labelStyle;

        [System.Serializable]
        private sealed class Style
        {
            public float width = 64;
            public float height = 9;
            public float border = 2;
            public string background = "#162238";
            public string fill = "#EF5564";
        }

        private void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            TextAsset json = string.IsNullOrEmpty(styleResource) ? null : Resources.Load<TextAsset>(styleResource);
            style = json != null ? JsonUtility.FromJson<Style>(json.text) : new Style();
            ColorUtility.TryParseHtmlString(style.background, out background);
            ColorUtility.TryParseHtmlString(style.fill, out fill);
        }

        private void OnGUI()
        {
            Camera camera = Camera.main;
            if (camera == null || health == null || health.IsDead) return;
            Vector3 screen = camera.WorldToScreenPoint(transform.position + worldOffset);
            Rect bar = new Rect(screen.x - style.width / 2f, Screen.height - screen.y, style.width, style.height);
            Color previousColor = GUI.color;
            GUI.color = background;
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = fill;
            float inset = style.border;
            GUI.DrawTexture(new Rect(bar.x + inset, bar.y + inset, (bar.width - 2f * inset) * health.Current / health.Max, bar.height - 2f * inset), Texture2D.whiteTexture);
            GUI.color = Color.white;
            labelStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            string text = $"{health.Current} / {health.Max} HP";
            Rect label = new Rect(bar.x - 12f, bar.y - 22f, bar.width + 24f, 22f);
            labelStyle.normal.textColor = Color.black;
            GUI.Label(new Rect(label.x + 1f, label.y + 1f, label.width, label.height), text, labelStyle);
            labelStyle.normal.textColor = Color.white;
            GUI.Label(label, text, labelStyle);
            GUI.color = previousColor;
        }
    }
}
