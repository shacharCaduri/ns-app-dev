namespace WizardArena.Stage
{
    // What GameSession.Retry()/Continue() actually do, supplied by StageRunner (same
    // assembly). An interface rather than a concrete StageRunner reference, so GameSession
    // needs no lookup; a scene with none simply falls back to reloading itself.
    internal interface IStageProgression
    {
        void RestartCurrentStage();
        void AdvanceToNextStage();
    }
}
