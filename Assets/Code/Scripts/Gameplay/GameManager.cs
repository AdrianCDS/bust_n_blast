using UnityEngine;
using Fusion;
using FusionHelpers;
using System.Linq;

namespace TeamBasedShooter
{
    public class GameManager : FusionSession
    {
        private bool _restart;

        [Networked] public float AttackersTotalPoints { get; set; }
        [Networked] public float DefendersTotalPoints { get; set; }

        [Networked] public Team Winner { get; set; } = Team.None;

        [Networked]
        [HideInInspector]
        public TickTimer RemainingTime { get; set; }
        public float GameDuration = 60f;

        public override void Spawned()
        {
            base.Spawned();
            Runner.RegisterSingleton(this);

            DontDestroyOnLoad(gameObject);

            if (Object.HasStateAuthority)
            {
                LoadLevel(-1);
            }
            else if (CurrentPlayState != PlayState.LOBBY)
            {
                _restart = true;
            }
        }

        protected override void OnPlayerAvatarAdded(FusionPlayer fusionPlayer)
        {

        }

        protected override void OnPlayerAvatarRemoved(FusionPlayer fusionPlayer)
        {

        }

        public void Restart(ShutdownReason shutdownReason)
        {
            MusicPlayer.Instance.ReplayMusic(0.25f);

            if (!Runner.IsShutdown)
            {
                Runner.Shutdown(false, shutdownReason);
                _restart = false;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Restart()
        {
            MusicPlayer.Instance.ReplayMusic(0.25f);

            if (!Runner.IsShutdown)
            {
                Runner.Shutdown(false, ShutdownReason.Ok);
                _restart = false;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public const ShutdownReason ShutdownReason_GameAlreadyRunning = (ShutdownReason)100;

        public override void FixedUpdateNetwork()
        {
            if (RemainingTime.Expired(Runner) && CurrentPlayState != PlayState.ENDGAME)
            {
                if (Runner.IsServer)
                {
                    CurrentPlayState = PlayState.ENDGAME;
                }

                Winner = AttackersTotalPoints >= DefendersTotalPoints ? Team.Attackers : Team.Defenders;
                
                RespawnPlayers();
            }

            if (RemainingTime.ExpiredOrNotRunning(Runner) && CurrentPlayState == PlayState.LEVEL)
            {
                RemainingTime = TickTimer.CreateFromSeconds(Runner, GameDuration);
            }
        }

        private void Update()
        {
            LevelManager lm = Runner.GetLevelManager();
            lm.readyUpManager.UpdateUI(CurrentPlayState, AllPlayers, OnAllPlayersReady);

            if (_restart)
            {
                Restart(_restart ? ShutdownReason_GameAlreadyRunning : ShutdownReason.Ok);
                _restart = false;
            }
        }

        public void OnAllPlayersReady()
        {
            Runner.SessionInfo.IsOpen = false;
            Runner.SessionInfo.IsVisible = false;

            RespawnPlayers();

            if (Runner.IsServer && CurrentPlayState == PlayState.LOBBY)
            {
                CurrentPlayState = PlayState.LEVEL;
                FragmentPoolManager.Instance.Initialize();

                // Cache all spawn points
                _npcSpawnPoints = GameObject.FindGameObjectsWithTag("NPCSpawnPoint")
                    .Select(go => go.transform)
                    .ToArray();

                SpawnNPCs(20);
            }

            // TODO: Fix in the future for a second scene
            // Currently disabled because it causes background issues
            // LoadLevel(Runner.GetLevelManager().GetArenaLevel());
        }

        private void LoadLevel(int nextLevelIndex)
        {
            if (Runner.IsSceneAuthority)
                Runner.GetLevelManager().LoadLevel(nextLevelIndex);
        }
    }
}
