using UnityEngine;

namespace WizardArena.Combat
{
    public readonly struct DamageInfo
    {
        public readonly int Amount;
        // Where the hit came from; receivers use it to push away from the source.
        public readonly Vector2 SourcePosition;
        // The attacker (for a projectile: whoever fired it). May be null.
        public readonly GameObject Source;
        public readonly Team Team;

        public DamageInfo(int amount, Vector2 sourcePosition, GameObject source, Team team)
        {
            Amount = amount;
            SourcePosition = sourcePosition;
            Source = source;
            Team = team;
        }
    }
}
