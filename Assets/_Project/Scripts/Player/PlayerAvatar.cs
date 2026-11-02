using Fusion;
using FusionMultiplayer.Core;
using FusionMultiplayer.Data;
using UnityEngine;

namespace FusionMultiplayer.Player
{
    /// <summary>
    /// Networked player body with visuals driven by replicated tint/name.
    /// Locomotion uses <see cref="NetworkCharacterController"/> (Fusion TRSP).
    /// </summary>
    [RequireComponent(typeof(NetworkCharacterController))]
    public class PlayerAvatar : NetworkBehaviour
    {
        public const float DefaultMaxHealth = 100f;
        public const float RespawnDelaySeconds = 3f;

        public static float ActiveMaxHealth =>
            GameConfigs.EnsureCurrent()?.PlayerStats != null
                ? GameConfigs.Current.PlayerStats.MaxHealth
                : DefaultMaxHealth;

        public static float ActiveRespawnDelaySeconds =>
            GameConfigs.EnsureCurrent()?.PlayerStats != null
                ? GameConfigs.Current.PlayerStats.RespawnDelaySeconds
                : RespawnDelaySeconds;
        private const float MaxHitDistance = 40f;

        [Networked] public int CharacterSlot { get; set; }
        [Networked] public Color VisualTint { get; set; }
        [Networked] public NetworkString<_32> DisplayName { get; set; }
        [Networked] public float MaxHealth { get; set; }
        [Networked] public float Health { get; set; }
        [Networked] public NetworkBool IsDead { get; set; }
        [Networked, OnChangedRender(nameof(OnHitCountChanged))]
        public int HitCount { get; set; }

        [Networked] private TickTimer _respawnTimer { get; set; }

        [SerializeField] private Renderer _bodyRenderer;
        [SerializeField] private NameTag _nameTag;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Color _baseTint = Color.white;
        private Color _appliedTint;
        private float _hitFlashUntil;
        private Renderer[] _visualRenderers;
        private Collider[] _hitColliders;
        private CharacterController _characterController;
        private NetworkCharacterController _networkCharacterController;
        private bool _corpseVisible = true;
        private MaterialPropertyBlock _mpb;
        private string _lastNameTag;
        private bool _releaseWhenHasAuthority;

        public bool IsAlive => !IsDead && Health > 0f;

        private void Awake()
        {
            CacheCorpseParts();
        }

        private void CacheCorpseParts()
        {
            _characterController = GetComponent<CharacterController>();
            _networkCharacterController = GetComponent<NetworkCharacterController>();
            _hitColliders = GetComponentsInChildren<Collider>(true);
            _visualRenderers = GetComponentsInChildren<Renderer>(true);
            if (_nameTag == null)
                _nameTag = GetComponentInChildren<NameTag>(true);
        }

        public override void Spawned()
        {
            if (_visualRenderers == null || _hitColliders == null)
                CacheCorpseParts();

            if (HasStateAuthority)
            {
                MaxHealth = ActiveMaxHealth;
                Health = MaxHealth;
                IsDead = false;
                PushFromLocalPlayerData();
            }
            else if (Object.InputAuthority == Runner.LocalPlayer)
            {
                Object.RequestStateAuthority();
            }

            var camTr = transform.Find("PlayerCamera");
            if (camTr != null)
            {
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
            }

            if (_nameTag != null)
                _nameTag.gameObject.SetActive(!HasInputAuthority && !IsDead);

            _baseTint = VisualTint;
            _appliedTint = new Color(-1f, -1f, -1f, -1f);
            _lastNameTag = null;
            if (_mpb == null)
                _mpb = new MaterialPropertyBlock();
            SetCorpseVisible(!IsDead);
            PlayerRegistry.RegisterAvatar(this);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority && Object != null && Object.IsValid &&
                Object.InputAuthority == Runner.LocalPlayer)
            {
                Object.RequestStateAuthority();
            }

            if (_releaseWhenHasAuthority && HasStateAuthority)
            {
                _releaseWhenHasAuthority = false;
                ReleaseBodyLocal();
                return;
            }

            if (HasStateAuthority)
            {
                PushFromLocalPlayerData();

                if (IsDead && _respawnTimer.Expired(Runner))
                    DoRespawn();
            }
        }

