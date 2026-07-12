using System.Text;
using FusionMultiplayer.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>
    /// Main-menu career leaderboard modal (MatchStatsDatabase). Not the in-match Tab scoreboard.
    /// </summary>
    public sealed class CareerStatsHud : MonoBehaviour
    {
        private GameObject _overlayRoot;
        private TMP_Text _body;
        private Button _closeButton;
        private bool _built;

        private void OnEnable()
        {
            EnsureUi();
            Close();
        }

        private void Update()
        {
            if (_overlayRoot == null || !_overlayRoot.activeSelf)
                return;

            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
                Close();
        }

        public void Open()
        {
            EnsureUi();
            if (_overlayRoot == null)
                return;

            Refresh();
            _overlayRoot.SetActive(true);
            _overlayRoot.transform.SetAsLastSibling();
        }

        public void Close()
        {
            if (_overlayRoot != null)
                _overlayRoot.SetActive(false);
        }

        public bool IsOpen => _overlayRoot != null && _overlayRoot.activeSelf;

        private void EnsureUi()
        {
            if (_built && _overlayRoot != null)
                return;

            var canvas = GetComponentInParent<Canvas>();
            var parent = canvas != null ? canvas.transform : transform;

            var existing = parent.Find("CareerStatsOverlay");
            if (existing != null)
                Destroy(existing.gameObject);

            var overlayGo = new GameObject("CareerStatsOverlay", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            overlayGo.transform.SetParent(parent, false);
            var overlayRt = overlayGo.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;

            var overlayImage = overlayGo.GetComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.55f);
            overlayImage.raycastTarget = true;

            var overlayButton = overlayGo.GetComponent<Button>();
            overlayButton.targetGraphic = overlayImage;
            overlayButton.transition = Selectable.Transition.None;
            overlayButton.onClick.AddListener(Close);
            _overlayRoot = overlayGo;

            var panelGo = new GameObject("CareerStatsPanel", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            panelGo.transform.SetParent(overlayGo.transform, false);
            var panelRt = panelGo.GetComponent<RectTransform>();
            UiRegionLayout.CenterInBand(panelRt, 0.18f, 0.82f, new Vector2(520f, 420f));
            var panelImage = panelGo.GetComponent<Image>();
            panelImage.color = UiTheme.PanelBackground;
            panelImage.raycastTarget = true;

            // Stop overlay click-through when pressing the panel itself.
            var panelBlocker = panelGo.AddComponent<Button>();
            panelBlocker.targetGraphic = panelImage;
            panelBlocker.transition = Selectable.Transition.None;

            var titleGo = new GameObject("CareerStatsTitle", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(panelGo.transform, false);
            UiRegionLayout.StretchBand(titleGo.GetComponent<RectTransform>(), 0.86f, 0.96f, 20f);
            var title = titleGo.GetComponent<TMP_Text>();
            title.text = UiCopy.CareerStatsTitle;
            title.alignment = TextAlignmentOptions.Center;
            title.fontSize = UiTypography.Subtitle;
            title.fontStyle = FontStyles.Bold;
            title.color = UiTheme.TitleAccent;
            title.raycastTarget = false;
            UiTypography.ApplySubtitle(title);

            var scrollGo = new GameObject("CareerStatsScroll", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
            scrollGo.transform.SetParent(panelGo.transform, false);
            UiRegionLayout.StretchBand(scrollGo.GetComponent<RectTransform>(), 0.18f, 0.84f, 20f);
            var scrollBg = scrollGo.GetComponent<Image>();
            scrollBg.color = new Color(0.06f, 0.07f, 0.09f, 0.9f);
            scrollBg.raycastTarget = true;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = new Vector2(8f, 8f);
            viewportRt.offsetMax = new Vector2(-8f, -8f);
            var viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            viewportImage.raycastTarget = true;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _body = contentGo.GetComponent<TMP_Text>();
            _body.fontSize = UiTypography.Body;
            _body.alignment = TextAlignmentOptions.TopLeft;
            _body.color = UiTheme.TextPrimary;
            _body.textWrappingMode = TextWrappingModes.Normal;
            _body.raycastTarget = false;
            _body.margin = new Vector4(4f, 4f, 4f, 4f);
            UiTypography.ApplyBody(_body, TextAlignmentOptions.TopLeft);

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewportRt;
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            var closeGo = new GameObject("BtnClose", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button));
            closeGo.transform.SetParent(panelGo.transform, false);
            UiRegionLayout.StretchBand(closeGo.GetComponent<RectTransform>(), 0.04f, 0.14f, 48f);
            var closeImg = closeGo.GetComponent<Image>();
            closeImg.color = UiTheme.ButtonBackground;
            _closeButton = closeGo.GetComponent<Button>();
            _closeButton.targetGraphic = closeImg;
            _closeButton.onClick.AddListener(Close);

            var closeTextGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            closeTextGo.transform.SetParent(closeGo.transform, false);
            var closeTextRt = closeTextGo.GetComponent<RectTransform>();
            closeTextRt.anchorMin = Vector2.zero;
            closeTextRt.anchorMax = Vector2.one;
            closeTextRt.offsetMin = Vector2.zero;
            closeTextRt.offsetMax = Vector2.zero;
            var closeText = closeTextGo.GetComponent<TMP_Text>();
            closeText.text = UiCopy.CareerStatsClose;
            closeText.alignment = TextAlignmentOptions.Center;
            closeText.raycastTarget = false;
            UiTypography.ApplyButtonLabel(closeText);

            _built = true;
            Close();
        }

        public void Refresh()
        {
            EnsureUi();
            if (_body == null)
                return;

            var rows = MatchStatsDatabase.ReadLeaderboard();
            var sb = new StringBuilder();
            if (rows == null || rows.Count == 0)
            {
                sb.Append(UiCopy.CareerStatsEmpty);
            }
            else
            {
                for (var i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    sb.AppendLine($"{i + 1}. {row.Nickname}  K:{row.Kills} D:{row.Deaths} M:{row.Matches}");
                }
            }

            _body.text = sb.ToString();
            _body.ForceMeshUpdate();
            var contentRt = _body.rectTransform;
            contentRt.sizeDelta = new Vector2(0f, Mathf.Max(_body.preferredHeight + 8f, 40f));
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
        }
    }
}
