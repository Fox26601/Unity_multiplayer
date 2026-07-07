using FusionMultiplayer.Core;
using FusionMultiplayer.Player;
using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>Screen-center crosshair while the local player can shoot.</summary>
    public sealed class CrosshairHud : MonoBehaviour
    {
        private const float ArmLength = 10f;
        private const float ArmThickness = 2f;
        private const float ArmGap = 5f;

        private GameObject _root;

        private void Awake()
        {
            UiCanvasFix.EnsureReadableCanvas(transform);
            EnsureCrosshair();
        }

        private void EnsureCrosshair()
        {
            _root = transform.Find("Crosshair")?.gameObject;
            if (_root != null)
                return;

            _root = new GameObject("Crosshair", typeof(RectTransform));
            _root.transform.SetParent(transform, false);

            var rt = _root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            CreateArm(_root.transform, "Left",
                new Vector2(ArmLength, ArmThickness),
                new Vector2(-(ArmGap + ArmLength * 0.5f), 0f));
            CreateArm(_root.transform, "Right",
                new Vector2(ArmLength, ArmThickness),
                new Vector2(ArmGap + ArmLength * 0.5f, 0f));
            CreateArm(_root.transform, "Top",
                new Vector2(ArmThickness, ArmLength),
                new Vector2(0f, ArmGap + ArmLength * 0.5f));
            CreateArm(_root.transform, "Bottom",
                new Vector2(ArmThickness, ArmLength),
                new Vector2(0f, -(ArmGap + ArmLength * 0.5f)));
            CreateArm(_root.transform, "Dot", new Vector2(2f, 2f), Vector2.zero);
        }

        private static void CreateArm(Transform parent, string name, Vector2 size, Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPosition;

            var img = go.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.9f);
            img.raycastTarget = false;
        }

        private void Update()
        {
            if (_root == null)
                return;

            SessionRuntime.Refresh();

            if (!SessionRuntime.AllowsShoot || !TryGetLocalAvatar(out var avatar) || !avatar.IsAlive)
            {
                _root.SetActive(false);
                return;
            }

            _root.SetActive(true);
        }

        private static bool TryGetLocalAvatar(out PlayerAvatar avatar)
        {
            avatar = null;
            foreach (var a in Object.FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
            {
                if (a.Object == null || !a.Object.IsValid || !a.HasInputAuthority)
                    continue;

                avatar = a;
                return true;
            }

            return false;
        }
    }
}
