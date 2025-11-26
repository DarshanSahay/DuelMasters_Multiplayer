public class DestroyOpponentCardInPlayEffect : IEffect
{
    private readonly int amount;
    public DestroyOpponentCardInPlayEffect(int a) { amount = a; }

    public void Apply(PlayerState self, PlayerState opponent)
    {
        for (int i = 0; i < amount; i++)
        {
            if (opponent.PlayedThisTurn.Count == 0) break;
            opponent.PlayedThisTurn.RemoveAt(0); // destroy lowest index
        }
    }
}