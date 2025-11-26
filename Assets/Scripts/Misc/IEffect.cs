public interface IEffect
{
    void Apply(PlayerState self, PlayerState opponent);
}