using FusionMultiplayer.Core;
using FusionMultiplayer.Player;
using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>
    /// Master-only end game control and session exit for all clients.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private GameObject _overlayRoot;
        [SerializeField] private Button _masterEndButton;
        [SerializeField] private Button _leaveButton;

        private void Awake()
        {
            UiCanvasFix.EnsureReadableCanvas(transform);
            if (_overlayRoot != null) _overlayRoot.SetActive(false);
            EnlargeMasterButton();
            if (_masterEndButton != null) _masterEndButton.onClick.AddListener(OnMasterEndClicked);
            if (_leaveButton != null) _leaveButton.onClick.AddListener(OnLeaveClicked);
            UiTypography.ApplyHierarchy(transform);
        }

        private void EnlargeMasterButton()
        {
            if (_masterEndButton == null)
                _masterEndButton = transform.Find("BtnEndGame")?.GetComponent<Button>();
            if (_masterEndButton == null) return;

            var rt = (RectTransform)_masterEndButton.transform;
            rt.anchorMin = new Vector2(0.82f, 0.58f);
            rt.anchorMax = new Vector2(1f, 0.62f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.offsetMin = new Vector2(8f, 0f);
            rt.offsetMax = new Vector2(-12f, 0f);
            UiTypography.StyleButton(_masterEndButton);
        }

        private void Update()
        {
            var runner = ConnectionManager.Instance != null ? ConnectionManager.Instance.Runner : null;
            GameSceneReadiness.TryGetGameManager(out var gm);
            var gmReady = gm != null;
            var over = gmReady && gm.GetIsGameOverSafe();

            if (_masterEndButton != null)
            {
                _masterEndButton.gameObject.SetActive(runner != null && runner.IsRunning && runner.IsSceneAuthority &&
                    !over);
            }

            if (_overlayRoot != null)
                _overlayRoot.SetActive(over);

            if (over)
                GameplayInputMode.SetMenu();
            else if (!GameplayInputMode.ChatBlockingGameplay && HasLocalAvatar())
                GameplayInputMode.SetGameplay();
        }

        private static bool HasLocalAvatar()
        {
            foreach (var avatar in Object.FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
            {
                if (avatar.Object != null && avatar.Object.IsValid && avatar.HasInputAuthority)
                    return true;
            }

            return false;
        }

        private void OnMasterEndClicked()
        {
            if (!GameSceneReadiness.TryGetGameManager(out var gm)) return;
            gm.MasterSetGameOver();
        }

        private async void OnLeaveClicked()
        {
            if (ConnectionManager.Instance != null)
                await ConnectionManager.Instance.ShutdownToMainMenuAsync();
        }
    }
}
