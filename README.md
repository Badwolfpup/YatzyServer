# YatzyServer

The SignalR backend server for the [Yatzy](https://github.com/Badwolfpup/Yatzy) multiplayer dice game. Handles matchmaking, game state, and real-time communication between players.

## Technologies

- ASP.NET Core, SignalR, C#

## Features

- **SignalR Hub** for real-time bidirectional communication
- **Lobby system** with player matchmaking
- **Game queue** management for pairing players
- **Player state** tracking during games

## Project Structure

```
YatzyServer/
â”œâ”€â”€ Program.cs          # Server configuration and startup
â”œâ”€â”€ LobbyHub.cs         # SignalR hub for game communication
â”œâ”€â”€ Player.cs           # Player model
â””â”€â”€ QueuedGame.cs       # Matchmaking queue model
```

## How to Run

```bash
dotnet restore
dotnet run
```

## Related

- **Client:** [Yatzy](https://github.com/Badwolfpup/Yatzy) - WPF game client
