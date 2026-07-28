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

        public int SlotIndex => _slotIndex;

        public void Configure(CharacterSelectUI ui, int slotIndex)
        {
            _ui = ui;
            _slotIndex = slotIndex;
        }

        public void BindLabels(TMP_Text numberLabel, TMP_Text ownerLabel, Image background)
        {
            _numberLabel = numberLabel;
            _ownerLabel = ownerLabel;
            _background = background;
        }

        public void RefreshVisual(PlayerRef owner, string ownerNick, Color ownerTint, bool localLocked,
            bool isLocalPlayer)
        {
            if (_numberLabel != null)
            {
                _numberLabel.text = _slotIndex.ToString();
                _numberLabel.color = owner == PlayerRef.None ? UiTheme.TitleAccent : UiTheme.TextSubtitle;
            }

            if (_ownerLabel != null)
            {
                if (owner == PlayerRef.None)
                    _ownerLabel.text = UiCopy.CharacterSlotFree;
                else if (isLocalPlayer)
                    _ownerLabel.text = "YOU";
                else
                    _ownerLabel.text = string.IsNullOrWhiteSpace(ownerNick) ? "TAKEN" : ownerNick;
            }

            if (_background != null)
            {
                if (owner == PlayerRef.None)
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
