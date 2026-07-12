using System;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// JSON DTO (≥3 fields) sent over RPC for match bootstrap / validation.
    /// </summary>
    [Serializable]
    public sealed class MatchConfigDto
    {
        public string RoomName;
        public int MaxPlayers;
        public int GameMode;
        public int Map;
        public int Difficulty;
        public string PlayerToken;
        public string Nickname;

        public static MatchConfigDto FromSessionData(string roomName)
        {
            return new MatchConfigDto
            {
                RoomName = roomName ?? string.Empty,
                MaxPlayers = SessionData.MaxPlayers,
                GameMode = (int)SessionData.SelectedGameMode,
                Map = (int)SessionCatalog.ResolveMapForHost(SessionData.SelectedMap),
                Difficulty = (int)SessionData.SelectedDifficulty,
                PlayerToken = SessionData.EnsureReconnectToken(),
                Nickname = SessionData.Nickname ?? "Player"
            };
        }

        public string ToJson() => JsonUtility.ToJson(this);

        public static bool TryFromJson(string json, out MatchConfigDto dto)
        {
            dto = null;
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                dto = JsonUtility.FromJson<MatchConfigDto>(json);
                return dto != null && !string.IsNullOrWhiteSpace(dto.RoomName);
            }
            catch (Exception)
            {
                dto = null;
                return false;
            }
        }
    }
}
