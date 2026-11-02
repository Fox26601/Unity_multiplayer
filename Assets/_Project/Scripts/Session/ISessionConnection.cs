using System.Threading.Tasks;

namespace FusionMultiplayer.Session
{
    /// <summary>Create, join, reconnect, and quick join.</summary>
    public interface ISessionConnection
    {
        Task<bool> CreateSessionAsync(string roomName);
        Task<bool> JoinSessionAsync(string roomName);
        Task<bool> ReconnectSessionAsync(string roomName);
        Task<bool> QuickJoinAsync();
    }
}
