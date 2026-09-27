using UnityEngine;
using WizardArena.World;

namespace WizardArena.Player
{
    // Walking, jumping, gravity, arena surfaces and knockback. Moves the transform directly.
    // WizardController calls Tick once per frame while the wizard is in play.
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;

        private float verticalVelocity;
        private float knockbackVelocity;
        // Floor height used only when the scene has no arena surfaces.
        private float fallbackGroundY;

        public bool IsGrounded { get; private set; }

        private void Awake()
        {
            fallbackGroundY = transform.position.y;
            IsGrounded = IsStandingOnGround(transform.position, out _);
        }

        // Clears residual fall/knockback speed, e.g. before a respawn, so the wizard does not
        // inherit old momentum when it reappears.
        internal void ResetVelocity()
        {
            verticalVelocity = 0f;
            knockbackVelocity = 0f;
        }

        // Pushes the wizard up and away from the source of a hit.
        public void Knockback(Vector2 sourcePosition)
        {
            float away = transform.position.x >= sourcePosition.x ? 1f : -1f;
            knockbackVelocity = away * config.KnockbackSpeed;
            verticalVelocity = config.KnockbackLift;
        }

        internal void Tick(float horizontal, bool jumpPressed, float deltaTime)
        {
            knockbackVelocity = Mathf.MoveTowards(knockbackVelocity, 0f, config.KnockbackDecay * deltaTime);
            Vector3 position = transform.position;
            bool grounded = IsStandingOnGround(position, out ArenaSurface support);
            bool jumping = false;
            if (grounded && jumpPressed)
            {
                verticalVelocity = config.JumpSpeed * (support != null ? support.jumpMultiplier : 1f);
                jumping = true;
            }

            verticalVelocity -= config.Gravity * deltaTime;
            float horizontalVelocity = horizontal * config.MoveSpeed * (support != null ? support.speedMultiplier : 1f) + knockbackVelocity;
            Vector3 next = position + new Vector3(horizontalVelocity, verticalVelocity, 0f) * deltaTime;
            if (ArenaSurface.Active.Count > 0)
            {
                next = ArenaSurface.Move(position, next, ref verticalVelocity, grounded && !jumping);
            }
            else if (next.y <= fallbackGroundY)
            {
                next.y = fallbackGroundY;
                verticalVelocity = 0f;
            }

            next.x = Mathf.Clamp(next.x, -config.ArenaHalfWidth, config.ArenaHalfWidth);
            transform.position = next;
            IsGrounded = IsStandingOnGround(next, out _);
        }

        private bool IsStandingOnGround(Vector3 feet, out ArenaSurface support)
        {
            support = ArenaSurface.Support(feet);
            bool onFloor = support != null || (ArenaSurface.Active.Count == 0 && feet.y <= fallbackGroundY + 0.001f);
            return onFloor && verticalVelocity <= 0f;
        }
    }
}
