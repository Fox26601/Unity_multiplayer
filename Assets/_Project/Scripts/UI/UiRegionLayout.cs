using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>Non-overlapping anchor bands for runtime UI (1280x720 reference).</summary>
    public static class UiRegionLayout
    {
        /// <summary>Top-center match timer band (screen Y anchors).</summary>
        public const float MatchTimerBandYMin = 0.935f;
        public const float MatchTimerBandYMax = 0.99f;

        /// <summary>Match info strip directly under the timer.</summary>
        public const float MatchInfoBandYMin = 0.82f;
        public const float MatchInfoBandYMax = 0.93f;

        public static void StretchBand(RectTransform rt, float yMin, float yMax, float xPad = 24f)
        {
            rt.anchorMin = new Vector2(0f, yMin);
            rt.anchorMax = new Vector2(1f, yMax);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(xPad, 0f);
            rt.offsetMax = new Vector2(-xPad, 0f);
        }

        public static void StretchRegion(RectTransform rt, float xMin, float yMin, float xMax, float yMax,
            float padLeft = 16f, float padBottom = 12f, float padRight = 16f, float padTop = 12f)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = new Vector2(padLeft, padBottom);
            rt.offsetMax = new Vector2(-padRight, -padTop);
        }

        public static void CenterInBand(RectTransform rt, float yMin, float yMax, Vector2 size)
        {
            rt.anchorMin = new Vector2(0.5f, yMin);
            rt.anchorMax = new Vector2(0.5f, yMax);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
        }

        internal static void PlaceButtonRow(Transform parent, string prefix, int startIndex, int count,
            float rowY, float buttonSize, float bandWidth = 1180f)
        {
            var spacing = count > 1 ? (bandWidth - count * buttonSize) / (count - 1) : 0f;
            var totalWidth = count * buttonSize + (count - 1) * spacing;
            var startX = -totalWidth * 0.5f + buttonSize * 0.5f;

            for (var i = 0; i < count; i++)
            {
                var btn = parent.Find($"{prefix}{startIndex + i}") as RectTransform;
                if (btn == null) continue;
                btn.anchorMin = new Vector2(0.5f, 1f);
                btn.anchorMax = new Vector2(0.5f, 1f);
                btn.pivot = new Vector2(0.5f, 0.5f);
                btn.anchoredPosition = new Vector2(startX + i * (buttonSize + spacing), rowY);
                btn.sizeDelta = new Vector2(buttonSize, buttonSize);
            }
        }

        internal static void LayoutLabeledField(RectTransform label, RectTransform field, float yMin, float yMax,
            float fieldHeight = 56f)
        {
            if (label != null)
            {
                StretchBand(label, yMax - 0.04f, yMax, 48f);
                var labelRt = label;
                labelRt.offsetMin = new Vector2(48f, 0f);
                labelRt.offsetMax = new Vector2(-48f, -4f);
            }

            if (field != null)
            {
                StretchBand(field, yMin, yMin + (yMax - yMin) * 0.55f, 48f);
                field.offsetMin = new Vector2(48f, 0f);
                field.offsetMax = new Vector2(-48f, 0f);
                field.sizeDelta = new Vector2(0f, fieldHeight);
            }
        }
    }
}
