using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using PurrNet;
using System.Collections;

public class HostGameManager : MonoBehaviour
{
    private NetworkGameAPI net;
    private CardLoader loader;

    private Dictionary<PlayerID, string> playerRefToId = new();
    private Dictionary<string, PlayerState> players = new();

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

        if (msg == null || string.IsNullOrEmpty(msg.action))
        {
            Debug.LogWarning("Invalid JSON received on server: " + json);
            return;
        }

        switch (msg.action)
        {
            case "joinRequest":
                StartCoroutine(AssignPlayerId(sender, msg.playerId));
                break;

            case "join":
                HandleJoin(sender, msg.playerId);
                break;

            case "revealCards":
                HandleReveal(msg);
                break;

            case "endTurn":
                HandleEndTurn(msg);
                break;

            case "requestFullState":
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
            action = "assignPlayerId",
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

            p.Score = 0;
            p.Hand.Clear();
            p.PlayedThisTurn.Clear();
            p.Flags_DrawExtra = 0;

            // Starting hand of 3 cards
            for (int i = 0; i < 3; i++)
                p.DrawCard(loader.Cards[UnityEngine.Random.Range(0, loader.Cards.Count)]);
        }

        BroadcastGameState();
    }

    void HandleEndTurn(NetMessage msg)
    {
        // needs implementation
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
        foreach (var kv in players)
        {
            string pid = kv.Key;
            var state = kv.Value;

            state.PlayedThisTurn.Clear();

            if (reveals.TryGetValue(pid, out var ids))
            {
                foreach (var id in ids)
                {
                    var card = loader.Cards.FirstOrDefault(c => c.Id == id);
                    if (card != null)
                        state.PlayedThisTurn.Add(card);
                }
            }
        }

        // Apply destructive abilities
        foreach (var pid in players.Keys.ToList())
            ApplyDestructiveAbilities(pid);

        // Apply normal abilities
        foreach (var pid in players.Keys.ToList())
            ApplyNormalAbilities(pid);

        // Apply power ? score
        foreach (var kv in players)
        {
            var p = kv.Value;
            p.Score += p.PlayedThisTurn.Sum(c => c.Power);
        }

        foreach (var kv in players)
        {
            var p = kv.Value;
            for (int i = 0; i < p.Flags_DrawExtra; i++)
                p.DrawCard(loader.Cards[UnityEngine.Random.Range(0, loader.Cards.Count)]);
            p.Flags_DrawExtra = 0;
        }

        foreach (var kv in players)
        {
            var p = kv.Value;
            p.DrawCard(loader.Cards[UnityEngine.Random.Range(0, loader.Cards.Count)]);
        }

        var revealMsg = new NetMessage
        {
            action = "revealResult",
            turn = currentTurn,
            scores = ToScoreList(players),
            playedCards = ToPlayedCardsList(players)
        };

        string json = JsonUtility.ToJson(revealMsg);
        net.BroadcastJsonToClients(json, revealMsg.purrPlayer);

        foreach (var kv in players)
            ClearTransientTurnState(kv.Value);
    }

    void ClearTransientTurnState(PlayerState p)
    {
        p.PlayedThisTurn.Clear();
        p.Flags_DrawExtra = 0;
    }

    void ApplyDestructiveAbilities(string pid)
    {
        var self = players[pid];
        var opponent = players.First(k => k.Key != pid).Value;

        var destructives = self.PlayedThisTurn
            .Select(c => EffectFactory.CreateEffect(c))
            .Where(e => e is DestroyOpponentCardInPlayEffect || e is DiscardOpponentRandomEffect)
            .ToList();

        foreach (var e in destructives)
            e?.Apply(self, opponent);
    }

    void ApplyNormalAbilities(string pid)
    {
        var self = players[pid];
        var opponent = players.First(k => k.Key != pid).Value;

        var normal = self.PlayedThisTurn
            .Select(c => EffectFactory.CreateEffect(c))
            .Where(e => !(e is DestroyOpponentCardInPlayEffect) && !(e is DiscardOpponentRandomEffect))
            .ToList();

        foreach (var e in normal)
            e?.Apply(self, opponent);
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
            action = "gameState",
            fullState = state
        };

        string json = JsonUtility.ToJson(msg);
        net.BroadcastJsonToClients(json, msg.purrPlayer);
    }

    void BroadcastTimer()
    {
        var msg = new NetMessage
        {
            action = "timer",
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
            action = "reconnectedFullState",
            fullState = full
        };

        string json = JsonUtility.ToJson(msg);
        net.SendJsonToClient(requester, json);
    }

    void EndMatch()
    {
        var endMsg = new NetMessage
        {
            action = "endMatch",
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
            arr[i++] = new StringIntArrayPair
            {
                key = kv.Key,
                values = kv.Value.PlayedThisTurn.ConvertAll(c => c.Id).ToArray()
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
            arr[i++] = new PlayerEntry
            {
                playerId = kv.Key,
                state = new PlayerStateDTO
                {
                    score = kv.Value.Score,
                    handCount = kv.Value.Hand.Count,
                    handCardIds = kv.Value.Hand.ConvertAll(c => c.Id).ToArray(),
                    playedThisTurn = kv.Value.PlayedThisTurn.ConvertAll(c => c.Id).ToArray()
                }
            };
        }
        return new PlayerEntryList { list = arr };
    }
}
