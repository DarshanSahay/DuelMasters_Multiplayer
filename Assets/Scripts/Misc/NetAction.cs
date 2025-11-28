public enum NetAction
{
    None = 0,

    JoinRequest,
    AssignPlayerId,
    Join,
    RevealCards,
    EndTurn,
    GameState,
    RevealResult,
    EndMatch,
    RequestFullState,
    ReconnectedFullState,
    Timer
}