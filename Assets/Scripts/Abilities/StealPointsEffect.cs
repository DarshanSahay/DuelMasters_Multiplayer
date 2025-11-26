using System;

public class StealPointsEffect : IEffect
{
    private readonly int value;
    public StealPointsEffect(int v) { value = v; }

    public void Apply(PlayerState self, PlayerState opponent)
    {
        int stolen = Math.Min(opponent.Score, value);
        opponent.Score -= stolen;
        self.Score += stolen;
    }
}