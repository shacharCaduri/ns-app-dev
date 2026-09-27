namespace WizardArena.Stage
{
    // Playing until the stage ends one way or the other. Victory is set by [10] after the last stage.
    public enum GameState
    {
        Playing,
        Defeated,
        StageCleared,
        Victory
    }
}
