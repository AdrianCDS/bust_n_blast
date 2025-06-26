using UnityEngine;
using Fusion;

namespace TeamBasedShooter
{
    public class PlacingTool : Weapon
    {
        [Header("Object settings")]
        [SerializeField] private ProtectionShield objectPrefab;
        [SerializeField] private GameObject ghostPrefab;

        [Header("Placement settings")]
        [SerializeField] private LayerMask layerMask;
        [SerializeField] private float placementDistance = 5f;
        [SerializeField] private float cooldown = 7f;

        private GameObject currentGhost;
        private Camera playerCamera;

        public override Sprite Icon { get; set; }

        [Header("Visuals")]
        [SerializeField] private Sprite icon;

        [Networked] public NetworkBool IsBuilding { get; set; }

        [Networked] public TickTimer PlacingCooldown { get; set; }

        private float objectWidth;
        private float objectHeight;

        public override int MaxClipAmmo { get; set; }

        [Networked] public override int ClipAmmo { get; set; }

        [Networked] public override NetworkBool IsReloading { get; set; }

        public int maxClipAmmo = 1;

        public override void Spawned()
        {
            Icon = icon;

            if (HasStateAuthority)
            {
                MaxClipAmmo = maxClipAmmo;
                ClipAmmo = MaxClipAmmo;
            }

            if (Object.HasInputAuthority)
            {
                playerCamera = Camera.main;
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (Object.HasInputAuthority)
            {
                if (IsBuilding && currentGhost != null)
                {
                    UpdateGhostPosition();
                }
            }

            if (IsReloading && PlacingCooldown.ExpiredOrNotRunning(Runner))
            {
                IsReloading = false;
            }
        }

        public override void Attack(bool justPressed, bool wasReleased)
        {
            if (justPressed == false) return;
            if (PlacingCooldown.ExpiredOrNotRunning(Runner) == false) return;

            Random.InitState(Runner.Tick * unchecked((int)Object.Id.Raw));

            if (IsBuilding)
            {
                ExitBuildingMode();
            }
            else
            {
                EnterBuildingMode();
            }
        }

        public override float GetReloadProgress()
        {
            if (PlacingCooldown.ExpiredOrNotRunning(Runner))
                return 1f;

            return 1f - PlacingCooldown.RemainingTime(Runner).GetValueOrDefault() / cooldown;
        }

        private void EnterBuildingMode()
        {
            IsBuilding = true;

            if (currentGhost == null)
            {
                currentGhost = Instantiate(ghostPrefab);
                currentGhost.transform.position = new Vector3(0f, -1000f, 0f);

                Renderer renderer = currentGhost.GetComponent<Renderer>();

                if (renderer)
                {
                    objectWidth = renderer.bounds.size.x;
                    objectHeight = renderer.bounds.size.y;
                }
            }
        }

        private void ExitBuildingMode()
        {
            IsBuilding = false;

            if (currentGhost != null)
            {
                ConfirmPlacement();
                Destroy(currentGhost);
                currentGhost = null;
            }
        }

        private void UpdateGhostPosition()
        {
            if (currentGhost == null || playerCamera == null) return;

            Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

            if (Physics.Raycast(ray, out RaycastHit hit, placementDistance, layerMask))
            {
                currentGhost.transform.position = hit.point;

                currentGhost.transform.position -= currentGhost.transform.right * (objectWidth / 2);
                currentGhost.transform.position += Vector3.up * (objectHeight / 2);

                Vector3 directionToCamera = playerCamera.transform.position - hit.point;
                directionToCamera.y = 0;

                currentGhost.transform.rotation = Quaternion.LookRotation(-directionToCamera);
            }
        }

        private void ConfirmPlacement()
        {
            Vector3 position = new(currentGhost.transform.position.x, currentGhost.transform.position.y - 0.1f, currentGhost.transform.position.z);

            Quaternion rotation = currentGhost.transform.rotation;

            if (HasInputAuthority)
            {
                RPC_SpawnObject(position, rotation);
            }

            PlacingCooldown = TickTimer.CreateFromSeconds(Runner, cooldown);
            IsReloading = true;
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_SpawnObject(Vector3 position, Quaternion rotation)
        {
            ProtectionShield shield = Runner.Spawn(objectPrefab, position, rotation, Object.InputAuthority);
            shield.Place(position, rotation);
        }
    }
}
