using UnityEngine;
using Fusion;

namespace TeamBasedShooter
{
    public class HathoraManager : MonoBehaviour
    {
        public App AppRef;
        public NetworkRunner RunnerPrefab;
        public LevelManager LevelManager;

        public HathoraServer ServerPrefab;
        public HathoraClient ClientPrefab;

        private HathoraServer _serverInstance;
        private HathoraClient _clientInstance;

        public HathoraServer GetOrCreateServerInstance()
        {
            if (_serverInstance == null)
            {
                _serverInstance = Instantiate(ServerPrefab);
            }

            return _serverInstance;
        }

        public HathoraClient GetOrCreateClientInstance()
        {
            if (_clientInstance == null)
            {
                _clientInstance = Instantiate(ClientPrefab);
            }

            return _clientInstance;
        }

        private void Awake()
        {
            DontDestroyOnLoad(this);

            LevelManager.onStatusUpdate = AppRef.OnConnectionStatusUpdate;
        }

        private async void Start()
        {
            // Main application entry point for the headless server only, clients do not run this
            if (Application.isEditor == false)
            {
                if (Application.platform == RuntimePlatform.LinuxServer || Application.platform == RuntimePlatform.WindowsServer || Application.platform == RuntimePlatform.OSXServer)
                {
                    HathoraServer hathoraServer = GetOrCreateServerInstance();
                    if (hathoraServer != null)
                    {
                        bool result = await hathoraServer.Initialize(RunnerPrefab, LevelManager);
                        if (result == false)
                        {
                            Application.Quit();
                        }
                    }
                }
            }
        }
    }
}
