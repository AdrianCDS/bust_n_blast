using System.Collections.Generic;
using Fusion;
using UnityEngine;
using TeamBasedShooter;
using PlayFab;
using PlayFab.ClientModels;

namespace FusionHelpers
{
    public abstract class FusionSession : NetworkBehaviour
    {
        public enum PlayState { LOBBY, LEVEL, TRANSITION, ENDGAME }

        [Networked] public PlayState CurrentPlayState { get; set; }

        public NPC Npc;
        public FusionPlayer TemplatePlayer;

        private const int MAX_PLAYERS = 10;

        [Networked] public PlayerRef LastJoinedPlayerRef { get; set; }

        [Networked, Capacity(MAX_PLAYERS)] public NetworkDictionary<int, PlayerRef> PlayerRefByIndex { get; }
        private Dictionary<PlayerRef, FusionPlayer> _players = new();

        public Player MaverickCharacter;
        public Player BulwarkCharacter;
        public Player NadjaCharacter;
        public Player ZaphyrCharacter;

        protected abstract void OnPlayerAvatarAdded(FusionPlayer fusionPlayer);
        protected abstract void OnPlayerAvatarRemoved(FusionPlayer fusionPlayer);

        public IEnumerable<FusionPlayer> AllPlayers => _players.Values;
        public int PlayerCount => _players.Count;
        public int SessionCount => PlayerRefByIndex.Count;

        private Player _playerToUse;

        protected Transform[] _npcSpawnPoints;

        public override void Spawned()
        {
            base.Spawned();
            Runner.RegisterSingleton(this);
        }

        public override void Render()
        {
            if (Runner && _players.Count != PlayerRefByIndex.Count && CurrentPlayState != PlayState.LEVEL && CurrentPlayState != PlayState.ENDGAME)
                MaybeSpawnNextAvatar();
        }

        private void MaybeSpawnNextAvatar()
        {
            foreach (KeyValuePair<int, PlayerRef> refByIndex in PlayerRefByIndex)
            {
                if (Runner.IsServer)
                {
                    if (!_players.TryGetValue(refByIndex.Value, out _))
                    {
                        FusionPlayer playerPrefab = TemplatePlayer;

                        Runner.Spawn(playerPrefab, Vector3.zero, Quaternion.identity, refByIndex.Value, (runner, o) =>
                        {
                            Runner.SetPlayerObject(refByIndex.Value, o);
                            FusionPlayer player = o.GetComponent<FusionPlayer>();

                            if (player != null)
                            {
                                byte[] connectionToken = Runner.GetPlayerConnectionToken(refByIndex.Value);
                                if (connectionToken != null)
                                {
                                    string uniqueId = System.Text.Encoding.UTF8.GetString(connectionToken);

                                    player.PlayerUniqueId = uniqueId;
                                    Debug.Log($"<color=orange>[MaybeSpawnNextAvatar]</color>: PlayerID {refByIndex.Value} | PlayerUniqueId {uniqueId}");
                                    player.NetworkedPlayerIndex = refByIndex.Key;
                                    player.InitNetworkState();
                                }
                            }
                        });
                    }
                }
            }
        }

        public void AddPlayerAvatar(FusionPlayer fusionPlayer)
        {
            _players[fusionPlayer.PlayerId] = fusionPlayer;

            LevelManager lm = Runner.GetLevelManager();
            lm.readyUpManager.UpdateLobbyInfo(_players.Count);

            OnPlayerAvatarAdded(fusionPlayer);
        }

        public void RemovePlayerAvatar(FusionPlayer fusionPlayer)
        {
            _players.Remove(fusionPlayer.PlayerId);

            // if (Object != null && Object.IsValid)
            //     PlayerRefByIndex.Remove(fusionPlayer.PlayerIndex);

            OnPlayerAvatarRemoved(fusionPlayer);
        }

        public T GetPlayer<T>(PlayerRef playerRef) where T : FusionPlayer
        {
            _players.TryGetValue(playerRef, out FusionPlayer player);
            return (T)player;
        }

        public T GetPlayerByIndex<T>(int idx) where T : FusionPlayer
        {
            foreach (FusionPlayer player in _players.Values)
            {
                if (player.Object != null && player.Object.IsValid && player.PlayerIndex == idx)
                    return (T)player;
            }

            return default;
        }

        private int NextPlayerIndex()
        {
            for (int idx = 0; idx < Runner.Config.Simulation.PlayerCount; idx++)
            {
                if (!PlayerRefByIndex.TryGet(idx, out _))
                    return idx;
            }

            return -1;
        }

        public void PlayerLeft(PlayerRef playerRef)
        {
            if (!Runner.IsShutdown)
            {
                FusionPlayer player = GetPlayer<FusionPlayer>(playerRef);

                if (player && player.Object.IsValid)
                {
                    Runner.Despawn(player.Object);
                }

                if (Runner.SessionInfo.PlayerCount == 1)
                {
                    Runner.Shutdown(false);
                }
            }
        }

