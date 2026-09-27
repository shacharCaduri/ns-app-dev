using System;
using UnityEngine;
using WizardArena.Combat;

namespace WizardArena.Enemies
{
    // Tunable numbers and art for one enemy type. Assets live in Assets/Config/Enemies/.
    [CreateAssetMenu(menuName = "Wizard Arena/Enemies/Enemy Config", fileName = "EnemyConfig")]
    public sealed class EnemyConfig : ScriptableObject
    {
        [Header("Health")]
        [SerializeField] private HealthConfig health;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 2.2f;
        [Tooltip("Seconds between random changes of heading (min, max).")]
        [SerializeField] private Vector2 turnInterval = new Vector2(1.2f, 3f);
        [Tooltip("Smallest sideways part of a random heading, so the enemy never flies straight up or down.")]
        [SerializeField, Range(0f, 1f)] private float minimumSideways = 0.35f;

        [Header("Contact damage")]
        [SerializeField, Min(0)] private int contactDamage = 2;
        [SerializeField, Min(0f)] private float contactCooldown = 0.5f;
        [Tooltip("How far the enemy jumps back after touching a target.")]
        [SerializeField, Min(0f)] private float contactBounce = 0.45f;
        [Tooltip("Seconds before the next random heading after a touch (min, max).")]
        [SerializeField] private Vector2 turnIntervalAfterContact = new Vector2(1.2f, 2.5f);

        [Header("Hurt")]
        [SerializeField, Min(0.01f)] private float hurtSeconds = 0.3f;
        [SerializeField, Min(0f)] private float knockbackSpeed = 3f;
        [SerializeField, Min(0f)] private float wobbleDegrees = 12f;
        [Tooltip("Upward part of the heading after a hit (the sideways part is 1, away from the hit).")]
        [SerializeField] private float knockbackLift = 0.5f;
        [Tooltip("Seconds the enemy keeps flying away from the hit, after the hurt reaction, before turning at random again.")]
        [SerializeField, Min(0f)] private float fleeSeconds = 0.7f;

        [Header("Death")]
        [SerializeField, Min(0f)] private float fallGravity = 9f;
        [Tooltip("Seconds the corpse stays after landing before it starts to fade.")]
        [SerializeField, Min(0f)] private float corpseHoldSeconds = 2f;
        [SerializeField, Min(0.01f)] private float corpseFadeSeconds = 3f;

        [Header("Art")]
        [SerializeField, Min(0.1f)] private float animationFramesPerSecond = 7f;
        [SerializeField] private EnemySprites sprites = new EnemySprites();

        public HealthConfig Health => health;
        public float MoveSpeed => moveSpeed;
        public Vector2 TurnInterval => turnInterval;
        public float MinimumSideways => minimumSideways;
        public int ContactDamage => contactDamage;
        public float ContactCooldown => contactCooldown;
        public float ContactBounce => contactBounce;
        public Vector2 TurnIntervalAfterContact => turnIntervalAfterContact;
        public float HurtSeconds => hurtSeconds;
        public float KnockbackSpeed => knockbackSpeed;
        public float WobbleDegrees => wobbleDegrees;
        public float KnockbackLift => knockbackLift;
        public float FleeSeconds => fleeSeconds;
        public float FallGravity => fallGravity;
        public float CorpseHoldSeconds => corpseHoldSeconds;
        public float CorpseFadeSeconds => corpseFadeSeconds;
        public float AnimationFramesPerSecond => animationFramesPerSecond;
        public EnemySprites Sprites => sprites;

        // For enemies and tests built from code; everything else keeps its default.
        public static EnemyConfig Create(HealthConfig health)
        {
            EnemyConfig config = CreateInstance<EnemyConfig>();
            config.health = health;
            return config;
        }
    }

    // Left/right art for each state. Missing sprites are simply not shown (the current one stays).
    [Serializable]
    public sealed class EnemySprites
    {
        [SerializeField] private Sprite[] moveRight = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] moveLeft = Array.Empty<Sprite>();
        [SerializeField] private Sprite hurtRight;
        [SerializeField] private Sprite hurtLeft;
        [SerializeField] private Sprite deadRight;
        [SerializeField] private Sprite deadLeft;

        public Sprite[] Move(bool facingRight) => facingRight ? moveRight : moveLeft;
        public Sprite Hurt(bool facingRight) => facingRight ? hurtRight : hurtLeft;
        public Sprite Dead(bool facingRight) => facingRight ? deadRight : deadLeft;
    }
}
