using System.Linq;
using Fusion;
using FusionHelpers;
using UnityEngine;

namespace TeamBasedShooter
{
    public class LevelBehaviour : MonoBehaviour
    {
        private SpawnPoint[] _playerSpawnPoints;

        private void Awake()
        {
            _playerSpawnPoints = GetComponentsInChildren<SpawnPoint>(true);
        }

        public SpawnPoint GetPlayerSpawnPoint(PlayerRef player, Team playerTeam)
        {
            var availableSpawn = _playerSpawnPoints.FirstOrDefault(point => (point.BelongsTo == playerTeam && point.IsFree) || (point.BelongsTo == playerTeam && point.OwnedBy == player));
            return availableSpawn.GetComponent<SpawnPoint>();
        }
    }
}
