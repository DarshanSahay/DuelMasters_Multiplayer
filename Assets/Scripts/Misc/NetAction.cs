public enum NetAction
{
    None = 0,

    JoinRequest,
    AssignPlayerId,
    Join,
    GameStart,
    RevealCards,
    TimerExpire,
    EndTurn,
    GameState,
    RevealResult,
    EndMatch,
    RequestFullState,
    ReconnectedFullState,
    Timer
}