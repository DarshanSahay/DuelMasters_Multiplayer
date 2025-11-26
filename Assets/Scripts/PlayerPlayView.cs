using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class PlayerPlayView : MonoBehaviour
{
    [SerializeField] private Transform playedContainer;
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private TMP_Text scoreText;

    public void RenderPlayedCards(IEnumerable<Card> cards)
    {
        foreach (Transform child in playedContainer)
            Destroy(child.gameObject);

        foreach (var card in cards)
        {
            var view = Instantiate(cardPrefab, playedContainer);
            view.Bind(card);
        }
    }

    public void SetScore(int score)
    {
        scoreText.text = score.ToString();
    }
}