using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>Rebuilds lobby UI at runtime with non-overlapping anchor bands.</summary>
    public static class LobbyRuntimeRebuild
    {
        private const int BuildVersion = 6;

        public static void EnsureBuilt(Transform canvas)
        {
            if (canvas == null || canvas.GetComponent<LobbyUI>() == null)
                return;

            UiCanvasFix.EnsureReadableCanvas(canvas);
            UiRuntimeBuildKit.EnsureCanvasScaler(canvas);

            var panel = canvas.Find("Panel");
            if (panel != null && UiRuntimeBuildKit.VersionMatches(panel, BuildVersion) &&
                panel.Find("SessionInfo")?.GetComponent<TMP_Text>() != null &&
                panel.Find("BtnLeave") != null)
                return;

            if (panel == null)
                panel = UiRuntimeBuildKit.CreatePanel(canvas);
            else
                UiRuntimeBuildKit.ClearChildren(panel);

            const float bandTitleBottom = 0.86f;
            const float bandSessionBottom = 0.80f;
            const float bandSessionTop = 0.86f;
            const float bandListBottom = 0.28f;
            const float bandListTop = 0.79f;
            const float bandStatusBottom = 0.16f;
            const float bandStatusTop = 0.26f;
            const float bandButtonBottom = 0.03f;
            const float bandButtonTop = 0.14f;

            var title = UiRuntimeBuildKit.CreateLabel(panel, "Title", UiCopy.LobbyTitle, UiTypography.Title,
                UiTheme.TitleAccent, FontStyles.Bold,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center);
            UiRegionLayout.StretchBand(title.rectTransform, bandTitleBottom, 1f, 32f);

            var sessionInfo = UiRuntimeBuildKit.CreateLabel(panel, "SessionInfo", string.Empty, UiTypography.Body,
                UiTheme.TextSubtitle, FontStyles.Normal,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center);
            UiRegionLayout.StretchBand(sessionInfo.rectTransform, bandSessionBottom, bandSessionTop, 40f);

            var list = UiRuntimeBuildKit.CreateLabel(panel, "PlayerList", UiCopy.LobbyPlayersWaiting,
                UiTypography.Body, UiTheme.TextPrimary, FontStyles.Normal,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.TopLeft);
            UiRegionLayout.StretchBand(list.rectTransform, bandListBottom, bandListTop, 40f);
            list.textWrappingMode = TextWrappingModes.Normal;
            list.overflowMode = TextOverflowModes.Overflow;
            UiTypography.ApplyBody(list, TextAlignmentOptions.TopLeft);

            var status = UiRuntimeBuildKit.CreateLabel(panel, "Status", UiCopy.LobbyMasterStatus,
                UiTypography.Body, UiTheme.TextSubtitle, FontStyles.Normal,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineLeft);
            UiRegionLayout.StretchBand(status.rectTransform, bandStatusBottom, bandStatusTop, 48f);

            var leave = UiRuntimeBuildKit.CreateButton(panel, "BtnLeave", UiCopy.LobbyLeave,
                Vector2.zero, new Vector2(260f, 64f));
            var leaveRt = (RectTransform)leave.transform;
            leaveRt.anchorMin = new Vector2(0f, bandButtonBottom);
            leaveRt.anchorMax = new Vector2(0.48f, bandButtonTop);
            leaveRt.offsetMin = Vector2.zero;
            leaveRt.offsetMax = Vector2.zero;
            UiTypography.StyleButton(leave);
            var leaveLabel = leave.GetComponentInChildren<TMP_Text>();
            if (leaveLabel != null)
            {
                leaveLabel.text = UiCopy.LobbyLeave;
                leaveLabel.textWrappingMode = TextWrappingModes.Normal;
            }

            var start = UiRuntimeBuildKit.CreateButton(panel, "BtnStart", "START GAME",
                Vector2.zero, new Vector2(260f, 64f));
            var startRt = (RectTransform)start.transform;
            startRt.anchorMin = new Vector2(0.52f, bandButtonBottom);
            startRt.anchorMax = new Vector2(1f, bandButtonTop);
            startRt.offsetMin = Vector2.zero;
            startRt.offsetMax = Vector2.zero;
            UiTypography.StyleButton(start);
            var startLabel = start.GetComponentInChildren<TMP_Text>();
            if (startLabel != null)
            {
                startLabel.text = "START GAME";
                startLabel.textWrappingMode = TextWrappingModes.Normal;
            }

            canvas.GetComponent<LobbyUI>()?.BindRuntimeTmp(list, status, start, sessionInfo, leave);
            UiRuntimeBuildKit.HideVersionMarker(panel, BuildVersion);
        }
    }
}
