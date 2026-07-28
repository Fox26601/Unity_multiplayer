using UnityEngine;

namespace FusionMultiplayer.Player
{
    /// <summary>
    /// World-space nickname label (legacy TextMesh).
    /// </summary>
    [RequireComponent(typeof(TextMesh))]
    public class NameTag : MonoBehaviour
    {
        private TextMesh _textMesh;
        private Camera _cachedCamera;
        private string _lastText;

        private void Awake()
        {
            _textMesh = GetComponent<TextMesh>();
            _textMesh.anchor = TextAnchor.MiddleCenter;
            _textMesh.alignment = TextAlignment.Center;
            _textMesh.fontSize = 72;
            _textMesh.characterSize = 0.14f;
            _textMesh.fontStyle = FontStyle.Bold;
            _textMesh.color = Color.white;
            _textMesh.offsetZ = -0.02f;
        }

        private void LateUpdate()
        {
            if (_cachedCamera == null || !_cachedCamera.isActiveAndEnabled)
                _cachedCamera = Camera.main;
            if (_cachedCamera == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - _cachedCamera.transform.position,
                Vector3.up);
        }

        public void SetText(string value)
        {
            value ??= string.Empty;
            if (value == _lastText)
                return;
            _lastText = value;
            if (_textMesh == null) _textMesh = GetComponent<TextMesh>();
            if (_textMesh != null) _textMesh.text = value;
        }
    }
}
