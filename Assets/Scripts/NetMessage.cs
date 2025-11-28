using PurrNet;
using System;

[Serializable]
public class NetMessage
{
    public NetAction action;
    public string playerId;
    public int[] cardIds;
    public PlayerID purrPlayer;
    public float timeLeft;
    public int turn;

    public StringIntPairList scores;  
    public StringIntArrayPairList playedCards;        

    public FullGameState fullState;

    public PlayerID GetPurrPlayerID(PlayerID player)
    {
        PlayerID newPlayer = new(player.id, false);
        return newPlayer;
    }
}

[Serializable]
public class FullGameState
{
    public int turn;
    public int totalTurns;

    public PlayerEntryList players;     // list of players & their state
}

[Serializable]
public class PlayerStateDTO
{
    public int score;
    public int handCount;
    public int[] handCardIds;
    public int[] playedThisTurn;
}
