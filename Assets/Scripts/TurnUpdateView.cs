using UnityEngine;
using TMPro;

public class TurnUpdateView : MonoBehaviour
{
    [SerializeField] TMP_Text turnText;
    [SerializeField] TMP_Text timerText;

    public void SetTurn(int current, int total)
    {
        turnText.text = $"Turn {current}/{total}";
    }

    public void SetTimer(float seconds)
    {
        timerText.text = Mathf.CeilToInt(seconds).ToString();
    }
}
