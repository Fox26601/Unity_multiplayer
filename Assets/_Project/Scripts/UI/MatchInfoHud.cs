using FusionMultiplayer.Core;
using FusionMultiplayer.Player;
using TMPro;
using UnityEngine;

namespace FusionMultiplayer.UI
{
    /// <summary>
    /// In-match strip: mode / map / difficulty, match event, local K/D, bot badge.
    /// </summary>
    public sealed class MatchInfoHud : MonoBehaviour
    {
        private TMP_Text _metaText;
        private TMP_Text _eventText;
        private TMP_Text _kdText;
        private TMP_Text _botText;
        private GameObject _root;
        private string _lastMeta;
        private string _lastEvent;
        private int _lastScore = int.MinValue;
        private int _lastDeaths = int.MinValue;
        private bool _lastBot;

        private void Awake()
        {
            UiCanvasFix.EnsureReadableCanvas(transform);
            EnsureHud();
        }

        private void EnsureHud()
        {
            var existing = transform.Find("MatchInfoHud");
            if (existing != null)
            {
                _root = existing.gameObject;
                _metaText = existing.Find("Meta")?.GetComponent<TMP_Text>();
                _eventText = existing.Find("Event")?.GetComponent<TMP_Text>();
                _kdText = existing.Find("KD")?.GetComponent<TMP_Text>();
                _botText = existing.Find("Bot")?.GetComponent<TMP_Text>();
                if (_metaText != null && _eventText != null && _kdText != null && _botText != null)
                    return;
            }

            if (existing != null)
                Destroy(existing.gameObject);

            _root = new GameObject("MatchInfoHud", typeof(RectTransform));
            _root.transform.SetParent(transform, false);
            var rootRt = _root.GetComponent<RectTransform>();
            UiRegionLayout.StretchBand(rootRt, UiRegionLayout.MatchInfoBandYMin, UiRegionLayout.MatchInfoBandYMax, 80f);

            _metaText = CreateLine(_root.transform, "Meta", 0.72f, UiTheme.TextSubtitle);
            _eventText = CreateLine(_root.transform, "Event", 0.42f, UiTheme.TitleAccent);
            _kdText = CreateLine(_root.transform, "KD", 0.12f, UiTheme.TextPrimary);
            _botText = CreateLine(_root.transform, "Bot", 0.12f, UiTheme.BotBadge);
            var botRt = _botText.rectTransform;
            botRt.anchorMin = new Vector2(0.58f, 0.05f);
            botRt.anchorMax = new Vector2(0.72f, 0.35f);
            botRt.offsetMin = Vector2.zero;
            botRt.offsetMax = Vector2.zero;
            _botText.alignment = TextAlignmentOptions.Left;
            _botText.text = UiCopy.MatchInfoBotBadge;
            _botText.gameObject.SetActive(false);
        }

        private static TMP_Text CreateLine(Transform parent, string name, float yAnchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, yAnchor);
            rt.anchorMax = new Vector2(0.95f, yAnchor + 0.28f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var text = go.GetComponent<TMP_Text>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = UiTypography.ChatMetaCompact;
            text.enableAutoSizing = true;
            text.fontSizeMin = 14f;
            text.fontSizeMax = UiTypography.ChatMetaCompact;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            UiTypography.ApplyFontTo(text);
            return text;
        }

        private void Update()
        {
            if (_root == null || _metaText == null)
                return;

            if (!GameSceneReadiness.TryGetGameManager(out var gm) || gm.IsGameOver)
            {
                _root.SetActive(false);
                return;
            }

            _root.SetActive(true);

            var runner = ConnectionManager.Instance != null ? ConnectionManager.Instance.Runner : null;
            SessionRuntime.Refresh(runner);

            var mode = SessionCatalog.GetModeLabel(SessionRuntime.CurrentMode);
            var map = SessionCatalog.GetMapLabel(SessionData.SelectedMap == SessionCatalog.MapKind.Any
                ? SessionCatalog.MapKind.Arena
                : SessionData.SelectedMap);
            var difficulty = SessionCatalog.GetDifficultyLabel(SessionData.SelectedDifficulty);

            if (runner != null && runner.IsRunning && runner.SessionInfo.IsValid)
            {
                if (SessionCatalog.TryGetMap(runner.SessionInfo, out var sessionMap))
                    map = SessionCatalog.GetMapLabel(sessionMap);
                if (SessionCatalog.TryGetDifficulty(runner.SessionInfo, out var sessionDiff))
                    difficulty = SessionCatalog.GetDifficultyLabel(sessionDiff);
            }

            var meta = string.Format(UiCopy.MatchInfoMetaFormat, mode, map, difficulty);
            if (meta != _lastMeta)
            {
                _lastMeta = meta;
                _metaText.text = meta;
            }

            var eventLabel = FormatMatchEvent(gm.MatchEventId);
            if (eventLabel != _lastEvent)
            {
                _lastEvent = eventLabel;
                _eventText.text = eventLabel;
            }

            PlayerData pd = null;
            var showKd = SessionRuntime.AllowsShoot && LocalPlayerHudCache.TryGetLocalPlayerData(out pd);
            if (showKd && pd != null)
            {
                _kdText.gameObject.SetActive(true);
                if (pd.Score != _lastScore || pd.Deaths != _lastDeaths)
                {
                    _lastScore = pd.Score;
                    _lastDeaths = pd.Deaths;
                    _kdText.text = string.Format(UiCopy.MatchInfoKdFormat, pd.Score, pd.Deaths);
                }

                if (_botText != null)
                {
                    var bot = (bool)pd.IsBotControlled;
                    if (bot != _lastBot)
                    {
                        _lastBot = bot;
                        _botText.gameObject.SetActive(bot);
                    }
                }
            }
            else
            {
                _kdText.gameObject.SetActive(false);
                if (_botText != null)
                    _botText.gameObject.SetActive(false);
                _lastScore = int.MinValue;
                _lastBot = false;
            }
        }

        private static string FormatMatchEvent(int eventId) => eventId switch
        {
            0 => UiCopy.MatchEventCalm,
            1 => UiCopy.MatchEventStorm,
            2 => UiCopy.MatchEventLowGravity,
            _ => UiCopy.MatchEventUnknown
        };
    }
}
