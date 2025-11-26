public class DiscardOpponentRandomEffect : IEffect
{
    private readonly int amount;
    public DiscardOpponentRandomEffect(int a) { amount = a; }

    public void Apply(PlayerState self, PlayerState opponent)
    {
        for (int i = 0; i < amount; i++)
        {
            if (opponent.PlayedThisTurn.Count == 0) break;
            int idx = UnityEngine.Random.Range(0, opponent.PlayedThisTurn.Count);
            opponent.PlayedThisTurn.RemoveAt(idx);
        }
    }
}