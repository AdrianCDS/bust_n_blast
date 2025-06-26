using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Fusion.LagCompensation;
using System;

namespace TeamBasedShooter
{
    public enum ProjectileDamageType
    {
        BlastRadius,
        AreaOfEffect
    }

    public class StandaloneProjectile : NetworkBehaviour
    {
        [SerializeField]
        private ProjectileDamageType _damageType;
        [SerializeField]
        private bool _hasAppliedGravity;
        [SerializeField]
        private float _appliedGravity = 1.0f;
        [SerializeField]
        private float _damage = 150f;
        [SerializeField]
        private float _speed = 50f;
        [SerializeField]
        private float _lifeTime = 4f;
        [SerializeField]
        private LayerMask _hitMask;
        [SerializeField]
        private LayerMask _blockMask;
        [SerializeField]
        private GameObject _hitEffect;
        [SerializeField]
        private float _lifeTimeAfterHit = 2f;
        [SerializeField]
        private GameObject _visualsRoot;
        [SerializeField]
        private TrailRenderer _trail;

        [Networked]
        private int _fireTick { get; set; }
        [Networked]
        private Vector3 _firePosition { get; set; }
        [Networked]
        private Vector3 _fireVelocity { get; set; }
        [Networked]
        private NetworkBool _isDestroyed { get; set; }
        [Networked]
        private TickTimer _lifeCooldown { get; set; }
        [Networked]
        private Vector3 _hitPosition { get; set; }

        private bool _isInitializedRender;
        private bool _isDestroyedRender;

        private readonly List<LagCompensatedHit> _playerHits = new List<LagCompensatedHit>();
        private readonly List<LagCompensatedHit> _environmentHits = new List<LagCompensatedHit>();

