using UnityEngine;

namespace FusionMultiplayer.UI
{
    /// <summary>Shared UI colors for readable contrast on dark panels.</summary>
    public static class UiTheme
    {
        public static readonly Color PanelBackground = new Color(0.1f, 0.1f, 0.12f, 0.94f);
        public static readonly Color ButtonBackground = new Color(0.22f, 0.28f, 0.38f, 1f);
        public static readonly Color ButtonHighlighted = new Color(0.28f, 0.35f, 0.48f, 1f);
        public static readonly Color ButtonPressed = new Color(0.18f, 0.22f, 0.32f, 1f);
        public static readonly Color InputBackground = new Color(0.15f, 0.15f, 0.18f, 1f);
        public static readonly Color TextPrimary = Color.white;
        public static readonly Color TextSubtitle = new Color(0.85f, 0.88f, 0.92f, 1f);
        public static readonly Color TitleAccent = new Color(1f, 0.85f, 0.2f, 1f);
        public static readonly Color StatusOk = new Color(0.55f, 1f, 0.65f, 1f);
        public static readonly Color StatusError = new Color(1f, 0.45f, 0.45f, 1f);
        public static readonly Color Placeholder = new Color(0.78f, 0.82f, 0.88f, 0.92f);
        /// <summary>Private / whisper lines in chat log (rich text).</summary>
        public static readonly Color ChatWhisperText = new Color(0.45f, 0.78f, 1f, 1f);
    }
}
