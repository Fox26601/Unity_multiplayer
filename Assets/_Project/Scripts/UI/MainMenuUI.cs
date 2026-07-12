using System.Threading.Tasks;
using FusionMultiplayer.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>
    /// Main menu: landing profile, create-room page, join-room page with session dropdown.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class MainMenuUI : MonoBehaviour
    {
        public enum MainMenuPage
        {
            Landing,
            Create,
            Join
        }

        [SerializeField] private GameObject _landingPage;
        [SerializeField] private GameObject _createPage;
        [SerializeField] private GameObject _joinPage;

        [SerializeField] private TMP_InputField _nicknameField;
        [SerializeField] private TMP_InputField _roomField;
        [SerializeField] private Button _createButton;
        [SerializeField] private Button _joinSessionButton;
        [SerializeField] private Button _goCreateButton;
        [SerializeField] private Button _goJoinButton;
        [SerializeField] private Button _backFromCreateButton;
        [SerializeField] private Button _backFromJoinButton;
        [SerializeField] private Image _colorPreview;
        [SerializeField] private Button _randomColorButton;
        [SerializeField] private Button _quickJoinButton;
        [SerializeField] private Button _reconnectButton;
        [SerializeField] private SessionFlowUI _sessionFlow;
        [SerializeField] private SessionBrowserUI _sessionBrowser;

        private InputField _nicknameLegacy;
        private InputField _roomLegacy;
        private bool _wired;

        private void OnEnable()
        {
            CleanupDropdownOverlay();
            MainMenuRuntimeRebuild.EnsureBuilt(transform);
        }

        private void Awake()
        {
            MainMenuRuntimeRebuild.EnsureBuilt(transform);
            WireUi();
            if (_sessionFlow == null) _sessionFlow = GetComponent<SessionFlowUI>();
            if (_sessionFlow == null) _sessionFlow = gameObject.AddComponent<SessionFlowUI>();
            if (_sessionBrowser == null) _sessionBrowser = GetComponent<SessionBrowserUI>();
            if (GetComponent<CareerStatsHud>() == null)
                gameObject.AddComponent<CareerStatsHud>();
        }

        /// <summary>Called by MainMenuRuntimeRebuild after it creates TMP UI widgets.</summary>
        public void BindRuntimeTmp(
            GameObject landingPage,
            GameObject createPage,
            GameObject joinPage,
            TMP_InputField nickname,
            TMP_InputField room,
            Button goCreate,
            Button goJoin,
            Button backCreate,
            Button backJoin,
            Button create,
            Button joinSession,
            Image colorPreview,
            Button randomColor,
            SessionBrowserUI browser,
            StringDropdownSelector createGameMode,
            StringDropdownSelector createMap,
            StringDropdownSelector createDifficulty,
            StringDropdownSelector createMaxPlayers,
            Toggle hiddenToggle,
            StringDropdownSelector joinGameMode,
            StringDropdownSelector joinMap,
            StringDropdownSelector joinDifficulty,
            Transform sessionListContent,
            Button refresh,
            TMP_Text emptyHint,
            TMP_Text modeHint,
            Button quickJoin,
            Button reconnect)
        {
            _landingPage = landingPage;
            _createPage = createPage;
            _joinPage = joinPage;
            _nicknameField = nickname;
            _roomField = room;
            _goCreateButton = goCreate;
            _goJoinButton = goJoin;
            _backFromCreateButton = backCreate;
            _backFromJoinButton = backJoin;
            _createButton = create;
            _joinSessionButton = joinSession;
            _colorPreview = colorPreview;
            _randomColorButton = randomColor;
            _quickJoinButton = quickJoin;
            _reconnectButton = reconnect;
            _sessionBrowser = browser;
            _nicknameLegacy = null;
            _roomLegacy = null;
            _wired = false;
            WireUi();

            _sessionBrowser?.BindRuntime(createGameMode, createMap, createDifficulty, createMaxPlayers, hiddenToggle,
                joinGameMode, joinMap, joinDifficulty, sessionListContent, refresh, joinSession, emptyHint, modeHint);
        }

        /// <summary>Legacy uGUI fields (fallback).</summary>
        public void BindRuntime(InputField nickname, InputField room, Button create, Button join,
            Image colorPreview, Button randomColor)
        {
            _nicknameLegacy = nickname;
            _roomLegacy = room;
            _createButton = create;
            _joinSessionButton = join;
            _colorPreview = colorPreview;
            _randomColorButton = randomColor;
            _nicknameField = null;
            _roomField = null;
            _wired = false;
            WireUi();
        }

        private void WireUi()
        {
            if (_wired)
                return;

            ResolveRefs();

            _goCreateButton?.onClick.RemoveListener(OnGoCreateClicked);
            _goJoinButton?.onClick.RemoveListener(OnGoJoinClicked);
            _backFromCreateButton?.onClick.RemoveListener(OnBackFromCreateClicked);
            _backFromJoinButton?.onClick.RemoveListener(OnBackFromJoinClicked);
            _createButton?.onClick.RemoveListener(OnCreateClicked);
            _joinSessionButton?.onClick.RemoveListener(OnJoinSessionClicked);
            _randomColorButton?.onClick.RemoveListener(OnRandomColor);
            _quickJoinButton?.onClick.RemoveListener(OnQuickJoinClicked);
            _reconnectButton?.onClick.RemoveListener(OnReconnectClicked);

            if (_goCreateButton != null) _goCreateButton.onClick.AddListener(OnGoCreateClicked);
            if (_goJoinButton != null) _goJoinButton.onClick.AddListener(OnGoJoinClicked);
            if (_backFromCreateButton != null) _backFromCreateButton.onClick.AddListener(OnBackFromCreateClicked);
            if (_backFromJoinButton != null) _backFromJoinButton.onClick.AddListener(OnBackFromJoinClicked);
            if (_createButton != null) _createButton.onClick.AddListener(OnCreateClicked);
            if (_joinSessionButton != null) _joinSessionButton.onClick.AddListener(OnJoinSessionClicked);
            if (_randomColorButton != null) _randomColorButton.onClick.AddListener(OnRandomColor);
            if (_quickJoinButton != null) _quickJoinButton.onClick.AddListener(OnQuickJoinClicked);
            if (_reconnectButton != null) _reconnectButton.onClick.AddListener(OnReconnectClicked);

            if (_sessionBrowser != null)
            {
                _sessionBrowser.JoinRequested -= OnJoinSelectedSession;
                _sessionBrowser.JoinRequested += OnJoinSelectedSession;
            }

            _wired = true;
        }

        private void ResolveRefs()
        {
            var panel = transform.Find("Panel");
            if (panel == null)
                return;

            _landingPage ??= panel.Find("LandingPage")?.gameObject;
            _createPage ??= panel.Find("CreatePage")?.gameObject;
            _joinPage ??= panel.Find("JoinPage")?.gameObject;

            if (_nicknameField == null)
                _nicknameField = panel.Find("LandingPage/NicknameField")?.GetComponent<TMP_InputField>();
            if (_nicknameField == null)
                _nicknameLegacy = panel.Find("LandingPage/NicknameField")?.GetComponent<InputField>();

            if (_roomField == null)
                _roomField = panel.Find("CreatePage/FormRoot/RoomFieldRow/RoomField")?.GetComponent<TMP_InputField>();
            if (_roomField == null)
                _roomField = panel.Find("CreatePage/RoomField")?.GetComponent<TMP_InputField>();
            if (_roomField == null)
                _roomLegacy = panel.Find("CreatePage/RoomField")?.GetComponent<InputField>();

            _goCreateButton ??= panel.Find("LandingPage/BtnGoCreate")?.GetComponent<Button>();
            _goJoinButton ??= panel.Find("LandingPage/BtnGoJoin")?.GetComponent<Button>();
            _backFromCreateButton ??= panel.Find("CreatePage/FormRoot/BtnBackRow/BtnBack")?.GetComponent<Button>();
            _backFromJoinButton ??= panel.Find("JoinPage/FormRoot/BtnBackRow/BtnBack")?.GetComponent<Button>();
            _backFromCreateButton ??= panel.Find("CreatePage/BtnBack")?.GetComponent<Button>();
            _backFromJoinButton ??= panel.Find("JoinPage/BtnBack")?.GetComponent<Button>();
            _createButton ??= panel.Find("CreatePage/FormRoot/BtnCreate")?.GetComponent<Button>();
            _createButton ??= panel.Find("CreatePage/BtnCreate")?.GetComponent<Button>();
            _joinSessionButton ??= panel.Find("JoinPage/FormRoot/JoinActionsRow/BtnJoin")?.GetComponent<Button>();
            _joinSessionButton ??= panel.Find("JoinPage/BtnJoin")?.GetComponent<Button>();
            _randomColorButton ??= panel.Find("LandingPage/BtnRandomColor")?.GetComponent<Button>();
            _colorPreview ??= panel.Find("LandingPage/ColorPreview")?.GetComponent<Image>();

            _sessionBrowser ??= GetComponent<SessionBrowserUI>();
        }

        private void Start()
        {
            CleanupDropdownOverlay();
            var nick = GetNicknameText();
            if (string.IsNullOrEmpty(nick))
                SetNicknameText(SessionData.Nickname);
            if (string.IsNullOrEmpty(GetRoomText()))
                SetRoomText("Room1");
            UpdatePreview();
            ShowPage(MainMenuPage.Landing);
            RefreshReconnectButton();
            GetComponent<CareerStatsHud>()?.Refresh();
        }

        private void RefreshReconnectButton()
        {
            var has = SessionReconnectStore.TryLoad(out _, out _, out _);
            if (_reconnectButton != null)
                _reconnectButton.gameObject.SetActive(has);
        }

        private void OnQuickJoinClicked() => _ = StartQuickJoinAsync();

        private void OnReconnectClicked() => _ = StartReconnectAsync();

        private async Task StartQuickJoinAsync()
        {
            if (_sessionFlow != null && _sessionFlow.IsConnecting)
                return;

            ApplySessionFields();
            SetMenuInteractable(false);
            if (ConnectionManager.Instance == null)
            {
                _sessionFlow?.SetStatus("ConnectionManager is missing in this scene.", true);
                SetMenuInteractable(true);
                return;
            }

            var ok = await ConnectionManager.Instance.QuickJoinAsync();
            if (!ok)
                SetMenuInteractable(true);
        }

        private async Task StartReconnectAsync()
        {
            if (!SessionReconnectStore.TryLoad(out var room, out var token, out var mode))
            {
                _sessionFlow?.SetStatus(UiCopy.QuickJoinNoMatch, true);
                return;
            }

            SessionData.SetReconnectToken(token);
            SessionData.SelectedGameMode = mode;
            ApplySessionFields();
            SetMenuInteractable(false);
            if (ConnectionManager.Instance == null)
            {
                SetMenuInteractable(true);
                return;
            }

            var ok = await ConnectionManager.Instance.ReconnectSessionAsync(room);
            if (!ok)
                SetMenuInteractable(true);
        }

        public void ShowPage(MainMenuPage page)
        {
            CleanupDropdownOverlay();

            if (_landingPage != null)
                _landingPage.SetActive(page == MainMenuPage.Landing);
            if (_createPage != null)
                _createPage.SetActive(page == MainMenuPage.Create);
            if (_joinPage != null)
                _joinPage.SetActive(page == MainMenuPage.Join);

            var subtitle = transform.Find("Panel/Subtitle");
            if (subtitle != null)
                subtitle.gameObject.SetActive(page == MainMenuPage.Landing);

            _sessionBrowser?.SetJoinPageActive(page == MainMenuPage.Join);

            if (page == MainMenuPage.Create || page == MainMenuPage.Join)
                _sessionBrowser?.RefreshSelectors();
        }

        private void CleanupDropdownOverlay()
        {
            DropdownOverlayRegistry.CloseAll();

            foreach (var selector in GetComponentsInChildren<StringDropdownSelector>(true))
                selector.CloseList();

            var overlay = transform.Find("Panel/DropdownOverlayLayer");
            if (overlay == null)
                return;

            DropdownOverlayRegistry.CleanupOverlayLayer(overlay);
            if (Application.isPlaying)
                Destroy(overlay.gameObject);
            else
                DestroyImmediate(overlay.gameObject);
        }

        private void OnGoCreateClicked() => ShowPage(MainMenuPage.Create);

        private void OnGoJoinClicked() => ShowPage(MainMenuPage.Join);

        private void OnBackFromCreateClicked() => ShowPage(MainMenuPage.Landing);

        private void OnBackFromJoinClicked() => ShowPage(MainMenuPage.Landing);

        private void OnRandomColor()
        {
            SessionData.Tint = Random.ColorHSV(0f, 1f, 0.55f, 1f, 0.75f, 1f);
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (_colorPreview != null)
                _colorPreview.color = SessionData.Tint;
        }

        private void OnCreateClicked() => _ = StartCreateAsync();

        private void OnJoinSessionClicked() => _sessionBrowser?.RequestJoinSelected();

        private void OnJoinSelectedSession(string sessionName) => _ = StartJoinByNameAsync(sessionName);

        private async Task StartCreateAsync()
        {
            await StartSessionInternalAsync(create: true, roomOverride: null);
        }

        private async Task StartJoinByNameAsync(string roomOverride)
        {
            await StartSessionInternalAsync(create: false, roomOverride: roomOverride);
        }

        private async Task StartSessionInternalAsync(bool create, string roomOverride)
        {
            if (_sessionFlow != null && _sessionFlow.IsConnecting)
                return;

            ApplySessionFields();
            SetMenuInteractable(false);
            _sessionBrowser?.SetJoinInProgress(true);

            var cm = ConnectionManager.Instance;
            if (cm == null)
            {
                cm = FindFirstObjectByType<ConnectionManager>();
                if (cm == null)
                {
                    _sessionFlow?.SetStatus(UiCopy.SessionBrowserNoConnection, true);
                    SetMenuInteractable(true);
                    _sessionBrowser?.SetJoinInProgress(false);
                    return;
                }
            }

            var room = !string.IsNullOrWhiteSpace(roomOverride) ? roomOverride.Trim() : GetRoomText();
            if (string.IsNullOrWhiteSpace(room))
                room = "Room1";

            var ok = create
                ? await cm.CreateSessionAsync(room)
                : await cm.JoinSessionAsync(room);

            if (!ok)
            {
                SetMenuInteractable(true);
                _sessionBrowser?.SetJoinInProgress(false);
            }
        }

        private void SetMenuInteractable(bool interactable)
        {
            if (_goCreateButton != null)
                _goCreateButton.interactable = interactable;
            if (_goJoinButton != null)
                _goJoinButton.interactable = interactable;
            if (_backFromCreateButton != null)
                _backFromCreateButton.interactable = interactable;
            if (_backFromJoinButton != null)
                _backFromJoinButton.interactable = interactable;
            if (_createButton != null)
                _createButton.interactable = interactable;
            if (_joinSessionButton != null)
                _joinSessionButton.interactable = interactable;
            if (_randomColorButton != null)
                _randomColorButton.interactable = interactable;
            if (_quickJoinButton != null)
                _quickJoinButton.interactable = interactable;
            if (_reconnectButton != null)
                _reconnectButton.interactable = interactable;
            if (_nicknameField != null)
                _nicknameField.interactable = interactable;
            if (_roomField != null)
                _roomField.interactable = interactable;
            if (_nicknameLegacy != null)
                _nicknameLegacy.interactable = interactable;
            if (_roomLegacy != null)
                _roomLegacy.interactable = interactable;
        }

        private void ApplySessionFields()
        {
            var nick = GetNicknameText();
            SessionData.Nickname = !string.IsNullOrWhiteSpace(nick) ? nick.Trim() : "Player";
        }

        private string GetNicknameText()
        {
            if (_nicknameField != null)
                return _nicknameField.text;
            return _nicknameLegacy != null ? _nicknameLegacy.text : string.Empty;
        }

        private void SetNicknameText(string value)
        {
            if (_nicknameField != null)
                _nicknameField.text = value;
            else if (_nicknameLegacy != null)
                _nicknameLegacy.text = value;
        }

        private string GetRoomText()
        {
            if (_roomField != null)
                return _roomField.text;
            return _roomLegacy != null ? _roomLegacy.text : string.Empty;
        }

        private void SetRoomText(string value)
        {
            if (_roomField != null)
                _roomField.text = value;
            else if (_roomLegacy != null)
                _roomLegacy.text = value;
        }
    }
}
