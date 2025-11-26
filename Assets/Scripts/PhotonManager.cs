using System;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;


public class PhotonManager : MonoBehaviourPunCallbacks
{
    public static PhotonManager Instance;

    public Action<List<RoomInfo>> OnRoomListUpdated;
    private readonly Dictionary<string, RoomInfo> cachedRooms = new Dictionary<string, RoomInfo>();
    [SerializeField] private string lobbyName = "DuelMasters";
    [SerializeField] private byte defaultMaxPlayers = 2;

    [SerializeField] private GameObject creatingRoomOverlay;
    [SerializeField] private GameObject joiningLobbyOverlay;
    [SerializeField] private GameObject loadingOcverlay;
    [SerializeField] private GameObject browseScreen;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
            PhotonNetwork.ConnectUsingSettings();

        loadingOcverlay.SetActive(true);
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master server.");
        TypedLobby typed = new TypedLobby(lobbyName, LobbyType.Default);
        PhotonNetwork.JoinLobby(typed);

        loadingOcverlay.SetActive(false);
        joiningLobbyOverlay.SetActive(true);
    }

    public override void OnJoinedLobby()
    {
        joiningLobbyOverlay.SetActive(false);
        Debug.Log($"Joined Lobby '{lobbyName}'");
        cachedRooms.Clear();
        // UI will receive OnRoomListUpdate soon
    }

    public override void OnLeftLobby()
    {
        cachedRooms.Clear();
        OnRoomListUpdated?.Invoke(new List<RoomInfo>(cachedRooms.Values));
    }

    // Called by Photon with deltas
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        bool changed = false;

        foreach (var room in roomList)
        {
            if (room.RemovedFromList)
            {
                if (cachedRooms.Remove(room.Name))
                    changed = true;
            }
            else
            {
                cachedRooms[room.Name] = room;
                changed = true;
            }
        }

        if (changed)
            OnRoomListUpdated?.Invoke(new List<RoomInfo>(cachedRooms.Values));
    }

    public void OnBrowseButton()
    {
        browseScreen.SetActive(true);
    }


    // ---------- Public API for UI buttons ----------

    // Create room with provided name. If name is empty/null, a unique name is generated.
    public void CreateRoom()
    {
        CreateUniqueRoom();

        creatingRoomOverlay.SetActive(true);
    }

    // Create a unique room name (Room_<8chars>)
    public void CreateUniqueRoom()
    {
        string id = Guid.NewGuid().ToString("N").Substring(0, 8);
        string roomName = "Room_" + id;

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = defaultMaxPlayers,
            IsVisible = true,
            IsOpen = true
        };

        PhotonNetwork.CreateRoom(roomName, options);
        Debug.Log("Creating unique room: " + roomName);
    }

    // Join room by name (UI should provide the exact room name)
    public void JoinRoom(string roomName)
    {
        if (string.IsNullOrWhiteSpace(roomName))
        {
            Debug.LogWarning("JoinRoom called with empty roomName");
            return;
        }

        PhotonNetwork.JoinRoom(roomName);
        Debug.Log("Attempting to join room: " + roomName);
    }

    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"CreateRoom failed: {message}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"JoinRoom failed: {message}");
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("Room created successfully: " + PhotonNetwork.CurrentRoom.Name);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room: " + PhotonNetwork.CurrentRoom.Name);

        PhotonNetwork.LoadLevel("GameScene");
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Left room.");
    }

    public List<RoomInfo> GetCachedRooms()
    {
        return new List<RoomInfo>(cachedRooms.Values);
    }
}