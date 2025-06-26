using Fusion;
using UnityEngine;
using FusionHelpers;

namespace TeamBasedShooter
{
    public class Rifle : Weapon
    {
        [Header("Fire Setup")]
        public bool IsAutomatic = true;
        public float Damage = 10f;
        public int FireRate = 400;
        public int ProjectilesPerShot = 1;
        public float Dispersion = 0f;
        public LayerMask HitMask;
        public float MaxHitDistance = 300f;


        public override int MaxClipAmmo { get; set; }
        public override int StartAmmo { get; set; }

        [Header("Ammo")]
        public int maxClipAmmo = 64;
        public int startAmmo = 384;
        public float ReloadTime = 1.5f;

        public override Sprite Icon { get; set; }

        [Header("Visuals")]
        public Sprite icon;

        [Header("Fire Effect")]
        public Transform FirePoint;
        public Projectile ProjectilePrefab;
        public GameObject MuzzleShotEffectPrefab;

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

        private int _fireTicks;
        private int _visibleFireCount;

        private bool startedReloading;

        private GameObject muzzleShotEffect;

        [Networked, Capacity(32)]
        private NetworkArray<ProjectileData> _projectileData { get; }

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

            muzzleShotEffect = Instantiate(MuzzleShotEffectPrefab, FirePoint);
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

            for (int i = _visibleFireCount; i < _fireCount; i++)
            {
                var data = _projectileData[i % _projectileData.Length];

                var projectileVisual = Instantiate(ProjectilePrefab, FirePoint.position, FirePoint.rotation);
                projectileVisual.SetHit(data.HitPosition, data.HitNormal, data.ShowHitEffect);
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

            if (justPressed == false && IsAutomatic == false) return;

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
            var projectileData = new ProjectileData();

            var hitOptions = HitOptions.IncludePhysX | HitOptions.IgnoreInputAuthority;

            if (Runner.LagCompensation.Raycast(firePosition, fireDirection, MaxHitDistance, Object.InputAuthority, out var hit, HitMask, hitOptions))
            {
                projectileData.HitPosition = hit.Point;
                projectileData.HitNormal = hit.Normal;
                projectileData.ShowHitEffect = true;

                if (hit.Hitbox != null)
                {
                    ApplyDamage(hit.Hitbox, hit.Point, fireDirection);
                }
            }

            _projectileData.Set(_fireCount % _projectileData.Length, projectileData);
            _fireCount++;
        }

        private void ApplyDamage(Hitbox enemyHitbox, Vector3 position, Vector3 direction)
        {
            var enemyHealth = enemyHitbox.Root.GetComponent<Health>();
            if (enemyHealth == null || enemyHealth.IsAlive == false) return;

            float damageMultiplier = enemyHitbox is BodyHitbox bodyHitbox ? bodyHitbox.DamageMultiplier : 1f;
            bool isCriticalHit = damageMultiplier > 1f;

            float damage = Damage * damageMultiplier;

            if (enemyHealth.ApplyDamage(Object.InputAuthority, damage, position, direction, isCriticalHit) == false)
            {
                return;
            }
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
    }
}
