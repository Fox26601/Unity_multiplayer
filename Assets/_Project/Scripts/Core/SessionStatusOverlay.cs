using System;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// Runtime→UI bridge for connection status text without referencing UI types.
    /// </summary>
    public static class SessionStatusOverlay
    {
        public static Action<string, bool> Present;

        public static void Set(string message, bool isError) => Present?.Invoke(message, isError);
    }
}
