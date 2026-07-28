using Fusion;
using FusionMultiplayer.Core;
using FusionMultiplayer.Player;
using UnityEngine;

namespace FusionMultiplayer.Chat
{
    public interface IChatPresenter
    {
        void DisplayMessage(PlayerRef sender, string text, Color senderTint, bool isWhisper, PlayerRef recipient);
    }

    /// <summary>
    /// Replicated chat: broadcast to everyone or whisper to a single peer via targeted RPC.
    /// </summary>
    public class ChatManager : NetworkBehaviour
    {
        /// <summary>Max chars per RPC message (NetworkString capacity must fit Fusion 512-byte RPC limit).</summary>
        private const int RpcTextCapacity = 64;

        public static ChatManager Instance { get; private set; }

        private static IChatPresenter _presenter;

        public static void AttachPresenter(IChatPresenter presenter)
        {
            if (presenter != null)
                _presenter = presenter;
        }

        public static void DetachPresenter(IChatPresenter presenter)
        {
            if (_presenter == presenter)
                _presenter = null;
        }

        public override void Spawned()
        {
            Instance = this;
            ChatDiagnostics.LogChatManagerState("spawned", true, Object != null && Object.IsValid);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this)
            {
                Instance = null;
                ChatDiagnostics.Log("manager despawned");
            }

            base.Despawned(runner, hasState);
        }

        /// <returns>True when the message was queued for send.</returns>
        public bool RequestSend(string message, bool whisper, PlayerRef whisperTarget)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                ChatDiagnostics.LogSend(message, whisper, whisperTarget, Instance != null, false, false);
                return false;
            }

            if (Runner == null || !Runner.IsRunning)
            {
                ChatDiagnostics.LogSend(message, whisper, whisperTarget, Instance != null, false, false);
                return false;
            }

            if (Object == null || !Object.IsValid)
            {
                ChatDiagnostics.LogSend(message, whisper, whisperTarget, Instance != null, false, false);
                return false;
            }

            var text = Clamp(message);
            if (!whisper)
            {
                RPC_Broadcast(text);
                ChatDiagnostics.LogSend(message, false, PlayerRef.None, true, true, true);
                return true;
            }

            if (whisperTarget == PlayerRef.None)
            {
                ChatDiagnostics.LogSend(message, true, whisperTarget, true, true, false);
                return false;
            }

            if (HasStateAuthority)
                RPC_NotifyWhisper(whisperTarget, Runner.LocalPlayer, text);
            else
                RPC_RequestWhisper(whisperTarget, text);

            ChatDiagnostics.LogSend(message, true, whisperTarget, true, true, true);
            return true;
        }

        private static NetworkString<_64> Clamp(string s)
        {
            if (string.IsNullOrEmpty(s)) return default;
            if (s.Length > RpcTextCapacity) s = s.Substring(0, RpcTextCapacity);
            return s;
        }

        private static Color ResolveTint(PlayerRef sender)
        {
            foreach (var pd in PlayerRegistry.EnumerateAllData())
            {
                if (pd.Object != null && pd.Object.IsValid && pd.Object.InputAuthority == sender)
                    return pd.Tint;
            }

            return Color.white;
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RPC_Broadcast(NetworkString<_64> text, RpcInfo info = default)
        {
            DeliverMessage(info.Source, text.ToString(), ResolveTint(info.Source), false, PlayerRef.None);
        }

        /// <summary>Non-authority clients ask scene authority to fan out a whisper.</summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestWhisper(PlayerRef recipient, NetworkString<_64> text, RpcInfo info = default)
        {
            RPC_NotifyWhisper(recipient, info.Source, text);
        }

        /// <summary>Deliver PM to sender and recipient only (reliable in Shared Mode).</summary>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All, InvokeLocal = false)]
        private void RPC_NotifyWhisper(PlayerRef recipient, PlayerRef sender, NetworkString<_64> text)
        {
            var local = Runner.LocalPlayer;
            if (local != recipient && local != sender)
                return;

            DeliverMessage(sender, text.ToString(), ResolveTint(sender), true, recipient);
        }

        private static void DeliverMessage(PlayerRef sender, string text, Color tint, bool isWhisper,
            PlayerRef recipient)
        {
            ChatDiagnostics.LogReceive(sender, text, isWhisper, recipient);

            if (_presenter == null)
            {
                ChatDiagnostics.LogPresenterMissing("DeliverMessage");
                return;
            }

            _presenter.DisplayMessage(sender, text, tint, isWhisper, recipient);
        }
    }
}
