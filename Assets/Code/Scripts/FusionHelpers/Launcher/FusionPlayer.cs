using System;
using Fusion;
using TeamBasedShooter;
using System.Collections.Generic;
using UnityEngine;
using Fusion.Photon.Realtime;

namespace FusionHelpers
{
    public enum Team
    {
        Attackers,
        Defenders,
        None
    }

    public enum Character
    {
        Bulwark,
        Maverick,
        Zaphyr,
        Nadja
    }

    public abstract class FusionPlayer : NetworkBehaviour
    {
        [Networked] public int NetworkedPlayerIndex { private get; set; }
        [Networked] public bool Ready { get; set; }
        [Networked] public NetworkString<_128> PlayerUniqueId { get; set; }
        [Networked] public bool PlayerLoggedIn { get; set; }

        public PlayerRef PlayerId { get; set; } = PlayerRef.None;
        public int PlayerIndex { get; set; } = -1;
        public string PlayerDeviceId { get; set; } = "";

        [Networked] public Team PlayerTeam { get; set; } = Team.Defenders;
        [Networked] public Character PlayerCharacter { get; set; } = Character.Maverick;

        [Networked] public Player PrefabToUse { get; set; }

        private TickAlignedEventRelay _eventStub;

        [Networked] public string PlayerRankXp { get; set; }
        [Networked] public string PlayerRank { get; set; }
        [Networked] public string PlayerBalance { get; set; }

        public override void Spawned()
        {
            PlayerId = Object.InputAuthority;
            PlayerIndex = NetworkedPlayerIndex;
            PlayerDeviceId = (string)PlayerUniqueId;

            _eventStub = gameObject.AddComponent<TickAlignedEventRelay>();

            Runner.WaitForSingleton<FusionSession>(session => session.AddPlayerAvatar(this));
        }

        public virtual void Respawn(float inSeconds = 0) { }

        public virtual void TeleportOut() { }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            Runner.WaitForSingleton<FusionSession>(session => session.RemovePlayerAvatar(this));
        }

        protected void RegisterEventListener<T>(Action<T> listener) where T : unmanaged, INetworkEvent
        {
            _eventStub.RegisterEventListener(listener);
        }

        public void RaiseEvent<T>(T evt) where T : unmanaged, INetworkEvent
        {
            FusionPlayer stateAuth = Runner.GetPlayerObject(Runner.LocalPlayer).GetComponent<FusionPlayer>();
            stateAuth._eventStub.RaiseEventFor(_eventStub, evt);
        }

        public void SwitchCharacter(int characterIndex)
        {
            PlayerCharacter = (Character)characterIndex;

            switch (PlayerCharacter)
            {
                case Character.Bulwark:
                    PlayerTeam = Team.Defenders;
                    break;
                case Character.Maverick:
                    PlayerTeam = Team.Defenders;
                    break;
                case Character.Zaphyr:
                    PlayerTeam = Team.Attackers;
                    break;
                case Character.Nadja:
                    PlayerTeam = Team.Attackers;
                    break;
            }
        }

        public void ToggleReady()
        {
            // Don't allow player to be ready if there are not enough players or teams are unbalanced
            if (Runner.TryGetSingleton(out GameManager gameManager))
            {
                if (gameManager.NotEnoughPlayers()) return;

                if (gameManager.TeamsUnbalanced()) return;
            }

            Ready = !Ready;
        }

        public void ResetReady()
        {
            Ready = false;
        }

        public bool IsReady()
        {
            if (Ready)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public abstract void InitNetworkState();
    }
}
