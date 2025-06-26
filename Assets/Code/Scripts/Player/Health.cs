using Fusion;
using UnityEngine;
using FusionHelpers;

namespace TeamBasedShooter
{
    public class Health : NetworkBehaviour
    {
        public Player player;
        public NPC npc;

        private GameManager _gameManager;

        public float MaxHealth = 100f;

        public bool IsAlive => CurrentHealth > 0f;

        [Networked]
        public float CurrentHealth { get; set; }

        [Networked]
        private int _hitCount { get; set; }
        [Networked]
        private Vector3 _lastHitPosition { get; set; }
        [Networked]
        private Vector3 _lastHitDirection { get; set; }

        private int _visibleHitCount;

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                CurrentHealth = MaxHealth;
            }

            if (Runner.TryGetSingleton(out GameManager gameManager))
            {
                _gameManager = gameManager;
            }

            _visibleHitCount = _hitCount;
        }

        public override void Render()
        {
            _visibleHitCount = _hitCount;
        }

        public bool ApplyDamage(PlayerRef killerPlayerRef, float damage, Vector3 position, Vector3 direction, bool isCritical)
        {
            if (_gameManager.PlayersInSameTeam(Object.InputAuthority, killerPlayerRef) || _gameManager.DefenderAttacksNPC(npc != null, killerPlayerRef)) return false;

            if (CurrentHealth <= 0f) return false;

            CurrentHealth -= damage;

            if (CurrentHealth <= 0f)
            {
                CurrentHealth = 0f;

                OnDeath(killerPlayerRef);
            }

            _lastHitPosition = position - transform.position;
            _lastHitDirection = -direction;

            _hitCount++;

            return true;
        }

        private void OnDeath(PlayerRef killerPlayerRef)
        {
            PlayerEvents.NotifyOnKillConfirmed(killerPlayerRef);

            if (npc != null)
            {
                npc.OnDeath();
            }
            else
            {
                player.OnDeath();
            }
        }
    }
}
