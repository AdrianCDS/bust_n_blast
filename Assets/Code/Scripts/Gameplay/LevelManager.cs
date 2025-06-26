using System;
using System.Collections;
using Fusion;
using FusionHelpers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace TeamBasedShooter
{

    public class LevelManager : NetworkSceneManagerDefault
    {
        [SerializeField] private int _lobby;
        [SerializeField] private int[] _levels;

        [FormerlySerializedAs("_readyupManager")][SerializeField] private ReadyUpManager _readyUpManager;

        private LevelBehaviour _currentLevel;
        private SceneRef _loadedScene = SceneRef.None;

        public Action<NetworkRunner, FusionLauncher.ConnectionStatus, string> onStatusUpdate { get; set; }

        public ReadyUpManager readyUpManager => _readyUpManager;

        private void Awake()
        {

        }

        public override void Shutdown()
        {
            _currentLevel = null;
            if (_loadedScene.IsValid)
            {
                SceneManager.UnloadSceneAsync(_loadedScene.AsIndex);
                _loadedScene = SceneRef.None;
            }

            base.Shutdown();
        }

        public int GetArenaLevel()
        {
            return _levels[0];
        }

        public SpawnPoint GetPlayerSpawnPoint(PlayerRef player, Team playerTeam)
        {
            if (_currentLevel != null)
                return _currentLevel.GetPlayerSpawnPoint(player, playerTeam);

            return null;
        }

        public void LoadLevel(int nextLevelIndex)
        {
            _currentLevel = null;

            if (_loadedScene.IsValid)
            {
                Runner.UnloadScene(_loadedScene);
                _loadedScene = SceneRef.None;
            }

            if (nextLevelIndex < 0)
            {
                Runner.LoadScene(SceneRef.FromIndex(_lobby), new LoadSceneParameters(LoadSceneMode.Additive), true);
            }
            else
            {
                Runner.LoadScene(SceneRef.FromIndex(nextLevelIndex), new LoadSceneParameters(LoadSceneMode.Additive), true);
            }
        }

        protected override IEnumerator UnloadSceneCoroutine(SceneRef prevScene)
        {
            GameManager gameManager;
            while (!Runner.TryGetSingleton(out gameManager))
            {
                yield return null;
            }

            if (Runner.IsServer)
                gameManager.CurrentPlayState = GameManager.PlayState.TRANSITION;

            if (prevScene.AsIndex > 0)
            {
                yield return new WaitForSeconds(1.0f);

                foreach (FusionPlayer fusionPlayer in gameManager.AllPlayers)
                {
                    fusionPlayer.TeleportOut();

                    yield return new WaitForSeconds(0.1f);
                }

                yield return new WaitForSeconds(1.5f - gameManager.PlayerCount * 0.1f);
            }

            yield return base.UnloadSceneCoroutine(prevScene);
        }

        protected override IEnumerator OnSceneLoaded(SceneRef newScene, Scene loadedScene, NetworkLoadSceneParameters sceneFlags)
        {
            yield return base.OnSceneLoaded(newScene, loadedScene, sceneFlags);

            if (newScene.AsIndex == 0)
                yield break;

            onStatusUpdate?.Invoke(Runner, FusionLauncher.ConnectionStatus.Loading, "");

            yield return null;

            _loadedScene = newScene;

            yield return null;

            onStatusUpdate?.Invoke(Runner, FusionLauncher.ConnectionStatus.Loaded, "");

            _currentLevel = FindObjectOfType<LevelBehaviour>();

            yield return new WaitForSeconds(0.3f);

            GameManager gameManager;
            while (!Runner.TryGetSingleton(out gameManager))
            {
                yield return null;
            }

            foreach (FusionPlayer fusionPlayer in gameManager.AllPlayers)
            {
                fusionPlayer.Respawn();

                yield return new WaitForSeconds(0.3f);
            }

            if (_loadedScene.AsIndex == _lobby)
            {
                if (Runner.IsServer)
                    gameManager.CurrentPlayState = GameManager.PlayState.LOBBY;
            }
            // else if (_loadedScene.AsIndex > _lobby)
            // {
            //     if (Runner.IsServer)
            //     {
            //         gameManager.CurrentPlayState = GameManager.PlayState.LEVEL;
            //     }
            // }
        }
    }
}
