using System;
using System.Collections.Generic;
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

        [Header("Ground movement")]
        [Tooltip("Used by ground-walking states (GroundMotion) while alive; flying enemies ignore it.")]
        [SerializeField, Min(0f)] private float gravity = 9f;
        [Tooltip("Distance at which a ground enemy notices the wizard and starts reacting (slime: hops closer; skeleton: approaches).")]
        [SerializeField, Min(0f)] private float chaseRange = 3f;

        [Header("Melee (skeleton warrior)")]
        [SerializeField, Min(0f)] private float meleeRange = 1f;
        [SerializeField, Min(0)] private int meleeDamage = 2;
        [Tooltip("How long the wind-up pose is held before the swing lands, so the player can react.")]
        [SerializeField, Min(0f)] private float attackWindUpSeconds = 0.5f;
        [SerializeField, Min(0f)] private float attackRecoverSeconds = 0.5f;

        [Header("Hop (blue slime)")]
        [SerializeField, Min(0.01f)] private float hopInterval = 0.45f;
        [SerializeField, Min(0f)] private float hopSpeed = 3.2f;

        [Header("Art")]
        [SerializeField, Min(0.1f)] private float animationFramesPerSecond = 7f;
        [SerializeField] private EnemySprites sprites = new EnemySprites();

        [Header("Drops")]
        [Tooltip("Rolled independently per entry on death (EnemyDropper); more than one can drop from the same enemy.")]
        [SerializeField] private DropEntry[] drops = Array.Empty<DropEntry>();

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
        public float Gravity => gravity;
        public float ChaseRange => chaseRange;
        public float MeleeRange => meleeRange;
        public int MeleeDamage => meleeDamage;
        public float AttackWindUpSeconds => attackWindUpSeconds;
        public float AttackRecoverSeconds => attackRecoverSeconds;
        public float HopInterval => hopInterval;
        public float HopSpeed => hopSpeed;
        public float AnimationFramesPerSecond => animationFramesPerSecond;
        public EnemySprites Sprites => sprites;
        public IReadOnlyList<DropEntry> Drops => drops;

        // For enemies and tests built from code; everything else keeps its default.
        public static EnemyConfig Create(HealthConfig health, DropEntry[] drops = null)
        {
            EnemyConfig config = CreateInstance<EnemyConfig>();
            config.health = health;
            config.drops = drops ?? Array.Empty<DropEntry>();
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
        [SerializeField] private Sprite attackRight;
        [SerializeField] private Sprite attackLeft;

        public Sprite[] Move(bool facingRight) => facingRight ? moveRight : moveLeft;
        public Sprite Hurt(bool facingRight) => facingRight ? hurtRight : hurtLeft;
        public Sprite Dead(bool facingRight) => facingRight ? deadRight : deadLeft;
        // Single wind-up/attack pose (also doubles as the slime's hop pose); no animation.
        public Sprite Attack(bool facingRight) => facingRight ? attackRight : attackLeft;
    }

    // One possible drop: a self-contained prefab (e.g. a Pickup) and the chance [0, 1] it
    // drops, rolled independently of every other entry. See EnemyDropper.
    [Serializable]
    public sealed class DropEntry
    {
        [SerializeField] private GameObject prefab;
        [SerializeField, Range(0f, 1f)] private float chance;

        public GameObject Prefab => prefab;
        public float Chance => chance;

        public static DropEntry At(GameObject prefab, float chance)
        {
            return new DropEntry { prefab = prefab, chance = chance };
        }
    }
}
