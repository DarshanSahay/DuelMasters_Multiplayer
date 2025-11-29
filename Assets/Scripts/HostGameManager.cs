using PurrNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HostGameManager : MonoBehaviour
{
    private NetworkGameAPI net;
    private CardLoader loader;

    private Dictionary<PlayerID, string> playerRefToId = new();
    private Dictionary<string, PlayerState> players = new();

    private Dictionary<string, List<Card>> playedBoards = new();

    private Dictionary<string, int[]> reveals = new();

    private float turnTimer = 30f;
    private bool timerRunning = false;
    private float timerBroadcastAccumulator = 0f;

    private int totalTurns = 6;
    private int currentTurn = 1;
    private bool matchStarted = false;

    void Awake()
    {
        loader = CardLoader.Instance;
    }

    IEnumerator Start()
    {
        net = NetworkGameAPI.Instance;
        if (!net) throw new Exception("NetworkGameAPI not found in scene");

        yield return new WaitUntil(() => net.networkInitialized);

        if (net.isServer)
            net.OnServerJson += HandleJsonFromClient;
    }

    void Update()
    {
        if (!matchStarted) return;
        if (!timerRunning) return;

        turnTimer -= Time.deltaTime;

        if (turnTimer <= 0f)
        {
            timerRunning = false;
            ForceEndTurnForBothPlayers();
        }

        timerBroadcastAccumulator += Time.deltaTime;
        if (timerBroadcastAccumulator >= 1f)
        {
            timerBroadcastAccumulator = 0f;
            BroadcastTimer();
        }
    }

    void HandleJsonFromClient(string json, PlayerID sender)
    {
        var msg = JsonUtility.FromJson<NetMessage>(json);

        if (msg == null || msg.action == NetAction.None)
        {
            Debug.LogWarning("Invalid JSON received on server: " + json);
            return;
        }

        switch (msg.action)
        {
            case NetAction.JoinRequest:
                StartCoroutine(AssignPlayerId(sender, msg.playerId));
                break;

            case NetAction.Join:
                HandleJoin(sender, msg.playerId);
                break;

            case NetAction.RevealCards:
                HandleReveal(msg);
                break;

            case NetAction.EndTurn:
                HandleEndTurn(msg);
                break;

            case NetAction.RequestFullState:
                HandleStateRequest(sender, msg.playerId);
                break;

            default:
                Debug.LogWarning("Unknown action on server: " + msg.action);
                break;
        }
    }

    IEnumerator AssignPlayerId(PlayerID sender, string id)
    {
        Debug.Log($"Assigning player ID to {sender}...");

        string assignedId = id;

        var msg = new NetMessage
        {
            action = NetAction.AssignPlayerId,
            playerId = assignedId
        };

        yield return new WaitForSeconds(5f);

        net.SendJsonToClient(sender, JsonUtility.ToJson(msg));
    }

    void HandleJoin(PlayerID sender, string playerId)
    {
        if (!playerRefToId.ContainsKey(sender))
        {
            playerRefToId[sender] = playerId;
            players[playerId] = new PlayerState();
            playedBoards[playerId] = new List<Card>();
            Debug.Log($"Player joined: {playerId}. Players now: {players.Keys.Count}");
        }

        if (players.Count == 2 && !matchStarted)
            StartMatch();
    }

    void StartMatch()
    {
        Debug.Log("Starting match!");
        matchStarted = true;
        currentTurn = 1;

        foreach (var kv in players)
        {
            var p = kv.Value;
            var pid = kv.Key;

            p.Score = 0;
            p.Hand.Clear();
            p.PlayedThisTurn.Clear();
            p.Flags_DrawExtra = 0;

            // ensure playedBoards exists & is empty
            if (!playedBoards.ContainsKey(pid))
                playedBoards[pid] = new List<Card>();
            else
                playedBoards[pid].Clear();

            // Starting hand of 3 cards
            for (int i = 0; i < 3; i++)
                p.DrawCard(loader.Cards[UnityEngine.Random.Range(0, loader.Cards.Count)]);
        }

        net.OnGameStartedEvent();

        BroadcastGameState();
    }

    void HandleEndTurn(NetMessage msg)
    {
        // not used
    }

    void HandleReveal(NetMessage msg)
    {
        if (!CostIsValid(msg.playerId, msg.cardIds))
        {
            Debug.LogWarning($"{msg.playerId} attempted invalid cost!");
        }

        reveals[msg.playerId] = msg.cardIds ?? new int[0];

        if (reveals.Count == players.Count)
        {
            ResolveTurn();
            reveals.Clear();
            currentTurn++;

            if (currentTurn > totalTurns)
                EndMatch();
            else
                BroadcastGameState();
        }
    }

    bool CostIsValid(string pid, int[] cardIds)
    {
        int max = Mathf.Clamp(currentTurn, 1, 6);
        int cost = 0;

        foreach (int id in cardIds)
        {
            var card = loader.Cards.First(c => c.Id == id);
            cost += card.Cost;
        }

        return cost <= max;
    }

    void ResolveTurn()
    {
        BuildPlayedCardLists();

        List<AbilityEvent> abilityEvents = new List<AbilityEvent>();

        MoveRevealedCardsToBoard();

        ApplyDestructiveAbilities(abilityEvents);
        ApplyNormalAbilities(abilityEvents);

        ApplyPowerToScore();

        ApplyExtraDraws();

        BroadcastRevealResult(abilityEvents);

        foreach (var kv in players)
            ClearTransientTurnState(kv.Value);
    }

    void BuildPlayedCardLists()
    {
        foreach (var kv in players)
        {
            string pid = kv.Key;
            var state = kv.Value;

            state.PlayedThisTurn.Clear();

            if (reveals.TryGetValue(pid, out var ids) && ids != null && ids.Length > 0)
            {
                foreach (int id in ids)
                {
                    var card = loader.Cards.FirstOrDefault(c => c.Id == id);
                    if (card != null)
                        state.PlayedThisTurn.Add(card);
                }
            }
        }
    }

    void MoveRevealedCardsToBoard()
    {
        foreach (var pid in players.Keys.ToList())
        {
            if (!reveals.TryGetValue(pid, out var ids) || ids == null || ids.Length == 0)
                continue; // nothing to move for this player

            var state = players[pid];
            var board = playedBoards[pid];

            foreach (int id in ids)
            {
                var cardInHand = state.Hand.FirstOrDefault(c => c.Id == id);

                var card = cardInHand ?? loader.Cards.FirstOrDefault(c => c.Id == id);

                if (card == null) continue;

                if (cardInHand != null)
                    state.Hand.Remove(cardInHand);

                if (!board.Any(c => c.Id == card.Id))
                    board.Add(card);
            }
        }
    }

    void ApplyDestructiveAbilities(List<AbilityEvent> abilityEvents)
    {
        foreach (var pid in players.Keys.ToList())
        {
            var self = players[pid];
            var opponent = players.First(k => k.Key != pid).Value;

            var destructives = self.PlayedThisTurn
                .Select(c => new { Card = c, Effect = EffectFactory.CreateEffect(c) })
                .Where(x => x.Effect is DestroyOpponentCardInPlayEffect ||
                            x.Effect is DiscardOpponentRandomEffect)
                .ToList();

            foreach (var x in destructives)
            {
                x.Effect.Apply(self, opponent);

                abilityEvents.Add(new AbilityEvent
                {
                    playerId = pid,
                    cardId = x.Card.Id,
                    abilityName = x.Card.Ability.ToString(),
                    description = $"{x.Card.Name} — {x.Card.Description}"
                });
            }
        }
    }

    void ApplyNormalAbilities(List<AbilityEvent> abilityEvents)
    {
        foreach (var pid in players.Keys.ToList())
        {
            var self = players[pid];
            var opponent = players.First(k => k.Key != pid).Value;

            var normals = self.PlayedThisTurn
                .Select(c => new { Card = c, Effect = EffectFactory.CreateEffect(c) })
                .Where(x => !(x.Effect is DestroyOpponentCardInPlayEffect) &&
                            !(x.Effect is DiscardOpponentRandomEffect))
                .ToList();

            foreach (var x in normals)
            {
                x.Effect.Apply(self, opponent);

                abilityEvents.Add(new AbilityEvent
                {
                    playerId = pid,
                    cardId = x.Card.Id,
                    abilityName = x.Card.Ability.ToString(),
                    description = x.Card.Name + " - " + x.Card.Description
                });
            }
        }
    }

    void ApplyPowerToScore()
    {
        foreach (var kv in players)
        {
            var p = kv.Value;
            p.Score += p.PlayedThisTurn.Sum(c => c.Power);
        }
    }

    void ApplyExtraDraws()
    {
        foreach (var kv in players)
        {
            var p = kv.Value;

            for (int i = 0; i < p.Flags_DrawExtra; i++)
                p.DrawCard(loader.Cards[UnityEngine.Random.Range(0, loader.Cards.Count)]);

            p.Flags_DrawExtra = 0;
        }
    }

    void BroadcastRevealResult(List<AbilityEvent> abilityEvents)
    {
        foreach (var kv in players)
        {
            var p = kv.Value;
            p.DrawCard(loader.Cards[UnityEngine.Random.Range(0, loader.Cards.Count)]);
        }

        var revealMsg = new NetMessage
        {
            action = NetAction.RevealResult,
            turn = currentTurn,
            scores = ToScoreList(players),
            playedCards = ToPlayedCardsList(players),
            abilityEvents = new AbilityEventList { list = abilityEvents.ToArray() }
        };

        string json = JsonUtility.ToJson(revealMsg);

        //net.BroadcastJsonToClients(json, revealMsg.purrPlayer);
        net.BroadcastJsonToClients(json, net.localPlayer.Value);
    }

    void ClearTransientTurnState(PlayerState p)
    {
        p.PlayedThisTurn.Clear();
        p.Flags_DrawExtra = 0;
    }

    void BroadcastGameState()
    {
        turnTimer = 30f;
        timerRunning = true;
        BroadcastTimer();

        var state = new FullGameState
        {
            turn = currentTurn,
            totalTurns = totalTurns,
            players = ToPlayerEntryList(players)
        };

        var msg = new NetMessage
        {
            action = NetAction.GameState,
            fullState = state
        };

        string json = JsonUtility.ToJson(msg);
        net.BroadcastJsonToClients(json, msg.purrPlayer);
    }

    void BroadcastTimer()
    {
        var msg = new NetMessage
        {
            action = NetAction.Timer,
            timeLeft = turnTimer
        };

        net.BroadcastJsonToClients(JsonUtility.ToJson(msg), msg.purrPlayer);
    }

    void ForceEndTurnForBothPlayers()
    {
        Debug.Log("Timer ended — forcing reveal.");

        foreach (var pid in players.Keys)
        {
            if (!reveals.ContainsKey(pid))
                reveals[pid] = new int[0];
        }

        ResolveTurn();
        reveals.Clear();
        currentTurn++;

        if (currentTurn > totalTurns)
            EndMatch();
        else
            BroadcastGameState();
    }

    void HandleStateRequest(PlayerID requester, string playerId)
    {
        var full = new FullGameState
        {
            turn = currentTurn,
            totalTurns = totalTurns,
            players = ToPlayerEntryList(players)
        };

        var msg = new NetMessage
        {
            action = NetAction.ReconnectedFullState,
            fullState = full
        };

        string json = JsonUtility.ToJson(msg);
        net.SendJsonToClient(requester, json);
    }

    void EndMatch()
    {
        var endMsg = new NetMessage
        {
            action = NetAction.EndMatch,
            turn = currentTurn,
            scores = ToScoreList(players),
            fullState = new FullGameState
            {
                turn = currentTurn,
                totalTurns = totalTurns,
                players = ToPlayerEntryList(players)
            }
        };

        string json = JsonUtility.ToJson(endMsg);
        net.BroadcastJsonToClients(json, endMsg.purrPlayer);

        timerRunning = false;
        matchStarted = false;
    }

    StringIntPairList ToScoreList(Dictionary<string, PlayerState> players)
    {
        var arr = new StringIntPair[players.Count];
        int i = 0;
        foreach (var kv in players)
        {
            arr[i++] = new StringIntPair
            {
                key = kv.Key,
                value = kv.Value.Score
            };
        }
        return new StringIntPairList { list = arr };
    }

    StringIntArrayPairList ToPlayedCardsList(Dictionary<string, PlayerState> players)
    {
        var arr = new StringIntArrayPair[players.Count];
        int i = 0;
        foreach (var kv in players)
        {
            var pid = kv.Key;
            var board = playedBoards.ContainsKey(pid) ? playedBoards[pid] : new List<Card>();

            arr[i++] = new StringIntArrayPair
            {
                key = pid,
                values = board.ConvertAll(c => c.Id).ToArray()
            };
        }
        return new StringIntArrayPairList { list = arr };
    }

    PlayerEntryList ToPlayerEntryList(Dictionary<string, PlayerState> players)
    {
        var arr = new PlayerEntry[players.Count];
        int i = 0;
        foreach (var kv in players)
        {
            var pid = kv.Key;
            arr[i++] = new PlayerEntry
            {
                playerId = pid,
                state = new PlayerStateDTO
                {
                    score = kv.Value.Score,
                    handCount = kv.Value.Hand.Count,
                    handCardIds = kv.Value.Hand.ConvertAll(c => c.Id).ToArray(),
                    playedThisTurn = (playedBoards.ContainsKey(pid) ? playedBoards[pid] : new List<Card>()).ConvertAll(c => c.Id).ToArray()
                }
            };
        }
        return new PlayerEntryList { list = arr };
    }
}