public class GainPointsEffect : IEffect
{
    private readonly int value;
    public GainPointsEffect(int v) { value = v; }

    public void Apply(PlayerState self, PlayerState opponent)
    {
        self.Score += value;
    }
}