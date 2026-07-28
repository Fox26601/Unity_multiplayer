using System.Text;
using FusionMultiplayer.Core;
using FusionMultiplayer.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>Hold Tab to show the live kills/deaths scoreboard during Combat and Sandbox.</summary>
    public sealed class LeaderboardUI : MonoBehaviour
    {
        [SerializeField] private GameObject _overlayRoot;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _tableText;

        private void Awake()
        {
            UiCanvasFix.EnsureReadableCanvas(transform);
        }

        private void EnsureOverlay()
        {
            _overlayRoot ??= transform.Find("LeaderboardOverlay")?.gameObject;
            if (_overlayRoot != null)
            {
                _titleText ??= _overlayRoot.transform.Find("LeaderboardPanel/LeaderboardTitle")?.GetComponent<TMP_Text>();
                _tableText ??= _overlayRoot.transform.Find("LeaderboardPanel/LeaderboardTable")?.GetComponent<TMP_Text>();
                if (_tableText != null)
                    return;
            }

            var overlayGo = new GameObject("LeaderboardOverlay", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            overlayGo.transform.SetParent(transform, false);
            var overlayRt = overlayGo.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;
            var overlayImage = overlayGo.GetComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.45f);
            overlayImage.raycastTarget = false;
            _overlayRoot = overlayGo;

            var panelGo = new GameObject("LeaderboardPanel", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            panelGo.transform.SetParent(overlayGo.transform, false);
            var panelRt = panelGo.GetComponent<RectTransform>();
            UiRegionLayout.CenterInBand(panelRt, 0.22f, 0.78f, new Vector2(560f, 360f));
            var panelImage = panelGo.GetComponent<Image>();
            panelImage.color = UiTheme.PanelBackground;
            panelImage.raycastTarget = false;

            var titleGo = new GameObject("LeaderboardTitle", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(panelGo.transform, false);
            UiRegionLayout.StretchBand(titleGo.GetComponent<RectTransform>(), 0.82f, 0.96f, 24f);
            _titleText = titleGo.GetComponent<TMP_Text>();
            _titleText.text = UiCopy.LeaderboardTitle;
            _titleText.alignment = TextAlignmentOptions.Center;
            _titleText.fontSize = UiTypography.Subtitle;
            _titleText.fontStyle = FontStyles.Bold;
            _titleText.color = UiTheme.TitleAccent;
            _titleText.raycastTarget = false;
            UiTypography.ApplySubtitle(_titleText);

            var tableGo = new GameObject("LeaderboardTable", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            tableGo.transform.SetParent(panelGo.transform, false);
            UiRegionLayout.StretchBand(tableGo.GetComponent<RectTransform>(), 0.08f, 0.78f, 32f);
            _tableText = tableGo.GetComponent<TMP_Text>();
            _tableText.text = UiCopy.GameOverNoScores;
            _tableText.alignment = TextAlignmentOptions.Top;
            _tableText.fontSize = UiTypography.Body;
            _tableText.color = UiTheme.TextPrimary;
            _tableText.textWrappingMode = TextWrappingModes.Normal;
            _tableText.raycastTarget = false;
            UiTypography.ApplyBody(_tableText, TextAlignmentOptions.Top);
        }

        private void Update()
        {
            if (!ShouldShow())
            {
                if (_overlayRoot != null && _overlayRoot.activeSelf)
                    _overlayRoot.SetActive(false);
                return;
            }

            EnsureOverlay();
            if (_overlayRoot == null || _tableText == null)
                return;

            if (!_overlayRoot.activeSelf)
                _overlayRoot.SetActive(true);

            var sb = new StringBuilder();
            var any = CombatScoreboardTable.AppendRows(sb, UiCopy.GameOverResultsHeader);
            _tableText.text = any ? sb.ToString() : UiCopy.GameOverNoScores;
        }

        private static bool ShouldShow()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.tabKey.isPressed)
                return false;

            SessionRuntime.Refresh();
            return SessionRuntime.AllowsShoot && LocalPlayerHudCache.TryGetLocalAvatar(out _);
        }
    }
}
