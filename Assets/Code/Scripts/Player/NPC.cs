using System.Collections.Generic;
using Fusion;
using Fusion.Addons.SimpleKCC;
using FusionHelpers;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace TeamBasedShooter
{
    public class NPC : NetworkBehaviour
    {
        [Header("Setup")]
        private SimpleKCC KCC;
        public GameObject[] NpcVariants;
        public GameObject CurrentVariant;
        public AudioSource WomanScreamSound;
        public AudioSource ManScreamSound;
        public AudioSource DeathSound;

        [Header("Components")]
        public HitboxRoot HitboxRoot;
        public Health Health;

        [Networked]
        public int CurrentVariantIndex { get; set; }

        [Header("Movement")]
        public float MoveSpeed = 6f;
        public float UpGravity = 15f;
        public float DownGravity = 25f;
        public float GroundAcceleration = 55f;
        public float GroundDeceleration = 25f;
        public float AirAcceleration = 25f;
        public float AirDeceleration = 1.3f;
        public float JumpImpulse;
        public float JumpForce = 5f;

        [Networked]
        private Vector3 MoveVelocity { get; set; }

        public Animator CurrentAnimator;
        private AIHandler aiHandler;

        [Networked]
        private int _deathCount { get; set; }

        private int _visibleDeathCount;

        private void Awake()
        {
            KCC = GetComponent<SimpleKCC>();
            aiHandler = GetComponent<AIHandler>();
        }

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                CurrentVariantIndex = Random.Range(0, NpcVariants.Length);
            }

            SetActiveVariant(CurrentVariantIndex);
        }

        private void SetActiveVariant(int variantIndex)
        {
            // Deactivate all variants first
            foreach (var variant in NpcVariants)
            {
                if (variant != null)
                    variant.SetActive(false);
            }

            if (CurrentVariantIndex == 0 || CurrentVariantIndex == 1)
            {
                WomanScreamSound.Play();
            }
            else
            {
                ManScreamSound.Play();
            }

            // Activate the selected variant
            CurrentVariant = NpcVariants[variantIndex];
            CurrentVariant.SetActive(true);
            CurrentAnimator = CurrentVariant.GetComponent<Animator>();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {

        }

        public override void FixedUpdateNetwork()
        {
            if (Object.HasStateAuthority)
            {
                // CheckRespawn();
            }

            if (Health.IsAlive == false)
            {
                Move();

                // KCC.SetColliderLayer(LayerMask.NameToLayer("Ignore Raycast"));
                // KCC.SetCollisionLayerMask(LayerMask.GetMask("Environment", "Ground"));

                return;
            }

            Move(MoveSpeed);
        }

        public override void Render()
        {
            if (!CurrentAnimator) return;

            var moveVelocity = GetAnimationMoveVelocity();

            CurrentAnimator.SetFloat("InputX", moveVelocity.x, 0.05f, Time.deltaTime);
            CurrentAnimator.SetFloat("InputY", moveVelocity.z, 0.05f, Time.deltaTime);
            CurrentAnimator.SetBool("IsGrounded", KCC.IsGrounded);
            CurrentAnimator.SetFloat("MovementSpeed", moveVelocity.magnitude);
            CurrentAnimator.SetBool("IsAlive", Health.IsAlive);
            CurrentAnimator.SetBool("IsMoving", moveVelocity.x != 0 && moveVelocity.z != 0);

            if (_visibleDeathCount < _deathCount)
            {
                DeathSound.PlayOneShot(DeathSound.clip);
            }

            _visibleDeathCount = _deathCount;
        }

        public void Move(float moveSpeed = 0f)
        {
            Vector3 directionToTarget = aiHandler.GetDirectionToTarget(Health.IsAlive, out float distanceToTarget);

            if (directionToTarget != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
                KCC.SetLookRotation(lookRotation);
            }

            KCC.SetGravity(KCC.RealVelocity.y >= 0f ? -UpGravity : -DownGravity);

            JumpImpulse = 0f;
            if (KCC.IsGrounded && KCC.RealVelocity.magnitude < 0.1f && !aiHandler.ReachedShield && Health.IsAlive)
            {
                JumpImpulse = JumpForce;
                aiHandler.IsMovingToTarget = false;
            }

            Vector3 inverseDirection = KCC.Transform.InverseTransformDirection(directionToTarget);
            Vector2 moveDirection = new Vector2(inverseDirection.x, inverseDirection.z);

            var direction = KCC.TransformRotation * new Vector3(moveDirection.x, 0f, moveDirection.y);
            var moveVelocity = direction * moveSpeed;

            float acceleration;

            if (moveVelocity == Vector3.zero)
            {
                acceleration = KCC.IsGrounded == true ? GroundDeceleration : AirDeceleration;
            }
            else
            {
                acceleration = KCC.IsGrounded == true ? GroundAcceleration : AirAcceleration;
            }

            MoveVelocity = Vector3.Lerp(MoveVelocity, moveVelocity, acceleration * Runner.DeltaTime);
            KCC.Move(MoveVelocity, JumpImpulse);

            if (distanceToTarget < 2)
            {
                aiHandler.IsMovingToTarget = false;

                if (aiHandler.target != null && aiHandler.NextPointIsShield)
                {
                    aiHandler.ReachedShield = true;
                }
            }
        }

        public void OnDeath()
        {
            _deathCount++;

            aiHandler.Stop();

            if (Runner.TryGetSingleton(out GameManager gameManager))
            {
                gameManager.AttackersTotalPoints += 25;
            }
        }

        public void Revive()
        {
            Health.CurrentHealth = Health.MaxHealth;

            if (Runner.TryGetSingleton(out GameManager gameManager))
            {
                gameManager.DefendersTotalPoints += 50;
            }
        }

        private Vector3 GetAnimationMoveVelocity()
        {
            if (KCC.RealSpeed < 0.01f)
                return default;

            var velocity = KCC.RealVelocity;
            velocity.y = 0f;

            if (velocity.sqrMagnitude > 1f)
            {
                velocity.Normalize();
            }

            return transform.InverseTransformVector(velocity);
        }
    }
}
