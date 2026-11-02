using FusionMultiplayer.Data;
using UnityEngine;

namespace FusionMultiplayer.UI
{
    /// <summary>Shared UI colors from <see cref="UiThemeConfig"/> (hardcoded fallbacks if hub missing).</summary>
    public static class UiTheme
    {
        private static readonly Color FallbackPanelBackground = new(0.1f, 0.1f, 0.12f, 0.94f);
        private static readonly Color FallbackButtonBackground = new(0.22f, 0.28f, 0.38f, 1f);
        private static readonly Color FallbackButtonHighlighted = new(0.28f, 0.35f, 0.48f, 1f);
        private static readonly Color FallbackButtonPressed = new(0.18f, 0.22f, 0.32f, 1f);
        private static readonly Color FallbackInputBackground = new(0.15f, 0.15f, 0.18f, 1f);
        private static readonly Color FallbackTextSubtitle = new(0.85f, 0.88f, 0.92f, 1f);
        private static readonly Color FallbackTitleAccent = new(1f, 0.85f, 0.2f, 1f);
        private static readonly Color FallbackStatusOk = new(0.55f, 1f, 0.65f, 1f);
        private static readonly Color FallbackStatusError = new(1f, 0.45f, 0.45f, 1f);
        private static readonly Color FallbackPlaceholder = new(0.78f, 0.82f, 0.88f, 0.92f);
        private static readonly Color FallbackBotBadge = new(1f, 0.55f, 0.2f, 1f);
        private static readonly Color FallbackCrosshair = new(1f, 1f, 1f, 0.9f);
        private static readonly Color FallbackDimOverlay = new(0f, 0f, 0f, 0.72f);
        private static readonly Color FallbackChatWhisper = new(0.45f, 0.78f, 1f, 1f);

        private static UiThemeConfig Theme => GameConfigs.EnsureCurrent()?.UiTheme;

        public static Color PanelBackground => Theme != null ? Theme.PanelBackground : FallbackPanelBackground;
        public static Color ButtonBackground => Theme != null ? Theme.ButtonBackground : FallbackButtonBackground;
        public static Color ButtonHighlighted => Theme != null ? Theme.ButtonHighlighted : FallbackButtonHighlighted;
        public static Color ButtonPressed => Theme != null ? Theme.ButtonPressed : FallbackButtonPressed;
        public static Color InputBackground => Theme != null ? Theme.InputBackground : FallbackInputBackground;
        public static Color TextPrimary => Theme != null ? Theme.TextPrimary : Color.white;
        public static Color TextSubtitle => Theme != null ? Theme.TextSubtitle : FallbackTextSubtitle;
        public static Color TitleAccent => Theme != null ? Theme.TitleAccent : FallbackTitleAccent;
        public static Color StatusOk => Theme != null ? Theme.StatusOk : FallbackStatusOk;
        public static Color StatusError => Theme != null ? Theme.StatusError : FallbackStatusError;
        public static Color Placeholder => Theme != null ? Theme.Placeholder : FallbackPlaceholder;
        public static Color BotBadge => Theme != null ? Theme.BotBadge : FallbackBotBadge;
        public static Color Crosshair => Theme != null ? Theme.Crosshair : FallbackCrosshair;
        public static Color DimOverlay => Theme != null ? Theme.DimOverlay : FallbackDimOverlay;
        public static Color ChatWhisperText => Theme != null ? Theme.ChatWhisperText : FallbackChatWhisper;
    }
}
