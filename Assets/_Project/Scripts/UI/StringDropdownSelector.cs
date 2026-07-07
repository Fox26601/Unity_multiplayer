using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>String-option dropdown for main menu filters (same pattern as PmChannelSelector).</summary>
    public sealed class StringDropdownSelector : MonoBehaviour
    {
        private readonly OverlayDropdownList _dropdown = new();
        private readonly List<string> _options = new();

        private TMP_Text _caption;
        private int _selectedIndex;
        private int _openedOnFrame = -1;
        private bool _pendingOptionsRebuild;
        private List<string> _pendingLabels;
        private int _pendingSelectedIndex;

        public int SelectedIndex => _selectedIndex;
        public string SelectedLabel =>
            _options.Count == 0 ? string.Empty : _options[Mathf.Clamp(_selectedIndex, 0, _options.Count - 1)];
        public bool IsListOpen => _dropdown.IsOpen;
        public int OptionCount => _options.Count;

        public event Action<int, string> SelectionChanged;

        internal void BindRuntime(Button fieldButton, TMP_Text caption, RectTransform listRoot,
            RectTransform listContent, Transform overlayParent, bool useInlinePopup = false)
        {
            _caption = caption;
            _dropdown.Bind(fieldButton, listRoot, listContent, overlayParent, useInlinePopup);
            _dropdown.WireToggle(ToggleList);
            UpdateCaption();
            CloseList();
        }

        public void SetOptions(IReadOnlyList<string> labels, int selectedIndex, bool notify = false)
        {
            if (_dropdown.IsOpen && !notify)
            {
                _pendingOptionsRebuild = true;
                _pendingLabels = labels != null ? new List<string>(labels) : new List<string>();
                _pendingSelectedIndex = selectedIndex;
                return;
            }

            ApplyOptions(labels, selectedIndex, notify);
        }

        public void CloseList()
        {
            if (_openedOnFrame == Time.frameCount)
                return;

            _dropdown.Close();

            if (_pendingOptionsRebuild)
            {
                _pendingOptionsRebuild = false;
                ApplyOptions(_pendingLabels, _pendingSelectedIndex, false);
                _pendingLabels = null;
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
            if (OptionCount <= 0)
                return;

            RebuildOptionButtons();
            _openedOnFrame = Time.frameCount;
            _dropdown.Open(OptionCount, CloseList);
        }

        private void ApplyOptions(IReadOnlyList<string> labels, int selectedIndex, bool notify)
        {
            _options.Clear();
            if (labels != null)
                _options.AddRange(labels);

            _selectedIndex = _options.Count == 0 ? 0 : Mathf.Clamp(selectedIndex, 0, _options.Count - 1);
            RebuildOptionButtons();
            UpdateCaption();

            if (notify)
                SelectionChanged?.Invoke(_selectedIndex, SelectedLabel);
        }

        private void RebuildOptionButtons()
        {
            _dropdown.ClearContent();
            if (_dropdown.ListContent == null)
                return;

            for (var i = 0; i < _options.Count; i++)
            {
                var index = i;
                var button = UiRuntimeBuildKit.CreatePmChannelOptionButton(_dropdown.ListContent, $"Opt_{i}",
                    _options[i]);
                button.onClick.AddListener(() => OnOptionClicked(index));
            }

            _dropdown.ForceRebuildLayout();
        }

        private void OnOptionClicked(int index)
        {
            _selectedIndex = index;
            UpdateCaption();
            CloseList();
            SelectionChanged?.Invoke(_selectedIndex, SelectedLabel);
        }

        private void UpdateCaption()
        {
            if (_caption == null)
                return;

            _caption.text = string.IsNullOrEmpty(SelectedLabel) ? "—" : SelectedLabel;
            UiTypography.ApplyInputText(_caption, false);
            _caption.fontSize = 26;
            _caption.color = UiTheme.TextPrimary;
            _caption.textWrappingMode = TextWrappingModes.NoWrap;
            _caption.overflowMode = TextOverflowModes.Ellipsis;
            _caption.ForceMeshUpdate();
        }
    }
}
