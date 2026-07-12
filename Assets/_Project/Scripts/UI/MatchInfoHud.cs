using Fusion;
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
                if (_metaText != null && _eventText != null && _kdText != null)
                    return;
            }

            if (existing != null)
                Destroy(existing.gameObject);

            _root = new GameObject("MatchInfoHud", typeof(RectTransform));
            _root.transform.SetParent(transform, false);
            var rootRt = _root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 1f);
            rootRt.anchorMax = new Vector2(0.5f, 1f);
            rootRt.pivot = new Vector2(0.5f, 1f);
            rootRt.anchoredPosition = new Vector2(0f, -52f);
            rootRt.sizeDelta = new Vector2(720f, 72f);

            _metaText = CreateLine(_root.transform, "Meta", 0f, UiTheme.TextSubtitle);
            _eventText = CreateLine(_root.transform, "Event", -22f, UiTheme.TitleAccent);
            _kdText = CreateLine(_root.transform, "KD", -44f, UiTheme.TextPrimary);
            _botText = CreateLine(_root.transform, "Bot", -44f, new Color(1f, 0.55f, 0.2f, 1f));
            var botRt = _botText.rectTransform;
            botRt.anchorMin = new Vector2(0.5f, 1f);
            botRt.anchorMax = new Vector2(0.5f, 1f);
            botRt.pivot = new Vector2(0f, 1f);
            botRt.anchoredPosition = new Vector2(70f, -44f);
            botRt.sizeDelta = new Vector2(80f, 22f);
            _botText.alignment = TextAlignmentOptions.Left;
            _botText.text = UiCopy.MatchInfoBotBadge;
            _botText.gameObject.SetActive(false);
        }

        private static TMP_Text CreateLine(Transform parent, string name, float y, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(700f, 22f);

            var text = go.GetComponent<TMP_Text>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 18f;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
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

            _metaText.text = string.Format(UiCopy.MatchInfoMetaFormat, mode, map, difficulty);
            _eventText.text = FormatMatchEvent(gm.MatchEventId);

            if (TryGetLocalPlayerData(runner, out var pd))
            {
                _kdText.gameObject.SetActive(true);
                _kdText.text = string.Format(UiCopy.MatchInfoKdFormat, pd.Score, pd.Deaths);
                if (_botText != null)
                    _botText.gameObject.SetActive(pd.IsBotControlled);
            }
            else
            {
                _kdText.gameObject.SetActive(false);
                if (_botText != null)
                    _botText.gameObject.SetActive(false);
            }
        }

        private static string FormatMatchEvent(int eventId) => eventId switch
        {
            0 => UiCopy.MatchEventCalm,
            1 => UiCopy.MatchEventStorm,
            2 => UiCopy.MatchEventLowGravity,
            _ => UiCopy.MatchEventUnknown
        };

        private static bool TryGetLocalPlayerData(NetworkRunner runner, out PlayerData data)
        {
            data = null;
            if (runner == null || !runner.IsRunning)
                return false;

            var local = runner.LocalPlayer;
            foreach (var pd in Object.FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
            {
                if (pd.Object == null || !pd.Object.IsValid)
                    continue;
                if (pd.Object.InputAuthority != local)
                    continue;
                data = pd;
                return true;
            }

            return false;
        }
    }
}
