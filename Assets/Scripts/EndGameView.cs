using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndGameView : MonoBehaviour
{
    [SerializeField] private TMP_Text localPlayerScoreText;
    [SerializeField] private TMP_Text opponentScoreText;
    [SerializeField] private TMP_Text statusText;

    public void SetGameEnd(string myScore, string opponentScore, string status)
    {
        this.gameObject.SetActive(true);

        localPlayerScoreText.text = myScore;
        opponentScoreText.text = opponentScore;
        statusText.text = status;
    }
}
