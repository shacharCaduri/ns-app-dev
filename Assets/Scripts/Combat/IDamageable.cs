namespace WizardArena.Combat
{
    public interface IDamageable
    {
        // Returns false when the hit was ignored (dead, invulnerable, same team, disabled).
        bool TakeDamage(in DamageInfo damage);
    }
}
