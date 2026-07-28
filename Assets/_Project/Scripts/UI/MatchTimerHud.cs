using FusionMultiplayer.Core;
using TMPro;
using UnityEngine;

namespace FusionMultiplayer.UI
{
    /// <summary>Top-center match countdown while the round is active.</summary>
    public sealed class MatchTimerHud : MonoBehaviour
    {
        private TMP_Text _timerText;
        private int _lastTotalSeconds = int.MinValue;
        private bool _wasActive;

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
                {
                    UiRegionLayout.StretchBand(_timerText.rectTransform, UiRegionLayout.MatchTimerBandYMin,
                        UiRegionLayout.MatchTimerBandYMax, 200f);
                    return;
                }
            }

            var go = new GameObject("MatchTimerHud", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            UiRegionLayout.StretchBand(rt, UiRegionLayout.MatchTimerBandYMin, UiRegionLayout.MatchTimerBandYMax, 200f);

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
                if (_wasActive)
                {
                    _timerText.gameObject.SetActive(false);
                    _wasActive = false;
                    _lastTotalSeconds = int.MinValue;
                }

                return;
            }

            if (!_wasActive)
            {
                _timerText.gameObject.SetActive(true);
                _wasActive = true;
            }

            var total = Mathf.CeilToInt(seconds);
            if (total == _lastTotalSeconds)
                return;

            _lastTotalSeconds = total;
            var minutes = total / 60;
            var secs = total % 60;
            _timerText.text = $"{minutes}:{secs:00}";
        }
    }
}
