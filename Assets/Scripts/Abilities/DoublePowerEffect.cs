public class DoublePowerEffect : IEffect
{
    private readonly int mult;
    public DoublePowerEffect(int m) { mult = m; }

    public void Apply(PlayerState self, PlayerState opponent)
    {
        foreach (var c in self.PlayedThisTurn)
            self.Score += c.Power * (mult - 1);
    }
}