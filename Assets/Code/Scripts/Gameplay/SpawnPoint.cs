using Fusion;
using FusionHelpers;
using UnityEngine;

namespace TeamBasedShooter
{
    public class SpawnPoint : NetworkBehaviour
    {
        public Team BelongsTo;
        public bool IsFree { get; set; } = true;
        public PlayerRef OwnedBy { get; set; } = PlayerRef.None;

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
