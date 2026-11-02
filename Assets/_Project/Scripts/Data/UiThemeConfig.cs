using UnityEngine;

namespace FusionMultiplayer.Data
{
    [CreateAssetMenu(fileName = "UiThemeConfig", menuName = "Fusion Multiplayer/UI Theme Config")]
    public sealed class UiThemeConfig : ScriptableObject
    {
        [SerializeField] private Color _panelBackground = new(0.1f, 0.1f, 0.12f, 0.94f);
        [SerializeField] private Color _buttonBackground = new(0.22f, 0.28f, 0.38f, 1f);
        [SerializeField] private Color _buttonHighlighted = new(0.28f, 0.35f, 0.48f, 1f);
        [SerializeField] private Color _buttonPressed = new(0.18f, 0.22f, 0.32f, 1f);
        [SerializeField] private Color _inputBackground = new(0.15f, 0.15f, 0.18f, 1f);
        [SerializeField] private Color _textPrimary = Color.white;
        [SerializeField] private Color _textSubtitle = new(0.85f, 0.88f, 0.92f, 1f);
        [SerializeField] private Color _titleAccent = new(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private Color _statusOk = new(0.55f, 1f, 0.65f, 1f);
        [SerializeField] private Color _statusError = new(1f, 0.45f, 0.45f, 1f);
        [SerializeField] private Color _placeholder = new(0.78f, 0.82f, 0.88f, 0.92f);
        [SerializeField] private Color _botBadge = new(1f, 0.55f, 0.2f, 1f);
        [SerializeField] private Color _crosshair = new(1f, 1f, 1f, 0.9f);
        [SerializeField] private Color _dimOverlay = new(0f, 0f, 0f, 0.72f);
        [SerializeField] private Color _chatWhisperText = new(0.45f, 0.78f, 1f, 1f);

        public Color PanelBackground => _panelBackground;
        public Color ButtonBackground => _buttonBackground;
        public Color ButtonHighlighted => _buttonHighlighted;
        public Color ButtonPressed => _buttonPressed;
        public Color InputBackground => _inputBackground;
        public Color TextPrimary => _textPrimary;
        public Color TextSubtitle => _textSubtitle;
        public Color TitleAccent => _titleAccent;
        public Color StatusOk => _statusOk;
        public Color StatusError => _statusError;
        public Color Placeholder => _placeholder;
        public Color BotBadge => _botBadge;
        public Color Crosshair => _crosshair;
        public Color DimOverlay => _dimOverlay;
        public Color ChatWhisperText => _chatWhisperText;
    }
}
