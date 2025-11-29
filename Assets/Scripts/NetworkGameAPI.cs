using PurrNet;
using PurrNet.Transports;
using System;
using UnityEngine;
using PurrNet.Modules;
using Photon.Pun;
using System.Collections;

public class NetworkGameAPI : NetworkBehaviour
{
    public static NetworkGameAPI Instance;
    public PurrTransport Transport;

    public Action<PlayerID> OnPlayerJoinedGame;
    public Action OnGameStarted;
    public Action<string, PlayerID> OnServerJson;
    public Action<string, PlayerID> OnClientJson;
    public NetworkManager NetworkManager;
    public bool networkInitialized = false;

    void Awake()
    {
        Instance = this;

        //Transport.roomName = "Test1"; // PhotonNetwork.CurrentRoom.Name;
        Transport.roomName = PhotonNetwork.CurrentRoom.Name;
    }

    private IEnumerator Start()
    {
        NetworkManager.onNetworkStarted += OnNetworkStart;
        

        if (PhotonNetwork.IsMasterClient && NetworkManager.serverState == ConnectionState.Disconnected)
        {
            NetworkManager.startServerFlags = StartFlags.Editor;
            NetworkManager.startServerFlags = StartFlags.ServerBuild;
            NetworkManager.StartServer();
        }

        if(PhotonNetwork.IsMasterClient)
        {
            yield return new WaitUntil(() => NetworkManager.serverState == ConnectionState.Connected);
        }
        

        if(NetworkManager.clientState == ConnectionState.Disconnected)
        {
            NetworkManager.startClientFlags = StartFlags.Editor;
            NetworkManager.startClientFlags = StartFlags.Clone;
            NetworkManager.startClientFlags = StartFlags.ClientBuild;
            NetworkManager.StartClient();
        }

        yield return new WaitUntil(() => NetworkManager.clientState == ConnectionState.Connected);        
    }

    private void OnNetworkStart(NetworkManager manager, bool asServer)
    {
        if (asServer)
            return;

        networkInitialized = true;
        NetworkManager.onPlayerJoined += HandlePlayerJoined;
    }

    private void HandlePlayerJoined(PlayerID player, bool isReconnect, bool asServer)
    {
        Debug.Log($"Player joined: {player}, AsServer: {asServer}");

        OnPlayerJoinedGame?.Invoke(player);
    }

    public void OnGameStartedEvent()
    {
        OnGameStarted.Invoke();
    }

    public void SendJsonToServer(string json, PlayerID sender)
    {
        if(!isServer)
        {
            Debug.Log($"Received JSON from client {sender.id}: {json}");
        }

        Rpc_ClientToServer(json, sender);
    }

    [ServerRpc]
    void Rpc_ClientToServer(string json, PlayerID sender)
    {
        Debug.Log($"Received JSON from client {sender.id}: {json}");

        OnServerJson?.Invoke(json, sender);
    }

    public void BroadcastJsonToClients(string json, PlayerID sender)
    {
        Rpc_ServerToClients(json, sender);
    }

    [ObserversRpc]
    void Rpc_ServerToClients(string json, PlayerID sender)
    {
        if (isClient)
        {
            OnClientJson?.Invoke(json, sender);
        }
    }

    public void SendJsonToClient(PlayerID target, string json)
    {
        Rpc_ServerToOneClient(target, json);
    }

    [TargetRpc]
    void Rpc_ServerToOneClient(PlayerID target, string json)
    {
        if (isClient)
        {
            OnClientJson?.Invoke(json, target);
        }
    }
}