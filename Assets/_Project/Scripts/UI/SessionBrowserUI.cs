using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using FusionMultiplayer.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>Main-menu session browser: filters, scrollable room list, refresh, join.</summary>
    [DefaultExecutionOrder(110)]
    public sealed class SessionBrowserUI : MonoBehaviour
    {
        [SerializeField] private StringDropdownSelector _createGameModeSelector;
        [SerializeField] private StringDropdownSelector _createMapSelector;
        [SerializeField] private StringDropdownSelector _createDifficultySelector;
        [SerializeField] private StringDropdownSelector _createMaxPlayersSelector;
        [SerializeField] private Toggle _hiddenToggle;
        [SerializeField] private StringDropdownSelector _joinGameModeSelector;
        [SerializeField] private StringDropdownSelector _joinMapSelector;
        [SerializeField] private StringDropdownSelector _joinDifficultySelector;
        [SerializeField] private Transform _listContent;
        [SerializeField] private Button _refreshButton;
        [SerializeField] private Button _joinSelectedButton;
        [SerializeField] private TMP_Text _emptyHint;
        [SerializeField] private TMP_Text _modeHint;

        private readonly List<SessionInfo> _filteredSessions = new();
        private readonly List<bool> _sessionRowStarted = new();
        private readonly List<Button> _sessionRowButtons = new();
        private int _selectedSessionIndex = -1;
        private float _mapFilterDeadline;
        private bool _mapFilterExpired;
        private bool _wired;
        private bool _joinInProgress;
        private bool _joinPageActive;
        private float _nextDynamicRefresh;
        private bool _refreshInFlight;

        public event Action<string> JoinRequested;

        private void OnEnable()
        {
            ConnectionManager.SessionListUpdated += OnSessionListUpdated;
        }

        private void OnDisable()
        {
            ConnectionManager.SessionListUpdated -= OnSessionListUpdated;
        }

        internal void BindRuntime(
            StringDropdownSelector createGameMode,
            StringDropdownSelector createMap,
            StringDropdownSelector createDifficulty,
            StringDropdownSelector createMaxPlayers,
            Toggle hiddenToggle,
            StringDropdownSelector joinGameMode,
            StringDropdownSelector joinMap,
            StringDropdownSelector joinDifficulty,
            Transform listContent,
            Button refresh,
            Button joinSelected,
            TMP_Text emptyHint,
            TMP_Text modeHint)
        {
            _createGameModeSelector = createGameMode;
            _createMapSelector = createMap;
            _createDifficultySelector = createDifficulty;
            _createMaxPlayersSelector = createMaxPlayers;
            _hiddenToggle = hiddenToggle;
            _joinGameModeSelector = joinGameMode;
            _joinMapSelector = joinMap;
            _joinDifficultySelector = joinDifficulty;
            _listContent = listContent;
            _refreshButton = refresh;
            _joinSelectedButton = joinSelected;
            _emptyHint = emptyHint;
            _modeHint = modeHint;
            _wired = false;
            WireUi();
            ApplySelectorsFromSessionData();
        }

        private void Awake()
        {
            WireUi();
            ApplySelectorsFromSessionData();
        }

        public void SetJoinPageActive(bool active)
        {
            _joinPageActive = active;
            if (active)
                _ = BootstrapAsync();
        }

        public void RefreshSelectors() => ApplySelectorsFromSessionData();

        public void RequestJoinSelected()
        {
            OnJoinSelectedClicked();
        }

        private void WireUi()
        {
            if (_wired)
                return;

            if (_createGameModeSelector == null || _createMapSelector == null ||
                _joinGameModeSelector == null || _joinMapSelector == null)
                return;

            _createGameModeSelector.SelectionChanged -= OnGameModeChanged;
            _createMapSelector.SelectionChanged -= OnCreateMapChanged;
            _joinGameModeSelector.SelectionChanged -= OnGameModeChanged;
            _joinMapSelector.SelectionChanged -= OnJoinMapChanged;
            _createGameModeSelector.SelectionChanged += OnGameModeChanged;
            _createMapSelector.SelectionChanged += OnCreateMapChanged;
            _joinGameModeSelector.SelectionChanged += OnGameModeChanged;
            _joinMapSelector.SelectionChanged += OnJoinMapChanged;

            if (_createDifficultySelector != null)
            {
                _createDifficultySelector.SelectionChanged -= OnDifficultyChanged;
                _createDifficultySelector.SelectionChanged += OnDifficultyChanged;
            }

            if (_joinDifficultySelector != null)
            {
                _joinDifficultySelector.SelectionChanged -= OnDifficultyChanged;
                _joinDifficultySelector.SelectionChanged += OnDifficultyChanged;
            }

            if (_createMaxPlayersSelector != null)
            {
                _createMaxPlayersSelector.SelectionChanged -= OnMaxPlayersChanged;
                _createMaxPlayersSelector.SelectionChanged += OnMaxPlayersChanged;
            }

            if (_hiddenToggle != null)
            {
                _hiddenToggle.onValueChanged.RemoveListener(OnHiddenChanged);
                _hiddenToggle.onValueChanged.AddListener(OnHiddenChanged);
            }

            if (_refreshButton != null)
            {
                _refreshButton.onClick.RemoveListener(OnRefreshClicked);
                _refreshButton.onClick.AddListener(OnRefreshClicked);
            }

            if (_joinSelectedButton != null)
            {
                _joinSelectedButton.onClick.RemoveListener(OnJoinSelectedClicked);
                _joinSelectedButton.onClick.AddListener(OnJoinSelectedClicked);
            }

            _wired = true;
        }

        private void ApplySelectorsFromSessionData()
        {
            var modeLabels = GetModeLabels();
            var mapLabels = GetMapLabels();
            var difficultyLabels = GetDifficultyLabels();
            var maxPlayerLabels = GetMaxPlayerLabels();

            var modeIndex = Array.IndexOf(SessionCatalog.AllGameModes, SessionData.SelectedGameMode);
            if (modeIndex < 0) modeIndex = 0;

            var mapIndex = 0;
            if (SessionData.SelectedMap != SessionCatalog.MapKind.Any)
            {
                mapIndex = Array.IndexOf(SessionCatalog.SelectableMaps, SessionData.SelectedMap) + 1;
                if (mapIndex < 1) mapIndex = 1;
            }

            var diffIndex = Array.IndexOf(SessionCatalog.AllDifficulties, SessionData.SelectedDifficulty);
            if (diffIndex < 0) diffIndex = 1;

            var maxIndex = Mathf.Clamp(SessionData.MaxPlayers - SessionData.MinPlayers, 0,
                SessionData.MaxPlayersCap - SessionData.MinPlayers);

            _createGameModeSelector?.SetOptions(modeLabels, modeIndex);
            _createMapSelector?.SetOptions(mapLabels, mapIndex);
            _createDifficultySelector?.SetOptions(difficultyLabels, diffIndex);
            _createMaxPlayersSelector?.SetOptions(maxPlayerLabels, maxIndex);
            _joinGameModeSelector?.SetOptions(modeLabels, modeIndex);
            _joinMapSelector?.SetOptions(mapLabels, mapIndex);
            _joinDifficultySelector?.SetOptions(difficultyLabels, diffIndex);

            if (_hiddenToggle != null)
                _hiddenToggle.isOn = SessionData.HiddenSession;

            UpdateModeHint();
        }

        private async Task BootstrapAsync()
        {
            ResetMapFilterTimer();
            await RefreshLobbyAsync();
        }

        private void Update()
        {
            if (!_joinPageActive)
                return;

            if (!_mapFilterExpired && SessionData.SelectedMap != SessionCatalog.MapKind.Any &&
                Time.unscaledTime >= _mapFilterDeadline)
            {
                _mapFilterExpired = true;
                RebuildList(ConnectionManager.Instance != null
                    ? ConnectionManager.Instance.CachedSessionList
                    : Array.Empty<SessionInfo>());
            }

            // Dynamic matchmaking: keep the lobby list fresh while browsing.
            if (Time.unscaledTime >= _nextDynamicRefresh)
            {
                _nextDynamicRefresh = Time.unscaledTime + 4f;
                if (!_refreshInFlight)
                    _ = RefreshLobbyAsync();
            }
        }

        private void OnDifficultyChanged(int index, string label)
        {
            if (index < 0 || index >= SessionCatalog.AllDifficulties.Length)
                return;

            SessionData.SelectedDifficulty = SessionCatalog.AllDifficulties[index];
            _createDifficultySelector?.SetOptions(GetDifficultyLabels(), index);
            _joinDifficultySelector?.SetOptions(GetDifficultyLabels(), index);
            if (_joinPageActive)
                RebuildList(ConnectionManager.Instance != null
                    ? ConnectionManager.Instance.CachedSessionList
                    : Array.Empty<SessionInfo>());
        }

        private void OnMaxPlayersChanged(int index, string label)
        {
            SessionData.MaxPlayers = SessionData.ClampMaxPlayers(SessionData.MinPlayers + index);
        }

        private void OnGameModeChanged(int index, string label)
        {
            if (index < 0 || index >= SessionCatalog.AllGameModes.Length)
                return;

            SessionData.SelectedGameMode = SessionCatalog.AllGameModes[index];
            SyncModeSelectors(index);
            UpdateModeHint();
            ResetMapFilterTimer();

            if (_joinPageActive)
                _ = RefreshLobbyAsync();
        }

        private void OnCreateMapChanged(int index, string label)
        {
            ApplyMapSelection(index);
            SyncMapSelectors(index);
        }

        private void OnJoinMapChanged(int index, string label)
        {
            ApplyMapSelection(index);
            SyncMapSelectors(index);
            RebuildList(ConnectionManager.Instance != null
                ? ConnectionManager.Instance.CachedSessionList
                : Array.Empty<SessionInfo>());
        }

        private static void ApplyMapSelection(int index)
        {
            SessionData.SelectedMap = index <= 0
                ? SessionCatalog.MapKind.Any
                : SessionCatalog.SelectableMaps[index - 1];
        }

        private void SyncModeSelectors(int index)
        {
            _createGameModeSelector?.SetOptions(GetModeLabels(), index);
            _joinGameModeSelector?.SetOptions(GetModeLabels(), index);
        }

        private void SyncMapSelectors(int index)
        {
            _createMapSelector?.SetOptions(GetMapLabels(), index);
            _joinMapSelector?.SetOptions(GetMapLabels(), index);
        }

        private static List<string> GetModeLabels()
        {
            var labels = new List<string>();
            foreach (var mode in SessionCatalog.AllGameModes)
                labels.Add(SessionCatalog.GetModeLabel(mode));
            return labels;
        }

        private static List<string> GetMapLabels()
        {
            var labels = new List<string> { SessionCatalog.GetMapLabel(SessionCatalog.MapKind.Any) };
            foreach (var map in SessionCatalog.SelectableMaps)
                labels.Add(SessionCatalog.GetMapLabel(map));
            return labels;
        }

        private static List<string> GetDifficultyLabels()
        {
            var labels = new List<string>();
            foreach (var d in SessionCatalog.AllDifficulties)
                labels.Add(SessionCatalog.GetDifficultyLabel(d));
            return labels;
        }

        private static List<string> GetMaxPlayerLabels()
        {
            var labels = new List<string>();
            for (var i = SessionData.MinPlayers; i <= SessionData.MaxPlayersCap; i++)
                labels.Add(i.ToString());
            return labels;
        }

        private void OnHiddenChanged(bool value)
        {
            SessionData.HiddenSession = value;
        }

        private void OnRefreshClicked() => _ = RefreshLobbyAsync();

        private void OnJoinSelectedClicked()
        {
            if (_joinInProgress || _selectedSessionIndex < 0 || _selectedSessionIndex >= _filteredSessions.Count)
                return;

            var session = _filteredSessions[_selectedSessionIndex];
            if (!session.IsValid || SessionCatalog.IsSessionStarted(session))
                return;

            JoinRequested?.Invoke(session.Name);
        }

        private async Task RefreshLobbyAsync()
        {
            if (!_joinPageActive || _refreshInFlight)
                return;

            if (ConnectionManager.Instance == null)
            {
                SetEmptyHint(UiCopy.SessionBrowserNoConnection);
                RebuildList(Array.Empty<SessionInfo>());
                return;
            }

            if (SessionData.UseOfflineMode)
            {
                SetEmptyHint(UiCopy.SessionBrowserOffline);
                RebuildList(Array.Empty<SessionInfo>());
                return;
            }

            _refreshInFlight = true;
            SetEmptyHint(UiCopy.SessionBrowserLoading);
            try
            {
                var ok = await ConnectionManager.Instance.EnsureSessionLobbyAsync(SessionData.SelectedGameMode);
                if (!_joinPageActive)
                    return;

                if (!ok)
                {
                    SetEmptyHint(UiCopy.SessionBrowserLobbyFailed);
                    RebuildList(Array.Empty<SessionInfo>());
                    return;
                }

                RebuildList(ConnectionManager.Instance.CachedSessionList);
            }
            finally
            {
                _refreshInFlight = false;
            }
        }

        private void OnSessionListUpdated(IReadOnlyList<SessionInfo> sessions)
        {
            if (!_joinPageActive)
                return;

            RebuildList(sessions);
        }

        private void RebuildList(IReadOnlyList<SessionInfo> sessions)
        {
            _filteredSessions.Clear();
            _sessionRowStarted.Clear();
            _selectedSessionIndex = -1;
            ClearSessionRows();

            var openCount = 0;
            if (sessions != null)
            {
                foreach (var session in sessions)
                {
                    if (!session.IsValid || !PassesMapFilter(session) || !PassesDifficultyFilter(session))
                        continue;

                    var started = SessionCatalog.IsSessionStarted(session);
                    if (!started)
                        openCount++;

                    _filteredSessions.Add(session);
                    _sessionRowStarted.Add(started);
                }
            }

            if (_filteredSessions.Count == 0)
            {
                if (_emptyHint != null)
                {
                    if (_emptyHint.text == UiCopy.SessionBrowserLoading)
                        _emptyHint.text = UiCopy.SessionBrowserEmpty;
                    else if (string.IsNullOrWhiteSpace(_emptyHint.text) ||
                             _emptyHint.text == UiCopy.SessionBrowserStartedOnly)
                        _emptyHint.text = UiCopy.SessionBrowserEmpty;
                    _emptyHint.gameObject.SetActive(true);
                }

                UpdateJoinButton();
                return;
            }

            if (_emptyHint != null)
            {
                if (openCount == 0)
                {
                    _emptyHint.text = UiCopy.SessionBrowserStartedOnly;
                    _emptyHint.gameObject.SetActive(true);
                }
                else
                {
                    _emptyHint.text = string.Empty;
                    _emptyHint.gameObject.SetActive(false);
                }
            }

            if (_listContent != null)
            {
                for (var i = 0; i < _filteredSessions.Count; i++)
                {
                    var index = i;
                    var started = _sessionRowStarted[i];
                    var label = SessionCatalog.FormatSessionRow(_filteredSessions[i]);
                    var button = UiRuntimeBuildKit.CreatePmChannelOptionButton(_listContent, $"Session_{i}", label);
                    button.interactable = !started;
                    if (button.targetGraphic is Image rowBg)
                        rowBg.color = started
                            ? new Color(0.22f, 0.22f, 0.24f, 0.85f)
                            : UiTheme.InputBackground;

                    if (!started)
                        button.onClick.AddListener(() => OnSessionRowClicked(index));

                    _sessionRowButtons.Add(button);
                }
            }

            UpdateJoinButton();
        }

        private void ClearSessionRows()
        {
            _sessionRowButtons.Clear();
            if (_listContent == null)
                return;

            for (var i = _listContent.childCount - 1; i >= 0; i--)
            {
                var child = _listContent.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }

        private void OnSessionRowClicked(int index)
        {
            if (index < 0 || index >= _sessionRowStarted.Count || _sessionRowStarted[index])
                return;

            _selectedSessionIndex = index;
            UpdateSessionRowHighlight();
            UpdateJoinButton();
        }

        private void UpdateSessionRowHighlight()
        {
            for (var i = 0; i < _sessionRowButtons.Count; i++)
            {
                if (_sessionRowButtons[i].targetGraphic is not Image img)
                    continue;

                if (_sessionRowStarted.Count > i && _sessionRowStarted[i])
                {
                    img.color = new Color(0.22f, 0.22f, 0.24f, 0.85f);
                    continue;
                }

                img.color = i == _selectedSessionIndex
                    ? new Color(UiTheme.TitleAccent.r, UiTheme.TitleAccent.g, UiTheme.TitleAccent.b, 0.45f)
                    : UiTheme.InputBackground;
            }
        }

        private bool PassesMapFilter(SessionInfo session)
        {
            if (SessionData.SelectedMap == SessionCatalog.MapKind.Any || _mapFilterExpired)
                return true;
            return SessionCatalog.TryGetMap(session, out var map) && map == SessionData.SelectedMap;
        }

        private static bool PassesDifficultyFilter(SessionInfo session)
        {
            if (!SessionCatalog.TryGetDifficulty(session, out var difficulty))
                return true;
            return difficulty == SessionData.SelectedDifficulty;
        }

        private void UpdateJoinButton()
        {
            if (_joinSelectedButton == null)
                return;

            var canJoin = !_joinInProgress && _selectedSessionIndex >= 0 &&
                          _selectedSessionIndex < _filteredSessions.Count &&
                          _selectedSessionIndex < _sessionRowStarted.Count &&
                          !_sessionRowStarted[_selectedSessionIndex] &&
                          _filteredSessions[_selectedSessionIndex].IsValid &&
                          !SessionCatalog.IsSessionStarted(_filteredSessions[_selectedSessionIndex]);
            _joinSelectedButton.interactable = canJoin;
        }

        public void SetJoinInProgress(bool inProgress)
        {
            _joinInProgress = inProgress;
            UpdateJoinButton();
            if (_refreshButton != null)
                _refreshButton.interactable = !inProgress;
        }

        private void SetEmptyHint(string message)
        {
            if (_emptyHint != null)
            {
                _emptyHint.text = message;
                _emptyHint.gameObject.SetActive(true);
            }

            ClearSessionRows();
            _selectedSessionIndex = -1;
            UpdateJoinButton();
        }

        private void ResetMapFilterTimer()
        {
            _mapFilterExpired = SessionData.SelectedMap == SessionCatalog.MapKind.Any;
            _mapFilterDeadline = Time.unscaledTime + SessionCatalog.MapFilterTimeoutSeconds;
        }

        private void UpdateModeHint()
        {
            if (_modeHint == null)
                return;

            _modeHint.text = SessionCatalog.GetModeDescription(SessionData.SelectedGameMode);
            if (SessionData.SelectedMap != SessionCatalog.MapKind.Any)
                _modeHint.text += " · " + SessionCatalog.GetMapDescription(SessionData.SelectedMap);
        }
    }
}
