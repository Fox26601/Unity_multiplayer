using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using FusionMultiplayer.UI;
using UnityEngine;

namespace FusionMultiplayer.Core
{
    public partial class ConnectionManager
    {
        /// <summary>Joins the Photon session lobby for browsing rooms of the selected game mode.</summary>
        public Task<bool> EnsureSessionLobbyAsync(SessionCatalog.GameModeKind mode)
        {
            if (SessionData.UseOfflineMode)
            {
                _sessionList.Clear();
                SessionListUpdated?.Invoke(_sessionList);
                return Task.FromResult(true);
            }

            // Already in the requested lobby — session list keeps updating via callbacks.
            if (_inSessionLobby && _lobbyGameMode == mode)
                return Task.FromResult(true);

            // Single-flight: concurrent Refresh/Update must not call JoinSessionLobby twice.
            if (_lobbyJoinTask != null && !_lobbyJoinTask.IsCompleted)
            {
                if (_lobbyJoinMode == mode)
                    return _lobbyJoinTask;

                // Different mode requested while joining — wait, then re-enter for the new mode.
                return AwaitThenEnsureLobbyAsync(mode);
            }

            _lobbyJoinMode = mode;
            _lobbyJoinTask = JoinSessionLobbyInternalAsync(mode);
            return _lobbyJoinTask;
        }

        /// <summary>
        /// Ensures lobby membership. Photon pushes list updates; re-joining while JoiningLobby causes errors.
        /// </summary>
        public Task<bool> RefreshSessionLobbyAsync(SessionCatalog.GameModeKind mode) =>
            EnsureSessionLobbyAsync(mode);

        private async Task<bool> AwaitThenEnsureLobbyAsync(SessionCatalog.GameModeKind mode)
        {
            try
            {
                if (_lobbyJoinTask != null)
                    await _lobbyJoinTask;
            }
            catch
            {
                // ignored — retry below
            }

            return await EnsureSessionLobbyAsync(mode);
        }

        private async Task<bool> JoinSessionLobbyInternalAsync(SessionCatalog.GameModeKind mode)
        {
            // Active match session — cannot browse lobbies on the same runner.
            if (_runner != null && _runner.IsRunning && !_inSessionLobby && !string.IsNullOrEmpty(_activeRoomName))
                return false;

            EnsureRunnerComponent(provideInput: true);
            if (_runner == null)
                return false;

            if (_inSessionLobby && _lobbyGameMode == mode)
                return true;

            _lobbyGameMode = mode;
            var lobbyName = SessionCatalog.LobbyNameForMode(mode);
            _sessionList.Clear();
            try
            {
                var result = await _runner.JoinSessionLobby(SessionLobby.Custom, lobbyName);
                if (!result.Ok)
                {
                    var msg = FormatStartGameError(result);
                    Debug.LogWarning($"Session lobby join failed: {msg}");
                    SessionFailed?.Invoke(msg);
                    _inSessionLobby = false;
                    await DisposeRunnerAsync();
                    return false;
                }
            }
            catch (Exception ex)
            {
                var msg = $"Session lobby join failed: {ex.Message}";
                Debug.LogWarning(msg);
                SessionFailed?.Invoke(msg);
                _inSessionLobby = false;
                await DisposeRunnerAsync();
                return false;
            }

            _inSessionLobby = true;
            return true;
        }

