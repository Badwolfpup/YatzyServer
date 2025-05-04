using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Numerics;
using System.Threading.Tasks;
using YatzyServer;

public class LobbyHub : Hub
{
    // In-memory store for players (replace with a database for persistence)
    private static readonly ConcurrentDictionary<string, Player> _players = new();

    private List<Player> _queuedplayer = new();

    private List<QueuedGame> _games = new();

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
            await Clients.All.SendAsync("PlayerLeft", player.UserName);
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
            
            if (_queuedplayer.Count >= 2)
            {
                var otherplayers = _queuedplayer.Where(p => p.ConnectionId != Context.ConnectionId).ToList();
                var matchup = new List<Player>();
                matchup.Add(player);
                Random random = new Random();
                matchup.Add(otherplayers[random.Next(otherplayers.Count)]);
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
    }

    public async Task LeaveQueue()
    {
        _queuedplayer.Remove(_players[Context.ConnectionId]);
    }

    //public Task StartRandomGame()
    //{
    //    return Task.CompletedTask;
    //}

    //public Task RollDices(List<string> dices)
    //{
    //    // Handle dice roll logic here
    //    return Task.CompletedTask;
    //}
}