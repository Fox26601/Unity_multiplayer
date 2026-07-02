using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>
    /// TMP on Canvas requires extra shader channels; without them text renders tiny or broken.
    /// </summary>
    public static class UiCanvasFix
    {
        private static readonly AdditionalCanvasShaderChannels RequiredChannels =
            AdditionalCanvasShaderChannels.TexCoord1
            | AdditionalCanvasShaderChannels.Normal
            | AdditionalCanvasShaderChannels.Tangent;

        public static void EnsureReadableCanvas(Transform canvasRoot)
        {
            if (canvasRoot == null) return;

            var canvas = canvasRoot.GetComponent<Canvas>();
            if (canvas != null)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.worldCamera = null;
                }

                ApplyCanvasSettings(canvas);
            }

            var scaler = canvasRoot.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = UiTypography.CanvasReferenceResolution;
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        public static void ApplyCanvasSettings(Canvas canvas)
        {
            if (canvas == null) return;

            if ((canvas.additionalShaderChannels & RequiredChannels) != RequiredChannels)
                canvas.additionalShaderChannels |= RequiredChannels;

#if UNITY_2022_2_OR_NEWER
            canvas.vertexColorAlwaysGammaSpace = true;
#endif
        }
    }
}
