using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class LobbyEntry : MonoBehaviour
{

    [SerializeField] private TMP_Text lobbyNameText;
    [SerializeField] private TMP_Text playersText;
    public Button roomBtn;

    public void SetRoomDetails(string roomName, string players)
    {
        lobbyNameText.text = roomName;
        playersText.text = players;
    }
}