        /// <summary>Quick-join first open session matching mode / map / difficulty.</summary>
        public async Task<bool> QuickJoinAsync()
        {
            var preferredMode = SessionData.SelectedGameMode;
            var map = SessionData.SelectedMap;
            var difficulty = SessionData.SelectedDifficulty;

#if UNITY_EDITOR
            // MPPM virtual players: join the room the main editor just hosted.
            if (MppmUtility.IsVirtualInstance && MppmSessionBridge.TryReadRoom(out var mppmRoom))
            {
                Debug.Log($"[FusionMultiplayer] Quick Join via MPPM bridge → '{mppmRoom}'");
                return await JoinSessionAsync(mppmRoom);
            }
#endif

            // Prefer the selected mode lobby; if empty, scan other mode lobbies (map + difficulty still apply).
            var modesToTry = new List<SessionCatalog.GameModeKind> { preferredMode };
            foreach (var mode in SessionCatalog.AllGameModes)
            {
                if (mode != preferredMode)
                    modesToTry.Add(mode);
            }

            SessionInfo best = default;
            var found = false;

            foreach (var mode in modesToTry)
            {
                if (!await EnsureSessionLobbyAsync(mode))
                    continue;

                await WaitForSessionListAsync(2.5f);

                SessionInfo bestInLobby = default;
                var foundInLobby = false;
                foreach (var info in _sessionList)
                {
                    if (!SessionCatalog.MatchesFilters(info, mode, map, difficulty, requireOpen: true))
                        continue;

                    if (!foundInLobby || info.PlayerCount > bestInLobby.PlayerCount)
                    {
                        bestInLobby = info;
                        foundInLobby = true;
                    }
                }

                if (!foundInLobby)
                    continue;

                best = bestInLobby;
                found = true;
                // modesToTry is preferred-first — take the first lobby that has a match.
                break;
            }

            if (!found)
            {
                var filters =
                    $"{SessionCatalog.GetModeLabel(preferredMode)} / {SessionCatalog.GetMapLabel(map)} / {SessionCatalog.GetDifficultyLabel(difficulty)}";
                SessionFailed?.Invoke(UiCopy.QuickJoinNoMatchDetail(filters));
                return false;
            }

            if (SessionCatalog.TryGetGameMode(best, out var joinedMode) && joinedMode != preferredMode)
            {
                Debug.Log(
                    $"[FusionMultiplayer] Quick Join: no '{preferredMode}' rooms; joining '{joinedMode}' room '{best.Name}'.");
                SessionData.SelectedGameMode = joinedMode;
            }

            return await JoinSessionAsync(best.Name);
        }

        /// <summary>True when the room name appears in the current lobby session list.</summary>
        public async Task<bool> ProbeReconnectRoomAsync(string roomName, SessionCatalog.GameModeKind mode)
        {
            if (string.IsNullOrWhiteSpace(roomName) || SessionData.UseOfflineMode)
                return false;

            var target = roomName.Trim();
            try
            {
                if (!await EnsureSessionLobbyAsync(mode))
                    return false;

                return await WaitForNamedSessionAsync(target, 2.5f);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FusionMultiplayer] Reconnect room list check failed: {ex.Message}");
            }

            return false;
        }

        /// <summary>Waits for Photon to push a session list after joining a lobby.</summary>
        private async Task WaitForSessionListAsync(float timeoutSeconds)
        {
            if (_sessionList.Count > 0)
            {
                await Task.Delay(150);
                return;
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            void OnList(IReadOnlyList<SessionInfo> _)
            {
                tcs.TrySetResult(true);
            }

            SessionListUpdated += OnList;
            try
            {
                var timeout = Task.Delay(TimeSpan.FromSeconds(Mathf.Max(0.5f, timeoutSeconds)));
                await Task.WhenAny(tcs.Task, timeout);
                await Task.Delay(200);
            }
            finally
            {
                SessionListUpdated -= OnList;
            }
        }

        /// <summary>
        /// Waits until <paramref name="roomName"/> appears in the lobby list, or the timeout elapses.
        /// Does not treat "any sessions present" as ready (avoids false negatives after lobby churn).
        /// </summary>
        private async Task<bool> WaitForNamedSessionAsync(string roomName, float timeoutSeconds)
        {
            if (SessionListContainsRoom(roomName))
                return true;

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            void OnList(IReadOnlyList<SessionInfo> _)
            {
                if (SessionListContainsRoom(roomName))
                    tcs.TrySetResult(true);
            }

            SessionListUpdated += OnList;
            try
            {
                // One more check in case the list arrived between the first scan and subscribe.
                if (SessionListContainsRoom(roomName))
                    return true;

                var timeout = Task.Delay(TimeSpan.FromSeconds(Mathf.Max(0.5f, timeoutSeconds)));
                var completed = await Task.WhenAny(tcs.Task, timeout);
                if (completed == tcs.Task)
                    return true;

                return SessionListContainsRoom(roomName);
            }
            finally
            {
                SessionListUpdated -= OnList;
            }
        }

        private bool SessionListContainsRoom(string roomName)
        {
            foreach (var info in _sessionList)
            {
                if (info != null && info.IsValid &&
                    string.Equals(info.Name, roomName, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            _sessionList.Clear();
            if (sessionList != null)
                _sessionList.AddRange(sessionList);
            SessionListUpdated?.Invoke(_sessionList);
        }
    }
}