        /// <summary>
        /// Shooter asks Shared master to validate + roll damage; victim SA applies rolled amount.
        /// </summary>
        public void RequestHit(float damage, PlayerRef attacker, Vector3 hitOrigin)
        {
            if (damage <= 0f || Object == null || !Object.IsValid)
                return;

            var victimRef = PlayerOwnership.ResolveLogicalOwner(this);
            if (victimRef == PlayerRef.None)
                return;

            var gm = GameManager.Instance;
            if (gm != null && gm.IsNetworkActive)
                gm.RequestMasterHit(victimRef, damage, attacker, hitOrigin);
        }

        /// <summary>Victim SA applies master-rolled damage (no local RNG).</summary>
        public void ApplyMasterHitLocal(float rolledDamage, PlayerRef attacker, Vector3 hitOrigin)
        {
            if (!HasStateAuthority || rolledDamage <= 0f || IsDead)
                return;

            SessionRuntime.Refresh(Runner);
            if (!SessionRuntime.AllowsShoot)
                return;

            var victimRef = PlayerOwnership.ResolveLogicalOwner(this);
            if (attacker == PlayerRef.None || victimRef == PlayerRef.None || attacker == victimRef)
                return;

            var planar = hitOrigin - transform.position;
            planar.y = 0f;
            if (planar.sqrMagnitude > MaxHitDistance * MaxHitDistance)
                return;

            ApplyDamageInternal(rolledDamage, attacker);
        }

        private void ApplyDamageInternal(float amount, PlayerRef attacker)
        {
            if (!HasStateAuthority || amount <= 0f || IsDead || !SessionRuntime.AllowsShoot)
                return;

            SessionRuntime.Refresh(Runner);
            if (!SessionRuntime.AllowsShoot)
                return;

            Health = Mathf.Max(0f, Health - amount);
            HitCount++;

            if (Health <= 0f)
                HandleDeath(attacker);
        }

        private void HandleDeath(PlayerRef killer)
        {
            if (!HasStateAuthority || IsDead)
                return;

            IsDead = true;
            Health = 0f;
            _respawnTimer = TickTimer.CreateFromSeconds(Runner, ActiveRespawnDelaySeconds);
            SetCorpseVisible(false);

            var victimRef = PlayerOwnership.ResolveLogicalOwner(this);
            if (victimRef != PlayerRef.None)
                IncrementPlayerDeaths(victimRef);

            if (killer != PlayerRef.None && killer != victimRef)
                IncrementPlayerScore(killer);
        }

        private void DoRespawn()
        {
            if (!HasStateAuthority)
                return;

            var gm = GameManager.Instance;
            if (gm != null && CharacterSlot >= 0 && CharacterSlot < PlayerRegistry.MaxCharacterSlots)
            {
                var sp = gm.GetSpawnPoint(CharacterSlot);
                if (_networkCharacterController == null)
                    _networkCharacterController = GetComponent<NetworkCharacterController>();
                if (_networkCharacterController != null)
                    _networkCharacterController.Teleport(sp.position, sp.rotation);
                else
                    transform.SetPositionAndRotation(sp.position, sp.rotation);
            }

            Health = MaxHealth;
            IsDead = false;
            _respawnTimer = default;
            SetCorpseVisible(true);
        }

        /// <summary>
        /// Hides body/hitbox while dead so the corpse does not stay on the map.
        /// Keeps local PlayerCamera enabled.
        /// </summary>
        private void SetCorpseVisible(bool visible)
        {
            if (_corpseVisible == visible && _visualRenderers != null)
                return;

            _corpseVisible = visible;

            if (_visualRenderers != null)
            {
                for (var i = 0; i < _visualRenderers.Length; i++)
                {
                    if (_visualRenderers[i] != null)
                        _visualRenderers[i].enabled = visible;
                }
            }

            if (_hitColliders != null)
            {
                for (var i = 0; i < _hitColliders.Length; i++)
                {
                    if (_hitColliders[i] != null)
                        _hitColliders[i].enabled = visible;
                }
            }

            // Proxies: NCC interpolates. Local IA/SA keep CC enabled for simulation.
            if (_characterController != null)
                _characterController.enabled = visible && (HasStateAuthority || HasInputAuthority);

            if (_nameTag != null)
                _nameTag.gameObject.SetActive(visible && !HasInputAuthority);
        }

