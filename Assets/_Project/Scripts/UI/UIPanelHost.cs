using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>
    /// Panel root: cached RectTransform and background image. Lives in the player assembly so generated scenes work in Play Mode and builds (Editor-only scripts must not be placed on scene objects).
    /// </summary>
    public sealed class UIPanelHost : MonoBehaviour
    {
        public RectTransform rectTransform;
        public Image image;
    }
}
