using UnityEngine;
using Fusion;
using Fusion.Addons.Physics;
using System.Collections;
using System.Collections.Generic;
using Fusion.LagCompensation;

namespace TeamBasedShooter
{
    [RequireComponent(typeof(NetworkRigidbody3D))]
    public class TearGas : Weapon
    {
        [SerializeField] private float initialImpulse = 5f;
        [SerializeField] private float lifeTime = 60f;
        [SerializeField] private float detonationTime = 1f;
        [SerializeField] private GameObject smokeEffect;
        [SerializeField] private float damage = 50f;

        [Networked] private TickTimer LifeCooldown { get; set; }
        [Networked] private TickTimer DetonationCooldown { get; set; }
        [Networked] private NetworkBool IsDestroyed { get; set; }
        [Networked] private NetworkBool IsDetonated { get; set; }
        [Networked] private NetworkBool CanDamage { get; set; }
        [Networked] private NetworkBool AlreadyCollided { get; set; }

        [Networked] private Vector3 DamageAreaOrigin { get; set; }

        [Header("Visuals")]
        public Sprite icon;

        private NetworkRigidbody3D _rigidbody;
        private Collider _collider;

        private readonly List<LagCompensatedHit> _hits = new List<LagCompensatedHit>();

        public override int MaxClipAmmo { get; set; }

        [Networked]
        public override int ClipAmmo { get; set; }

        public override Sprite Icon { get; set; }

        public int maxClipAmmo = 1;

        protected void Awake()
        {
            _rigidbody = GetComponent<NetworkRigidbody3D>();
            _collider = GetComponentInChildren<Collider>();

            _collider.enabled = false;

            if (smokeEffect != null)
            {
                smokeEffect.SetActive(false);
            }
        }

        public override void Spawned()
        {
             if (HasStateAuthority)
            {
                MaxClipAmmo = maxClipAmmo;
                ClipAmmo = MaxClipAmmo;
            }

            Icon = icon;
        }

        public override void FixedUpdateNetwork()
        {
            _collider.enabled = IsDestroyed == false;

            if (LifeCooldown.IsRunning == true && LifeCooldown.Expired(Runner) == true)
            {
                IsDetonated = false;
                Runner.Despawn(Object);
            }

            if (CanDamage && DetonationCooldown.Expired(Runner) == true)
            {
                DamageAreaOrigin = transform.position;
                StartCoroutine(ApplyDamageOverTime(DamageAreaOrigin));
                CanDamage = false;
            }
        }

        public override void Render()
        {
            if (IsDetonated && DetonationCooldown.Expired(Runner) == true)
            {
                ShowSmokeEffect();
            }
        }

        public void InitPosition(Vector3 position, Quaternion rotation, Vector3 throwVelocity)
        {
            _rigidbody.Teleport(position, rotation);

            _rigidbody.Rigidbody.isKinematic = false;
            _rigidbody.Rigidbody.velocity = throwVelocity;

            // Set cooldown after which the tear gas should be despawned
            if (lifeTime > 0f)
            {
                LifeCooldown = TickTimer.CreateFromSeconds(Runner, lifeTime);
            }
        }

        private void ShowSmokeEffect()
        {
            smokeEffect.SetActive(true);
        }

        protected void OnCollisionEnter(Collision collision)
        {
            if (Object != null)
            {
                if (!AlreadyCollided)
                {
                    IsDetonated = true;

                    DetonationCooldown = TickTimer.CreateFromSeconds(Runner, detonationTime);

                    CanDamage = true;
                    AlreadyCollided = true;
                }
            }
        }

        private IEnumerator ApplyDamageOverTime(Vector3 center)
        {
            float duration = lifeTime;
            float tickRate = 0.5f;
            float timer = 0f;

            while (timer < duration)
            {
                DamageAsAreaOfEffect(center);
                timer += tickRate;
                yield return new WaitForSeconds(tickRate);
            }
        }

        private void DamageAsAreaOfEffect(Vector3 center)
        {
            float radius = 3f;

            var hitOptions = HitOptions.SubtickAccuracy | HitOptions.IgnoreInputAuthority;
            Runner.LagCompensation.OverlapSphere(center, radius, player: Object.InputAuthority, _hits, options: hitOptions);

            foreach (var h in _hits)
            {
                if (h.Hitbox == null) continue;

                Vector3 targetPosition = h.Hitbox.transform.position;
                Vector3 directionToTarget = (targetPosition - h.Point).normalized;

                ApplyDamage(h.Hitbox, targetPosition, directionToTarget, damage / 2);
            }
        }

        private void ApplyDamage(Hitbox enemyHitbox, Vector3 position, Vector3 direction, float damage)
        {
            if (enemyHitbox == null) return;

            var enemyHealth = enemyHitbox.Root.GetComponent<Health>();
            if (enemyHealth == null || enemyHealth.IsAlive == false) return;

            float damageMultiplier = enemyHitbox is BodyHitbox bodyHitbox ? bodyHitbox.DamageMultiplier : 1f;
            bool isCriticalHit = damageMultiplier > 1f;

            if (enemyHealth.ApplyDamage(Object.InputAuthority, damage * damageMultiplier, position, direction, isCriticalHit) == false)
            {
                return;
            }
        }
    }
}