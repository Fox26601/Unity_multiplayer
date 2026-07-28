using Fusion;
using FusionMultiplayer.Core;
using FusionMultiplayer.Player;
using UnityEngine;

namespace FusionMultiplayer.UI
{
    /// <summary>Cached local-player lookups for HUD Update loops.</summary>
    internal static class LocalPlayerHudCache
    {
        private static PlayerAvatar _avatar;
        private static PlayerData _data;
        private static PlayerRef _boundPlayer = PlayerRef.None;
        private static float _nextRefresh;

        public static bool TryGetLocalAvatar(out PlayerAvatar avatar)
        {
            EnsureFresh();
            avatar = _avatar;
            return avatar != null && avatar.Object != null && avatar.Object.IsValid;
        }

        public static bool TryGetLocalPlayerData(out PlayerData data)
        {
            EnsureFresh();
            data = _data;
            return data != null && data.Object != null && data.Object.IsValid;
        }

        public static void Invalidate()
        {
            _avatar = null;
            _data = null;
            _boundPlayer = PlayerRef.None;
            _nextRefresh = 0f;
        }

        private static void EnsureFresh()
        {
            var runner = ConnectionManager.Instance != null ? ConnectionManager.Instance.Runner : null;
            if (runner == null || !runner.IsRunning)
            {
                Invalidate();
                return;
            }

            var local = runner.LocalPlayer;
            if (local == PlayerRef.None)
            {
                Invalidate();
                return;
            }

            var needRefresh = local != _boundPlayer ||
                              Time.unscaledTime >= _nextRefresh ||
                              _avatar == null ||
                              _avatar.Object == null ||
                              !_avatar.Object.IsValid ||
                              _data == null ||
                              _data.Object == null ||
                              !_data.Object.IsValid;

            if (!needRefresh)
                return;

            _boundPlayer = local;
            _avatar = PlayerOwnership.FindAvatar(local);
            _data = PlayerOwnership.FindPlayerData(local, _avatar != null ? _avatar.CharacterSlot : -1);
            _nextRefresh = Time.unscaledTime + 0.5f;
        }
    }
}
