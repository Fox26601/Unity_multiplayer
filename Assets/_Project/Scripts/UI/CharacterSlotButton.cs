using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>Character slot button with number + owner nickname labels.</summary>
    public class CharacterSlotButton : MonoBehaviour
    {
        [SerializeField] private int _slotIndex;
        [SerializeField] private CharacterSelectUI _ui;
        [SerializeField] private TMP_Text _numberLabel;
        [SerializeField] private TMP_Text _ownerLabel;
        [SerializeField] private Image _background;

        private PlayerRef _lastOwner = PlayerRef.None;
        private string _lastNick;
        private Color _lastTint;
        private bool _lastLocalLocked;
        private bool _lastIsLocal;
        private bool _lastIsBot;
        private bool _hasVisualCache;

        public int SlotIndex => _slotIndex;

        public void Configure(CharacterSelectUI ui, int slotIndex)
        {
            _ui = ui;
            _slotIndex = slotIndex;
            _hasVisualCache = false;
        }

        public void BindLabels(TMP_Text numberLabel, TMP_Text ownerLabel, Image background)
        {
            _numberLabel = numberLabel;
            _ownerLabel = ownerLabel;
            _background = background;
            _hasVisualCache = false;
        }

        public void RefreshVisual(PlayerRef owner, string ownerNick, Color ownerTint, bool localLocked,
            bool isLocalPlayer, bool isBot = false)
        {
            var occupied = owner != PlayerRef.None || isBot;
            if (_hasVisualCache &&
                owner == _lastOwner &&
                ownerNick == _lastNick &&
                ownerTint == _lastTint &&
                localLocked == _lastLocalLocked &&
                isLocalPlayer == _lastIsLocal &&
                isBot == _lastIsBot)
                return;

            _hasVisualCache = true;
            _lastOwner = owner;
            _lastNick = ownerNick;
            _lastTint = ownerTint;
            _lastLocalLocked = localLocked;
            _lastIsLocal = isLocalPlayer;
            _lastIsBot = isBot;

            if (_numberLabel != null)
            {
                _numberLabel.text = _slotIndex.ToString();
                _numberLabel.color = occupied ? UiTheme.TextSubtitle : UiTheme.TitleAccent;
            }

            if (_ownerLabel != null)
            {
                if (!occupied)
                    _ownerLabel.text = UiCopy.CharacterSlotFree;
                else if (isLocalPlayer)
                    _ownerLabel.text = "YOU";
                else if (isBot)
                    _ownerLabel.text = string.IsNullOrWhiteSpace(ownerNick)
                        ? UiCopy.CharacterSlotBot
                        : ownerNick;
                else
                    _ownerLabel.text = string.IsNullOrWhiteSpace(ownerNick) ? "TAKEN" : ownerNick;
            }

            if (_background != null)
            {
                if (!occupied)
                {
                    _background.color = UiTheme.ButtonBackground;
                }
                else
                {
                    var baseColor = new Color(0.32f, 0.32f, 0.36f, 1f);
                    var tinted = Color.Lerp(baseColor, ownerTint, 0.5f);
                    if (isLocalPlayer)
                        tinted = Color.Lerp(tinted, ownerTint, 0.35f);
                    _background.color = tinted;
                }
            }
        }

        public void OnClick()
        {
            if (_ui != null)
                _ui.OnPickCharacter(_slotIndex);
        }
    }
}
