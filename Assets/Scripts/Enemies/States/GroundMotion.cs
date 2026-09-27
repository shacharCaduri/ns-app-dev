using UnityEngine;
using WizardArena.World;

namespace WizardArena.Enemies
{
    // Walks a ground enemy along ArenaSurface floors and stairs, reusing the same physics
    // as the player (ArenaSurface.Move): gravity, stepping up small stairs and blocking at
    // walls. It also tells the caller when a ledge or a wall stopped the horizontal move,
    // so a state can turn around instead of walking off a platform or into a wall.
    internal sealed class GroundMotion
    {
        // How far ahead (in the direction of travel) to check for a ledge before stepping.
        private const float LedgeProbeDistance = 0.3f;
        // A drop bigger than this ahead counts as a ledge; a smaller one is just a stair.
        private const float MaxStepDown = 0.6f;

        private float verticalVelocity;

        // Kicks the enemy upward immediately (a slime's hop); gravity pulls it back down
        // over the following Step calls.
        internal void Hop(float speed)
        {
            verticalVelocity = speed;
        }

        // Moves from position by heading (-1/0/+1) * moveSpeed, applying gravity and arena
        // surface collision. blocked is true when a ledge or a wall stopped the horizontal
        // move (the caller should turn around), even though position may still change
        // vertically (falling/landing).
        internal Vector3 Step(Vector3 position, float heading, float moveSpeed, float gravity, float deltaTime, out bool blocked)
        {
            bool wasGrounded = ArenaSurface.Support(position) != null;
            // Checked regardless of grounded: a hop's brief airborne arc must not let the
            // enemy drift past a ledge that it would have stopped at while walking.
            blocked = heading != 0f && HasLedgeAhead(position, heading);
            float appliedHeading = blocked ? 0f : heading;

            verticalVelocity -= gravity * deltaTime;
            float intendedDx = appliedHeading * moveSpeed * deltaTime;
            Vector3 requested = position + new Vector3(intendedDx, verticalVelocity * deltaTime, 0f);
            Vector3 moved = ArenaSurface.Active.Count > 0
                ? ArenaSurface.Move(position, requested, ref verticalVelocity, wasGrounded)
                : requested;

            if (!blocked && intendedDx != 0f && Mathf.Abs(moved.x - position.x) < Mathf.Abs(intendedDx) * 0.5f)
                blocked = true; // a wall took most of the intended step

            if (ArenaSurface.Support(moved) != null) verticalVelocity = 0f;
            return moved;
        }

        private static bool HasLedgeAhead(Vector3 position, float heading)
        {
            Vector2 probe = new Vector2(position.x + heading * LedgeProbeDistance, position.y);
            if (!ArenaSurface.TryGetGroundBelow(probe, out float probeGround)) return true;
            return ArenaSurface.TryGetGroundBelow(position, out float currentGround) && currentGround - probeGround > MaxStepDown;
        }
    }
}
