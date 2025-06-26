using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

namespace FusionHelpers
{
    public class IPhotonConnectionArgs
    {
        /// <summary>
        /// The session / Photon room name. Can be null.
        /// </summary>
        public string Session { get; set; }
        /// <summary>
        /// The preferred Photon region. Null = best region.
        /// </summary>
        public string PreferredRegion { get; set; }
        /// <summary>
        /// The region to use for the connection.
        /// </summary>
        public string Region { get; set; }
        /// <summary>
        /// The Photon AppVersion to use.
        /// </summary>
        public string AppVersion { get; set; }
        /// <summary>
        /// The max player count for the connection.
        /// </summary>
        public int MaxPlayerCount { get; set; }
        /// <summary>
        /// Toggle creation then uses the supplied <see cref="Session"/>.
        /// </summary>
        public bool Creating { get; set; }
    }

    public class FusionLauncher : MonoBehaviour, INetworkRunnerCallbacks
    {
        private Action<NetworkRunner, ConnectionStatus, string> _connectionCallback;
        private FusionSession _sessionPrefab;
        private NetworkRunner _runner;

        private HathoraClient _clientInstance;

        public IPhotonConnectionArgs ConnectionArgs = new IPhotonConnectionArgs();

        public enum ConnectionStatus
        {
            Disconnected,
            Connecting,
            Failed,
            Connected,
            Loading,
            Loaded
        }

        public static FusionLauncher Launch(NetworkRunner runnerPrefab, HathoraClient clientPrefab, FusionSession sessionPrefab,
            INetworkSceneManager sceneLoader,
            bool dedicatedServer,
            Action<NetworkRunner, ConnectionStatus, string> onConnect)
        {
            FusionLauncher existingLauncher = FindObjectOfType<FusionLauncher>();
            if (existingLauncher != null)
            {
                existingLauncher.InternalLaunch(runnerPrefab, clientPrefab, sessionPrefab, sceneLoader, dedicatedServer, onConnect);
                return existingLauncher;
            }
            else
            {
                FusionLauncher launcher = new GameObject("FusionLauncher").AddComponent<FusionLauncher>();
                launcher.InternalLaunch(runnerPrefab, clientPrefab, sessionPrefab, sceneLoader, dedicatedServer, onConnect);
                return launcher;
            }
        }

        public async void Terminate()
        {
            _runner.RemoveCallbacks(this);
            await _runner.Shutdown(true);

            SetConnectionStatus(_runner, ConnectionStatus.Disconnected, "User terminated the session.");
        }

        private async void InternalLaunch(NetworkRunner runnerPrefab,
        HathoraClient clientPrefab,
            FusionSession sessionPrefab,
            INetworkSceneManager sceneManager,
            bool dedicatedServer,
            Action<NetworkRunner, ConnectionStatus, string> onConnect)
        {
            _sessionPrefab = sessionPrefab;
            _connectionCallback = onConnect;

            DontDestroyOnLoad(gameObject);

            SetConnectionStatus(runnerPrefab, ConnectionStatus.Connecting, "");

            ConnectionArgs.Session = null;
            ConnectionArgs.Creating = false;
            ConnectionArgs.Region = ConnectionArgs.PreferredRegion;

            switch (dedicatedServer)
            {
                case true:
                    await SetupClientConnection(runnerPrefab, clientPrefab, ConnectionArgs, sceneManager);
                    break;
                case false:
                    _runner = CreateOrGetRunner(runnerPrefab);

                    NetworkSceneInfo sceneInfo = new NetworkSceneInfo();
                    sceneInfo.AddSceneRef(SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex));

                    var startGameArgs = new StartGameArgs()
                    {
                        GameMode = GameMode.AutoHostOrClient,
                        Scene = sceneInfo,
                        SceneManager = sceneManager,
                        ConnectionToken = System.Text.Encoding.UTF8.GetBytes(PlayerPrefs.GetString("DeviceId"))
                    };

                    await _runner.StartGame(startGameArgs);

                    break;
            }
        }

