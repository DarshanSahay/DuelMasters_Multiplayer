using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomListUI : MonoBehaviour
{
    [SerializeField] Transform listRoot;
    [SerializeField] GameObject roomButtonPrefab;

    void Start()
    {
        PhotonManager.Instance.OnRoomListUpdated += RefreshUI;
    }

    void RefreshUI(List<RoomInfo> rooms)
    {
        foreach (Transform t in listRoot)
            Destroy(t.gameObject);

        foreach (var r in rooms)
        {
            var go = Instantiate(roomButtonPrefab, listRoot);

            go.GetComponent<LobbyEntry>().SetRoomDetails(r.Name, (r.PlayerCount / r.MaxPlayers).ToString());

            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                PhotonManager.Instance.JoinRoom(r.Name);
            });
        }
    }
}