using UnityEngine;
using WizardArena.World;

namespace WizardArena.Enemies
{
    // Falls onto the ground below, shows the dead sprite, holds, fades out, then destroys
    // the enemy. The ground is the highest walkable ArenaSurface under the enemy; without
    // one it is the bottom of the ArenaBounds area, or the spot where it died.
    internal sealed class DeadState : IEnemyState
    {
        private readonly Enemy enemy;
        private float fallSpeed;
        private float groundY;
        private bool landed;
        private float timeSinceLanding;

        internal DeadState(Enemy enemy)
        {
            this.enemy = enemy;
        }

        public void Enter()
        {
            fallSpeed = 0f;
            landed = false;
            timeSinceLanding = 0f;
            Vector2 position = enemy.transform.position;
            if (ArenaSurface.TryGetGroundBelow(position, out float surfaceTop)) groundY = surfaceTop;
            else if (ArenaBounds.Active != null) groundY = ArenaBounds.Active.Area.yMin;
            else groundY = enemy.Renderer.bounds.min.y;
        }

        public void Tick(float deltaTime)
        {
            if (landed) FadeOut(deltaTime);
            else Fall(deltaTime);
        }

        public void Exit()
        {
        }

        private void Fall(float deltaTime)
        {
            fallSpeed += enemy.Config.FallGravity * deltaTime;
            Transform body = enemy.transform;
            body.position += Vector3.down * (fallSpeed * deltaTime);
            if (enemy.Renderer.bounds.min.y > groundY) return;

            landed = true;
            enemy.ShowSprite(enemy.Config.Sprites.Dead(enemy.FacingRight));
            body.rotation = Quaternion.identity;
            body.position += Vector3.up * (groundY - enemy.Renderer.bounds.min.y);
        }

        private void FadeOut(float deltaTime)
        {
            EnemyConfig config = enemy.Config;
            timeSinceLanding += deltaTime;
            Color color = enemy.Renderer.color;
            color.a = 1f - Mathf.Clamp01((timeSinceLanding - config.CorpseHoldSeconds) / config.CorpseFadeSeconds);
            enemy.Renderer.color = color;
            if (timeSinceLanding >= config.CorpseHoldSeconds + config.CorpseFadeSeconds) Object.Destroy(enemy.gameObject);
        }
    }
}
