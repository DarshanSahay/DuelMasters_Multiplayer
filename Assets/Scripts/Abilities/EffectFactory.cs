public static class EffectFactory
{
    public static IEffect CreateEffect(Card card)
    {
        switch (card.Ability)
        {
            case Abilities.GainPoints:
                return new GainPointsEffect(card.AbilityValue);

            case Abilities.StealPoints:
                return new StealPointsEffect(card.AbilityValue);

            case Abilities.DoublePower:
                return new DoublePowerEffect(card.AbilityValue);

            case Abilities.DrawExtraCard:
                return new DrawExtraCardEffect(card.AbilityValue);

            case Abilities.DiscardOpponentRandomCard:
                return new DiscardOpponentRandomEffect(card.AbilityValue);

            case Abilities.DestroyOpponentCardInPlay:
                return new DestroyOpponentCardInPlayEffect(card.AbilityValue);
        }

        return null;
    }
}