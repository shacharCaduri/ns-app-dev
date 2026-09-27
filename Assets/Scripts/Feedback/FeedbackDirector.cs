using System.Collections;
using UnityEngine;
using WizardArena.Combat;
using WizardArena.Enemies;
using WizardArena.Hazards;

namespace WizardArena.Feedback
{
    // Listens to Health events (the player's, and every enemy's via EnemyRegistry) and to
    // ExplosiveHazard.Exploded, and turns them into hit-stop, camera shake, hit flash and
    // particle bursts. Nothing in Player/Enemies/Hazards calls into this class -- it only
    // subscribes to events they already raise for other reasons.
    //
    // Hit-stop vs. the Pause menu: only two things ever write Time.timeScale -- PauseMenuController
    // (0 while paused, 1 on resume) and the hit-stop coroutine below (a brief dip). The coroutine
    // never starts while timeScale is already 0 (paused), and when it finishes it only restores
    // timeScale to 1 if timeScale is still exactly the dip value it set -- if Pause changed it
    // meanwhile, it leaves timeScale alone so Resume() is the only thing that un-pauses.
    public sealed class FeedbackDirector : MonoBehaviour
    {
        [SerializeField] private FeedbackConfig config;
        [SerializeField] private Health playerHealth;
        [SerializeField] private Camera targetCamera;

        private Vector3 cameraBasePosition;
        private float shakeTrauma;
        private Coroutine hitStopRoutine;

        internal FeedbackConfig Config => config;
        // How much camera shake is currently playing out (0 = settled). Public for tests/tools.
        public float ShakeTrauma => shakeTrauma;

        // For code-built directors (the scene builder, tests). Camera defaults to Camera.main.
        public void Configure(FeedbackConfig feedbackConfig, Health player, Camera camera = null)
        {
            config = feedbackConfig;
            UnsubscribePlayer();
            playerHealth = player;
            if (isActiveAndEnabled) SubscribePlayer();
            targetCamera = camera != null ? camera : Camera.main;
            CaptureCameraBase();
        }

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            CaptureCameraBase();
        }

        private void OnEnable()
        {
            EnemyRegistry.Spawned += OnEnemySpawned;
            ExplosiveHazard.Exploded += OnExplosion;
            SubscribePlayer();
        }

        private void OnDisable()
        {
            EnemyRegistry.Spawned -= OnEnemySpawned;
            ExplosiveHazard.Exploded -= OnExplosion;
            UnsubscribePlayer();
            // A scene unload/reload can destroy this object mid-dip, aborting the HitStop
            // coroutine before its own restore line runs -- Time.timeScale is global and would
            // otherwise stay stuck below 1 forever. Only touch it if it still looks like our dip.
            if (hitStopRoutine != null && config != null && Mathf.Approximately(Time.timeScale, config.HitStopTimeScale))
                Time.timeScale = 1f;
            hitStopRoutine = null;
        }

        private void Update()
        {
            TickShake(Time.unscaledDeltaTime);
        }

        private void SubscribePlayer()
        {
            if (playerHealth != null) playerHealth.Damaged += OnPlayerDamaged;
        }

        private void UnsubscribePlayer()
        {
            if (playerHealth != null) playerHealth.Damaged -= OnPlayerDamaged;
        }

        private void CaptureCameraBase()
        {
            if (targetCamera != null) cameraBasePosition = targetCamera.transform.localPosition;
        }

        private void OnPlayerDamaged(DamageInfo damage)
        {
            if (config == null) return;
            if (config.HitStopEnabled) TriggerHitStop();
            if (config.ShakeEnabled) AddShake(config.PlayerHurtShakeTrauma);
        }

        private void OnEnemySpawned(Enemy enemy)
        {
            enemy.gameObject.AddComponent<EnemyFeedbackLink>().Bind(this);
        }

        private void OnExplosion(Vector3 position)
        {
            if (config == null) return;
            if (config.ShakeEnabled) AddShake(config.ExplosionShakeTrauma);
            if (config.ParticlesEnabled)
                ParticleBurstFactory.Spawn(position, config.ExplosionBurst, config.ParticleStartColor, config.ParticleEndColor);
        }

        // Called by EnemyFeedbackLink too, so every hit -- player or enemy -- shares one dip.
        internal void TriggerHitStop()
        {
            if (config == null || Time.timeScale <= 0f) return; // paused: don't fight it
            if (hitStopRoutine != null) StopCoroutine(hitStopRoutine);
            hitStopRoutine = StartCoroutine(HitStop());
        }

        private IEnumerator HitStop()
        {
            float dip = config.HitStopTimeScale;
            Time.timeScale = dip;
            yield return new WaitForSecondsRealtime(config.HitStopSeconds);
            if (Mathf.Approximately(Time.timeScale, dip)) Time.timeScale = 1f;
            hitStopRoutine = null;
        }

        private void AddShake(float trauma)
        {
            if (targetCamera == null) return;
            shakeTrauma = Mathf.Clamp01(shakeTrauma + trauma);
        }

        private void TickShake(float unscaledDeltaTime)
        {
            if (targetCamera == null || config == null) return;
            if (shakeTrauma <= 0f)
            {
                targetCamera.transform.localPosition = cameraBasePosition;
                return;
            }

            shakeTrauma = Mathf.Max(0f, shakeTrauma - config.ShakeDecayPerSecond * unscaledDeltaTime);
            float amount = shakeTrauma * shakeTrauma;
            Vector2 offset = Random.insideUnitCircle * config.ShakeMaxOffset * amount;
            targetCamera.transform.localPosition = cameraBasePosition + (Vector3)offset;
        }
    }
}
