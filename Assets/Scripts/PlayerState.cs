using System.Collections.Generic;
using System.Linq;

public class PlayerState
{
    public List<Card> Hand = new();
    public List<Card> PlayedThisTurn = new();
    public int Score = 0;

    // Ability flags for reveal resolution
    public int Flags_DrawExtra = 0;

    public void DrawCard(Card c)
    {
        Hand.Add(c);
    }

    public bool CanPlayCard(Card card, int maxCost)
    {
        int current = PlayedThisTurn.Sum(c => c.Cost);
        return current + card.Cost <= maxCost;
    }

    public void PlayCard(Card card)
    {
        Hand.Remove(card);
        PlayedThisTurn.Add(card);
    }

    public int TotalPlayedCost() =>
        PlayedThisTurn.Sum(c => c.Cost);

    public void ResetForNextTurn()
    {
        PlayedThisTurn.Clear();
        Flags_DrawExtra = 0;
    }
}