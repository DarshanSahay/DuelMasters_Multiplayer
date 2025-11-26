using UnityEngine;
using TMPro;

public class CostDisplayView : MonoBehaviour
{
    [SerializeField] TMP_Text text;

    public void SetCost(int used, int max)
    {
        text.text = $"{used}/{max}";
    }
}