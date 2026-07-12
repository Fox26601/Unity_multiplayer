using System.Text;
using FusionMultiplayer.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>Shows persistent career stats from MatchStatsDatabase on the main menu.</summary>
    public sealed class CareerStatsHud : MonoBehaviour
    {
        private TMP_Text _body;
        private bool _built;

        private void OnEnable()
        {
            EnsureUi();
            Refresh();
        }

        private void EnsureUi()
        {
            if (_built)
                return;

            var canvas = GetComponentInParent<Canvas>();
            var parent = canvas != null ? canvas.transform : transform;

            var go = new GameObject("CareerStatsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.02f, 0.02f);
            rt.anchorMax = new Vector2(0.38f, 0.28f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = go.GetComponent<Image>();
            bg.color = new Color(0.08f, 0.09f, 0.12f, 0.82f);
            bg.raycastTarget = false;

            var textGo = new GameObject("CareerStatsText", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10f, 8f);
            textRt.offsetMax = new Vector2(-10f, -8f);

            _body = textGo.GetComponent<TMP_Text>();
            _body.fontSize = 18f;
            _body.alignment = TextAlignmentOptions.TopLeft;
            _body.color = Color.white;
            _body.raycastTarget = false;
            UiTypography.ApplyFontTo(_body);

            _built = true;
        }

        public void Refresh()
        {
            EnsureUi();
            if (_body == null)
                return;

            var rows = MatchStatsDatabase.ReadLeaderboard();
            var sb = new StringBuilder();
            sb.AppendLine(UiCopy.CareerStatsTitle);
            if (rows == null || rows.Count == 0)
            {
                sb.Append(UiCopy.CareerStatsEmpty);
            }
            else
            {
                var limit = Mathf.Min(5, rows.Count);
                for (var i = 0; i < limit; i++)
                {
                    var row = rows[i];
                    sb.AppendLine($"{i + 1}. {row.Nickname}  K:{row.Kills} D:{row.Deaths} M:{row.Matches}");
                }
            }

            _body.text = sb.ToString();
        }
    }
}
