using UnityEngine;

namespace WizardArena.Player
{
    // All tunable numbers for the player character. Sprites live on PlayerAnimator.
    [CreateAssetMenu(menuName = "Wizard Arena/Player/Player Config", fileName = "PlayerConfig")]
    public sealed class PlayerConfig : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 4f;
        [SerializeField, Min(0f)] private float jumpSpeed = 10.5f;
        [SerializeField, Min(0f)] private float gravity = 20f;
        [Tooltip("The wizard's x position is clamped to +/- this value.")]
        [SerializeField, Min(0f)] private float arenaHalfWidth = 9.2f;

        [Header("Knockback when hit")]
        [SerializeField, Min(0f)] private float knockbackSpeed = 13f;
        [SerializeField, Min(0f)] private float knockbackLift = 5f;
        [Tooltip("How fast the sideways knockback speed fades, in units/s per second.")]
        [SerializeField, Min(0f)] private float knockbackDecay = 30f;

        [Header("Casting")]
        [Tooltip("Seconds into the cast when the projectile leaves the staff.")]
        [SerializeField, Min(0f)] private float castReleaseTime = 0.28f;
        [SerializeField, Min(0.01f)] private float castDuration = 0.56f;
        [Tooltip("Projectile spawn point relative to the feet, for facing right (x is mirrored).")]
        [SerializeField] private Vector2 muzzleOffset = new Vector2(0.75f, 0.85f);

        [Header("Animation")]
        [SerializeField, Min(0.1f)] private float walkFramesPerSecond = 8f;
        [SerializeField, Min(0.01f)] private float castFrameSeconds = 0.14f;

        [Header("Hit feedback")]
        [SerializeField, Min(0.01f)] private float hitFeedbackSeconds = 0.45f;
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.25f, 0.25f);
        [Tooltip("Peak stretch (x) and squash (y) of the hit pulse.")]
        [SerializeField] private Vector2 hitSquash = new Vector2(0.18f, 0.12f);

        [Header("Transitions")]
        [SerializeField, Min(0.01f)] private float appearSeconds = 0.9f;
        [SerializeField, Min(0.01f)] private float vanishSeconds = 1.4f;

        public float MoveSpeed => moveSpeed;
        public float JumpSpeed => jumpSpeed;
        public float Gravity => gravity;
        public float ArenaHalfWidth => arenaHalfWidth;
        public float KnockbackSpeed => knockbackSpeed;
        public float KnockbackLift => knockbackLift;
        public float KnockbackDecay => knockbackDecay;
        public float CastReleaseTime => castReleaseTime;
        public float CastDuration => castDuration;
        public Vector2 MuzzleOffset => muzzleOffset;
        public float WalkFramesPerSecond => walkFramesPerSecond;
        public float CastFrameSeconds => castFrameSeconds;
        public float HitFeedbackSeconds => hitFeedbackSeconds;
        public Color HitFlashColor => hitFlashColor;
        public Vector2 HitSquash => hitSquash;
        public float AppearSeconds => appearSeconds;
        public float VanishSeconds => vanishSeconds;
    }
}
