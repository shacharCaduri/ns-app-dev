using UnityEngine;
using WizardArena.Combat;

namespace WizardArena.Enemies
{
    // Slow ground patrol; hops toward the wizard once within Config.ChaseRange. GroundMotion
    // keeps it on its platform even mid-chase: blocked by a ledge or wall, it just waits at
    // the edge facing the target instead of falling or turning away.
    internal sealed class SlimeState : IEnemyState
    {
        private readonly Enemy enemy;
        private readonly GroundMotion motion = new GroundMotion();
        private float heading;
        private float hopTimer;
        private float animationTime;

        internal SlimeState(Enemy enemy)
        {
            this.enemy = enemy;
        }

        public void Enter()
        {
            heading = enemy.Heading.x != 0f ? Mathf.Sign(enemy.Heading.x) : 1f;
            hopTimer = 0f;
        }

        public void Exit()
        {
        }

        public void Tick(float deltaTime)
        {
            EnemyConfig config = enemy.Config;
            Vector3 position = enemy.transform.position;
            bool chasing = PlayerSensor.TryFind(position, config.ChaseRange, out Health target);

            if (chasing)
            {
                float toTarget = target.transform.position.x - position.x;
                if (Mathf.Abs(toTarget) > 0.01f) heading = Mathf.Sign(toTarget);

                hopTimer -= deltaTime;
                if (hopTimer <= 0f)
                {
                    motion.Hop(config.HopSpeed);
                    hopTimer = config.HopInterval;
                }
            }
            else
            {
                hopTimer = 0f;
            }

            Vector3 moved = motion.Step(position, heading, config.MoveSpeed, config.Gravity, deltaTime, out bool blocked);
            if (blocked && !chasing) heading *= -1f;

            enemy.transform.position = moved;
            enemy.Heading = new Vector2(heading, 0f);
            Animate(config, chasing, deltaTime);
        }

        private void Animate(EnemyConfig config, bool chasing, float deltaTime)
        {
            if (chasing)
            {
                enemy.ShowSprite(config.Sprites.Attack(enemy.FacingRight));
                return;
            }

            Sprite[] frames = config.Sprites.Move(enemy.FacingRight);
            if (frames == null || frames.Length == 0) return;
            animationTime += deltaTime;
            int frame = Mathf.FloorToInt(animationTime * config.AnimationFramesPerSecond) % frames.Length;
            enemy.ShowSprite(frames[frame]);
        }
    }
}
