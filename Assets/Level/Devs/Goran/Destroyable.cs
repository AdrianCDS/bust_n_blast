using Fusion;
using UnityEngine;
using FusionHelpers;

namespace TeamBasedShooter
{
    public class Destroyable : NetworkBehaviour
    {
        [SerializeField] private float MaxHealth = 100f;
        [SerializeField] private GameObject buildingVisuals;
        [SerializeField] private GameObject destroyedBuildingVisuals;

        [Networked] public float CurrentHealth { get; set; }

        [Networked] public NetworkBool IsInvincible { get; set; }
        [Networked] public NetworkBool IsDestroyed { get; set; }

        [Networked] public Vector3 Position { get; set; }
        [Networked] public Vector3 Rotation { get; set; }

        private MeshCollider meshCollider;

        public int FragmentCount = 2;
        public float ExplodeForce = 1;

        public BuildingFragment fragmentPrefab;

        private bool initializedRender;

        protected void Awake()
        {
            meshCollider = GetComponent<MeshCollider>();
        }

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                CurrentHealth = MaxHealth;
            }

            ShowActive();
        }

        public override void Render()
        {
            InitializeRender();

            transform.position = Position;
            transform.rotation = Quaternion.Euler(Rotation.x, Rotation.y, Rotation.z);

            if (CurrentHealth <= 0f && !IsDestroyed)
            {
                ShowDestroyed();
            }
        }

        private void InitializeRender()
        {
            if (initializedRender == true) return;

            if (IsProxy == true)
            {
                transform.position = Position;
                transform.rotation = Quaternion.Euler(Rotation.x, Rotation.y, Rotation.z);
            }

            initializedRender = true;
        }

        public void Place(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation * Vector3.forward;
        }

        public void ApplyDamage(float damage)
        {
            if (IsInvincible) return;

            CurrentHealth -= damage;

            if (CurrentHealth <= 0f)
            {
                CurrentHealth = 0f;
                OnDeath();
            }
        }

        private void OnDeath()
        {
            if (HasStateAuthority)
            {
                // When a Destroyable is gone, give points to the attackers
                if (Runner.TryGetSingleton(out GameManager gameManager))
                {
                    gameManager.AttackersTotalPoints += 200;
                }

                RPC_SpawnFragmentDebris();
            }
        }

        private void ShowActive()
        {
            if (destroyedBuildingVisuals != null)
            {
                buildingVisuals.SetActive(true);
                destroyedBuildingVisuals.SetActive(false);

            }

            UpdateMeshCollider(buildingVisuals);
            IsDestroyed = false;
        }

        private void ShowDestroyed()
        {
            if (destroyedBuildingVisuals != null)
            {
                buildingVisuals.SetActive(false);
                destroyedBuildingVisuals.SetActive(true);

                UpdateMeshCollider(destroyedBuildingVisuals);

                IsDestroyed = true;
            }
        }

        private void UpdateMeshCollider(GameObject targetVisuals)
        {
            MeshFilter meshFilter = targetVisuals.GetComponentInChildren<MeshFilter>();
            if (meshFilter != null)
            {
                meshCollider.sharedMesh = meshFilter.sharedMesh;
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_SpawnFragmentDebris()
        {
            for (var c = 0; c < FragmentCount; c++)
            {
                BuildingFragment fragment = FragmentPoolManager.Instance.GetFragment(transform.position, transform.rotation, ExplodeForce);
            }

            if (destroyedBuildingVisuals == null)
            {
                Runner.Despawn(Object);
            }
        }
    }
}
