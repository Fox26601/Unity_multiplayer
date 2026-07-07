namespace FusionMultiplayer.Core
{
    /// <summary>Notifies UI when replicated game-over state changes.</summary>
    public static class GameOverBridge
    {
        public static System.Action<bool> StateChanged;

        public static void NotifyStateChanged(bool isGameOver) => StateChanged?.Invoke(isGameOver);
    }
}
