using FusionMultiplayer.Core;
using TMPro;
using UnityEngine;

namespace FusionMultiplayer.UI
{
    /// <summary>Top-center match countdown while the round is active.</summary>
    public sealed class MatchTimerHud : MonoBehaviour
    {
        private TMP_Text _timerText;

        private void Awake()
        {
            UiCanvasFix.EnsureReadableCanvas(transform);
            EnsureHud();
        }

        private void EnsureHud()
        {
            var existing = transform.Find("MatchTimerHud");
            if (existing != null)
            {
                _timerText = existing.GetComponent<TMP_Text>();
                if (_timerText != null)
                    return;
            }

            var go = new GameObject("MatchTimerHud", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -12f);
            rt.sizeDelta = new Vector2(180f, 40f);

            _timerText = go.GetComponent<TMP_Text>();
            _timerText.text = "5:00";
            _timerText.alignment = TextAlignmentOptions.Center;
            _timerText.fontSize = UiTypography.Subtitle;
            _timerText.fontStyle = FontStyles.Bold;
            _timerText.color = UiTheme.TitleAccent;
            _timerText.raycastTarget = false;
            UiTypography.ApplySubtitle(_timerText);
        }

        private void Update()
        {
            if (_timerText == null)
                return;

            if (!GameSceneReadiness.TryGetGameManager(out var gm) || !gm.TryGetMatchRemaining(out var seconds))
            {
                _timerText.gameObject.SetActive(false);
                return;
            }

            _timerText.gameObject.SetActive(true);
            var total = Mathf.CeilToInt(seconds);
            var minutes = total / 60;
            var secs = total % 60;
            _timerText.text = $"{minutes}:{secs:00}";
        }
    }
}
