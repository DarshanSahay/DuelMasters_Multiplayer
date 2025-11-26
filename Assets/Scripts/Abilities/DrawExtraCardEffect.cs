public class DrawExtraCardEffect : IEffect
{
    private readonly int amount;
    public DrawExtraCardEffect(int a) { amount = a; }

    public void Apply(PlayerState self, PlayerState opponent)
    {
        //self.Flags_DrawExtra = amount; // flag used after reveal
    }
}