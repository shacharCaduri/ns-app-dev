namespace WizardArena.Combat
{
    // Which side a damage source or target is on. Neutral (hazards) hurts everyone.
    public enum Team
    {
        Player,
        Enemy,
        Neutral
    }

    public static class TeamExtensions
    {
        // Same-side damage is ignored, except that anything Neutral hurts and can be hurt by all.
        public static bool CanHurt(this Team attacker, Team target)
        {
            return attacker == Team.Neutral || attacker != target;
        }
    }
}
