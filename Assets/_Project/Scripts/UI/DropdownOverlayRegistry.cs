using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>Tracks overlay dropdowns and owns one shared backdrop per overlay root.</summary>
    internal static class DropdownOverlayRegistry
    {
        private static readonly List<OverlayDropdownList> Active = new();
        private static readonly Dictionary<int, Button> SharedBackdrops = new();

        public static void RegisterOpen(OverlayDropdownList dropdown)
        {
            if (dropdown == null)
                return;

            for (var i = Active.Count - 1; i >= 0; i--)
            {
                if (Active[i] != null && Active[i] != dropdown && Active[i].IsOpen)
                    Active[i].Close();
            }

            if (!Active.Contains(dropdown))
                Active.Add(dropdown);
        }

        public static void RegisterClosed(OverlayDropdownList dropdown)
        {
            if (dropdown == null)
                return;

            Active.Remove(dropdown);
        }

        public static void CloseAll()
        {
            for (var i = Active.Count - 1; i >= 0; i--)
                Active[i]?.Close();
            Active.Clear();
        }

        /// <summary>Hide orphan lists/backdrops left on the overlay layer (blocks clicks to form controls below).</summary>
        public static void CleanupOverlayLayer(Transform overlayRoot)
        {
            if (overlayRoot == null)
                return;

            for (var i = overlayRoot.childCount - 1; i >= 0; i--)
                overlayRoot.GetChild(i).gameObject.SetActive(false);

            if (SharedBackdrops.TryGetValue(overlayRoot.GetInstanceID(), out var backdrop) && backdrop != null)
                backdrop.gameObject.SetActive(false);
        }

        internal static Button GetSharedBackdrop(Transform overlayRoot)
        {
            if (overlayRoot == null)
                return null;

            var id = overlayRoot.GetInstanceID();
            if (SharedBackdrops.TryGetValue(id, out var existing) && existing != null)
                return existing;

            var go = new GameObject(UiRuntimeBuildKit.DropdownBackdropName, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(overlayRoot, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.01f);
            // Must not block clicks to form controls on the sibling layer below.
            img.raycastTarget = false;

            var button = go.GetComponent<Button>();
            button.targetGraphic = img;
            button.interactable = false;
            go.SetActive(false);
            SharedBackdrops[id] = button;
            return button;
        }

        internal static void HideSharedBackdrop(Transform overlayRoot)
        {
            if (overlayRoot == null)
                return;

            if (SharedBackdrops.TryGetValue(overlayRoot.GetInstanceID(), out var backdrop) && backdrop != null)
                backdrop.gameObject.SetActive(false);
        }
    }
}
