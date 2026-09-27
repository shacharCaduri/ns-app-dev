namespace WizardArena.UI
{
    // Stops Play Mode in the editor, or exits the app in a build. Shared by every "Quit" button.
    internal static class QuitCommand
    {
        internal static void Execute()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }
    }
}
