using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Addons.Physics;
using System.Linq;

namespace TeamBasedShooter
{
    public class BuildingFragment : NetworkBehaviour
    {
        public GameObject[] FragmentVariants;
        public GameObject CurrentVariant;

        [Networked] public TickTimer DestroyTimer { get; set; }

        [SerializeField] private float lifeTime = 30f;

        private NetworkRigidbody3D _rigidbody;
        private MeshCollider meshCollider;

        [Networked] public int VisibleFragmentIndex { get; set; }
        [Networked] public Vector3 ExplosionOffset { get; set; }
        [Networked] public float ExplosionForce { get; set; }
        [Networked] public Vector3 ExplosionApplicationPoint { get; set; }

        protected void Awake()
        {
            _rigidbody = GetComponent<NetworkRigidbody3D>();
            meshCollider = GetComponent<MeshCollider>();
        }

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                VisibleFragmentIndex = UnityEngine.Random.Range(0, FragmentVariants.Length);
            }

            SetActiveVariant(VisibleFragmentIndex);
        }

        private void SetActiveVariant(int variantIndex)
        {
            foreach (var variant in FragmentVariants)
            {
                if (variant != null)
                    variant.SetActive(false);
            }

            CurrentVariant = FragmentVariants[variantIndex];
            CurrentVariant.SetActive(true);

            MeshFilter meshFilter = CurrentVariant.gameObject.GetComponentInChildren<MeshFilter>();
            if (meshFilter != null)
            {
                meshCollider.sharedMesh = meshFilter.sharedMesh;
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (DestroyTimer.IsRunning == true && DestroyTimer.Expired(Runner) == true)
            {
                FragmentPoolManager.Instance.ReturnFragment(this);
            }
        }

        public void Initialize(Vector3 position, Quaternion rotation, float force)
        {
            _rigidbody.Teleport(position, rotation);
            _rigidbody.Rigidbody.isKinematic = false;

            Vector3 explosionCenter = position;
            Vector3 fragmentToCenter = (position - explosionCenter).normalized;

            if (Object.HasStateAuthority)
            {
                ExplosionOffset = UnityEngine.Random.insideUnitSphere * 0.5f;
                ExplosionForce = force * UnityEngine.Random.Range(0.8f, 1.2f);
                ExplosionApplicationPoint = position + UnityEngine.Random.insideUnitSphere * 0.5f;
            }

            Vector3 forceDirection = (fragmentToCenter + ExplosionOffset).normalized;

            _rigidbody.Rigidbody.AddForceAtPosition(forceDirection * ExplosionForce, ExplosionApplicationPoint, ForceMode.Impulse);
            
            DestroyTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
        }

        public void Reset()
        {
            Vector3 resetPosition = new Vector3(1000, 10, 0);
            _rigidbody.Teleport(resetPosition, Quaternion.identity);
        }
    }
}