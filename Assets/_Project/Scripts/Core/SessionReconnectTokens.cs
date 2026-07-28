using System.Text;
using Fusion;
using FusionMultiplayer.Player;

namespace FusionMultiplayer.Core
{
    /// <summary>UTF-8 connection tokens for crash reconnect validation.</summary>
    public static class SessionReconnectTokens
    {
        public static byte[] ToBytes(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return System.Array.Empty<byte>();
            return Encoding.UTF8.GetBytes(token.Trim());
        }

        public static bool TryRead(byte[] tokenBytes, out string token)
        {
            token = string.Empty;
            if (tokenBytes == null || tokenBytes.Length == 0)
                return false;

            try
            {
                token = Encoding.UTF8.GetString(tokenBytes).Trim();
                return !string.IsNullOrWhiteSpace(token);
            }
            catch
            {
                return false;
            }
        }

        public static bool TryReadPlayerToken(NetworkRunner runner, PlayerRef player, out string token)
        {
            token = string.Empty;
            if (runner == null || !runner.IsRunning || player == PlayerRef.None)
                return false;

            var bytes = runner.GetPlayerConnectionToken(player);
            return TryRead(bytes, out token);
        }

        public static bool IsKnownToken(string token) => PlayerRegistry.IsKnownToken(token);
    }
}
