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
        public const float DefaultMaxHealth = 100f;
        public const float RespawnDelaySeconds = 3f;
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

        private Color _baseTint = Color.white;
        private float _hitFlashUntil;
        private Renderer[] _visualRenderers;
        private Collider[] _hitColliders;
        private CharacterController _characterController;
        private bool _corpseVisible = true;

        public bool IsAlive => !IsDead && Health > 0f;

        private void Awake()
        {
            CacheCorpseParts();
        }

        private void CacheCorpseParts()
        {
            _characterController = GetComponent<CharacterController>();
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
                MaxHealth = DefaultMaxHealth;
                Health = MaxHealth;
                IsDead = false;
                PushFromLocalPlayerData();
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
            SetCorpseVisible(!IsDead);
            PlayerRegistry.RegisterAvatar(this);
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority)
            {
                PushFromLocalPlayerData();

                if (IsDead && _respawnTimer.Expired(Runner))
                    DoRespawn();
            }
        }

        /// <summary>Server-only hit application (projectile / weapon StateAuthority).</summary>
        public void ApplyServerHit(float damage, PlayerRef attacker, Vector3 hitOrigin)
        {
            if (!HasStateAuthority || damage <= 0f || IsDead)
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

            ApplyDamageInternal(CombatRules.RollDamage(damage), attacker);
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
            _respawnTimer = TickTimer.CreateFromSeconds(Runner, RespawnDelaySeconds);
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

            if (_characterController != null)
                _characterController.enabled = visible;

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
                _bodyRenderer.material.color = tint;
            }

            if (_nameTag != null && !HasInputAuthority)
                _nameTag.SetText(DisplayName.ToString());
        }

        private void OnHitCountChanged()
        {
            if (HitCount <= 0)
                return;

            _hitFlashUntil = Time.time + 0.18f;
            SpawnLocalHitBurst();
        }

        private void SpawnLocalHitBurst()
        {
            var origin = _bodyRenderer != null && _bodyRenderer.enabled
                ? _bodyRenderer.bounds.center
                : transform.position + Vector3.up;
            var burst = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            burst.name = "HitBurst";
            burst.transform.position = origin;
            burst.transform.localScale = Vector3.one * 0.35f;
            Destroy(burst.GetComponent<Collider>());
            var renderer = burst.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(1f, 0.35f, 0.2f, 0.85f);
            Destroy(burst, 0.25f);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            PlayerRegistry.UnregisterAvatar(this);

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

            var owner = Object.IsValid ? Object.InputAuthority : runner.LocalPlayer;
            if (owner != PlayerRef.None)
                gm.ReleaseCharacterSlot(CharacterSlot, owner);

            base.Despawned(runner, hasState);
        }
    }
}
