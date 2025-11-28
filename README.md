# Game Duel — Technical Overview

## Networking Solution
The project uses PurrNet as an authoritative client–server networking layer.

- The host runs the complete game state (hands, played cards, scores, turn flow, timer).
- Clients communicate with the server using JSON messages via:
  - OnServerJson (server receives events)
  - OnClientJson (clients receive updates)

All turn resolution, scoring, and ability logic are computed server-side for full determinism.

---

## JSON Usage for Cards and Abilities
Card data is defined using JSON-like structures loaded through CardLoader.  
Each card contains an ID, name, cost, power, and an ability string:

```json
{
  "Id": 12,
  "Name": "Flame Burst",
  "Cost": 2,
  "Power": 3,
  "Ability": "DestroyOpponentCardInPlay"
}
```

Abilities are mapped using a factory pattern:

```csharp
switch (card.Ability)
{
    case "DestroyOpponentCardInPlay":
        return new DestroyOpponentCardInPlayEffect(card);
    case "DiscardOpponentRandom":
        return new DiscardOpponentRandomEffect(card);
    default:
        return new NoEffect(card);
}
```

This makes the ability system data-driven and easy to extend.

---

## Running and Testing the Game

### 1. Start the Host (Server)
1. Open Unity.
2. Load the main game scene.
3. Press Play — the Unity Editor becomes the authoritative server.

### 2. Start a Client
1. Build the project.
2. Launch the build — it automatically performs the JSON handshake:
   { "action": "joinRequest" }

### 3. Play and Verify Game Flow
- Players start with 3 cards.
- Ending a turn sends:
  { "action": "revealCards", "cardIds": [...] }
- The server resolves:
  - card abilities
  - scoring
  - cost validation
  - +1 card draw per turn
- Played cards persist on the board.
- Hands stay synced from the server.

### 4. Reconnect Testing
Client requests a full state sync with:
{ "action": "requestFullState" }

Server responds with:
{ "action": "reconnectedFullState", "fullState": ... }

This restores board, hand, scores, and turn state.