        public HathoraClient GetOrCreateClientInstance(HathoraClient clientPrefab)
        {
            if (_clientInstance == null)
            {
                _clientInstance = Instantiate(clientPrefab);
            }

            return _clientInstance;
        }

        public async Task<ConnectionStatus> SetupClientConnection(NetworkRunner runnerPrefab, HathoraClient clientPrefab, IPhotonConnectionArgs connectionArgs, INetworkSceneManager sceneManager)
        {
            HathoraClient hathoraClient = GetOrCreateClientInstance(clientPrefab);
            await hathoraClient.Initialize(runnerPrefab, connectionArgs.Region, connectionArgs.Session);

            if (hathoraClient.HasValidSession == true)
            {
                connectionArgs.Creating = false;
                connectionArgs.Session = hathoraClient.SessionName;
                connectionArgs.Region = hathoraClient.SessionRegion;
            }
            else
            {
                SetConnectionStatus(runnerPrefab, ConnectionStatus.Disconnected, "Unable to setup connection");
                return ConnectionStatus.Disconnected;
            }

            // Hathora + Fusion session found, connecting
            return await ConnectAsync(runnerPrefab, connectionArgs, sceneManager);
        }

        public virtual async Task<ConnectionStatus> ConnectAsync(NetworkRunner runnerPrefab, IPhotonConnectionArgs connectionArgs, INetworkSceneManager sceneManager)
        {
            _runner = CreateOrGetRunner(runnerPrefab);

            var appSettings = PhotonAppSettings.Global.AppSettings.GetCopy();
            appSettings.FixedRegion = connectionArgs.Region;

            NetworkSceneInfo sceneInfo = new NetworkSceneInfo();
            sceneInfo.AddSceneRef(SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex));

            var startGameArgs = new StartGameArgs()
            {
                SessionName = connectionArgs.Session,
                PlayerCount = connectionArgs.MaxPlayerCount,
                GameMode = GameMode.Client,
                Scene = sceneInfo,
                SceneManager = sceneManager,
                CustomPhotonAppSettings = appSettings
            };

            await _runner.StartGame(startGameArgs);

            return ConnectionStatus.Connected;
        }

        private NetworkRunner CreateOrGetRunner(NetworkRunner runnerPrefab)
        {
            if (_runner == null)
            {
                var runner = GameObject.Instantiate(runnerPrefab);
                runner.ProvideInput = true;
                runner.AddCallbacks(this);
                return runner;
            }
            else
            {
                return _runner;
            }
        }

        public void SetConnectionStatus(NetworkRunner runner, ConnectionStatus status, string message)
        {
            if (_connectionCallback != null)
                _connectionCallback(runner, status, message);
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
            SetConnectionStatus(runner, ConnectionStatus.Connected, "");
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            SetConnectionStatus(runner, ConnectionStatus.Disconnected, "");
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
            request.Accept();
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            SetConnectionStatus(runner, ConnectionStatus.Failed, reason.ToString());
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            // For non-dedicated-server mode, this callback should not be called on the dedicated server
            if (runner.IsServer)
            {
                if (!runner.TryGetSingleton(out FusionSession session) && _sessionPrefab != null)
                {
                    session = runner.Spawn(_sessionPrefab);
                }

                session.PlayerJoined(player);
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            // For non-dedicated-server mode, this callback should not be called on the dedicated server
            if (runner.TryGetSingleton(out FusionSession session))
                session.PlayerLeft(player);
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            string message;

            switch (shutdownReason)
            {
                case ShutdownReason.IncompatibleConfiguration:
                    message = "This room already exist in a different game mode.";
                    break;
                case ShutdownReason.Ok:
                    message = "User terminated the session.";
                    break;
                case ShutdownReason.Error:
                    message = "Unknown network error.";
                    break;
                case ShutdownReason.ServerInRoom:
                    message = "There is already a host in this room.";
                    break;
                case ShutdownReason.DisconnectedByPluginLogic:
                    message = "Server connection terminated.";
                    break;
                default:
                    message = shutdownReason.ToString();
                    break;
            }

            SetConnectionStatus(runner, ConnectionStatus.Disconnected, message);
            runner.ClearRunnerSingletons();
            Destroy(gameObject);
        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    }
}
