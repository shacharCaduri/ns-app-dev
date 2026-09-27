using UnityEngine;
using WizardArena.Combat;

namespace WizardArena.Enemies
{
    // Walks toward the wizard, stops once inside Config.MeleeRange, winds up for
    // Config.AttackWindUpSeconds (a visible cue the player can react to), then swings --
    // only hitting a target still inside MeleeRange at that instant -- and pauses to
    // recover before approaching again. Contact damage (light, passive) stays active the
    // whole time via the shared ContactDamage component.
    internal sealed class SkeletonState : IEnemyState
    {
        private enum Phase { Approach, WindUp, Recover }

        private readonly Enemy enemy;
        private readonly GroundMotion motion = new GroundMotion();
        private float heading;
        private Phase phase;
        private float phaseTimer;
        private float animationTime;

        internal SkeletonState(Enemy enemy)
        {
            this.enemy = enemy;
        }

        public void Enter()
        {
            heading = enemy.Heading.x != 0f ? Mathf.Sign(enemy.Heading.x) : 1f;
            phase = Phase.Approach;
            phaseTimer = 0f;
        }

        public void Exit()
        {
        }

        public void Tick(float deltaTime)
        {
            EnemyConfig config = enemy.Config;
            Vector3 position = enemy.transform.position;
            bool hasTarget = PlayerSensor.TryFind(position, config.ChaseRange, out Health target);
            if (hasTarget)
            {
                float toTarget = target.transform.position.x - position.x;
                if (Mathf.Abs(toTarget) > 0.01f) heading = Mathf.Sign(toTarget);
            }

            switch (phase)
            {
                case Phase.Approach:
                    TickApproach(config, position, hasTarget, target, deltaTime);
                    break;
                case Phase.WindUp:
                    TickWindUp(config, deltaTime);
                    break;
                default:
                    TickRecover(config, deltaTime);
                    break;
            }

            enemy.Heading = new Vector2(heading, 0f);
        }

        private void TickApproach(EnemyConfig config, Vector3 position, bool hasTarget, Health target, float deltaTime)
        {
            if (hasTarget && Vector2.Distance(position, target.transform.position) <= config.MeleeRange)
            {
                phase = Phase.WindUp;
                phaseTimer = config.AttackWindUpSeconds;
                enemy.ShowSprite(config.Sprites.Attack(enemy.FacingRight));
                return;
            }

            Vector3 moved = motion.Step(position, heading, config.MoveSpeed, config.Gravity, deltaTime, out bool blocked);
            if (blocked && !hasTarget) heading *= -1f;
            enemy.transform.position = moved;
            Animate(config, deltaTime);
        }

        private void TickWindUp(EnemyConfig config, float deltaTime)
        {
            enemy.ShowSprite(config.Sprites.Attack(enemy.FacingRight));
            phaseTimer -= deltaTime;
            if (phaseTimer > 0f) return;

            Swing(config);
            phase = Phase.Recover;
            phaseTimer = config.AttackRecoverSeconds;
        }

        private void TickRecover(EnemyConfig config, float deltaTime)
        {
            phaseTimer -= deltaTime;
            if (phaseTimer <= 0f) phase = Phase.Approach;
        }

        // Re-checks range at the instant the swing lands: only a target still inside
        // MeleeRange right now is hit, not whoever triggered the wind-up a moment ago.
        private void Swing(EnemyConfig config)
        {
            Vector3 position = enemy.transform.position;
            if (!PlayerSensor.TryFind(position, config.MeleeRange, out Health target)) return;
            target.TakeDamage(new DamageInfo(config.MeleeDamage, position, enemy.gameObject, enemy.Health.Team));
        }

        private void Animate(EnemyConfig config, float deltaTime)
        {
            Sprite[] frames = config.Sprites.Move(enemy.FacingRight);
            if (frames == null || frames.Length == 0) return;
            animationTime += deltaTime;
            int frame = Mathf.FloorToInt(animationTime * config.AnimationFramesPerSecond) % frames.Length;
            enemy.ShowSprite(frames[frame]);
        }
    }
}
