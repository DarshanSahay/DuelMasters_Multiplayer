using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private Image artworkImage;
    [SerializeField] private Button button;

    private int cardId;
    public System.Action<int> OnCardClicked;

    public void Bind(Card card)
    {
        cardId = card.Id;
        nameText.text = card.Name;
        costText.text = card.Cost.ToString();
        powerText.text = card.Power.ToString();
    }

    void Awake()
    {
        button.onClick.AddListener(() => OnCardClicked?.Invoke(cardId));
    }
}