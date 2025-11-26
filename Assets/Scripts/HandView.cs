using UnityEngine;
using System;
using System.Collections.Generic;

public class HandView : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private CardView cardPrefab;

    private readonly List<CardView> cards = new();

    public Action<int> OnCardClicked;

    public void SetInteractable(bool value)
    {
        canvasGroup.interactable = value;
    }

    public void RenderHand(IEnumerable<Card> hand)
    {
        Clear();

        foreach (var c in hand)
        {
            var view = Instantiate(cardPrefab, cardContainer);
            view.Bind(c);
            view.OnCardClicked = OnCardClicked;
            cards.Add(view);
        }
    }

    public void Clear()
    {
        foreach (var cv in cards)
            Destroy(cv.gameObject);
        cards.Clear();
    }
}