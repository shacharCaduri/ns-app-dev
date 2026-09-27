using UnityEngine;

namespace WizardArena.Stage
{
    [CreateAssetMenu(menuName = "Wizard Arena/Stage/Stage Portal Config", fileName = "StagePortalConfig")]
    public sealed class StagePortalConfig : ScriptableObject
    {
        [Tooltip("Seconds the portal takes to grow and fade in once it opens.")]
        [SerializeField, Min(0.01f)] private float revealSeconds = 0.8f;
        [Tooltip("Scale at the start of the reveal, as a fraction of the full size.")]
        [SerializeField, Range(0f, 1f)] private float revealStartScale = 0.7f;
        [Tooltip("The player enters when its feet are within this distance (x, y) of the portal's base.")]
        [SerializeField] private Vector2 entryHalfSize = new Vector2(0.65f, 0.5f);

        public float RevealSeconds => revealSeconds;
        public float RevealStartScale => revealStartScale;
        public Vector2 EntryHalfSize => entryHalfSize;

        public static StagePortalConfig Create(float revealSeconds, float revealStartScale, Vector2 entryHalfSize)
        {
            StagePortalConfig config = CreateInstance<StagePortalConfig>();
            config.revealSeconds = Mathf.Max(0.01f, revealSeconds);
            config.revealStartScale = Mathf.Clamp01(revealStartScale);
            config.entryHalfSize = entryHalfSize;
            return config;
        }
    }
}
