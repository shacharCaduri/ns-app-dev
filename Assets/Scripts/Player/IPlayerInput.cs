namespace WizardArena.Player
{
    // What the player wants this frame. Read once per frame by WizardController.
    public interface IPlayerInput
    {
        // -1 (left) .. 1 (right).
        float Horizontal { get; }
        // True only on the frame the button went down.
        bool JumpPressed { get; }
        bool CastPressed { get; }
    }
}
