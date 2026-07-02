using Fusion;
using FusionMultiplayer.Core;
using UnityEngine;

namespace FusionMultiplayer.Player
{
    /// <summary>
    /// Networked player body with visuals driven by replicated tint/name.
    /// </summary>
    [RequireComponent(typeof(NetworkTransform))]
    public class PlayerAvatar : NetworkBehaviour
    {
        [Networked] public int CharacterSlot { get; set; }
        [Networked] public Color VisualTint { get; set; }
        [Networked] public NetworkString<_32> DisplayName { get; set; }

        [SerializeField] private Renderer _bodyRenderer;
        [SerializeField] private NameTag _nameTag;

        public override void Spawned()
        {
            if (HasStateAuthority)
                PushFromLocalPlayerData();

            var camTr = transform.Find("PlayerCamera");
            if (camTr == null) return;
            var cam = camTr.GetComponent<Camera>();
            var al = camTr.GetComponent<AudioListener>();
            var local = HasInputAuthority;
            if (local)
            {
                foreach (var other in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                    other.enabled = false;

                if (GetComponent<GameplayCursorController>() == null)
                    gameObject.AddComponent<GameplayCursorController>();

                GameplayInputMode.SetGameplay();
            }

            if (cam != null) cam.enabled = local;
            if (al != null) al.enabled = local;
            if (local) camTr.gameObject.tag = "MainCamera";

            if (_nameTag != null)
                _nameTag.gameObject.SetActive(!HasInputAuthority);
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority)
                PushFromLocalPlayerData();
        }

        private void PushFromLocalPlayerData()
        {
            foreach (var pd in FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
            {
                if (pd.Object == null || !pd.Object.IsValid) continue;
                if (pd.Object.InputAuthority != Runner.LocalPlayer) continue;
                VisualTint = pd.Tint;
                DisplayName = pd.Nick;
                return;
            }
        }

        public override void Render()
        {
            if (_bodyRenderer != null)
                _bodyRenderer.material.color = VisualTint;

            if (_nameTag != null && !HasInputAuthority)
                _nameTag.SetText(DisplayName.ToString());
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            var gm = GameManager.Instance;
            if (runner == null || gm == null || !runner.IsRunning || runner.IsShutdown)
            {
                base.Despawned(runner, hasState);
                return;
            }

            if (CharacterSlot < 0 || CharacterSlot >= 10)
            {
                base.Despawned(runner, hasState);
                return;
            }

            var owner = Object.IsValid ? Object.InputAuthority : runner.LocalPlayer;
            if (owner != PlayerRef.None)
                gm.ReleaseCharacterSlot(CharacterSlot, owner);

            base.Despawned(runner, hasState);
        }
    }
}
