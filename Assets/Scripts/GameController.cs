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

    // Local state
    private List<Card> localHand = new();
    private List<Card> localPlayed = new();

    private int maxCost = 1;
    private int currentCost = 0;

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
        var msg = new NetMessage { action = NetAction.JoinRequest, playerId = player.id.ToString() };
        PlayerID newPlayer = msg.GetPurrPlayerID(player);
        msg.purrPlayer = newPlayer;

        StartCoroutine(WaitForNetwork(msg, newPlayer));
    }

    public void OnGameStarted()
    {
        ui.CloseWaitingForPlayersPanel();
    }

    public IEnumerator WaitForNetwork(NetMessage msg, PlayerID player)
    {
        yield return new WaitUntil(() => net.isFullySpawned);

        net.SendJsonToServer(JsonUtility.ToJson(msg), player);
    }

    void HandleClientJson(string json, PlayerID sender)
    {
        var msg = JsonUtility.FromJson<NetMessage>(json);
        if (msg == null) return;

        switch (msg.action)
        {
            case NetAction.AssignPlayerId:
                AssignLocalPlayerID(msg, sender);
                break;

            case NetAction.Timer:
                ui.UpdateTimer(msg.timeLeft);
                break;

            case NetAction.GameState:
                ApplyFullState(msg.fullState);
                break;

            case NetAction.RevealResult:
                ApplyRevealResult(msg);
                break;

            case NetAction.EndMatch:
                ApplyEndMatch(msg);
                break;

            case NetAction.ReconnectedFullState:
                ApplyFullState(msg.fullState);
                break;
        }
    }

    void AssignLocalPlayerID(NetMessage msg, PlayerID sender)
    {
        localPlayerId = sender.id.ToString();
        playerID = new(sender.id, false);

        Debug.Log("This player is : " + sender);

        var join = new NetMessage
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


    void ApplyRevealResult(NetMessage msg)
    {
        currentCost = 0;
        ui.UpdateCostUI(currentCost, maxCost);

        ui.UpdateTurn(msg.turn, 6);
        ui.ShowWaiting(false);

        localPlayed.Clear();
        ui.UpdateLocalPlayed(localPlayed);

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
            StartCoroutine(ui.PlayAbilitySequence(
                msg.abilityEvents.list.ToList(),
                localPlayerId
            ));
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

        var ids = localPlayed.Select(c => c.Id).ToArray();

        var msg = new NetMessage
        {
            action = NetAction.RevealCards,
            playerId = localPlayerId,
            cardIds = ids
        };

        net.SendJsonToServer(JsonUtility.ToJson(msg), msg.purrPlayer);
    }

    // For reconnect button or auto rejoin
    public void RequestFullState()
    {
        var msg = new NetMessage
        {
            action = NetAction.RequestFullState,
            playerId = localPlayerId
        };

        net.SendJsonToServer(JsonUtility.ToJson(msg), msg.purrPlayer);
    }

    void ApplyEndMatch(NetMessage msg)
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

        // Tell UI to open your end screen
        ui.ShowEndScreen(myScore, opponentScore);
    }
}
