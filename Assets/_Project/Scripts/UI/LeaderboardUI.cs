using System.Text;
using FusionMultiplayer.Core;
using FusionMultiplayer.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>Hold Tab to show live kills/deaths scoreboard during Combat and Sandbox.</summary>
    public sealed class LeaderboardUI : MonoBehaviour
    {
        [SerializeField] private GameObject _overlayRoot;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _tableText;

        private void Awake()
        {
            enabled = false;
            if (_overlayRoot != null)
                _overlayRoot.SetActive(false);
        }

        private void EnsureOverlayLayoutDisabled()
        {
            _overlayRoot ??= transform.Find("LeaderboardOverlay")?.gameObject;
            if (_overlayRoot != null)
            {
                _titleText ??= _overlayRoot.transform.Find("LeaderboardTitle")?.GetComponent<TMP_Text>();
                _tableText ??= _overlayRoot.transform.Find("LeaderboardTable")?.GetComponent<TMP_Text>();
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
            overlayGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
            _overlayRoot = overlayGo;

            var panelGo = new GameObject("LeaderboardPanel", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            panelGo.transform.SetParent(overlayGo.transform, false);
            var panelRt = panelGo.GetComponent<RectTransform>();
            UiRegionLayout.CenterInBand(panelRt, 0.22f, 0.78f, new Vector2(560f, 360f));
            panelGo.GetComponent<Image>().color = UiTheme.PanelBackground;

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
            UiTypography.ApplyBody(_tableText, TextAlignmentOptions.Top);
        }

        private void Update()
        {
            var visible = ShouldShow();
            if (_overlayRoot != null)
                _overlayRoot.SetActive(visible);

            if (!visible || _tableText == null)
                return;

            var sb = new StringBuilder();
            var any = CombatScoreboardTable.AppendRows(sb, UiCopy.GameOverResultsHeader);
            _tableText.text = any ? sb.ToString() : UiCopy.GameOverNoScores;
        }

        private static bool ShouldShow() => false;

        private static bool HasLocalAvatar()
        {
            foreach (var avatar in Object.FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
            {
                if (avatar.Object != null && avatar.Object.IsValid && avatar.HasInputAuthority)
                    return true;
            }

            return false;
        }
    }
}
