using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using FusionMultiplayer.Core;
using UnityEngine;

namespace FusionMultiplayer.Reconnect
{
    /// <summary>Main-menu reconnect button visibility (lobby list + stored session).</summary>
    public sealed class SessionReconnectPresenter
    {
        public event Action<bool> ReconnectAvailableChanged;

        private int _probeId;
        private float _lastListRefresh = -10f;
        private bool _probeRunning;
        private bool _probeQueued;
        private bool _landingActive;

        public void SetLandingActive(bool active)
        {
            _landingActive = active;
            if (!active)
                _probeId++;
        }

        public void OnSessionListUpdated(IReadOnlyList<SessionInfo> _)
        {
            if (!_landingActive)
                return;
            if (Time.unscaledTime - _lastListRefresh < 0.75f)
                return;
            _lastListRefresh = Time.unscaledTime;
            Refresh();
        }

        public void Cancel()
        {
            _probeQueued = false;
            _probeId++;
        }

        public void Refresh()
        {
            if (_probeRunning)
            {
                _probeQueued = true;
                return;
            }

            _ = RefreshLoopAsync();
        }

        public void NotifyReconnectFailed()
        {
            SessionReconnectStore.Clear();
            Refresh();
        }

        public bool TryLoadTarget(out string room, out string token, out SessionCatalog.GameModeKind mode,
            out bool hidden)
        {
            return SessionReconnectStore.TryLoad(out room, out token, out mode, out hidden);
        }

        private async Task RefreshLoopAsync()
        {
            _probeRunning = true;
            try
            {
                do
                {
                    _probeQueued = false;
                    await ProbeAsync();
                } while (_probeQueued);
            }
            finally
            {
                _probeRunning = false;
            }
        }

        private async Task ProbeAsync()
        {
            var probeId = ++_probeId;

            if (SessionData.UseOfflineMode)
            {
                SessionReconnectStore.Clear();
                if (probeId == _probeId && _landingActive)
                    ReconnectAvailableChanged?.Invoke(false);
                return;
            }

            if (!SessionReconnectStore.TryLoad(out var room, out _, out var mode, out var hidden))
            {
                if (probeId == _probeId && _landingActive)
                    ReconnectAvailableChanged?.Invoke(false);
                return;
            }

            var cm = ConnectionManager.Instance;
            if (cm == null)
            {
                // Keep store; show for Hidden without a runner, otherwise wait for ConnectionManager.
                if (probeId == _probeId && _landingActive)
                    ReconnectAvailableChanged?.Invoke(hidden);
                return;
            }

            var found = false;
            try
            {
                found = await cm.ProbeReconnectRoomAsync(room, mode);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FusionMultiplayer] Reconnect room check failed: {ex.Message}");
            }

            if (probeId != _probeId || !_landingActive)
                return;

            // Do not Clear on miss — false negatives from lobby list churn would wipe a valid session.
            ReconnectAvailableChanged?.Invoke(found || hidden);
        }
    }
}
