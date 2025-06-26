using System.Collections.Generic;
using Hathora.Core.Scripts.Runtime.Server;
using Hathora.Core.Scripts.Runtime.Common.Utils;
using Hathora.Core.Scripts.Runtime.Server.Models;
using HathoraCloud.Models.Shared;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using System.Net;
using Fusion.Sockets;
using Fusion.Photon.Realtime;
using UnityEngine.SceneManagement;
using System;
using FusionHelpers;

namespace TeamBasedShooter
{
    public class HathoraServer : HathoraServerMgr, INetworkRunnerCallbacks
    {
        [Header(nameof(Fusion))]
        [SerializeField]
        private bool _forceMultiPeerMode;

        private NetworkRunner _runner;

        private List<ServerPeer> _serverPeers = new List<ServerPeer>();

        [SerializeField] private GameManager _gameManagerPrefab;

        public async Task<bool> Initialize(NetworkRunner runner, INetworkSceneManager sceneManager)
        {
            if (_forceMultiPeerMode == true)
            {
                NetworkProjectConfig.Global.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
            }

            _runner = runner;

            HathoraServerContext serverContext = await GetCachedServerContextAsync();
            if (serverContext == null)
            {
                return false;
            }

            RefreshServers(sceneManager);
            return true;
        }

        private async void RefreshServers(INetworkSceneManager sceneManager)
        {
            NetworkRunner runner = _runner;
            var run = true;

            // Run update loop until the runner prefab changes.
            while (run)
            {
                // 1. Get the server context.
                HathoraServerContext serverContext = await GetCachedServerContextAsync();
                if (serverContext == null)
                {
                    await Task.Delay(1000);
                    continue;
                }

                // 2. Deinitialize all Fusion server instances that are still running, but their Hathora rooms has been destroyed.
                for (int i = _serverPeers.Count - 1; i >= 0; --i)
                {
                    ServerPeer serverPeer = _serverPeers[i];
                    if (FindActiveRoom(serverContext, serverPeer.RoomId) == default)
                    {
                        await TerminateServerPeer(serverPeer);
                        await Task.Delay(1000);
                    }
                }

                // 3. Initialize new Fusion server instance for first new active Hathora room.
                List<RoomWithoutAllocations> activeRooms = serverContext.ActiveRoomsForProcess;

                if (activeRooms != null)
                {
                    for (int i = 0; i < activeRooms.Count; ++i)
                    {
                        RoomWithoutAllocations activeRoom = activeRooms[i];
                        if (activeRoom != null && FindServerPeer(activeRoom.RoomId) == default)
                        {
                            await BootServer(runner, serverContext, activeRoom, sceneManager);
                            await Task.Delay(1000);
                            run = false;
                            break;
                        }
                    }
                }

                // 4. Repeat checks every second.
                await Task.Delay(1000);
            }
        }

        private async Task<bool> BootServer(NetworkRunner runner, HathoraServerContext serverContext, RoomWithoutAllocations room, INetworkSceneManager sceneManager)
        {
            if (FindUnusedPort(serverContext, out ProcessV3ExposedPort exposedPort) == false)
            {
                return false;
            }

            // 1. Get ports information and initialize the server peer.
            ushort publicPort = (ushort)exposedPort.Port;
            ushort containerPort = (ushort)HathoraServerConfig.HathoraDeployOpts.ContainerPortSerializable.Port;
            string portName = exposedPort.Name;

            string additionalContainer = "default-";
            if (portName.Contains(additionalContainer) == true)
            {
                if (ushort.TryParse(portName.Substring(additionalContainer.Length), out ushort containerPortOffset) == true)
                {
                    containerPort += containerPortOffset;
                }
            }

            ServerPeer serverPeer = new ServerPeer(room.RoomId, publicPort, containerPort, portName);
            _serverPeers.Add(serverPeer);

            // 2. Get network addresses.
            IPAddress ip = await HathoraUtils.ConvertHostToIpAddress(exposedPort.Host);
            NetAddress address = NetAddress.Any(containerPort);
            NetAddress publicAddress = NetAddress.CreateFromIpPort(ip.ToString(), publicPort);

            // 3. Set fixed Photon region based on Hathora server region.
            FusionAppSettings photonAppSettings = PhotonAppSettings.Global.AppSettings.GetCopy();
            photonAppSettings.FixedRegion = HathoraRegionCollection.HathoraToPhoton(serverContext.EnvVarRegion);

            // 4. Configure scene info
            NetworkSceneInfo sceneInfo = new NetworkSceneInfo();
            sceneInfo.AddSceneRef(SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex));

