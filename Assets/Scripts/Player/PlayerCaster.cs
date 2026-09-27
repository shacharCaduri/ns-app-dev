using UnityEngine;
using WizardArena.Combat;

namespace WizardArena.Player
{
    // Cast timing: a cast locks its direction, releases a projectile part-way through,
    // then ends. Firing goes through the ProjectileLauncher on the same object.
    [RequireComponent(typeof(ProjectileLauncher))]
    public sealed class PlayerCaster : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;

        private ProjectileLauncher launcher;
        private bool released;

        public bool IsCasting { get; private set; }
        public float CastFacing { get; private set; } = 1f;
        public float CastTime { get; private set; }

        private void Awake()
        {
            launcher = GetComponent<ProjectileLauncher>();
        }

        internal void TryBeginCast(float facing)
        {
            if (IsCasting) return;
            IsCasting = true;
            released = false;
            CastTime = 0f;
            CastFacing = facing;
        }

        internal void Cancel()
        {
            IsCasting = false;
        }

        // Returns true if a cast was running this frame (including the frame it ends on).
        internal bool Tick(float deltaTime)
        {
            if (!IsCasting) return false;
            CastTime += deltaTime;
            if (!released && CastTime >= config.CastReleaseTime)
            {
                released = true;
                Vector2 muzzle = config.MuzzleOffset;
                launcher.Fire(transform.position + new Vector3(CastFacing * muzzle.x, muzzle.y, 0f), new Vector2(CastFacing, 0f));
            }

            if (CastTime >= config.CastDuration) IsCasting = false;
            return true;
        }
    }
}
