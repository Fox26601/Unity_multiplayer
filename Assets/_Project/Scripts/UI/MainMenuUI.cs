using System.Threading.Tasks;
using FusionMultiplayer.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionMultiplayer.UI
{
    /// <summary>
    /// Main menu: nickname, tint, room name, create/join session.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _nicknameField;
        [SerializeField] private TMP_InputField _roomField;
        [SerializeField] private Button _createButton;
        [SerializeField] private Button _joinButton;
        [SerializeField] private Image _colorPreview;
        [SerializeField] private Button _randomColorButton;
        [SerializeField] private SessionFlowUI _sessionFlow;

        private InputField _nicknameLegacy;
        private InputField _roomLegacy;
        private bool _wired;

        private void OnEnable()
        {
            MainMenuRuntimeRebuild.EnsureBuilt(transform);
        }

        private void Awake()
        {
            MainMenuRuntimeRebuild.EnsureBuilt(transform);
            WireUi();
            if (_sessionFlow == null) _sessionFlow = GetComponent<SessionFlowUI>();
            if (_sessionFlow == null) _sessionFlow = gameObject.AddComponent<SessionFlowUI>();
        }

        /// <summary>Called by MainMenuRuntimeRebuild after it creates TMP UI widgets.</summary>
        public void BindRuntimeTmp(TMP_InputField nickname, TMP_InputField room, Button create, Button join,
            Image colorPreview, Button randomColor)
        {
            _nicknameField = nickname;
            _roomField = room;
            _nicknameLegacy = null;
            _roomLegacy = null;
            _createButton = create;
            _joinButton = join;
            _colorPreview = colorPreview;
            _randomColorButton = randomColor;
            _wired = false;
            WireUi();
        }

        /// <summary>Legacy uGUI fields (fallback).</summary>
        public void BindRuntime(InputField nickname, InputField room, Button create, Button join,
            Image colorPreview, Button randomColor)
        {
            _nicknameLegacy = nickname;
            _roomLegacy = room;
            _createButton = create;
            _joinButton = join;
            _colorPreview = colorPreview;
            _randomColorButton = randomColor;
            _nicknameField = null;
            _roomField = null;
            _wired = false;
            WireUi();
        }

        private void WireUi()
        {
            if (_wired) return;
            ResolveRefs();
            if (_createButton == null && _joinButton == null) return;

            _createButton?.onClick.RemoveListener(OnCreateClicked);
            _joinButton?.onClick.RemoveListener(OnJoinClicked);
            _randomColorButton?.onClick.RemoveListener(OnRandomColor);
            if (_createButton != null) _createButton.onClick.AddListener(OnCreateClicked);
            if (_joinButton != null) _joinButton.onClick.AddListener(OnJoinClicked);
            if (_randomColorButton != null) _randomColorButton.onClick.AddListener(OnRandomColor);
            _wired = true;
        }

        private void ResolveRefs()
        {
            var panel = transform.Find("Panel");
            if (panel == null) return;

            if (_nicknameField == null)
                _nicknameField = panel.Find("NicknameField")?.GetComponent<TMP_InputField>();
            if (_nicknameField == null)
                _nicknameLegacy = panel.Find("NicknameField")?.GetComponent<InputField>();

            if (_roomField == null)
                _roomField = panel.Find("RoomField")?.GetComponent<TMP_InputField>();
            if (_roomField == null)
                _roomLegacy = panel.Find("RoomField")?.GetComponent<InputField>();

            if (_createButton == null)
                _createButton = panel.Find("BtnCreate")?.GetComponent<Button>();
            if (_joinButton == null)
                _joinButton = panel.Find("BtnJoin")?.GetComponent<Button>();
            if (_randomColorButton == null)
                _randomColorButton = panel.Find("BtnRandomColor")?.GetComponent<Button>();
            if (_colorPreview == null)
                _colorPreview = panel.Find("ColorPreview")?.GetComponent<Image>();
        }

        private void Start()
        {
            var nick = GetNicknameText();
            if (string.IsNullOrEmpty(nick)) SetNicknameText(SessionData.Nickname);
            if (string.IsNullOrEmpty(GetRoomText())) SetRoomText("Room1");
            UpdatePreview();
        }

        private void OnRandomColor()
        {
            SessionData.Tint = Random.ColorHSV(0f, 1f, 0.55f, 1f, 0.75f, 1f);
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (_colorPreview != null) _colorPreview.color = SessionData.Tint;
        }

        private void OnCreateClicked() => _ = StartSessionAsync();

        private void OnJoinClicked() => _ = StartSessionAsync();

        private async Task StartSessionAsync()
        {
            if (_sessionFlow != null && _sessionFlow.IsConnecting) return;

            ApplySessionFields();
            SetMenuInteractable(false);

            if (ConnectionManager.Instance == null)
            {
                _sessionFlow?.SetStatus("ConnectionManager is missing in this scene.", true);
                SetMenuInteractable(true);
                return;
            }

            var room = GetRoomText();
            if (string.IsNullOrWhiteSpace(room)) room = "Room1";
            var ok = await ConnectionManager.Instance.StartSessionAsync(room);
            if (!ok)
                SetMenuInteractable(true);
        }

        private void SetMenuInteractable(bool interactable)
        {
            if (_createButton != null) _createButton.interactable = interactable;
            if (_joinButton != null) _joinButton.interactable = interactable;
            if (_randomColorButton != null) _randomColorButton.interactable = interactable;
            if (_nicknameField != null) _nicknameField.interactable = interactable;
            if (_roomField != null) _roomField.interactable = interactable;
            if (_nicknameLegacy != null) _nicknameLegacy.interactable = interactable;
            if (_roomLegacy != null) _roomLegacy.interactable = interactable;
        }

        private void ApplySessionFields()
        {
            var nick = GetNicknameText();
            SessionData.Nickname = !string.IsNullOrWhiteSpace(nick) ? nick.Trim() : "Player";
        }

        private string GetNicknameText()
        {
            if (_nicknameField != null) return _nicknameField.text;
            return _nicknameLegacy != null ? _nicknameLegacy.text : string.Empty;
        }

        private void SetNicknameText(string value)
        {
            if (_nicknameField != null) _nicknameField.text = value;
            else if (_nicknameLegacy != null) _nicknameLegacy.text = value;
        }

        private string GetRoomText()
        {
            if (_roomField != null) return _roomField.text;
            return _roomLegacy != null ? _roomLegacy.text : string.Empty;
        }

        private void SetRoomText(string value)
        {
            if (_roomField != null) _roomField.text = value;
            else if (_roomLegacy != null) _roomLegacy.text = value;
        }
    }
}