        public void Fire(Vector3 position, Vector3 direction)
        {
            _fireTick = Runner.Tick;
            _firePosition = position;
            _fireVelocity = direction * _speed;

            if (_lifeTime > 0f)
            {
                _lifeCooldown = TickTimer.CreateFromSeconds(Runner, _lifeTime);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (_lifeCooldown.IsRunning == true && _lifeCooldown.Expired(Runner) == true)
            {
                Runner.Despawn(Object);
                return;
            }

            if (_isDestroyed == true)
                return;

            var previousPosition = _hasAppliedGravity ? GetMovePositionWithFalloff(Runner.Tick - 1) : GetMovePosition(Runner.Tick - 1);
            var nextPosition = _hasAppliedGravity ? GetMovePositionWithFalloff(Runner.Tick) : GetMovePosition(Runner.Tick);

            var direction = nextPosition - previousPosition;

            float distance = direction.magnitude;
            direction /= distance; // Normalize

            var hitOptions = HitOptions.IncludePhysX | HitOptions.IgnoreInputAuthority;
            if (Runner.LagCompensation.Raycast(previousPosition, direction, distance,
                    Object.InputAuthority, out var hit, _hitMask, hitOptions) == true)
            {
                _isDestroyed = true;
                _lifeCooldown = TickTimer.CreateFromSeconds(Runner, _lifeTimeAfterHit);

                // Save hit position so hit effects are at correct position on proxies
                _hitPosition = hit.Point;

                switch (_damageType)
                {
                    case ProjectileDamageType.BlastRadius:
                        DamageAsBlastRadius(hit);
                        break;
                    case ProjectileDamageType.AreaOfEffect:
                        StartCoroutine(ApplyDamageOverTime(hit));
                        break;
                    default:
                        break;
                }
            }
        }

        public override void Render()
        {
            InitializeRender();

            if (_isDestroyed == true && _isDestroyedRender == false)
            {
                _isDestroyedRender = true;
                ShowDestroyEffect();
            }

            if (_isDestroyed == true)
                return;

            // For proxies we move projectiles in remote time frame, for input/state authority we use local time frame
            float renderTime = Object.IsProxy == true ? Runner.RemoteRenderTime : Runner.LocalRenderTime;
            float floatTick = renderTime / Runner.DeltaTime;

            transform.position = _hasAppliedGravity ? GetMovePositionWithFalloff(floatTick) : GetMovePosition(floatTick);
        }

        protected void Awake()
        {
            if (_hitEffect != null)
            {
                _hitEffect.SetActive(false);
            }

            if (_trail != null)
            {
                _trail.gameObject.SetActive(false);
            }
        }

        private void InitializeRender()
        {
            if (_isInitializedRender == true)
                return;

            // Set initial position and rotation on proxies
            if (IsProxy == true)
            {
                transform.position = _firePosition;
                transform.rotation = Quaternion.LookRotation(_fireVelocity);
            }

            if (_trail != null)
            {
                _trail.gameObject.SetActive(true);
                _trail.Clear();
            }

            _isInitializedRender = true;
        }

        private Vector3 GetMovePosition(float currentTick)
        {
            float time = (currentTick - _fireTick) * Runner.DeltaTime;

            if (time <= 0f)
                return _firePosition;

            return _firePosition + _fireVelocity * time;
        }

        private Vector3 GetMovePositionWithFalloff(float currentTick)
        {
            float time = (currentTick - _fireTick) * Runner.DeltaTime;

            if (time <= 0f)
                return _firePosition;

            Vector3 gravityEffect = _appliedGravity * (time * time) * Physics.gravity;

            return _firePosition + _fireVelocity * time + gravityEffect;
        }

        private void ShowDestroyEffect()
        {
            transform.position = _hitPosition;

            if (_hitEffect != null)
            {
                _hitEffect.SetActive(true);
            }

            _visualsRoot.SetActive(false);
        }

        private void DamageAsBlastRadius(LagCompensatedHit hit)
        {
            float explosionRadius = 7.5f; // Slightly bigger than the visuals
            float maxDamage = _damage;
            float minDamage = 25f;

            // Adjust explosion point slightly in the opposite direction of impact to avoid edge cases
            Vector3 explosionPoint = hit.Point + hit.Normal * 0.1f;

            var hitOptions = HitOptions.SubtickAccuracy | HitOptions.IncludePhysX | HitOptions.IgnoreInputAuthority;
            Runner.LagCompensation.OverlapSphere(explosionPoint, explosionRadius, player: Object.InputAuthority, _playerHits, options: hitOptions);
            Runner.LagCompensation.OverlapSphere(explosionPoint, 1.0f, player: Object.InputAuthority, _environmentHits, options: hitOptions);

            foreach (var h in _playerHits)
            {
                if (h.Hitbox == null) continue;

                // Calculate radius parameters
                Vector3 targetPosition = h.Point;
                Vector3 directionToTarget = (targetPosition - explosionPoint).normalized;
                float distanceToTarget = Vector3.Distance(targetPosition, explosionPoint);

                // Calculate damage falloff
                float damageFactor = 1f - Mathf.Clamp01(distanceToTarget / explosionRadius);
                float damage = Mathf.Lerp(minDamage / 2, maxDamage / 2, damageFactor);

                // Check if there's an obstacle blocking the explosion
                if (Runner.LagCompensation.Raycast(explosionPoint, directionToTarget, distanceToTarget, Object.InputAuthority, out var blockHit, _blockMask, hitOptions))
                {
                    if (blockHit.Type == HitType.PhysX)
                    {
                        // If the ray hit a wall before reaching the player, ignore damage to player
                        continue;
                    }
                }

                ApplyDamage(h.Hitbox, targetPosition, directionToTarget, damage);
            }

            foreach (var h in _environmentHits)
            {
                if (h.Hitbox != null) continue;

                if (h.GameObject.TryGetComponent<Destroyable>(out var destroyable))
                {
                    destroyable.ApplyDamage(_damage);
                }
            }
        }

        private IEnumerator ApplyDamageOverTime(LagCompensatedHit aoeCenter)
        {
            float duration = _lifeTimeAfterHit;
            float tickRate = 0.5f;
            float timer = 0f;

            while (timer < duration)
            {
                DamageAsAreaOfEffect(aoeCenter);
                timer += tickRate;
                yield return new WaitForSeconds(tickRate);
            }
        }

        private void DamageAsAreaOfEffect(LagCompensatedHit hit)
        {
            float radius = 3.25f;

            var hitOptions = HitOptions.SubtickAccuracy | HitOptions.IgnoreInputAuthority;
            Runner.LagCompensation.OverlapSphere(hit.Point, radius, player: Object.InputAuthority, _playerHits, options: hitOptions);

            foreach (var h in _playerHits)
            {
                if (h.Hitbox == null) continue;

                Vector3 targetPosition = h.Hitbox.transform.position;
                Vector3 directionToTarget = (targetPosition - h.Point).normalized;

                ApplyDamage(h.Hitbox, targetPosition, directionToTarget, _damage / 2);
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
