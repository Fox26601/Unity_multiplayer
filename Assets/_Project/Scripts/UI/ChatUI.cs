using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fusion;
using FusionMultiplayer.Chat;
using FusionMultiplayer.Core;
using FusionMultiplayer.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>
    /// UI for chat log, PM channel selector, and send.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class ChatUI : MonoBehaviour, IChatPresenter
    {
        [SerializeField] private TMP_InputField _input;
        [SerializeField] private Button _sendButton;
        [SerializeField] private TMP_Text _log;
        [SerializeField] private PmChannelSelector _pmChannel;
        [SerializeField] private TMP_Text _selfNick;
        [SerializeField] private ScrollRect _logScroll;

        private TMP_Text _whisperLabel;
        private Transform _pmRow;
        private bool _soloWelcomeShown;

        private InputField _inputLegacy;
        private Text _logLegacy;
        private bool _sendWired;
        private PlayerRef _selectedPmTarget = PlayerRef.None;
        private string _lastPmChannelSignature = string.Empty;
        private float _nextPmChannelPollTime;
        private bool _chatReadyWatchRunning;
        private bool _chatComposeOpen;
        private GameObject _composeRow;

        public void BindRuntimeTmp(TMP_InputField input, Button sendButton, TMP_Text log,
            PmChannelSelector pmChannel, TMP_Text selfNick = null, ScrollRect logScroll = null,
            TMP_Text whisperLabel = null, Transform pmRow = null)
        {
            if (_pmChannel != null)
                _pmChannel.ChannelChanged -= OnPmChannelChanged;

            _input = input;
            _inputLegacy = null;
            _sendButton = sendButton;
            _log = log;
            _logLegacy = null;
            _pmChannel = pmChannel;
            _selfNick = selfNick;
            _logScroll = logScroll;
            _whisperLabel = whisperLabel;
            _pmRow = pmRow;
            _sendWired = false;
            WireControls(force: true);
            ValidateLogBinding();
            if (_pmChannel != null)
                _pmChannel.ChannelChanged += OnPmChannelChanged;
            ChatManager.AttachPresenter(this);
            RefreshSelfNick();
            RefreshPmChannel(force: true);
            UpdatePmComposeHint();
            EnsureWelcomeLine();
            StartChatReadyWatch();
        }

        public void DisplayMessage(PlayerRef sender, string text, Color senderTint, bool isWhisper,
            PlayerRef recipient)
        {
            var runner = ConnectionManager.Instance != null ? ConnectionManager.Instance.Runner : null;
            if (isWhisper && runner != null)
            {
                var local = runner.LocalPlayer;
                if (local != recipient && local != sender)
                    return;
                if (local == sender)
                    return;
            }

            if (!isWhisper && runner != null && sender == runner.LocalPlayer)
                return;

            var nick = ResolveNick(sender);
            AppendChatLine(nick, text, senderTint, isWhisper, recipient, runner, sender);
        }

        private void Awake()
        {
            UiCanvasFix.EnsureReadableCanvas(transform);
        }

        private void Start()
        {
            GameUiRuntimeRebuild.EnsureBuilt(transform);
            ResolveRefs();
            WireControls();
            ValidateLogBinding();
            ChatManager.AttachPresenter(this);
            EnsureWelcomeLine();
            RefreshSelfNick();
            RefreshPmChannel(force: true);
            UpdatePmComposeHint();
            StartChatReadyWatch();
        }

        private void StartChatReadyWatch()
        {
            if (_chatReadyWatchRunning)
                return;

            _chatReadyWatchRunning = true;
            StartCoroutine(WaitForChatReady());
        }

        private IEnumerator WaitForChatReady()
        {
            try
            {
                yield return NetworkReadinessWatch.WaitUntil(
                    () =>
                    {
                        var cm = ChatManager.Instance;
                        return cm != null && cm.Object != null && cm.Object.IsValid;
                    },
                    (phase, instanceOk, objectValid) =>
                    {
                        var cm = ChatManager.Instance;
                        var hasInstance = cm != null;
                        var valid = cm != null && cm.Object != null && cm.Object.IsValid;
                        ChatDiagnostics.LogChatManagerState(phase, hasInstance, valid);
                    },
                    () => ChatDiagnostics.LogChatManagerState("ready", true, true),
                    () => ChatDiagnostics.LogWarning(
                        "ChatManager not ready after timeout — send may fail until scene objects spawn."),
                    8f,
                    1f);
            }
            finally
            {
                _chatReadyWatchRunning = false;
            }
        }

        private void Update()
        {
            if (!_sendWired)
                WireControls(force: true);

            HandleChatToggleKeys();

            if (Time.unscaledTime < _nextPmChannelPollTime)
                return;

            _nextPmChannelPollTime = Time.unscaledTime + 0.5f;
            RefreshSelfNick();
            RefreshPmChannel();
        }

        private void HandleChatToggleKeys()
        {
            var kb = Keyboard.current;
            if (kb == null)
                return;

            if (kb.escapeKey.wasPressedThisFrame && _chatComposeOpen)
            {
                CloseChat(clearText: false);
                return;
            }

            var enter = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;
            if (!enter)
                return;

            if (!_chatComposeOpen)
            {
                if (CanOpenChat())
                    OpenChat();
                return;
            }

            TrySendAndCloseChat();
        }

        private bool CanOpenChat()
        {
            var runner = ConnectionManager.Instance != null ? ConnectionManager.Instance.Runner : null;
            return runner != null && runner.IsRunning;
        }

        private void OpenChat()
        {
            ResolveComposeRow();
            if (_composeRow != null)
                _composeRow.SetActive(true);

            _chatComposeOpen = true;
            GameplayInputMode.ChatBlockingGameplay = true;
            GameplayInputMode.SetMenu();

            if (_input != null)
            {
                _input.interactable = true;
                _input.Select();
                _input.ActivateInputField();
            }
        }

        private void CloseChat(bool clearText)
        {
            _chatComposeOpen = false;
            GameplayInputMode.ChatBlockingGameplay = false;

            if (clearText)
                ClearInput();

            if (_input != null)
            {
                _input.DeactivateInputField();
                _input.interactable = false;
            }

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);

            ResolveComposeRow();
            if (_composeRow != null)
                _composeRow.SetActive(false);

            if (HasLocalAvatar() && !IsGameOverActive())
                GameplayInputMode.SetGameplay();
            else
                GameplayInputMode.SetMenu();
        }

        private void TrySendAndCloseChat()
        {
            var msg = GetInputText();
            if (!string.IsNullOrWhiteSpace(msg))
                OnSendClicked();

            CloseChat(clearText: true);
        }

        private void ResolveComposeRow()
        {
            if (_composeRow != null)
                return;

            var chatPanel = transform.Find("ChatPanel");
            _composeRow = chatPanel != null ? chatPanel.Find("ChatComposeRow")?.gameObject : null;
        }

        private static bool HasLocalAvatar()
        {
            foreach (var avatar in PlayerRegistry.EnumerateAllAvatars())
            {
                if (avatar.Object != null && avatar.Object.IsValid && avatar.HasInputAuthority)
                    return true;
            }

            return false;
        }

        private static bool IsGameOverActive()
        {
            if (!GameSceneReadiness.TryGetGameManager(out var gm))
                return false;

            return gm.GetIsGameOverSafe();
        }

        private void ResolveRefs()
        {
            var chatPanel = transform.Find("ChatPanel");
            if (chatPanel == null) return;

            if (_input == null)
                _input = chatPanel.Find("ChatComposeRow/ChatInput")?.GetComponent<TMP_InputField>()
                    ?? chatPanel.Find("ChatInput")?.GetComponent<TMP_InputField>();
            if (_input == null)
                _inputLegacy = chatPanel.Find("ChatInput")?.GetComponent<InputField>();

            if (_log == null)
                _log = chatPanel.Find("ChatLogScroll/Viewport/Content/ChatLog")?.GetComponent<TMP_Text>()
                    ?? chatPanel.Find("ChatLogScroll/Viewport/ChatLog")?.GetComponent<TMP_Text>()
                    ?? chatPanel.Find("ChatLog")?.GetComponent<TMP_Text>();
            if (_log == null)
                _logLegacy = chatPanel.Find("ChatLog")?.GetComponent<Text>();

            if (_pmChannel == null)
                _pmChannel = chatPanel.Find("ChatPmRow/PmChannelSelector")?.GetComponent<PmChannelSelector>();

            if (_sendButton == null)
                _sendButton = chatPanel.Find("ChatComposeRow/ChatSend")?.GetComponent<Button>()
                    ?? chatPanel.Find("ChatSend")?.GetComponent<Button>();

            if (_selfNick == null)
                _selfNick = chatPanel.Find("ChatHeaderBar/ChatSelfNick")?.GetComponent<TMP_Text>()
                    ?? chatPanel.Find("ChatSelfNick")?.GetComponent<TMP_Text>();

            if (_logScroll == null)
                _logScroll = chatPanel.Find("ChatLogScroll")?.GetComponent<ScrollRect>();

            if (_whisperLabel == null)
                _whisperLabel = chatPanel.Find("ChatPmRow/WhisperLabel")?.GetComponent<TMP_Text>()
                    ?? chatPanel.Find("WhisperLabel")?.GetComponent<TMP_Text>();

            if (_pmRow == null)
                _pmRow = chatPanel.Find("ChatPmRow");
        }

        private void ValidateLogBinding()
        {
            if (_log != null || _logLegacy != null) return;

            ResolveRefs();
            if (_log != null || _logLegacy != null) return;

            ChatDiagnostics.LogWarning("ChatLog TMP_Text not bound — messages will not appear.");
        }

        private void WireControls(bool force = false)
        {
            if (!force && _sendWired)
                return;

            ResolveRefs();

            if (_sendButton != null)
            {
                _sendButton.onClick.RemoveListener(TrySendAndCloseChat);
                _sendButton.onClick.AddListener(TrySendAndCloseChat);
            }

            if (_pmChannel != null)
            {
                _pmChannel.ChannelChanged -= OnPmChannelChanged;
                _pmChannel.ChannelChanged += OnPmChannelChanged;
            }

            if (_input != null)
            {
                _input.onSubmit.RemoveAllListeners();
                _input.interactable = false;
            }

            ResolveComposeRow();
            if (_composeRow != null)
                _composeRow.SetActive(false);

            _chatComposeOpen = false;

            _sendWired = _sendButton != null && (_input != null || _inputLegacy != null);
            if (!_sendWired)
                ChatDiagnostics.LogWarning("send button or input is not bound yet.");
        }

        private void OnPmChannelChanged(PlayerRef target, string nick, int index)
        {
            _selectedPmTarget = target;
            UpdatePmComposeHint();
            UpdateWhisperLabel();
            ChatDiagnostics.LogPmSelection(index, nick, target);
        }

        private void EnsureWelcomeLine()
        {
            if (_log == null)
            {
                ResolveRefs();
                if (_log == null) return;
            }

            if (!string.IsNullOrWhiteSpace(_log.text))
                return;

            _log.text = UiCopy.ChatSoloHint;
            _soloWelcomeShown = true;
            RebuildLogLayout();
            ScrollLogToBottom();
            ChatDiagnostics.LogWelcome(_log.text.Length);
        }

        private void OnEnable()
        {
            ChatManager.AttachPresenter(this);
            ConnectionManager.NetworkPlayersChanged += OnNetworkPlayersChanged;
            _nextPmChannelPollTime = 0f;
            RefreshSelfNick();
            RefreshPmChannel(force: true);
            StartChatReadyWatch();
        }

        private void OnDisable()
        {
            ChatManager.DetachPresenter(this);
            ConnectionManager.NetworkPlayersChanged -= OnNetworkPlayersChanged;
            if (_pmChannel != null)
                _pmChannel.ChannelChanged -= OnPmChannelChanged;

            if (_chatComposeOpen || GameplayInputMode.ChatBlockingGameplay)
            {
                _chatComposeOpen = false;
                GameplayInputMode.ChatBlockingGameplay = false;
            }
        }

        private void OnNetworkPlayersChanged()
        {
            RefreshPmChannel(force: true);
        }

        private void OnSendClicked()
        {
            var msg = GetInputText();
            if (string.IsNullOrWhiteSpace(msg))
                return;

            if (msg.Length > ChatMessage.MaxLength)
            {
                AppendLog($"[System] Message trimmed to {ChatMessage.MaxLength} characters.\n");
                msg = msg.Substring(0, ChatMessage.MaxLength);
            }

            var runner = ConnectionManager.Instance != null ? ConnectionManager.Instance.Runner : null;
            if (runner == null || !runner.IsRunning)
            {
                AppendLog("[System] Not connected to a session.\n");
                return;
            }

            if (!TryResolveWhisperTarget(out var target, out var whisper))
                whisper = false;

            var cm = ChatManager.Instance;
            var objectValid = cm != null && cm.Object != null && cm.Object.IsValid;
            if (cm == null)
            {
                AppendLog("[System] Chat is loading — try again in a moment.\n");
                ChatDiagnostics.LogSend(msg, whisper, target, false, false, false);
                return;
            }

            var sent = cm.RequestSend(msg, whisper, target);
            if (!sent)
            {
                AppendLog("[System] Chat not ready — wait for the game scene to finish loading.\n");
                ChatDiagnostics.LogSend(msg, whisper, target, true, objectValid, false);
                return;
            }

            if (!whisper)
                AppendOptimisticGlobalLine(runner, msg);
            else
                AppendOptimisticWhisperLine(runner, target, msg);

            ClearInput();
        }

        private void AppendOptimisticWhisperLine(NetworkRunner runner, PlayerRef target, string msg)
        {
            var nick = ResolveLocalNick();
            var tint = ResolveLocalTint();
            AppendChatLine(nick, msg, tint, true, target, runner, runner.LocalPlayer);
        }

        private void AppendOptimisticGlobalLine(NetworkRunner runner, string msg)
        {
            var nick = ResolveLocalNick();
            var tint = ResolveLocalTint();
            AppendChatLine(nick, msg, tint, false, PlayerRef.None, runner, runner.LocalPlayer);
        }

        private bool TryResolveWhisperTarget(out PlayerRef target, out bool whisper)
        {
            target = _selectedPmTarget;
            whisper = target != PlayerRef.None;
            return whisper;
        }

        private void UpdatePmComposeHint()
        {
            if (_input != null)
            {
                var placeholder = _input.placeholder as TMP_Text;
                if (placeholder != null)
                {
                    if (_selectedPmTarget != PlayerRef.None)
                    {
                        var nick = ResolveNick(_selectedPmTarget);
                        placeholder.text = string.Format(UiCopy.ChatPmInputPlaceholderFormat, nick);
                    }
                    else
                    {
                        placeholder.text = UiCopy.ChatPmInputPlaceholderGlobal;
                    }
                }
            }

            UpdateWhisperLabel();
        }

        private void UpdateWhisperLabel()
        {
            if (_whisperLabel == null)
                return;

            if (_selectedPmTarget != PlayerRef.None)
            {
                var nick = ResolveNick(_selectedPmTarget);
                _whisperLabel.text = $"PM → {nick}";
            }
            else
            {
                _whisperLabel.text = UiCopy.ChatWhisperToLabel;
            }
        }

        private string GetInputText()
        {
            if (_input != null) return _input.text;
            return _inputLegacy != null ? _inputLegacy.text : string.Empty;
        }

        private void ClearInput()
        {
            if (_input != null) _input.text = string.Empty;
            else if (_inputLegacy != null) _inputLegacy.text = string.Empty;
        }

        private void AppendLog(string line)
        {
            if (_log == null && _logLegacy == null)
            {
                ResolveRefs();
                ValidateLogBinding();
            }

            if (_log != null)
            {
                _log.text += line;
                RebuildLogLayout();
                ChatDiagnostics.LogAppend(
                    _log.text.Length,
                    true,
                    _log.rectTransform.rect.height,
                    _log.preferredHeight);
            }
            else if (_logLegacy != null)
            {
                _logLegacy.text += line;
                ChatDiagnostics.LogAppend(_logLegacy.text.Length, true);
            }
            else
            {
                ChatDiagnostics.LogAppend(0, false);
            }

            ScrollLogToBottom();
        }

        private void RebuildLogLayout()
        {
            if (_log == null) return;

            _log.ForceMeshUpdate();
            var preferredH = Mathf.Max(_log.preferredHeight, 24f);
            _log.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredH);
            if (_logScroll != null && _logScroll.content != null)
                _logScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredH);

            if (_logScroll != null)
            {
                if (_logScroll.content != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_logScroll.content);
                if (_logScroll.viewport != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_logScroll.viewport);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_log.rectTransform);
            Canvas.ForceUpdateCanvases();
        }

        private void ScrollLogToBottom()
        {
            if (_logScroll == null || _logScroll.content == null || _logScroll.viewport == null)
                return;

            Canvas.ForceUpdateCanvases();

            var contentHeight = _logScroll.content.rect.height;
            var viewportHeight = _logScroll.viewport.rect.height;
            _logScroll.verticalNormalizedPosition = contentHeight <= viewportHeight + 1f ? 1f : 0f;
        }

        private void RefreshSelfNick()
        {
            if (_selfNick == null) return;
            var nick = ResolveLocalNick();
            var tint = ResolveLocalTint();
            _selfNick.text = string.Format(UiCopy.ChatSelfNickFormat, nick);
            _selfNick.color = tint;
        }

        private void RefreshPmChannel(bool force = false)
        {
            var runner = ConnectionManager.Instance != null ? ConnectionManager.Instance.Runner : null;
            if (runner == null || !runner.IsRunning)
            {
                SetPmSectionVisible(false);
                _lastPmChannelSignature = string.Empty;
                return;
            }

            SetPmSectionVisible(true);

            var remotes = CollectRemotePlayers(runner);
            var signature = BuildPmChannelSignature(remotes);

            if (_log != null && remotes.Count > 0 && _soloWelcomeShown &&
                _log.text == UiCopy.ChatSoloHint)
            {
                _log.text = UiCopy.ChatWelcome;
                _soloWelcomeShown = false;
                RebuildLogLayout();
                ScrollLogToBottom();
            }

            if (_pmChannel == null)
                return;

            if (_pmChannel.IsListOpen && !force)
                return;

            if (!force && signature == _lastPmChannelSignature)
                return;

            if (_selectedPmTarget != PlayerRef.None &&
                !runner.ActivePlayers.Any(p => p == _selectedPmTarget))
            {
                _selectedPmTarget = PlayerRef.None;
                _pmChannel.SetSelection(PlayerRef.None, UiCopy.ChatPmGlobalOption, notify: false);
                AppendLog(UiCopy.ChatPmTargetLeft);
            }

            _lastPmChannelSignature = signature;

            var previousTarget = _selectedPmTarget;
            _pmChannel.SetOptions(remotes, force: true);

            if (previousTarget != PlayerRef.None && remotes.Any(r => r.player == previousTarget))
            {
                var nick = remotes.First(r => r.player == previousTarget).nick;
                _pmChannel.SetSelection(previousTarget, nick, notify: false);
                _selectedPmTarget = previousTarget;
            }
            else if (previousTarget == PlayerRef.None)
            {
                _pmChannel.SetSelection(PlayerRef.None, UiCopy.ChatPmGlobalOption, notify: false);
                _selectedPmTarget = PlayerRef.None;
            }

            var chatPanel = transform.Find("ChatPanel");
            if (chatPanel != null)
                UiTypography.ReapplyPmChannelStyles(chatPanel);

            var remoteNicks = new List<string>();
            foreach (var (_, nick) in remotes)
                remoteNicks.Add(nick);
            ChatDiagnostics.LogPmRoster(
                runner.ActivePlayers.Count(),
                runner.LocalPlayer.PlayerId,
                remoteNicks,
                _pmChannel.OptionCount);
            ChatDiagnostics.LogPmChannelOptions(_pmChannel.OptionCount, remoteNicks);

            UpdatePmComposeHint();
            RebuildPmRowLayout();
        }

        private void RebuildPmRowLayout()
        {
            if (_pmRow == null) return;
            if (_pmRow is RectTransform pmRt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(pmRt);
        }

        private static List<(PlayerRef player, string nick)> CollectRemotePlayers(NetworkRunner runner)
        {
            var remotes = new List<(PlayerRef player, string nick)>();
            foreach (var player in runner.ActivePlayers)
            {
                if (player == runner.LocalPlayer) continue;
                remotes.Add((player, ResolveNick(player)));
            }

            return remotes;
        }

        private static string BuildPmChannelSignature(List<(PlayerRef player, string nick)> remotes)
        {
            var sb = new StringBuilder();
            sb.Append("count:").Append(remotes.Count).Append('|');
            foreach (var (player, nick) in remotes)
                sb.Append(player.PlayerId).Append(':').Append(nick).Append('|');
            return sb.ToString();
        }

        private void SetPmSectionVisible(bool visible)
        {
            if (_pmRow != null)
                _pmRow.gameObject.SetActive(visible);
        }

        private void AppendChatLine(string nick, string text, Color senderTint, bool isWhisper, PlayerRef recipient,
            NetworkRunner runner, PlayerRef sender)
        {
            string line;
            if (isWhisper)
            {
                var pmColor = ColorUtility.ToHtmlStringRGB(UiTheme.ChatWhisperText);
                var safeText = SanitizeRichText(text);
                var local = runner != null ? runner.LocalPlayer : PlayerRef.None;
                if (local == sender)
                {
                    var toNick = SanitizeRichText(ResolveNick(recipient));
                    line = $"<color=#{pmColor}>[PM → {toNick}]: {safeText}</color>\n";
                }
                else
                {
                    var safeNick = SanitizeRichText(nick);
                    line = $"<color=#{pmColor}>[PM] {safeNick}: {safeText}</color>\n";
                }
            }
            else
            {
                var nickColor = ColorUtility.ToHtmlStringRGB(senderTint);
                line = $"<color=#{nickColor}>{SanitizeRichText(nick)}</color>: {SanitizeRichText(text)}\n";
            }

            AppendLog(line);
        }

        private static string SanitizeRichText(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("<", "\uFF1C");
        }

        private static string ResolveNick(PlayerRef sender)
        {
            foreach (var pd in PlayerRegistry.EnumerateAllData())
            {
                if (pd.Object != null && pd.Object.IsValid && pd.Object.InputAuthority == sender)
                    return pd.Nick.ToString();
            }

            return sender.ToString();
        }

        private static string ResolveLocalNick()
        {
            var runner = ConnectionManager.Instance != null ? ConnectionManager.Instance.Runner : null;
            if (runner != null)
            {
                foreach (var pd in PlayerRegistry.EnumerateAllData())
                {
                    if (pd.Object != null && pd.Object.IsValid && pd.Object.InputAuthority == runner.LocalPlayer)
                        return pd.Nick.ToString();
                }
            }

            return SessionData.Nickname;
        }

        private static Color ResolveLocalTint()
        {
            var runner = ConnectionManager.Instance != null ? ConnectionManager.Instance.Runner : null;
            if (runner != null)
            {
                foreach (var pd in PlayerRegistry.EnumerateAllData())
                {
                    if (pd.Object != null && pd.Object.IsValid && pd.Object.InputAuthority == runner.LocalPlayer)
                        return pd.Tint;
                }
            }

            return SessionData.Tint;
        }
    }
}
