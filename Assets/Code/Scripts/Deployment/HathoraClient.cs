using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Hathora.Core.Scripts.Runtime.Client;
using HathoraCloud.Models.Shared;
using HathoraCloud.Models.Operations;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using Fusion;
using TeamBasedShooter;

public class HathoraClient : HathoraClientMgr, INetworkRunnerCallbacks
{
    [Header(nameof(Fusion))]
    [SerializeField]
    private bool _forceSinglePeerMode;

    private float _initializeTime;
    private string _sessionName;
    private string _sessionRegion;
    private float _sessionTimer;
    private List<LobbyV3> _hathoraLobbies;

    public bool HasValidSession => string.IsNullOrEmpty(_sessionName) == false;
    public string SessionName => _sessionName;
    public string SessionRegion => _sessionRegion;

    public async Task<bool> Initialize(NetworkRunner runnerPrefab, string preferredRegion, string roomId = null)
    {
        if (_forceSinglePeerMode == true)
        {
            NetworkProjectConfig.Global.PeerMode = NetworkProjectConfig.PeerModes.Single;
        }

        _initializeTime = Time.realtimeSinceStartup;

        // Reset values to default.
        _sessionName = default;
        _sessionRegion = default;
        _hathoraLobbies = default;

        // 1. Login to Hathora cloud.
        PlayerTokenObject loginResponse = await AuthLoginAsync();
        if (string.IsNullOrEmpty(loginResponse.Token) == true)
        {
            return false;
        }

        // 2. Use preferred region or find a region with best ping.
        _sessionRegion = preferredRegion;
        if (string.IsNullOrEmpty(_sessionRegion) == true)
        {
            (bool bestRegionFound, Region bestHathoraRegion, double bestRegionPing) = await HathoraRegionCollection.FindBestRegion(HathoraSdk, Region.Frankfurt);
            _sessionRegion = HathoraRegionCollection.HathoraToPhoton(bestHathoraRegion);
        }

        Region hathoraRegion = HathoraRegionCollection.PhotonToHathora(_sessionRegion);
        LobbyV3 hathoraLobby = default;

        // 3. Get existing public lobbies for random join.
        if (string.IsNullOrEmpty(roomId) == true)
        {
            ListActivePublicLobbiesRequest publicLobbiesRequest = new ListActivePublicLobbiesRequest();
            publicLobbiesRequest.Region = hathoraRegion;
            _hathoraLobbies = await GetActivePublicLobbiesAsync(publicLobbiesRequest);
        }

        // 4. No public lobbies available, we have to create one.
        if (_hathoraLobbies == null || _hathoraLobbies.Count == 0)
        {
            hathoraLobby = await CreateLobbyAsync(null, hathoraRegion, roomId);

            if (hathoraLobby == null)
            {
                return false;
            }

            _hathoraLobbies = new List<LobbyV3>();
            _hathoraLobbies.Add(hathoraLobby);
        }

        // 5. Connect to Photon lobby. We need to connect to one of public lobbies (3) or to the session created (4).
        FusionAppSettings appSettings = PhotonAppSettings.Global.AppSettings.GetCopy();
        appSettings.FixedRegion = _sessionRegion;
        NetworkRunner lobbyRunner = Instantiate(runnerPrefab);
        lobbyRunner.AddCallbacks(this);

        StartGameResult joinLobbyResult = await lobbyRunner.JoinSessionLobby(SessionLobby.ClientServer, customAppSettings: appSettings);
        if (joinLobbyResult.Ok == false)
        {
            lobbyRunner.RemoveCallbacks(this);
            await lobbyRunner.Shutdown(true);
            return false;
        }

        // 6. Set a timeout for join.
        _sessionTimer = 30.0f;
        while (_sessionTimer > 0.0f)
        {
            // 7. Create a new lobby after 10sec of random join.
            if (_sessionTimer < 20.0f && hathoraLobby == null)
            {
                _hathoraLobbies.Clear();
                _sessionName = default;

                hathoraLobby = await CreateLobbyAsync(null, hathoraRegion, roomId);
                if (hathoraLobby == null)
                {
                    lobbyRunner.RemoveCallbacks(this);
                    await lobbyRunner.Shutdown(true);
                    return false;
                }

                _hathoraLobbies = new List<LobbyV3>();
                _hathoraLobbies.Add(hathoraLobby);
            }

            _sessionTimer -= 0.1f;
            await Task.Delay(100);
        }

        lobbyRunner.RemoveCallbacks(this);
        await lobbyRunner.Shutdown(true);

        return HasValidSession;
    }

    void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        foreach (SessionInfo session in sessionList)
        {
            if (session.IsOpen == false)
                continue;

            foreach (LobbyV3 hathoraLobby in _hathoraLobbies)
            {
                if (session.Name == hathoraLobby.RoomId && (session.PlayerCount < session.MaxPlayers || session.MaxPlayers <= 0))
                {
                    _sessionName = session.Name;
                    _sessionTimer = default;
                    return;
                }
            }
        }
    }

    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }

    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner) { }
}