            // 5. Configure start game arguments.
            StartGameArgs startGameArgs = new StartGameArgs();
            startGameArgs.SessionName = room.RoomId;
            startGameArgs.GameMode = GameMode.Server;
            startGameArgs.Scene = sceneInfo;
            startGameArgs.SceneManager = sceneManager;
            startGameArgs.IsVisible = true;
            startGameArgs.IsOpen = true;
            startGameArgs.Address = address;
            startGameArgs.CustomPublicAddress = publicAddress;
            startGameArgs.CustomPhotonAppSettings = photonAppSettings;

            // 6. Start Fusion NetworkRunner.
            serverPeer.Runner = Instantiate(runner);
            serverPeer.Runner.name += $"_{room.RoomId}";
            serverPeer.Runner.AddCallbacks(this);

            StartGameResult startGameResult = await serverPeer.Runner.StartGame(startGameArgs);
            if (startGameResult.Ok == true)
            {
                UnityEngine.Application.targetFrameRate = TickRate.Resolve(serverPeer.Runner.Config.Simulation.TickRateSelection).Server;

                return true;
            }
            else
            {
                serverPeer.Runner.RemoveCallbacks(this);
                await TerminateServerPeer(serverPeer);
                return false;
            }
        }

        private RoomWithoutAllocations FindActiveRoom(HathoraServerContext serverContext, string roomId)
        {
            List<RoomWithoutAllocations> activeRooms = serverContext.ActiveRoomsForProcess;
            if (activeRooms == null)
                return default;

            for (int i = 0, count = activeRooms.Count; i < count; ++i)
            {
                RoomWithoutAllocations activeRoom = activeRooms[i];
                if (activeRoom != null && activeRoom.RoomId == roomId)
                    return activeRoom;
            }

            return default;
        }

        private ServerPeer FindServerPeer(string roomId)
        {
            for (int i = 0, count = _serverPeers.Count; i < count; ++i)
            {
                ServerPeer serverPeer = _serverPeers[i];
                if (serverPeer.RoomId == roomId)
                {
                    return serverPeer;
                }
            }

            return default;
        }

        private ServerPeer FindServerPeer(ExposedPort port)
        {
            for (int i = 0, count = _serverPeers.Count; i < count; ++i)
            {
                ServerPeer serverPeer = _serverPeers[i];
                if (serverPeer.PortName == port.Name)
                    return serverPeer;
            }

            return default;
        }

        private ServerPeer FindServerPeer(ProcessV3ExposedPort port)
        {
            for (int i = 0, count = _serverPeers.Count; i < count; ++i)
            {
                ServerPeer serverPeer = _serverPeers[i];
                if (serverPeer.PortName == port.Name)
                    return serverPeer;
            }

            return default;
        }

        private bool FindUnusedPort(HathoraServerContext serverContext, out ProcessV3ExposedPort port)
        {
            port = default;

            if (serverContext == null || serverContext.ProcessInfo == null)
                return false;

            if (FindServerPeer(serverContext.ProcessInfo.ExposedPort) == null)
            {
                port = serverContext.ProcessInfo.ExposedPort;
                return true;
            }

            foreach (ExposedPort additionalPort in serverContext.ProcessInfo.AdditionalExposedPorts)
            {
                if (FindServerPeer(additionalPort) == null)
                {
                    port = ExposedPortToProcessV3ExposedPort(additionalPort);
                    return true;
                }
            }

            return false;
        }

        private ProcessV3ExposedPort ExposedPortToProcessV3ExposedPort(ExposedPort port)
        {
            ProcessV3ExposedPort newPort = new ProcessV3ExposedPort();
            newPort.Host = port.Host;
            newPort.Port = port.Port;
            newPort.Name = port.Name;
            newPort.TransportType = port.TransportType;
            return newPort;
        }

        private async Task TerminateServerPeer(ServerPeer serverPeer)
        {
            _serverPeers.Remove(serverPeer);

            if (serverPeer.Runner != null)
            {
                await serverPeer.Runner.Shutdown();
                serverPeer.Runner = null;
            }
        }

        private sealed class ServerPeer
        {
            public readonly string RoomId;
            public readonly ushort PublicPort;
            public readonly ushort ContainerPort;
            public readonly string PortName;

            public NetworkRunner Runner;

            public ServerPeer(string roomId, ushort publicPort, ushort containerPort, string portName)
            {
                RoomId = roomId;
                PublicPort = publicPort;
                ContainerPort = containerPort;
                PortName = portName;
            }
        }

        public void OnConnectedToServer(NetworkRunner runner) { }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
            request.Accept();
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer)
            {
                if (!runner.TryGetSingleton(out FusionSession session) && _gameManagerPrefab != null)
                {
                    session = runner.Spawn(_gameManagerPrefab);
                }

                session.PlayerJoined(player);
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (runner.TryGetSingleton(out FusionSession session))
                session.PlayerLeft(player);
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
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
