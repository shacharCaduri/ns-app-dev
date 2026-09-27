using UnityEngine;

namespace WizardArena.Pickups
{
    // One thing a pickup does to whatever collects it. Pickup calls Apply on every effect it
    // carries and only consumes itself if at least one of them actually did something -- so,
    // e.g., a health potion touched at full HP is left in place for later.
    public interface IPickupEffect
    {
        bool Apply(GameObject collector);
    }
}
