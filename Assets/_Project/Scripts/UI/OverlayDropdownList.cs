using System;
using Object = UnityEngine.Object;
using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>Shared dropdown list: floating page popup (main menu) or overlay reparent (chat PM).</summary>
    internal sealed class OverlayDropdownList
    {
        internal const float OptionHeight = 28f;
        internal const float ListPadding = 4f;

        private Button _fieldButton;
        private RectTransform _fieldRt;
        private RectTransform _listRoot;
        private RectTransform _listContent;
        private Transform _overlayParent;
        private Transform _floatingParent;
        private Transform _listHomeParent;
        private Action _onClose;
        private bool _isOpen;
        private bool _useFloatingPopup;

        public bool IsOpen => _isOpen;
        public RectTransform ListContent => _listContent;

        public void Bind(Button fieldButton, RectTransform listRoot, RectTransform listContent, Transform overlayParent,
            bool useFloatingPopup = false)
        {
            _fieldButton = fieldButton;
            _fieldRt = fieldButton != null ? fieldButton.GetComponent<RectTransform>() : null;
            _listRoot = listRoot;
            _listContent = listContent;
            _useFloatingPopup = useFloatingPopup;
            _overlayParent = useFloatingPopup ? null : ResolveOverlayRoot(overlayParent);
            _floatingParent = useFloatingPopup ? ResolvePageRoot(listRoot) : null;
            _listHomeParent = listRoot != null ? listRoot.parent : null;
        }

        public void WireToggle(Action toggle)
        {
            if (_fieldButton == null)
                return;

            _fieldButton.onClick.RemoveAllListeners();
            _fieldButton.onClick.AddListener(() => toggle?.Invoke());
            _fieldButton.interactable = true;
        }

        public void Open(int optionCount, Action onClose)
        {
            if (_useFloatingPopup)
            {
                OpenFloating(optionCount, onClose);
                return;
            }

            if (_listRoot == null || _fieldRt == null || _overlayParent == null || optionCount <= 0)
                return;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_fieldRt);

            _onClose = onClose;
            DropdownOverlayRegistry.RegisterOpen(this);

            if (_listRoot.parent != _overlayParent)
                _listRoot.SetParent(_overlayParent, false);

            var height = optionCount * OptionHeight + ListPadding * 2f;
            PositionListNearField(_overlayParent as RectTransform, height);
            ShowList(height);
        }

        private void OpenFloating(int optionCount, Action onClose)
        {
            if (_listRoot == null || _fieldRt == null || _floatingParent == null || optionCount <= 0)
                return;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_fieldRt);

            _onClose = onClose;
            DropdownOverlayRegistry.RegisterOpen(this);

            if (_listRoot.parent != _floatingParent)
                _listRoot.SetParent(_floatingParent, false);

            var height = optionCount * OptionHeight + ListPadding * 2f;
            PositionListNearField(_floatingParent as RectTransform, height);
            ShowList(height);
        }

        private void ShowList(float height)
        {
            _listRoot.SetAsLastSibling();
            _listRoot.sizeDelta = new Vector2(_listRoot.sizeDelta.x, height);

            var listCanvas = _listRoot.GetComponent<Canvas>();
            if (listCanvas != null)
            {
                listCanvas.overrideSorting = true;
                listCanvas.sortingOrder = UiRuntimeBuildKit.PmChannelListSortOrder;
            }

            _listRoot.gameObject.SetActive(true);
            _isOpen = true;
        }

        public void Close()
        {
            if (_listRoot != null)
                _listRoot.gameObject.SetActive(false);

            HideBackdrop();
            RestoreListHome();
            ResetHomeLayout();
            _isOpen = false;
            _onClose = null;
            DropdownOverlayRegistry.RegisterClosed(this);
        }

        public void ClearContent()
        {
            if (_listContent == null)
                return;

            for (var i = _listContent.childCount - 1; i >= 0; i--)
            {
                var child = _listContent.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Object.Destroy(child);
                else
                    Object.DestroyImmediate(child);
            }
        }

        public void ForceRebuildLayout()
        {
            if (_listContent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_listContent);
        }

        private void ResetHomeLayout()
        {
            if (_listRoot == null)
                return;

            _listRoot.anchorMin = new Vector2(0f, 1f);
            _listRoot.anchorMax = new Vector2(1f, 1f);
            _listRoot.pivot = new Vector2(0.5f, 0f);
            _listRoot.anchoredPosition = new Vector2(0f, -2f);
            _listRoot.sizeDelta = new Vector2(0f, 64f);
        }

        private static Transform ResolvePageRoot(Transform hint)
        {
            var current = hint;
            while (current != null)
            {
                if (current.name is "CreatePage" or "JoinPage" or "LandingPage")
                    return current;

                current = current.parent;
            }

            return hint != null ? hint.GetComponentInParent<Canvas>()?.transform : null;
        }

        private static Transform ResolveOverlayRoot(Transform hint)
        {
            if (hint == null)
                return null;

            if (hint.GetComponent<Canvas>() != null)
                return hint;

            var host = hint.GetComponentInParent<UIPanelHost>();
            if (host != null)
                return host.transform;

            var current = hint;
            while (current != null)
            {
                if (current.name == "Panel" && current is RectTransform)
                    return current;

                if (current.GetComponent<Canvas>() != null)
                    return current;

                current = current.parent;
            }

            return hint;
        }

        private void PositionListNearField(RectTransform parentRt, float height)
        {
            if (parentRt == null || _listRoot == null || _fieldRt == null)
                return;

            var corners = new Vector3[4];
            _fieldRt.GetWorldCorners(corners);

            var localBottomLeft = parentRt.InverseTransformPoint(corners[0]);
            var localTopLeft = parentRt.InverseTransformPoint(corners[1]);
            var localTopRight = parentRt.InverseTransformPoint(corners[2]);

            var width = Mathf.Max(localTopRight.x - localTopLeft.x, 160f);
            var centerX = (localTopLeft.x + localTopRight.x) * 0.5f;

            var spaceAbove = localTopLeft.y - parentRt.rect.yMin;
            var spaceBelow = parentRt.rect.yMax - localBottomLeft.y;
            var openAbove = spaceAbove >= height && spaceAbove >= spaceBelow;

            _listRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _listRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _listRoot.pivot = openAbove ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 1f);
            _listRoot.anchoredPosition = openAbove
                ? new Vector2(centerX, localTopLeft.y - 2f)
                : new Vector2(centerX, localBottomLeft.y + 2f);
            _listRoot.sizeDelta = new Vector2(width, height);
        }

        private void RestoreListHome()
        {
            if (_listRoot == null || _listHomeParent == null || _listRoot.parent == _listHomeParent)
                return;

            _listRoot.SetParent(_listHomeParent, false);
            ResetHomeLayout();
        }

        private void HideBackdrop()
        {
            if (_useFloatingPopup || _overlayParent == null)
                return;

            DropdownOverlayRegistry.HideSharedBackdrop(_overlayParent);
        }
    }
}
