🎮 Game Duel – Multiplayer Turn-Based Card Battler

A lightweight and fast-paced 1v1 multiplayer card duel game built in Unity, powered by PurrNet for networking and a clean JSON-driven card/ability system.
The server is fully authoritative, ensuring consistent gameplay, zero cheating, and stable synchronization across clients.

🚀 Features

⚡ Real-time online multiplayer using PurrNet's authoritative host model

🎴 JSON-defined cards with cost, power, and flexible ability strings

🔥 Ability system powered by a factory pattern (easy to extend!)

♻️ Persistent hands & persistent played boards

⏱️ Server-managed turn timer

📡 Full state resync for reconnecting players

🏁 End-match summary with clean UI hooks

🎨 Designed to be lightweight, predictable, and easy to modify


📡 Networking Architecture (PurrNet)

This project uses PurrNet – a simple and fast client-server networking solution.

🔄 Gameplay Flow
Start of Match

Server assigns each player a unique ID

Each player receives 3 starting cards

Every Turn

Players choose cards to reveal

Cards are removed from hand locally and added to the player's board

Server validates cost and abilities

Server resolves:

destructive abilities

normal abilities

score increments

Server draws +1 card for each player

Server broadcasts next full state

Played cards remain permanently on the board

Hand persists correctly without duplication

Turn Timer

Server runs a 30s timer

If expired → auto-resolve with empty reveal
