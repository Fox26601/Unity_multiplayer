using TMPro;
using UnityEngine;

namespace FusionMultiplayer.UI
{
    /// <summary>
    /// Legacy layout patcher — skipped when runtime rebuild markers are present.
    /// </summary>
    public static class UiLayoutRuntimeFix
    {
        public static void ApplyIfNeeded(Transform canvasRoot)
        {
            if (canvasRoot == null) return;

            if (canvasRoot.GetComponent<MainMenuUI>() != null)
            {
                var panel = canvasRoot.Find("Panel");
                if (panel != null &&
                    (panel.Find("MenuUiVersion")?.GetComponent<UiBuildVersionMarker>() != null ||
                     panel.Find("MenuUiVersion")?.GetComponent<TMP_Text>() != null))
                    return;
            }

            if (canvasRoot.GetComponent<LobbyUI>() != null)
            {
                var panel = canvasRoot.Find("Panel");
                if (panel != null && panel.Find("UiBuildVersion") != null)
                    return;
            }

            if (canvasRoot.GetComponent<CharacterSelectUI>() != null || canvasRoot.GetComponent<ChatUI>() != null)
            {
                var charPanel = canvasRoot.Find("CharacterSelectPanel");
                if (charPanel != null &&
                    charPanel.GetComponent<UiBuildVersionMarker>() != null)
                    return;
            }

            var panelLegacy = canvasRoot.Find("Panel");
            if (panelLegacy == null) return;

            if (canvasRoot.GetComponent<MainMenuUI>() != null)
                ApplyMainMenu(panelLegacy);

            if (canvasRoot.GetComponent<LobbyUI>() != null)
                ApplyLobby(panelLegacy);
        }

        private static void ApplyMainMenu(Transform panel)
        {
            Place(panel, "NicknameField", new Vector2(0f, 70f), new Vector2(640f, 80f));
            Place(panel, "RoomField", new Vector2(0f, -30f), new Vector2(640f, 80f));
            Place(panel, "ColorPreview", new Vector2(-250f, -130f), new Vector2(96f, 96f));
            Place(panel, "BtnRandomColor", new Vector2(150f, -130f), new Vector2(380f, 80f));
            Place(panel, "BtnCreate", new Vector2(0f, -250f), new Vector2(480f, 88f));
            Place(panel, "BtnJoin", new Vector2(0f, -360f), new Vector2(480f, 88f));
            Place(panel, "SessionStatus", new Vector2(0f, -460f), new Vector2(700f, 56f));
        }

        private static void ApplyLobby(Transform panel)
        {
            var list = panel.Find("PlayerList") as RectTransform;
            if (list != null)
            {
                list.anchorMin = new Vector2(0f, 0.24f);
                list.anchorMax = new Vector2(1f, 0.88f);
                list.offsetMin = new Vector2(48f, 0f);
                list.offsetMax = new Vector2(-48f, 0f);
            }

            var status = panel.Find("Status") as RectTransform;
            if (status != null)
            {
                status.anchorMin = new Vector2(0f, 0f);
                status.anchorMax = new Vector2(1f, 0f);
                status.pivot = new Vector2(0.5f, 0f);
                status.anchoredPosition = new Vector2(0f, 120f);
                status.sizeDelta = new Vector2(-56f, 64f);
            }

            Place(panel, "BtnStart", new Vector2(0f, 32f), new Vector2(560f, 80f), anchorBottom: true);
        }

        private static void Place(Transform panel, string childName, Vector2 anchoredPosition, Vector2 size,
            bool anchorBottom = false)
        {
            var tr = panel.Find(childName) as RectTransform;
            if (tr == null) return;

            if (anchorBottom)
            {
                tr.anchorMin = new Vector2(0.5f, 0f);
                tr.anchorMax = new Vector2(0.5f, 0f);
                tr.pivot = new Vector2(0.5f, 0f);
            }
            else
            {
                tr.anchorMin = new Vector2(0.5f, 0.5f);
                tr.anchorMax = new Vector2(0.5f, 0.5f);
                tr.pivot = new Vector2(0.5f, 0.5f);
            }

            tr.anchoredPosition = anchoredPosition;
            tr.sizeDelta = size;
        }
    }
}
