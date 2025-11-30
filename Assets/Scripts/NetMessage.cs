using PurrNet;
using System;

[Serializable]
public class NetEnvelope
{
    public NetAction action;
}

[Serializable]
public class JoinRequestMessage : NetEnvelope
{
    public string playerId;
}

[Serializable]
public class AssignPlayerIdMessage : NetEnvelope
{
    public string playerId;
}

[Serializable]
public class JoinMessage : NetEnvelope
{
    public string playerId;
}

[Serializable]
public class RevealCardsMessage : NetEnvelope
{
    public string playerId;
    public int[] cardIds;
}

[Serializable]
public class TimerMessage : NetEnvelope
{
    public float timeLeft;
}

[Serializable]
public class GameStateMessage : NetEnvelope
{
    public FullGameState fullState;
}

[Serializable]
public class RevealResultMessage : NetEnvelope
{
    public int turn;
    public StringIntPairList scores;
    public StringIntArrayPairList playedCards;
    public AbilityEventList abilityEvents;
}

[Serializable]
public class EndMatchMessage : NetEnvelope
{
    public int turn;
    public StringIntPairList scores;
    public FullGameState fullState;
}

[Serializable]
public class ReconnectedFullStateMessage : NetEnvelope 
{
    public FullGameState fullState;
}

[Serializable]
public class FullGameState
{
    public int turn;
    public int totalTurns;
    public PlayerEntryList players;
}

[Serializable]
public class PlayerStateDTO
{
    public int score;
    public int[] handCardIds;
    public int[] playedThisTurn;
}

[Serializable]
public class AbilityEvent
{
    public string playerId;
    public int cardId;
    public string abilityName;
    public string description;
}

[Serializable]
public class AbilityEventList
{
    public AbilityEvent[] list;
}