        public void PlayerJoined(PlayerRef player)
        {
            int nextIndex = NextPlayerIndex();

            PlayerRefByIndex.Set(nextIndex, player);

            LastJoinedPlayerRef = player;

            MaybeSpawnNextAvatar();
        }

        public Player UpdatePlayerPrefab(FusionPlayer fusionPlayer)
        {
            // check Player's chosen character and update its prefab

            _players[fusionPlayer.PlayerId] = fusionPlayer;

            switch (fusionPlayer.PlayerCharacter)
            {
                case Character.Bulwark:
                    _playerToUse = BulwarkCharacter;
                    break;
                case Character.Maverick:
                    _playerToUse = MaverickCharacter;
                    break;
                case Character.Zaphyr:
                    _playerToUse = ZaphyrCharacter;
                    break;
                case Character.Nadja:
                    _playerToUse = NadjaCharacter;
                    break;
            }

            fusionPlayer.PrefabToUse = _playerToUse;

            return _playerToUse;
        }

        public void RespawnPlayers()
        {
            var playersToRespawn = new Dictionary<PlayerRef, FusionPlayer>(_players);

            // Clear the players dictionary - we'll refill it with new player instances
            _players.Clear();

            foreach (var playerEntry in playersToRespawn)
            {
                FusionPlayer fusionPlayer = playerEntry.Value;
                PlayerRef playerId = fusionPlayer.PlayerId;
                int playerIndex = fusionPlayer.PlayerIndex;
                Team playerTeam = fusionPlayer.PlayerTeam;
                Character playerCharacter = fusionPlayer.PlayerCharacter;
                NetworkString<_128> playerUniqueId = fusionPlayer.PlayerUniqueId;
                NetworkBool playerLoggedIn = fusionPlayer.PlayerLoggedIn;
                string playerRankXp = fusionPlayer.PlayerRankXp;
                string playerRank = fusionPlayer.PlayerRank;
                string playerBalance = fusionPlayer.PlayerBalance;

                // Update the player prefab based on character selection
                Player playerPrefab = UpdatePlayerPrefab(fusionPlayer);

                // Only the server should despawn and respawn players
                if (Runner.IsServer && playerPrefab != null)
                {
                    // Despawn the template player
                    Runner.Despawn(fusionPlayer.Object);

                    // Spawn the actual character prefab
                    Runner.Spawn(playerPrefab, fusionPlayer.transform.position, fusionPlayer.transform.rotation, playerId, (runner, obj) =>
                    {
                        // Set up the newly spawned player
                        Player newPlayer = obj.GetComponent<Player>();
                        if (newPlayer != null)
                        {
                            newPlayer.PlayerId = playerId;
                            newPlayer.PlayerIndex = playerIndex;
                            newPlayer.PlayerTeam = playerTeam;
                            newPlayer.PlayerCharacter = playerCharacter;
                            newPlayer.PlayerUniqueId = playerUniqueId;
                            newPlayer.PlayerLoggedIn = playerLoggedIn;
                            newPlayer.PlayerRankXp = playerRankXp;
                            newPlayer.PlayerRank = playerRank;
                            newPlayer.PlayerBalance = playerBalance;

                            // Set the player object for this connection
                            Runner.SetPlayerObject(playerId, obj);

                            // Add to players dictionary with the same key
                            _players[playerId] = newPlayer;
                        }
                    });
                }
            }
        }

        public bool PlayersInSameTeam(PlayerRef victimPlayerRef, PlayerRef killerPlayerRef)
        {
            if (_players.TryGetValue(victimPlayerRef, out FusionPlayer victimPlayer) && _players.TryGetValue(killerPlayerRef, out FusionPlayer killerPlayer))
            {
                return victimPlayer.PlayerTeam == killerPlayer.PlayerTeam;
            }
            else
            {
                return false;
            }
        }

        public bool DefenderAttacksNPC(bool isNPC, PlayerRef killerPlayerRef)
        {
            if (isNPC)
            {
                return _players.TryGetValue(killerPlayerRef, out FusionPlayer killerPlayer) && killerPlayer.PlayerTeam == Team.Defenders;
            }
            else
            {
                return false;
            }
        }

        public bool NotEnoughPlayers()
        {
            return PlayerRefByIndex.Count < 2;
        }

        public bool TeamsUnbalanced()
        {
            int defendersCount = 0;
            int attackersCount = 0;

            foreach (var player in _players)
            {
                if (player.Value.PlayerTeam == Team.Defenders)
                {
                    defendersCount++;
                }

                if (player.Value.PlayerTeam == Team.Attackers)
                {
                    attackersCount++;
                }
            }

            return defendersCount != attackersCount;
        }

        public void SpawnNPCs(int countToSpawn)
        {
            if (_npcSpawnPoints == null || _npcSpawnPoints.Length == 0)
            {
                return;
            }

            for (int i = 0; i < countToSpawn; i++)
            {
                // Get a random spawn point
                Transform spawnPoint = _npcSpawnPoints[Random.Range(0, _npcSpawnPoints.Length)];

                Runner.Spawn(Npc, spawnPoint.position, Quaternion.identity, null, (runner, o) =>
                {
                    // Initialize NPC if needed
                });
            }
        }
    }
}