        private static void IncrementPlayerScore(PlayerRef player)
        {
            var pd = PlayerOwnership.FindPlayerData(player);
            if (pd == null)
                return;

            pd.ServerAwardScore(1);
        }

        private static void IncrementPlayerDeaths(PlayerRef player)
        {
            var pd = PlayerOwnership.FindPlayerData(player);
            if (pd == null)
                return;

            pd.ServerRegisterDeath(1);
        }

        private void PushFromLocalPlayerData()
        {
            var owner = PlayerOwnership.ResolveLogicalOwner(this);
            var pd = PlayerOwnership.FindPlayerData(owner, CharacterSlot);
            if (pd == null)
                return;

            VisualTint = pd.Tint;
            DisplayName = pd.Nick;
        }

        public override void Render()
        {
            SetCorpseVisible(!IsDead);

            if (IsDead)
                return;

            _baseTint = VisualTint;

            if (_bodyRenderer != null)
            {
                var tint = _baseTint;
                if (Time.time < _hitFlashUntil)
                    tint = Color.Lerp(_baseTint, Color.red, 0.65f);

                if (tint != _appliedTint)
                {
                    _appliedTint = tint;
                    if (_mpb == null)
                        _mpb = new MaterialPropertyBlock();
                    _bodyRenderer.GetPropertyBlock(_mpb);
                    _mpb.SetColor(ColorId, tint);
                    _mpb.SetColor(BaseColorId, tint);
                    _bodyRenderer.SetPropertyBlock(_mpb);
                }
            }

            if (_nameTag != null && !HasInputAuthority)
            {
                var name = DisplayName.ToString();
                if (name != _lastNameTag)
                {
                    _lastNameTag = name;
                    _nameTag.SetText(name);
                }
            }
        }

        private void OnHitCountChanged()
        {
            if (HitCount <= 0)
                return;

            _hitFlashUntil = Time.time + 0.18f;
            // Force tint re-apply next Render for hit flash (no CreatePrimitive alloc).
            _appliedTint = new Color(-1f, -1f, -1f, -1f);
        }

        /// <summary>
        /// Shared Mode: master (or any peer) asks the SA owner to stop bot AI and despawn this body.
        /// If SA is in limbo, session authority requests it and despawns on the next tick.
        /// </summary>
        public void RequestReleaseBody()
        {
            if (Object == null || !Object.IsValid)
                return;

            if (HasStateAuthority)
            {
                ReleaseBodyLocal();
                return;
            }

            RPC_ReleaseBody();

            if (Runner != null && NetworkAuthority.IsSessionAuthority(Runner))
            {
                _releaseWhenHasAuthority = true;
                Object.RequestStateAuthority();
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_ReleaseBody()
        {
            ReleaseBodyLocal();
        }

        private void ReleaseBodyLocal()
        {
            if (!HasStateAuthority || Object == null || !Object.IsValid || Runner == null)
                return;

            _releaseWhenHasAuthority = false;

            var brain = GetComponent<BotBrain>();
            if (brain != null)
                brain.Deactivate();

            Runner.Despawn(Object);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            PlayerRegistry.UnregisterAvatar(this);
            _releaseWhenHasAuthority = false;

            var gm = GameManager.Instance;
            if (runner == null || gm == null || !runner.IsRunning || runner.IsShutdown)
            {
                base.Despawned(runner, hasState);
                return;
            }

            if (CharacterSlot < 0 || CharacterSlot >= PlayerRegistry.MaxCharacterSlots)
            {
                base.Despawned(runner, hasState);
                return;
            }

            // Bot seats clear InputAuthority on leave — still free CharacterOwners via slot owner.
            var owner = Object.IsValid ? Object.InputAuthority : PlayerRef.None;
            if (owner == PlayerRef.None)
                owner = gm.GetCharacterOwner(CharacterSlot);
            if (owner != PlayerRef.None)
                gm.ReleaseCharacterSlot(CharacterSlot, owner);
            else
                gm.SetCharacterOwner(CharacterSlot, PlayerRef.None);

            base.Despawned(runner, hasState);
        }
    }
}
