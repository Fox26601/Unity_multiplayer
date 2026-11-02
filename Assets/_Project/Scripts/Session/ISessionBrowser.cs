using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using FusionMultiplayer.Core;

namespace FusionMultiplayer.Session
{
    /// <summary>Lobby membership and cached session list for the session browser.</summary>
    public interface ISessionBrowser
    {
        IReadOnlyList<SessionInfo> CachedSessionList { get; }
        Task<bool> EnsureSessionLobbyAsync(SessionCatalog.GameModeKind mode);
        Task<bool> ProbeReconnectRoomAsync(string roomName, SessionCatalog.GameModeKind mode);
    }
}
