using UnityEngine;
using WizardArena.Combat;
using WizardArena.World;

namespace WizardArena.Enemies
{
    // The shared part of every enemy: config, registry membership and the state machine.
    // It plays the common Hurt and Dead states itself; the enemy type's IEnemyBehaviour
    // component supplies the main state (e.g. BatEnemyController -> flying wander).
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health), typeof(SpriteRenderer))]
    public sealed class Enemy : MonoBehaviour
    {
        private static readonly Rect Unbounded = Rect.MinMaxRect(-100000f, -100000f, 100000f, 100000f);

        [SerializeField] private EnemyConfig config;

        private readonly EnemyStateMachine stateMachine = new EnemyStateMachine();
        private Health health;
        private ContactDamage contact;
        private IEnemyState mainState;
        private HurtState hurtState;
        private DeadState deadState;
        private bool died;

        public EnemyConfig Config => config;
        public Health Health => health;
        // True from the moment Health dies, while the corpse is still falling and fading.
        public bool IsDead => died;

        internal SpriteRenderer Renderer { get; private set; }
        internal ContactDamage Contact => contact;
        // Current movement direction; also decides which way the sprites face.
        internal Vector2 Heading { get; set; }
        internal bool FacingRight => Heading.x >= 0f;
        // Where the enemy's centre may go; the whole world if the scene has no ArenaBounds.
        internal Rect MoveArea => ArenaBounds.Active != null ? ArenaBounds.Active.Area : Unbounded;

        // For enemies built from code (spawners, tests). Call right after AddComponent.
        public void Configure(EnemyConfig enemyConfig)
        {
            config = enemyConfig;
            health.Configure(enemyConfig.Health, Team.Enemy);
        }

        private void Awake()
        {
            health = GetComponent<Health>();
            Renderer = GetComponent<SpriteRenderer>();
            hurtState = new HurtState(this, OnHurtFinished);
            deadState = new DeadState(this);
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            EnemyRegistry.Add(this);
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }
            EnemyRegistry.Remove(this);
        }

        // Start, not Awake: components added from code after Enemy exist by now.
        private void Start()
        {
            contact = GetComponent<ContactDamage>();
            IEnemyBehaviour behaviour = GetComponent<IEnemyBehaviour>();
            mainState = behaviour?.CreateMainState(this);
            if (stateMachine.Current == null) ChangeState(mainState);
        }

        private void Update()
        {
            stateMachine.Tick(Time.deltaTime);
        }

        internal void ShowSprite(Sprite sprite)
        {
            if (sprite != null) Renderer.sprite = sprite;
        }

        private void OnDamaged(DamageInfo damage)
        {
            hurtState.HitFrom = damage.SourcePosition;
            ChangeState(hurtState);
        }

        private void OnDied()
        {
            died = true;
            // Corpses are not solid: bolts fly through and nothing touches them.
            foreach (Collider2D part in GetComponents<Collider2D>()) part.enabled = false;
            EnemyRegistry.MarkDead(this);
        }

        private void OnHurtFinished()
        {
            ChangeState(died ? deadState : mainState);
        }

        // Only the main state deals contact damage (not while hurt or dead).
        private void ChangeState(IEnemyState next)
        {
            if (contact != null) contact.enabled = next != null && next == mainState;
            stateMachine.ChangeTo(next);
        }
    }
}
