using Fusion;
using UnityEngine;
using FusionHelpers;

namespace TeamBasedShooter
{
    public class ProtectionShield : NetworkBehaviour
    {

        [Networked] public Vector3 Position { get; set; }
        [Networked] public Vector3 Rotation { get; set; }
        [Networked] private TickTimer LifeCooldown { get; set; }

        [SerializeField] private float lifeTime = 30f;

        private bool initializedRender;

        public override void Spawned()
        {
            Events.NotifyProtectionShieldPlaced();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            Events.NotifyProtectionShieldRemoved(transform);
        }

        public override void FixedUpdateNetwork()
        {
            if (LifeCooldown.IsRunning == true && LifeCooldown.Expired(Runner) == true)
            {
                Runner.Despawn(Object);
            }
        }

        public override void Render()
        {
            InitializeRender();

            transform.position = Position;
            transform.rotation = Quaternion.Euler(Rotation.x, Rotation.y, Rotation.z);
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

            if (lifeTime > 0f)
            {
                LifeCooldown = TickTimer.CreateFromSeconds(Runner, lifeTime);
            }
        }
    }
}
