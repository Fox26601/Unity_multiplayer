using Fusion;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    public partial class GameManager
    {
        /// <summary>Client sends serialized MatchConfigDto JSON (≥3 fields) for server deserialize + ack.</summary>
        public void SubmitMatchConfigJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return;

            var clipped = json.Length > 500 ? json.Substring(0, 500) : json;
            RPC_SubmitMatchConfigJson(clipped);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_SubmitMatchConfigJson(NetworkString<_512> jsonPayload, RpcInfo info = default)
        {
            if (!HasStateAuthority)
                return;

            var json = jsonPayload.ToString();
            LastConfigJsonLength = json.Length;

            if (!MatchConfigDto.TryFromJson(json, out var dto))
            {
                Debug.LogWarning("[FusionMultiplayer] Rejected invalid MatchConfigDto JSON");
                return;
            }

            var source = info.Source;
            if (source == PlayerRef.None)
                source = Runner.LocalPlayer;

            // Reclaim is ProvisionPlayer → ReconnectService only (JSON is coursework serialize/ack).
            Debug.Log(
                $"[FusionMultiplayer] Deserialized MatchConfigDto from {source}: room={dto.RoomName}, max={dto.MaxPlayers}, mode={dto.GameMode}, map={dto.Map}, diff={dto.Difficulty}, nick={dto.Nickname}");

            if (source != PlayerRef.None)
                RPC_AckMatchConfigJson(source, json.Length, dto.MaxPlayers, dto.Difficulty);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_AckMatchConfigJson([RpcTarget] PlayerRef target, int jsonLength, int maxPlayers,
            int difficulty)
        {
            if (Runner == null || Runner.LocalPlayer != target)
                return;

            Debug.Log(
                $"[FusionMultiplayer] MatchConfig JSON ack: len={jsonLength}, maxPlayers={maxPlayers}, difficulty={difficulty}");
        }

    }
}
