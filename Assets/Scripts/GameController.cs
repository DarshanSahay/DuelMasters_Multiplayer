using PurrNet;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameController : MonoBehaviour, IGameController
{
    [SerializeField] private GameUIManager ui;
    [SerializeField] private CardLoader loader;
    [SerializeField] private NetworkGameAPI net;

    [SerializeField] private string localPlayerId;

    private List<Card> localHand = new();
    [SerializeField] private List<Card> localPlayed = new();
    [SerializeField] private List<Card> localPlayedThisTurn = new();

    private int maxCost = 1;
    private int currentCost = 0;

    // The PlayerID representing this client (used as purrPlayer when sending)
    public PlayerID playerID;

    void Awake()
    {
        if (!net.isServer)
        {
            net.OnClientJson += HandleClientJson;
        }

        ui.Bind(this);
    }

    IEnumerator Start()
    {
        yield return new WaitUntil(() => net.networkInitialized);
        yield return new WaitUntil(() => net.isFullySpawned);

        net.OnPlayerJoinedGame += PlayerJoinedGame;
        net.OnGameStarted += OnGameStarted;
    }

    void OnDestroy()
    {
        if (!net.isServer)
            net.OnClientJson -= HandleClientJson;
    }

    public void PlayerJoinedGame(PlayerID player)
    {
        var msg = new JoinRequestMessage
        {
            action = NetAction.JoinRequest,
            playerId = player.id.ToString()
        };

        //msg.purrPlayer = player;

        StartCoroutine(WaitForNetwork(msg, player));
    }

    public void OnGameStarted()
    {
        ui.CloseWaitingForPlayersPanel();
    }

    public IEnumerator WaitForNetwork(NetEnvelope msg, PlayerID player)
    {
        yield return new WaitUntil(() => net.isFullySpawned);

        net.SendJsonToServer(JsonUtility.ToJson(msg), player);
    }

    void HandleClientJson(string json, PlayerID sender)
    {
        var env = JsonUtility.FromJson<NetEnvelope>(json);
        if (env == null) return;

        switch (env.action)
        {
            case NetAction.AssignPlayerId:
                var assign = JsonUtility.FromJson<AssignPlayerIdMessage>(json);
                AssignLocalPlayerID(assign, sender);
                break;

            case NetAction.Timer:
                var timer = JsonUtility.FromJson<TimerMessage>(json);
                ui.UpdateTimer(timer.timeLeft);
                break;

            case NetAction.TimerExpire:
                AutoSendReveal();
                break;

            case NetAction.GameStart:
                ui.CloseWaitingForPlayersPanel();
                break;

            case NetAction.GameState:
                var gs = JsonUtility.FromJson<GameStateMessage>(json);
                ApplyFullState(gs.fullState);
                break;

            case NetAction.RevealResult:
                var rr = JsonUtility.FromJson<RevealResultMessage>(json);
                ApplyRevealResult(rr);
                break;

            case NetAction.EndMatch:
                var em = JsonUtility.FromJson<EndMatchMessage>(json);
                ApplyEndMatch(em);
                break;

            case NetAction.ReconnectedFullState:
                var rf = JsonUtility.FromJson<ReconnectedFullStateMessage>(json);
                ApplyFullState(rf.fullState);
                break;

        }
    }

    void AssignLocalPlayerID(AssignPlayerIdMessage msg, PlayerID sender)
    {
        localPlayerId = msg.playerId;
        playerID = new PlayerID(sender.id, false);

        Debug.Log("Assigned local player ID = " + localPlayerId);

        // Send Join (confirm) to server
        var join = new JoinMessage
        {
            action = NetAction.Join,
            playerId = localPlayerId
        };

        net.SendJsonToServer(JsonUtility.ToJson(join), sender);
    }

    void ApplyFullState(FullGameState full)
    {
        if (full == null) return;

        ui.UpdateTurn(full.turn, full.totalTurns);
        ui.UpdateTimer(30f);

        maxCost = Mathf.Clamp(full.turn, 1, 6);
        currentCost = 0;
        ui.UpdateCostUI(currentCost, maxCost);

        ui.SetHandInteractable(true);
        ui.SetEndTurnButtonActive(true);
        ui.ShowWaiting(false);

        if (full.players?.list == null) return;

        foreach (var entry in full.players.list)
        {
            var pid = entry.playerId;
            var dto = entry.state;

            if (pid == localPlayerId)
            {
                ui.playerArea.SetScore(dto.score);

                // Rebuild local hand from authoritative server state
                localHand = new List<Card>();
                if (dto.handCardIds != null)
                {
                    foreach (var id in dto.handCardIds)
                    {
                        var c = loader.Cards.FirstOrDefault(x => x.Id == id);
                        if (c != null)
                            localHand.Add(c);
                    }
                }

                ui.UpdateHandUI(localHand);
            }
            else
            {
                ui.opponentArea.SetScore(dto.score);
            }
        }
    }

    public void AutoSendReveal()
    {
        var ids = localPlayedThisTurn.Select(c => c.Id).ToArray();

        var msg = new RevealCardsMessage
        {
            action = NetAction.RevealCards,
            playerId = localPlayerId,
            cardIds = ids
        };

        net.SendJsonToServer(JsonUtility.ToJson(msg), playerID);
    }

    void ApplyRevealResult(RevealResultMessage msg)
    {
        currentCost = 0;
        ui.UpdateCostUI(currentCost, maxCost);

        ui.UpdateTurn(msg.turn, 6);
        ui.ShowWaiting(false);

        if (msg.scores != null && msg.scores.list != null)
        {
            foreach (var entry in msg.scores.list)
            {
                if (entry.key == localPlayerId)
                    ui.playerArea.SetScore(entry.value);
                else
                    ui.opponentArea.SetScore(entry.value);
            }
        }

        if (msg.abilityEvents != null && msg.abilityEvents.list != null && msg.abilityEvents.list.Length > 0)
        {
            StartCoroutine(ui.PlayAbilitySequence(msg.abilityEvents.list.ToList(), localPlayerId));
        }

        if (msg.playedCards != null && msg.playedCards.list != null)
        {
            foreach (var entry in msg.playedCards.list)
            {
                var cards = new List<Card>();

                foreach (var id in entry.values)
                {
                    var c = loader.Cards.FirstOrDefault(x => x.Id == id);
                    if (c != null) cards.Add(c);
                }

                if (entry.key == localPlayerId)
                    ui.UpdateLocalPlayed(cards);
                else
                    ui.UpdateOpponentPlayed(cards);
            }
        }
    }

    public void TrySelectCard(int cardId)
    {
        var card = localHand.FirstOrDefault(c => c.Id == cardId);
        if (card == null) return;

        if (currentCost + card.Cost > maxCost)
        {
            Debug.Log("Cannot play card: cost limit exceeded.");
            return;
        }

        currentCost += card.Cost;
        ui.UpdateCostUI(currentCost, maxCost);

        localPlayedThisTurn.Add(card);
        localPlayed.Add(card);
        localHand.Remove(card);

        ui.UpdateLocalPlayed(localPlayed);
        ui.UpdateHandUI(localHand);
    }

    public void EndTurn()
    {
        ui.SetHandInteractable(false);
        ui.SetEndTurnButtonActive(false);
        ui.ShowWaiting(true);

        var ids = localPlayedThisTurn.Select(c => c.Id).ToArray();
        localPlayedThisTurn.Clear();

        var msg = new RevealCardsMessage
        {
            action = NetAction.RevealCards,
            playerId = localPlayerId,
            cardIds = ids
        };

        net.SendJsonToServer(JsonUtility.ToJson(msg), playerID);
    }

    void ApplyEndMatch(EndMatchMessage msg)
    {
        ui.ShowWaiting(false);
        ui.SetHandInteractable(false);
        ui.SetEndTurnButtonActive(false);

        int myScore = 0;
        int opponentScore = 0;

        if (msg.scores != null && msg.scores.list != null)
        {
            foreach (var entry in msg.scores.list)
            {
                if (entry.key == localPlayerId)
                    myScore = entry.value;
                else
                    opponentScore = entry.value;
            }
        }

        ui.ShowEndScreen(myScore, opponentScore);
    }
}