using System;
using UnityEngine;

namespace WizardArena.Enemies
{
    // Shows the hurt sprite, slides away from the hit and wobbles, then calls back.
    // Enemy plays it on every accepted hit, including the lethal one.
    internal sealed class HurtState : IEnemyState
    {
        private readonly Enemy enemy;
        private readonly Action finished;
        private float timeLeft;
        private float pushDirection;

        internal HurtState(Enemy enemy, Action finished)
        {
            this.enemy = enemy;
            this.finished = finished;
        }

        // Where the hit came from; set before entering.
        internal Vector2 HitFrom { get; set; }

        public void Enter()
        {
            EnemyConfig config = enemy.Config;
            enemy.ShowSprite(config.Sprites.Hurt(enemy.FacingRight));
            timeLeft = config.HurtSeconds;
            pushDirection = enemy.transform.position.x >= HitFrom.x ? 1f : -1f;
            // After recovering, keep flying away from the hit for a moment.
            enemy.Heading = new Vector2(pushDirection, config.KnockbackLift).normalized;
        }

        public void Tick(float deltaTime)
        {
            EnemyConfig config = enemy.Config;
            timeLeft = Mathf.Max(0f, timeLeft - deltaTime);
            float remaining = timeLeft / config.HurtSeconds;
            Rect area = enemy.MoveArea;
            Transform body = enemy.transform;
            Vector3 position = body.position;
            position.x = Mathf.Clamp(position.x + pushDirection * config.KnockbackSpeed * remaining * deltaTime, area.xMin, area.xMax);
            body.position = position;
            body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin((1f - remaining) * Mathf.PI * 2f) * config.WobbleDegrees * remaining);
            if (timeLeft <= 0f) finished();
        }

        public void Exit()
        {
            enemy.transform.localRotation = Quaternion.identity;
        }
    }
}
