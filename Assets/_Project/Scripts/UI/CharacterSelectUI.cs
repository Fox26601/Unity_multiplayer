using System.Collections;
using Fusion;
using FusionMultiplayer.Core;
using FusionMultiplayer.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>
    /// Character selection with master-approved slots and live occupancy UI.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class CharacterSelectUI : MonoBehaviour
    {
        [SerializeField] private Button[] _characterButtons = new Button[10];
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private GameObject _panelRoot;

        private Text _statusLegacy;
        private bool _hasSpawnedAvatar;
        private int _pendingSlot = -1;
        private bool _retryPendingWhenGmReady;
        private TMP_Text _spawnBanner;
        private Coroutine _spawnBannerRoutine;
        private float _nextSlotPollTime;
        private bool _gameManagerWatchRunning;
        private bool _gameManagerTimedOut;

        /// <summary>Called by GameUiRuntimeRebuild after it creates TMP widgets.</summary>
        public void BindRuntimeTmp(Button[] characterButtons, TMP_Text statusText, GameObject panelRoot)
        {
            _characterButtons = characterButtons;
            _statusText = statusText;
            _panelRoot = panelRoot;
            _statusLegacy = null;
            _hasSpawnedAvatar = false;
            _pendingSlot = -1;
            _retryPendingWhenGmReady = false;
            _gameManagerTimedOut = false;
            RefreshAllSlots();
            StartGameManagerWatch();
        }

        private void Awake()
        {
            if (GetComponent<UiReadabilityBootstrap>() == null)
                gameObject.AddComponent<UiReadabilityBootstrap>();

            GameUiRuntimeRebuild.EnsureBuilt(transform);
            ResolveRefs();
        }

        private void ResolveRefs()
        {
            var panel = _panelRoot != null ? _panelRoot.transform : transform.Find("CharacterSelectPanel");
            if (panel == null) return;

            if (_statusText == null)
                _statusText = panel.Find("CharStatus")?.GetComponent<TMP_Text>();
            if (_statusText == null)
                _statusLegacy = panel.Find("CharStatus")?.GetComponent<Text>();

            if (_characterButtons == null || _characterButtons.Length == 0 || _characterButtons[0] == null)
            {
                _characterButtons = new Button[10];
                for (var i = 0; i < 10; i++)
                    _characterButtons[i] = panel.Find($"Char_{i}")?.GetComponent<Button>();
            }
        }

        private void OnEnable()
        {
            CharacterSelectBridge.Approved += OnApproved;
            CharacterSelectBridge.Rejected += OnRejected;
            CharacterSelectBridge.SlotsChanged += RefreshAllSlots;
            GameSceneReadiness.GameManagerReady += OnGameManagerReady;
            ConnectionManager.SceneLoaded += OnSceneLoaded;
            GameSceneReadiness.EnsureSubscribed();
            GameplayInputMode.SetMenu();
            StartGameManagerWatch();
        }

        private void OnDisable()
        {
            CharacterSelectBridge.Approved -= OnApproved;
            CharacterSelectBridge.Rejected -= OnRejected;
            CharacterSelectBridge.SlotsChanged -= RefreshAllSlots;
            GameSceneReadiness.GameManagerReady -= OnGameManagerReady;
            ConnectionManager.SceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            GameUiRuntimeRebuild.EnsureBuilt(transform);
            ResolveRefs();
            if (string.IsNullOrWhiteSpace(GetStatusText()))
                SetStatusText(UiCopy.CharacterSelectPrompt);
            RefreshAllSlots();
            StartGameManagerWatch();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextSlotPollTime)
                return;

            _nextSlotPollTime = Time.unscaledTime + 0.25f;
            RefreshAllSlots();
        }

        private void OnSceneLoaded(string sceneName)
        {
            if (!sceneName.Contains("02_Game"))
                return;

            _gameManagerTimedOut = false;
            StartGameManagerWatch();
        }

        private void OnGameManagerReady(GameManager gm)
        {
            _gameManagerTimedOut = false;
            RefreshAllSlots();
        }

        private void StartGameManagerWatch()
        {
            if (_gameManagerWatchRunning || _hasSpawnedAvatar)
                return;

            if (GameSceneReadiness.IsGameManagerReady)
                return;

            _gameManagerWatchRunning = true;
            StartCoroutine(WaitForGameManagerReady());
        }

        private IEnumerator WaitForGameManagerReady()
        {
            try
            {
                var ready = false;
                yield return GameSceneReadiness.WaitForGameManager(success => ready = success, 8f, 1f);

                if (!ready && !_hasSpawnedAvatar)
                {
                    _gameManagerTimedOut = true;
                    SetStatusText(UiCopy.CharacterSelectGameManagerTimeout);
                    RefreshAllSlots();
                }
            }
            finally
            {
                _gameManagerWatchRunning = false;
            }
        }

        private void RefreshAllSlots()
        {
            GameSceneReadiness.TryGetGameManager(out var gm);
            var runner = ConnectionManager.Instance != null ? ConnectionManager.Instance.Runner : null;
            var gmReady = gm != null;
            if (_characterButtons == null) return;

            if (runner != null && runner.IsRunning && !gmReady && !_hasSpawnedAvatar && _pendingSlot < 0 &&
                !_gameManagerTimedOut)
            {
                var status = GetStatusText();
                if (string.IsNullOrWhiteSpace(status) || status == UiCopy.CharacterSelectPrompt)
                    SetStatusText(UiCopy.CharacterSelectWaitingForGame);
            }

            var localPlayer = runner != null && runner.IsRunning ? runner.LocalPlayer : PlayerRef.None;

            for (var i = 0; i < _characterButtons.Length; i++)
            {
                var btn = _characterButtons[i];
                if (btn == null) continue;

                var owner = gmReady ? gm.GetCharacterOwner(i) : PlayerRef.None;
                var isLocal = owner != PlayerRef.None && owner == localPlayer;
                var taken = owner != PlayerRef.None;
                var pending = _pendingSlot == i && !_hasSpawnedAvatar;

                btn.interactable = gmReady && !_hasSpawnedAvatar && !taken && !_gameManagerTimedOut;

                var slot = btn.GetComponent<CharacterSlotButton>();
                slot?.RefreshVisual(owner, ResolveOwnerNickname(owner), ResolveOwnerTint(owner),
                    _hasSpawnedAvatar, isLocal);

                if (pending)
                {
                    var img = btn.targetGraphic as Image;
                    if (img != null)
                        img.color = new Color(0.28f, 0.38f, 0.52f, 1f);
                }
            }

            if (_retryPendingWhenGmReady && gmReady && _pendingSlot >= 0 && runner != null && runner.IsRunning &&
                !_hasSpawnedAvatar)
            {
                _retryPendingWhenGmReady = false;
                var slot = _pendingSlot;
                if (gm.GetCharacterOwner(slot) != PlayerRef.None)
                {
                    _pendingSlot = -1;
                    SetStatusText(UiCopy.CharacterSlotTaken(slot));
                }
                else
                {
                    SetStatusText(UiCopy.CharacterSelectRequesting(slot));
                    gm.RequestCharacter(slot, runner.LocalPlayer);
                }
            }
        }

        public void OnPickCharacter(int index)
        {
            if (_hasSpawnedAvatar || _gameManagerTimedOut) return;

            _pendingSlot = index;
            SetStatusText(UiCopy.CharacterSelectRequesting(index));
            RefreshAllSlots();

            var runner = ConnectionManager.Instance != null ? ConnectionManager.Instance.Runner : null;
            if (runner == null || !runner.IsRunning)
            {
                _pendingSlot = -1;
                SetStatusText(UiCopy.CharacterSelectNotConnected);
                RefreshAllSlots();
                return;
            }

            if (!GameSceneReadiness.TryGetGameManager(out var gm))
            {
                _retryPendingWhenGmReady = true;
                SetStatusText(UiCopy.CharacterSelectWaitingForGame);
                StartGameManagerWatch();
                return;
            }

            if (gm.GetCharacterOwner(index) != PlayerRef.None)
            {
                _pendingSlot = -1;
                SetStatusText(UiCopy.CharacterSlotTaken(index));
                RefreshAllSlots();
                return;
            }

            gm.RequestCharacter(index, runner.LocalPlayer);
        }

        private void OnApproved(int index, Vector3 position, Quaternion rotation)
        {
            _pendingSlot = -1;
            var runner = ConnectionManager.Instance != null ? ConnectionManager.Instance.Runner : null;
            if (!GameSceneReadiness.TryGetGameManager(out var gm) || runner == null || gm.PlayerAvatarPrefab == null)
                return;

            var avatar = runner.Spawn(gm.PlayerAvatarPrefab, position, rotation, runner.LocalPlayer);
            if (avatar != null)
            {
                var pa = avatar.GetComponent<PlayerAvatar>();
                if (pa != null) pa.CharacterSlot = index;
            }

            var pd = FindLocalPlayerData();
            if (pd != null && pd.Object != null && pd.Object.HasStateAuthority)
                pd.CharacterIndex = index;

            _hasSpawnedAvatar = true;
            if (_panelRoot != null) _panelRoot.SetActive(false);
            SetStatusText(UiCopy.CharacterSpawned);
            ShowSpawnBanner(index);
        }

        private void OnRejected(int index)
        {
            _pendingSlot = -1;
            SetStatusText(UiCopy.CharacterSlotTaken(index));
            RefreshAllSlots();
        }

        private void ShowSpawnBanner(int slotIndex)
        {
            if (_spawnBannerRoutine != null)
                StopCoroutine(_spawnBannerRoutine);

            EnsureSpawnBanner();
            if (_spawnBanner == null) return;

            _spawnBanner.text = UiCopy.CharacterSpawnedBanner(slotIndex);
            _spawnBanner.gameObject.SetActive(true);
            _spawnBannerRoutine = StartCoroutine(HideSpawnBannerAfter(3f));
        }

        private IEnumerator HideSpawnBannerAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (_spawnBanner != null)
                _spawnBanner.gameObject.SetActive(false);
            _spawnBannerRoutine = null;
        }

        private void EnsureSpawnBanner()
        {
            if (_spawnBanner != null) return;

            var existing = transform.Find("SpawnHintBanner");
            if (existing != null)
            {
                _spawnBanner = existing.GetComponent<TMP_Text>();
                return;
            }

            var go = new GameObject("SpawnHintBanner", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            UiRegionLayout.StretchRegion(rt, 0.72f, 0.58f, 1f, 0.62f, 8f, 0f, 12f, 0f);

            _spawnBanner = go.GetComponent<TMP_Text>();
            _spawnBanner.alignment = TextAlignmentOptions.MidlineRight;
            _spawnBanner.fontSize = UiTypography.Caption;
            _spawnBanner.color = UiTheme.TextPrimary;
            _spawnBanner.textWrappingMode = TextWrappingModes.NoWrap;
            UiTypography.ApplyFontTo(_spawnBanner);
            go.SetActive(false);
        }

        private string GetStatusText()
        {
            if (_statusText != null) return _statusText.text;
            return _statusLegacy != null ? _statusLegacy.text : string.Empty;
        }

        private void SetStatusText(string value)
        {
            if (_statusText != null) _statusText.text = value;
            else if (_statusLegacy != null) _statusLegacy.text = value;
        }

        private static string ResolveOwnerNickname(PlayerRef owner)
        {
            if (owner == PlayerRef.None) return string.Empty;
            foreach (var pd in UnityEngine.Object.FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
            {
                if (pd.Object != null && pd.Object.IsValid && pd.Object.InputAuthority == owner)
                    return pd.Nick.ToString();
            }

            return string.Empty;
        }

        private static Color ResolveOwnerTint(PlayerRef owner)
        {
            if (owner == PlayerRef.None) return Color.white;
            foreach (var pd in UnityEngine.Object.FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
            {
                if (pd.Object != null && pd.Object.IsValid && pd.Object.InputAuthority == owner)
                    return pd.Tint;
            }

            return new Color(0.45f, 0.55f, 0.75f, 1f);
        }

        private static PlayerData FindLocalPlayerData()
        {
            var runner = ConnectionManager.Instance != null ? ConnectionManager.Instance.Runner : null;
            if (runner == null) return null;
            foreach (var pd in UnityEngine.Object.FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
            {
                if (pd.Object != null && pd.Object.IsValid && pd.Object.InputAuthority == runner.LocalPlayer)
                    return pd;
            }

            return null;
        }
    }
}
