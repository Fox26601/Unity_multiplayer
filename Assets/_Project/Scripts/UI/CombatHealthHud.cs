using FusionMultiplayer.Core;
using TMPro;
using UnityEngine;

namespace FusionMultiplayer.UI
{
    /// <summary>Local HP readout for Combat and Sandbox modes.</summary>
    public sealed class CombatHealthHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text _healthText;

        private void Awake()
        {
            UiCanvasFix.EnsureReadableCanvas(transform);
            EnsureLayout();
            UiTypography.ApplyHierarchy(transform);
        }

        private void EnsureLayout()
        {
            _healthText ??= transform.Find("HealthText")?.GetComponent<TMP_Text>();
            if (_healthText == null)
            {
                var go = new GameObject("HealthText", typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
                go.transform.SetParent(transform, false);
                _healthText = go.GetComponent<TMP_Text>();
            }

            ApplyHealthTextLayout(_healthText);
        }

        private static void ApplyHealthTextLayout(TMP_Text healthText)
        {
            if (healthText == null)
                return;

            var rt = healthText.rectTransform;
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-24f, 24f);
            rt.sizeDelta = new Vector2(220f, 36f);

            healthText.alignment = TextAlignmentOptions.BottomRight;
            healthText.fontSize = UiTypography.Body;
            healthText.color = UiTheme.TextPrimary;
            healthText.textWrappingMode = TextWrappingModes.NoWrap;
            healthText.raycastTarget = false;
            UiTypography.ApplyBody(healthText, TextAlignmentOptions.BottomRight);
        }

        private void Update()
        {
            SessionRuntime.Refresh();

            if (_healthText == null)
                return;

            if (!SessionRuntime.AllowsShoot || !LocalPlayerHudCache.TryGetLocalAvatar(out var avatar))
            {
                _healthText.gameObject.SetActive(false);
                return;
            }

            _healthText.gameObject.SetActive(true);

            var hp = Mathf.CeilToInt(avatar.Health);
            var max = Mathf.CeilToInt(avatar.MaxHealth);
            if (avatar.IsDead)
                _healthText.text = UiCopy.CombatHealthDead;
            else
                _healthText.text = string.Format(UiCopy.CombatHealthFormat, hp, max);
        }
    }
}
