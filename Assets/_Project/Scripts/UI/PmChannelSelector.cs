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
        private readonly OverlayDropdownList _dropdown = new();

        private Button _fieldButton;
        private TMP_Text _caption;
        private RectTransform _listContent;

        private readonly List<(PlayerRef player, string nick)> _options = new();
        private PlayerRef _selectedTarget = PlayerRef.None;
        private string _selectedNick = UiCopy.ChatPmGlobalOption;
        private bool _pendingOptionsRebuild;
        private List<(PlayerRef player, string nick)> _pendingRemotes;

        public PlayerRef SelectedTarget => _selectedTarget;
        public string SelectedNick => _selectedNick;
        public bool IsListOpen => _dropdown.IsOpen;
        public int OptionCount => 1 + _options.Count;

        public event Action<PlayerRef, string, int> ChannelChanged;

        internal void BindRuntime(Button fieldButton, TMP_Text caption, RectTransform listRoot, RectTransform listContent,
            Transform overlayParent)
        {
            _fieldButton = fieldButton;
            _caption = caption;
            _listContent = listContent;
            _dropdown.Bind(fieldButton, listRoot, listContent, overlayParent);
            _dropdown.WireToggle(ToggleList);

            UpdateCaption();
            CloseList();
        }

        public void SetOptions(IReadOnlyList<(PlayerRef player, string nick)> remotes, bool force = false)
        {
            if (_dropdown.IsOpen && !force)
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
            var wasOpen = _dropdown.IsOpen;
            _dropdown.Close();

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
            if (_dropdown.IsOpen)
                CloseList();
            else
                OpenList();
        }

        private void OpenList()
        {
            if (_fieldButton == null || OptionCount <= 0)
                return;

            RebuildOptionButtons();
            _dropdown.Open(OptionCount, CloseList);
            ChatDiagnostics.LogPmChannelList(true, OptionCount);
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
            _dropdown.ClearContent();
            if (_listContent == null)
                return;

            CreateOptionButton(PlayerRef.None, UiCopy.ChatPmGlobalOption, 0);

            for (var i = 0; i < _options.Count; i++)
            {
                var (player, nick) = _options[i];
                CreateOptionButton(player, nick, i + 1);
            }

            _dropdown.ForceRebuildLayout();
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
