using Fusion;
using UnityEngine;

namespace TeamBasedShooter
{
    public class AimingPoint : NetworkBehaviour
    {
        private ChangeDetector _changeDetector;

        [Networked]
        public Vector3 AimPointPosition { get; private set; }

        public override void Spawned()
        {
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        }

        public void Update()
        {
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(AimPointPosition):
                        transform.position = Vector3.Lerp(transform.position, AimPointPosition, Time.deltaTime * 10f);
                        break;
                }
            }
        }

        public void SetNewAimPoint(Vector3 aimRigPoint)
        {
            AimPointPosition = aimRigPoint;
        }
    }
}
