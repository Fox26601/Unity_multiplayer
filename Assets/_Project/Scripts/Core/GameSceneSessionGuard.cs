using System.Collections;
using FusionMultiplayer.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// Game scene helper: overview camera while no player avatar exists; overlay when Play is pressed without a session.
    /// </summary>
    public sealed class GameSceneSessionGuard : MonoBehaviour
    {
        private Camera _overviewCamera;

        private IEnumerator Start()
        {
            yield return null;

            if (SceneManager.GetActiveScene().buildIndex != SceneIndices.Game)
                yield break;

            var runner = ConnectionManager.Instance != null ? ConnectionManager.Instance.Runner : null;
            if (runner != null && runner.IsRunning)
            {
                EnsureOverviewCamera();
                yield break;
            }

            HideGameHud();
            BuildNoSessionOverlay();
        }

        private void LateUpdate()
        {
            if (SceneManager.GetActiveScene().buildIndex != SceneIndices.Game)
                return;

            var main = Camera.main;
            if (main != null && main != _overviewCamera)
            {
                DisableOverviewCamera();
                return;
            }

            if (main == null)
                EnsureOverviewCamera();
        }

        private static void HideGameHud()
        {
            foreach (var charUi in Object.FindObjectsByType<CharacterSelectUI>(FindObjectsSortMode.None))
            {
                var canvas = charUi.GetComponent<Canvas>();
                if (canvas != null)
                    canvas.gameObject.SetActive(false);
            }
        }

        private void DisableOverviewCamera()
        {
            if (_overviewCamera == null) return;
            _overviewCamera.enabled = false;
            var listener = _overviewCamera.GetComponent<AudioListener>();
            if (listener != null)
                listener.enabled = false;
        }

        private void EnsureOverviewCamera()
        {
            if (_overviewCamera != null)
            {
                _overviewCamera.enabled = true;
                return;
            }

            if (Camera.main != null)
                return;

            var camGo = new GameObject("GameOverviewCamera");
            _overviewCamera = camGo.AddComponent<Camera>();
            _overviewCamera.tag = "MainCamera";
            _overviewCamera.clearFlags = CameraClearFlags.SolidColor;
            _overviewCamera.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f);
            _overviewCamera.nearClipPlane = 0.3f;
            _overviewCamera.farClipPlane = 200f;
            camGo.transform.position = new Vector3(0f, 20f, -16f);
            camGo.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
        }

        private static void BuildNoSessionOverlay()
        {
            var root = new GameObject("NoFusionSessionOverlay", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(UiReadabilityBootstrap));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = UiTypography.CanvasReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            UiCanvasFix.EnsureReadableCanvas(root.transform);

            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGo.transform.SetParent(root.transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            bgGo.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.98f);

            AddTitle(root.transform, UiCopy.SessionGuardTitle);

            var scrollGo = new GameObject("BodyScroll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(ScrollRect));
            scrollGo.transform.SetParent(root.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.04f, 0.04f);
            scrollRt.anchorMax = new Vector2(0.96f, 0.72f);
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;
            scrollGo.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.11f, 0.55f);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Mask));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = new Vector2(8f, 8f);
            viewportRt.offsetMax = new Vector2(-8f, -8f);
            viewportGo.GetComponent<Image>().color = Color.clear;
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI),
                typeof(ContentSizeFitter));
            bodyGo.transform.SetParent(viewportGo.transform, false);
            var bodyRt = bodyGo.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0f, 1f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.pivot = new Vector2(0.5f, 1f);
            bodyRt.offsetMin = new Vector2(4f, 0f);
            bodyRt.offsetMax = new Vector2(-4f, 0f);
            var body = bodyGo.GetComponent<TMP_Text>();
            body.text = UiCopy.SessionGuardBody;
            UiTypography.ApplyBody(body, TextAlignmentOptions.TopLeft);
            var fitter = bodyGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = bodyRt;
            scroll.viewport = viewportRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var camGo = new GameObject("GuardCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.depth = -100;
            cam.cullingMask = 0;
        }

        private static void AddTitle(Transform parent, string message)
        {
            var go = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.04f, 0.74f);
            rt.anchorMax = new Vector2(0.96f, 0.96f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var t = go.GetComponent<TMP_Text>();
            t.text = message;
            UiTypography.ApplyTitle(t);
        }
    }
}
