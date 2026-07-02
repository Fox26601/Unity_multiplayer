using System;
using System.Collections.Generic;
using Fusion;
using FusionMultiplayer.Chat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>Button-based PM channel picker (replaces TMP_Dropdown).</summary>
    public sealed class PmChannelSelector : MonoBehaviour
    {
        private const float OptionHeight = 28f;
        private const float ListPadding = 4f;

        private Button _fieldButton;
        private TMP_Text _caption;
        private RectTransform _fieldRt;
        private RectTransform _listRoot;
        private RectTransform _listContent;
        private Transform _overlayParent;
        private Transform _listHomeParent;

        private readonly List<(PlayerRef player, string nick)> _options = new();
        private PlayerRef _selectedTarget = PlayerRef.None;
        private string _selectedNick = UiCopy.ChatPmGlobalOption;
        private bool _listOpen;
        private bool _pendingOptionsRebuild;
        private List<(PlayerRef player, string nick)> _pendingRemotes;

        public PlayerRef SelectedTarget => _selectedTarget;
        public string SelectedNick => _selectedNick;
        public bool IsListOpen => _listOpen;
        public int OptionCount => 1 + _options.Count;

        public event Action<PlayerRef, string, int> ChannelChanged;

        internal void BindRuntime(Button fieldButton, TMP_Text caption, RectTransform listRoot, RectTransform listContent,
            Transform overlayParent)
        {
            _fieldButton = fieldButton;
            _caption = caption;
            _fieldRt = fieldButton != null ? fieldButton.GetComponent<RectTransform>() : null;
            _listRoot = listRoot;
            _listContent = listContent;
            _overlayParent = overlayParent;
            _listHomeParent = listRoot != null ? listRoot.parent : null;

            if (_fieldButton != null)
            {
                _fieldButton.onClick.RemoveListener(ToggleList);
                _fieldButton.onClick.AddListener(ToggleList);
            }

            UpdateCaption();
            CloseList();
        }

        public void SetOptions(IReadOnlyList<(PlayerRef player, string nick)> remotes, bool force = false)
        {
            if (_listOpen && !force)
            {
                _pendingOptionsRebuild = true;
                _pendingRemotes = remotes != null
                    ? new List<(PlayerRef player, string nick)>(remotes)
                    : new List<(PlayerRef player, string nick)>();
                return;
            }

            ApplyOptions(remotes);
        }

        public void SetSelection(PlayerRef target, string nick, bool notify = false)
        {
            _selectedTarget = target;
            _selectedNick = string.IsNullOrEmpty(nick)
                ? target == PlayerRef.None ? UiCopy.ChatPmGlobalOption : target.ToString()
                : nick;
            UpdateCaption();

            if (!notify)
                return;

            var index = IndexOfTarget(target);
            ChannelChanged?.Invoke(_selectedTarget, _selectedNick, index);
        }

        public void CloseList()
        {
            if (_listRoot != null)
                _listRoot.gameObject.SetActive(false);

            RestoreListHome();

            var wasOpen = _listOpen;
            _listOpen = false;

            if (wasOpen)
                ChatDiagnostics.LogPmChannelList(false, OptionCount);

            if (_pendingOptionsRebuild)
            {
                _pendingOptionsRebuild = false;
                ApplyOptions(_pendingRemotes);
                _pendingRemotes = null;
            }
        }

        private void ToggleList()
        {
            if (_listOpen)
                CloseList();
            else
                OpenList();
        }

        private void OpenList()
        {
            if (_listRoot == null || _fieldRt == null || _overlayParent == null)
                return;

            if (_listRoot.parent != _overlayParent)
                _listRoot.SetParent(_overlayParent, false);

            var height = OptionCount * OptionHeight + ListPadding * 2f;
            PositionListAboveField(height);
            _listRoot.SetAsLastSibling();
            _listRoot.gameObject.SetActive(true);
            _listOpen = true;
            ChatDiagnostics.LogPmChannelList(true, OptionCount);
        }

        private void PositionListAboveField(float height)
        {
            var panelRt = _overlayParent as RectTransform;
            if (panelRt == null || _listRoot == null || _fieldRt == null)
                return;

            var canvas = _fieldRt.GetComponentInParent<Canvas>();
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            var corners = new Vector3[4];
            _fieldRt.GetWorldCorners(corners);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                panelRt,
                RectTransformUtility.WorldToScreenPoint(cam, corners[1]),
                cam,
                out var topLeft);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                panelRt,
                RectTransformUtility.WorldToScreenPoint(cam, corners[2]),
                cam,
                out var topRight);

            var width = Mathf.Max(topRight.x - topLeft.x, 160f);
            var centerX = (topLeft.x + topRight.x) * 0.5f;

            _listRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _listRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _listRoot.pivot = new Vector2(0.5f, 0f);
            _listRoot.anchoredPosition = new Vector2(centerX, topLeft.y - 2f);
            _listRoot.sizeDelta = new Vector2(width, height);
        }

        private void RestoreListHome()
        {
            if (_listRoot == null || _listHomeParent == null || _listRoot.parent == _listHomeParent)
                return;

            _listRoot.SetParent(_listHomeParent, false);
            _listRoot.anchorMin = new Vector2(0f, 1f);
            _listRoot.anchorMax = new Vector2(1f, 1f);
            _listRoot.pivot = new Vector2(0.5f, 0f);
            _listRoot.anchoredPosition = new Vector2(0f, -2f);
        }

        private void ApplyOptions(IReadOnlyList<(PlayerRef player, string nick)> remotes)
        {
            _options.Clear();
            if (remotes != null)
            {
                foreach (var entry in remotes)
                    _options.Add(entry);
            }

            RebuildOptionButtons();

            if (_selectedTarget != PlayerRef.None && IndexOfTarget(_selectedTarget) < 0)
                SetSelection(PlayerRef.None, UiCopy.ChatPmGlobalOption, notify: false);
            else
                UpdateCaption();
        }

        private void RebuildOptionButtons()
        {
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

            CreateOptionButton(PlayerRef.None, UiCopy.ChatPmGlobalOption, 0);

            for (var i = 0; i < _options.Count; i++)
            {
                var (player, nick) = _options[i];
                CreateOptionButton(player, nick, i + 1);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_listContent);
        }

        private void CreateOptionButton(PlayerRef target, string nick, int index)
        {
            var name = target == PlayerRef.None ? "PmOption_Global" : $"PmOption_{target.PlayerId}";
            var button = UiRuntimeBuildKit.CreatePmChannelOptionButton(_listContent, name, nick);
            var capturedTarget = target;
            var capturedNick = nick;
            var capturedIndex = index;
            button.onClick.AddListener(() => OnOptionClicked(capturedTarget, capturedNick, capturedIndex));
        }

        private void OnOptionClicked(PlayerRef target, string nick, int index)
        {
            SetSelection(target, nick, notify: true);
            CloseList();
        }

        private int IndexOfTarget(PlayerRef target)
        {
            if (target == PlayerRef.None)
                return 0;

            for (var i = 0; i < _options.Count; i++)
            {
                if (_options[i].player == target)
                    return i + 1;
            }

            return -1;
        }

        private void UpdateCaption()
        {
            if (_caption == null)
                return;

            _caption.text = _selectedNick;
            UiTypography.ApplyChatDropdownLabel(_caption);
            _caption.ForceMeshUpdate();
        }
    }
}
