using UnityEngine;
using Fusion;

namespace TeamBasedShooter
{
    public class Throwable : Weapon
    {
        [Header("Object settings")]
        [SerializeField] private TearGas objectPrefab;
        [SerializeField] private float throwForce = 15f;
        [SerializeField] private float maxThrowDistance = 20f;
        [SerializeField] private float minThrowDistance = 5f;
        [SerializeField] private float trajectoryGravityScale = 1f;
        [SerializeField] private int trajectorySegments = 15;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private Transform throwPosition;
        [SerializeField] private float cooldown = 1f;

        [Header("Visuals")]
        [SerializeField] private Sprite icon;

        [Networked] public NetworkBool IsAiming { get; set; }
        [Networked] public Vector3 ThrowVelocity { get; set; }
        [Networked] public override NetworkBool IsReloading { get; set; }
        [Networked] public TickTimer ThrowCooldown { get; set; }

        private Camera playerCamera;

        private Vector3 startPosition;
        private Vector3 endPosition;
        private Vector3 throwDirection;

        private LineRenderer trajectoryLine;

        private Vector3[] trajectoryPoints;

        public override Sprite Icon { get; set; }

        public override int MaxClipAmmo { get; set; }

        [Networked]
        public override int ClipAmmo { get; set; }

        public int maxClipAmmo = 1;

        public override void Spawned()
        {
            Icon = icon;

            if (HasStateAuthority)
            {
                MaxClipAmmo = maxClipAmmo;
                ClipAmmo = MaxClipAmmo;
            }

            // Get reference to the player camera
            if (Object.HasInputAuthority)
            {
                playerCamera = Camera.main;

                // Initialize trajectory visuals
                trajectoryLine = gameObject.AddComponent<LineRenderer>();
                trajectoryLine.positionCount = trajectorySegments;
                trajectoryLine.startWidth = 0.1f;
                trajectoryLine.endWidth = 0.05f;
                trajectoryLine.material = new Material(Shader.Find("Sprites/Default"));
                trajectoryLine.startColor = Color.white;
                trajectoryLine.endColor = new Color(1, 1, 1, 0.5f);

                trajectoryLine.enabled = false;
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (Object.HasInputAuthority)
            {
                if (IsAiming)
                {
                    UpdateTrajectory();
                }
            }

            if (IsReloading && ThrowCooldown.ExpiredOrNotRunning(Runner))
            {
                IsReloading = false;
            }
        }

        public override void Attack(bool justPressed, bool wasReleased)
        {
            if (justPressed == false) return;
            if (ThrowCooldown.ExpiredOrNotRunning(Runner) == false) return;

            Random.InitState(Runner.Tick * unchecked((int)Object.Id.Raw));

            if (IsAiming)
            {
                Throw();
            }
            else
            {
                ShowTrajectory();
            }
        }

        public override float GetReloadProgress()
        {
            if (ThrowCooldown.ExpiredOrNotRunning(Runner))
                return 1f;

            return 1f - ThrowCooldown.RemainingTime(Runner).GetValueOrDefault() / cooldown;
        }

        private void ShowTrajectory()
        {
            IsAiming = true;

            startPosition = transform.position + transform.forward + transform.up * 1f;

            if (trajectoryLine != null)
            {
                trajectoryLine.enabled = true;
            }
        }

        private void Throw()
        {
            IsAiming = false;

            if (trajectoryLine != null)
            {
                trajectoryLine.enabled = false;
            }

            // Call RPC to spawn the object on the server
            if (HasInputAuthority)
            {
                RPC_SpawnObject(startPosition, transform.rotation, ThrowVelocity);
            }

            ThrowCooldown = TickTimer.CreateFromSeconds(Runner, cooldown);
            IsReloading = true;
        }

        private void UpdateTrajectory()
        {
            if (playerCamera == null) return;

            startPosition = transform.position + transform.forward + transform.up * 1f;

            Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            throwDirection = ray.direction;

            float mouseDistanceFactor = Mathf.Clamp01(Input.mousePosition.y / Screen.height);
            float throwDistance = Mathf.Lerp(minThrowDistance, maxThrowDistance, mouseDistanceFactor);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, throwDistance, groundMask))
            {
                endPosition = hit.point;
            }
            else
            {
                endPosition = startPosition + throwDirection * throwDistance;
            }

            if (trajectoryLine != null)
            {
                DrawTrajectory(startPosition, endPosition);
            }
        }

        private void DrawTrajectory(Vector3 start, Vector3 end)
        {
            Vector3 direction = (end - start).normalized;
            float distance = Vector3.Distance(start, end);

            float velocityMagnitude = Mathf.Sqrt(distance * Physics.gravity.magnitude / trajectoryGravityScale);
            Vector3 initialVelocity = direction * velocityMagnitude;

            ThrowVelocity = initialVelocity;

            trajectoryPoints = new Vector3[trajectorySegments];
            for (int i = 0; i < trajectorySegments; i++)
            {
                float t = i / (float)(trajectorySegments - 1);
                float timePoint = t * (distance / velocityMagnitude) * 2;

                trajectoryPoints[i] = start + initialVelocity * timePoint +
                                      0.5f * Physics.gravity * trajectoryGravityScale * timePoint * timePoint;
            }

            trajectoryLine.SetPositions(trajectoryPoints);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_SpawnObject(Vector3 startingPosition, Quaternion rotation, Vector3 throwVelocity)
        {
            TearGas tearGas = Runner.Spawn(objectPrefab, startingPosition, rotation, Object.InputAuthority);
            tearGas.InitPosition(startingPosition, rotation, throwVelocity);
        }
    }
}
