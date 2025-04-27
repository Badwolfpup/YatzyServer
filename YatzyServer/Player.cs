public enum Status
{
    Waiting,
    Playing,
    AFK
}

public class Player
{
    public string ConnectionId { get; set; }
    public string UserName { get; set; }
    public Status Status { get; set; }
}
