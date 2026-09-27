using UnityEngine;

namespace WizardArena.Enemies
{
    // Flies in a straight line, bouncing off the edges of the move area, and picks a new
    // random heading every few seconds. Jumps back after touching a target.
    internal sealed class FlyingWanderState : IEnemyState
    {
        private readonly Enemy enemy;
        private float turnTimer;
        private float animationTime;

        internal FlyingWanderState(Enemy enemy)
        {
            this.enemy = enemy;
        }

        public void Enter()
        {
            if (enemy.Contact != null) enemy.Contact.Touched += OnTouched;
            // First time: random heading. After a hit: keep fleeing along the knockback heading.
            if (enemy.Heading == Vector2.zero) ChooseHeading();
            else turnTimer = enemy.Config.FleeSeconds;
        }

        public void Exit()
        {
            if (enemy.Contact != null) enemy.Contact.Touched -= OnTouched;
        }

        public void Tick(float deltaTime)
        {
            turnTimer -= deltaTime;
            if (turnTimer <= 0f) ChooseHeading();

            Rect area = enemy.MoveArea;
            Vector2 heading = enemy.Heading;
            Vector2 next = (Vector2)enemy.transform.position + heading * (enemy.Config.MoveSpeed * deltaTime);
            if (next.x <= area.xMin || next.x >= area.xMax)
            {
                heading.x *= -1f;
                next.x = Mathf.Clamp(next.x, area.xMin, area.xMax);
            }
            if (next.y <= area.yMin || next.y >= area.yMax)
            {
                heading.y *= -1f;
                next.y = Mathf.Clamp(next.y, area.yMin, area.yMax);
            }

            enemy.Heading = heading;
            MoveTo(next);
            Animate(deltaTime);
        }

        private void OnTouched(Vector2 awayFromTarget)
        {
            Vector2 heading = awayFromTarget.sqrMagnitude > 0f ? awayFromTarget : -enemy.Heading;
            enemy.Heading = heading;
            Rect area = enemy.MoveArea;
            Vector2 next = (Vector2)enemy.transform.position + heading * enemy.Config.ContactBounce;
            MoveTo(new Vector2(Mathf.Clamp(next.x, area.xMin, area.xMax), Mathf.Clamp(next.y, area.yMin, area.yMax)));
            Vector2 interval = enemy.Config.TurnIntervalAfterContact;
            turnTimer = Random.Range(interval.x, interval.y);
        }

        private void ChooseHeading()
        {
            float minimumSideways = enemy.Config.MinimumSideways;
            Vector2 heading = Random.insideUnitCircle.normalized;
            if (Mathf.Abs(heading.x) < minimumSideways)
            {
                heading.x = Mathf.Sign(heading.x == 0f ? Random.Range(-1f, 1f) : heading.x) * minimumSideways;
                heading.Normalize();
            }
            enemy.Heading = heading;
            Vector2 interval = enemy.Config.TurnInterval;
            turnTimer = Random.Range(interval.x, interval.y);
        }

        private void MoveTo(Vector2 point)
        {
            enemy.transform.position = new Vector3(point.x, point.y, enemy.transform.position.z);
        }

        private void Animate(float deltaTime)
        {
            Sprite[] frames = enemy.Config.Sprites.Move(enemy.FacingRight);
            if (frames == null || frames.Length == 0) return;
            animationTime += deltaTime;
            int frame = Mathf.FloorToInt(animationTime * enemy.Config.AnimationFramesPerSecond) % frames.Length;
            enemy.ShowSprite(frames[frame]);
        }
    }
}
