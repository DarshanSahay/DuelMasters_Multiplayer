public interface ICard
{
    int Id { get; }
    string Name { get; }
    int Cost { get; }
    int Power { get; }
    Abilities Ability { get; }
    int AbilityValue { get; }
}
