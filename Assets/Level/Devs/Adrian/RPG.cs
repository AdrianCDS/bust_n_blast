using Fusion;
using UnityEngine;
using FusionHelpers;

namespace TeamBasedShooter
{
    public class RPG : Weapon
    {
        [Header("Fire Setup")]
        public float Damage = 10f;
        public int FireRate = 200;
        public int ProjectilesPerShot = 1;
        public float Dispersion = 0f;
        public LayerMask HitMask;
        public float MaxHitDistance = 200f;


        public override int MaxClipAmmo { get; set; }
        public override int StartAmmo { get; set; }

        [Header("Ammo")]
        public int maxClipAmmo = 12;
        public int startAmmo = 72;
        public float ReloadTime = 1.5f;

        public override Sprite Icon { get; set; }

        [Header("Visuals")]
        public Sprite icon;

        [Header("Fire Effect")]
        public Transform FirePoint;
        public StandaloneProjectile ProjectilePrefab;
        public GameObject MuzzleShotEffectPrefab;
        public Transform MuzzlePoint;
        public GameObject DummyRocket;

        [Header("Sounds")]
        public AudioSource FireSound;
        public AudioSource ReloadSound;

        public bool HasAmmo => ClipAmmo > 0 || RemainingAmmo > 0;

        [Networked]
        public override int ClipAmmo { get; set; }
        [Networked]
        public override int RemainingAmmo { get; set; }

        [Networked]
        public override NetworkBool IsReloading { get; set; }

        [Networked]
        private int _fireCount { get; set; }
        [Networked]
        private TickTimer _fireCooldown { get; set; }
        [Networked, Capacity(32)]
        private NetworkArray<ProjectileData> _projectileData { get; }

        private int _fireTicks;
        private int _visibleFireCount;

        private bool startedReloading;

        private GameObject muzzleShotEffect;

        private struct ProjectileData : INetworkStruct
        {
            public Vector3 HitPosition;
            public Vector3 HitNormal;
            public NetworkBool ShowHitEffect;
        }

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                MaxClipAmmo = maxClipAmmo;
                StartAmmo = startAmmo;

                ClipAmmo = Mathf.Clamp(StartAmmo, 0, MaxClipAmmo);
                RemainingAmmo = StartAmmo - ClipAmmo;
            }

            Icon = icon;

            _visibleFireCount = _fireCount;

            float fireTime = 60f / FireRate;
            _fireTicks = Mathf.CeilToInt(fireTime / Runner.DeltaTime);


            muzzleShotEffect = Instantiate(MuzzleShotEffectPrefab, MuzzlePoint);
            muzzleShotEffect.SetActive(false);
        }

        public override void FixedUpdateNetwork()
        {
            if (ClipAmmo == 0)
            {
                Reload();
            }

            if (IsReloading && _fireCooldown.ExpiredOrNotRunning(Runner))
            {
                IsReloading = false;

                if (Object.HasStateAuthority)
                {
                    RPC_SetRocketVisible(true);
                }

                int reloadAmmo = MaxClipAmmo - ClipAmmo;
                reloadAmmo = Mathf.Min(reloadAmmo, RemainingAmmo);

                ClipAmmo += reloadAmmo;
                RemainingAmmo -= reloadAmmo;

                _fireCooldown = TickTimer.CreateFromSeconds(Runner, 0.25f);
            }
        }

        public override void Render()
        {
            if (_visibleFireCount < _fireCount)
            {
                PlayFireEffect();
            }

            if (startedReloading != IsReloading)
            {
                if (IsReloading)
                {
                    ReloadSound.Play();
                }

                startedReloading = IsReloading;
            }

            _visibleFireCount = _fireCount;
        }

        public override void Reload()
        {
            if (ClipAmmo >= MaxClipAmmo)
                return;
            if (RemainingAmmo <= 0)
                return;
            if (IsReloading)
                return;
            if (_fireCooldown.ExpiredOrNotRunning(Runner) == false)
                return;

            IsReloading = true;
            _fireCooldown = TickTimer.CreateFromSeconds(Runner, ReloadTime);
        }

        public override float GetReloadProgress()
        {
            if (IsReloading == false)
                return 1f;

            return 1f - _fireCooldown.RemainingTime(Runner).GetValueOrDefault() / ReloadTime;
        }

        public override void Attack(Vector3 firePosition, Vector3 fireDirection, bool justPressed, bool wasReleased)
        {
            if (IsReloading) return;

            if (justPressed == false) return;

            if (_fireCooldown.ExpiredOrNotRunning(Runner) == false) return;

            if (ClipAmmo <= 0) return;

            Random.InitState(Runner.Tick * unchecked((int)Object.Id.Raw));

            for (int i = 0; i < ProjectilesPerShot; i++)
            {
                var projectileDirection = fireDirection;

                if (Dispersion > 0f)
                {
                    var dispersionRotation = Quaternion.Euler(Random.insideUnitSphere * Dispersion);
                    projectileDirection = dispersionRotation * fireDirection;
                }

                FireProjectile(firePosition, projectileDirection);
            }

            _fireCooldown = TickTimer.CreateFromTicks(Runner, _fireTicks);
            ClipAmmo--;
        }

        private void FireProjectile(Vector3 firePosition, Vector3 fireDirection)
        {
            if (Object.HasStateAuthority)
            {
                RPC_SetRocketVisible(false);
            }

            if (HasInputAuthority)
            {
                RPC_SpawnRocketProjectile(firePosition, fireDirection);
            }

            _fireCount++;
        }

        private void PlayFireEffect()
        {
            if (FireSound)
            {
                FireSound.PlayOneShot(FireSound.clip);
            }

            muzzleShotEffect.SetActive(false);
            muzzleShotEffect.SetActive(true);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_SetRocketVisible(bool isVisible)
        {
            DummyRocket.SetActive(isVisible);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_SpawnRocketProjectile(Vector3 firePosition, Vector3 fireDirection)
        {
            var projectile = Runner.Spawn(ProjectilePrefab, FirePoint.position, FirePoint.rotation, Object.InputAuthority);
            projectile.Fire(firePosition, fireDirection);
        }
    }
}
