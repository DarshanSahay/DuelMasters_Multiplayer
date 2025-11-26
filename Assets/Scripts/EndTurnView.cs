using UnityEngine;
using UnityEngine.UI;

public class EndTurnView : MonoBehaviour
{
    [SerializeField] Button button;

    public System.Action OnEndTurnClicked;

    void Awake()
    {
        button.onClick.AddListener(() => OnEndTurnClicked?.Invoke());
    }

    public void SetInteractable(bool value)
    {
        button.interactable = value;
    }
}
