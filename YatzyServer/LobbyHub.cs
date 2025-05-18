using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data.Common;
using System.Numerics;
using System.Threading.Tasks;
using YatzyServer;

public class LobbyHub : Hub
{
    // In-memory store for players (replace with a database for persistence)
    private static readonly ConcurrentDictionary<string, Player> _players = new();

    private static List<Player> _queuedplayer = new();

    private static List<QueuedGame> _games = new();

    // Called when a client connects
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    // Called when a client disconnects
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Remove player and broadcast updated list
        if (_players.TryRemove(Context.ConnectionId, out var player))
        {
            await PlayerLeft(Context.ConnectionId);
            await Clients.All.SendAsync("PlayerDisconnected", player.UserName);
            await BroadcastPlayerList();
        }
        await base.OnDisconnectedAsync(exception);
    }


    // Client calls this to join the lobby with a username
    public async Task JoinLobby(string username)
    {
        // Add player to the list
        var player = new Player { ConnectionId = Context.ConnectionId, UserName = username, Status = Status.Waiting };
        _players[Context.ConnectionId] = player;

        // Notify all clients of the new player
        await Clients.All.SendAsync("PlayerJoined", username);
        await BroadcastPlayerList();
    }

    // Client calls this to send a chat message
    public async Task SendMessage(string message)
    {
        if (_players.TryGetValue(Context.ConnectionId, out var player))
        {
            // Broadcast the message to all clients
            await Clients.All.SendAsync("ReceiveMessage", player.UserName, message);
        }
    }

    public async Task UpdateDicevalue(string json)
    {
        var game = _games.FirstOrDefault(g => g.Player1.ConnectionId == Context.ConnectionId || g.Player2.ConnectionId == Context.ConnectionId);
        if (game == null || game == default) return;
        await Clients.Client(game.Player1.ConnectionId).SendAsync("UpdateDiceValue", json);
        await Clients.Client(game.Player2.ConnectionId).SendAsync("UpdateDiceValue", json);
        //await Clients.All.SendAsync("UpdateDiceValue", json);
    }

    public async Task UpdateDiceBorder(string json)
    {
        var game = _games.FirstOrDefault(g => g.Player1.ConnectionId == Context.ConnectionId || g.Player2.ConnectionId == Context.ConnectionId);
        if (game == null || game == default) return;
        await Clients.Client(game.Player1.ConnectionId).SendAsync("UpdateDiceBorder", json);
        await Clients.Client(game.Player2.ConnectionId).SendAsync("UpdateDiceBorder", json);
        //await Clients.All.SendAsync("UpdateDiceBorder", json);
    }

    public async Task UpdateTurn()
    {
        var game = _games.FirstOrDefault(g => g.Player1.ConnectionId == Context.ConnectionId || g.Player2.ConnectionId == Context.ConnectionId);
        if (game == null || game == default) return;
        await Clients.Client(game.Player1.ConnectionId).SendAsync("UpdateTurn");
        await Clients.Client(game.Player2.ConnectionId).SendAsync("UpdateTurn");
        //await Clients.All.SendAsync("UpdateTurn");
    }

    public async Task UpdatePoints(string json)
    {
        var game = _games.FirstOrDefault(g => g.Player1.ConnectionId == Context.ConnectionId || g.Player2.ConnectionId == Context.ConnectionId);
        if (game == null || game == default) return;
        await Clients.Client(game.Player1.ConnectionId).SendAsync("UpdatePoints", json);
        await Clients.Client(game.Player2.ConnectionId).SendAsync("UpdatePoints", json);
        //await Clients.All.SendAsync("UpdatePoints", json);
    }

    // Helper to broadcast the current player list
    private async Task BroadcastPlayerList()
    {
        var playerList = _players.Values.Select(p => new { p.UserName, p.Status }).ToList();
        await Clients.All.SendAsync("UpdatePlayerList", playerList);
    }

    public Task Ping()
    {
        return Task.CompletedTask; // Responds to client ping
    }

    public async Task QueueForGame()
    {

        var player = _players[Context.ConnectionId];

        lock (_queuedplayer)
        {
            if (_queuedplayer.Contains(player))
            {
                // Player is already in the queue
                return;
            }
            _queuedplayer.Add(player);
            Clients.All.SendAsync("ReceiveMessage", player.UserName, $"{player.UserName} joined the randomqueue.");
            if (_queuedplayer.Count >= 2)
            {
                var otherplayers = _queuedplayer.Where(p => p.ConnectionId != Context.ConnectionId).ToList();
                var matchup = new List<Player>();
                matchup.Add(player);
                Random random = new Random();
                matchup.Add(otherplayers[random.Next(otherplayers.Count)]);
                //matchup.Add(new Player() { UserName = "AI", Status = Status.Waiting, ConnectionId = "AI" });
                // Create a new game and assign players
                var game = new QueuedGame(matchup[0], matchup[1]);
                _games.Add(game);
                _queuedplayer.Remove(matchup[0]);
                _queuedplayer.Remove(matchup[1]);
                StartQueuedGame(matchup);
            }
        }

    }

    private async Task StartQueuedGame(List<Player> matched)
    {

        // Notify players about the game start
        foreach (var player in matched)
        {
            player.Status = Status.Playing;
            await Clients.Client(player.ConnectionId).SendAsync("GameStarted", matched[0].UserName, matched[1].UserName);
        }
         await BroadcastPlayerList();
    }

    public async Task PlayerLeft()
    {
        var id = Context.ConnectionId;
        var game = _games.FirstOrDefault(g => g.Player1.ConnectionId == id || g.Player2.ConnectionId == id);
        if (game == null || game == default) return;
        var otherid = game.Player1.ConnectionId == id ? game.Player2.ConnectionId : game.Player1.ConnectionId;
        _players[Context.ConnectionId].Status = Status.Waiting;
        _players[otherid].Status = Status.Waiting;
        _games.Remove(game);
        await Clients.Client(otherid).SendAsync("PlayerLeft");
        await BroadcastPlayerList();
    }

    private async Task PlayerLeft(string id)
    {
        var game = _games.FirstOrDefault(g => g.Player1.ConnectionId == id || g.Player2.ConnectionId == id);
        if (game == null || game == default) return;
        var otherid = game.Player1.ConnectionId == id ? game.Player2.ConnectionId : game.Player1.ConnectionId;
        _players[otherid].Status = Status.Waiting;
        _games.Remove(game);
        await Clients.Client(otherid).SendAsync("PlayerLeft");
        await BroadcastPlayerList();
    }

    public async Task InvitePlayer(string username)
    {
        var invitedPlayer = _players.Values.FirstOrDefault(p => p.UserName.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (invitedPlayer == null || invitedPlayer == default) return;
        await Clients.Client(invitedPlayer.ConnectionId).SendAsync("InvitePlayer", Context.ConnectionId, _players[Context.ConnectionId].UserName);
    }

    public async Task AnswerToInvite(bool answer, string connectionid)
    {
        if (!answer)
        {
            await Clients.Client(connectionid).SendAsync("RejectedInvite", _players[Context.ConnectionId].UserName);
        }
        else
        {
            var player = new List<Player>()
            {
                _players[Context.ConnectionId],
                _players[connectionid]
            };
            var game = new QueuedGame(player[0], player[1]);
            _games.Add(game);
            StartQueuedGame(player);
        }
    }

    public async Task LeaveQueue()
    {
        _queuedplayer.Remove(_players[Context.ConnectionId]);
    }

   

    public async Task GameFinished()
    {
        _players[Context.ConnectionId].Status = Status.Waiting;
        var game = _games.FirstOrDefault(g => g.Player1.ConnectionId == Context.ConnectionId || g.Player2.ConnectionId == Context.ConnectionId);
        if (game == null || game == default) return;
        var otherid = game.Player1.ConnectionId == Context.ConnectionId ? game.Player2.ConnectionId : game.Player1.ConnectionId;
        _players[otherid].Status = Status.Waiting;
        await Clients.Client(otherid).SendAsync("GameFinished");
        _games.Remove(game);
        await BroadcastPlayerList();
    }
}